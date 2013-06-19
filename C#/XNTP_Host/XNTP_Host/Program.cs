using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace XNTP_Host
{
	// Define enumerations
	public enum PacketType : uint
	{
		NA,
		ConnectionTest,
		ConnectionRequest,
		ConnectionReply,
		PacketAck,
		RawData,
		FileData,
		Command,
		Data,
		Mixed
	};
	public enum HostType : uint
	{
		NA,
		LocalHost,
		RemoteHost
	};


	class Packet
	{
		/*
		 * Summary:
		 *		Implements the XNTP Packet Format.
		 *		(XNTP = eXtended Network Transmission Protocol)
		 *		For the time being XNTP will be implemented over TCP/IP layer.
		 */

		// Define Constants
		private const string PACKET_ID = "XNTP";

		// Packet details present in a packet structure
		public string		IpV4Src, IpV6Src;
		public string		IpV4Dest, IpV6Dest;
		public string[]		Protocol;
		private uint		size;
		private uint		reserved;
		private string		id;
		public PacketType	type;
		private uint		offsetData;
		public string		details;
		private uint		dataSize;
		public byte[]		data;

		// Member Functions
		public static byte[] toByteForm (string str, bool includeSize)
		{
			/*
			 * Summary:
			 *		Converts a string to byte array (with/without LEN(DWORD)).
			 *		
			 * Parameters:
			 *		str			= String to be converted
			 *		includeSize	= (true = include size / false = dont include size)
			 *		
			 * Returns:
			 *		Byte array form of the string.
			 */

			int			i, sz;
			byte[]		bStr, temp;

			// Get Byte representation of Length of string, and store it
			if (includeSize) sz = sizeof(uint);
			else sz = 0;
			bStr = new byte[sz + str.Length];
			if (includeSize)
			{
				temp = BitConverter.GetBytes((uint) str.Length);
				Array.Copy(temp, bStr, temp.Length);
			}

			// Store the string data in byte form
			for (i = 0 ; i < str.Length ; i++)
			{
				bStr[i + sz] = Convert.ToByte(str[i]);
			}

			// Return the byte converted string
			return (bStr);
		}

		public static byte[] toByteForm (string str)
		{
			/*
			 * Summary:
			 *		Converts a string to byte array (with LEN(DWORD)).
			 *		
			 * Parameters:
			 *		str			= String to be converted
			 *		
			 * Returns:
			 *		Byte array form of the string.
			 */

			// Return byte form of string with LEN
			return (toByteForm(str, true));
		}

		public static string toStringForm (int strSize, byte[] bStr, int startIndex)
		{
			/*
			 * Summary:
			 *		Converts a byte array (without LEN(DWORD)) to string.
			 *		
			 * Parameters:
			 *		strSize		= Size of the string (in bytes)
			 *		bStr		= byte array in which the string is present
			 *		startIndex	= Start index of the string
			 *		
			 * Returns:
			 *		string form of the byte array.
			 */

			int				i, j;
			char[]			cStr;
			StringBuilder	strBuild;

			// Convert the bytes to characters
			cStr = new char[strSize];
			for (i = 0, j = startIndex ; i < strSize ; i++, j++)
			{
				cStr[i] = Convert.ToChar(bStr[j]);
			}

			// Return string form of the characters
			strBuild = new StringBuilder();
			strBuild.Append(cStr);
			return (strBuild.ToString());
		}

		public static string toStringForm (int strSize, byte[] bStr)
		{
			/*
			 * Summary:
			 *		Converts a byte array (without LEN(DWORD)) to string.
			 *		
			 * Parameters:
			 *		strSize		= Size of the string (in bytes)
			 *		bStr		= byte array in which the string is present
			 *		(startIndex	=  0; Start index of the string)
			 *		
			 * Returns:
			 *		string form of the byte array.
			 */

			// Return string form of the byte array
			return (toStringForm(strSize, bStr, 0));
		}

		public static string toStringForm (byte[] bStr, int startIndex)
		{
			/*
			 * Summary:
			 *		Converts a byte array (with LEN(DWORD)) to string.
			 *		
			 * Parameters:
			 *		bStr		= byte array in which the LEN+string is present
			 *		startIndex	= Start index of the string
			 *		
			 * Returns:
			 *		string form of the byte array.
			 */

			int			strSize;

			// Get the length of the string
			strSize = (int) BitConverter.ToUInt32(bStr, startIndex);

			// Return string form of the characters
			return (toStringForm(strSize, bStr, startIndex + sizeof(uint)));
		}

		public static string toStringForm (byte[] bStr)
		{
			/*
			 * Summary:
			 *		Converts a byte array (with LEN(DWORD)) to string.
			 *		
			 * Parameters:
			 *		bStr		= byte array in which the LEN+string is present
			 *		(startIndex	= 0; Start index of the string)
			 *		
			 * Returns:
			 *		string form of the byte array.
			 */

			// Return the string form of the byte array
			return (toStringForm(bStr, 0));
		}

		public void updateChanges ()
		{
			/*
			 * Summary:
			 *		Updates the size information in the Packet.
			 */

			dataSize = (uint) data.Length;
			offsetData = dataSize + sizeof(uint) + (uint) details.Length;
			size = 7 * sizeof(uint) + (uint) details.Length + (uint) data.Length;
		}

		private void init ()
		{
			/*
			 * Summary:
			 *		Sets the default fields of a packet (Creates a Connection-test Packet).
			 */

			reserved = 0;
			id = PACKET_ID;
			type = PacketType.ConnectionTest;
			details = "";
			data = new byte[0];
			updateChanges();
		}

		public bool loadBytePacket (uint packetSize, byte[] bytePacket, int startIndex)
		{
			/*
			 * Summary:
			 *		Gets the Packet information from the given byte Packet.
			 *		
			 * Parameters:
			 *		packetSize	= Size of Packet (in bytes)
			 *		bytePacket	= byte Packet without Packet Size
			 *		startIndex	= Starting index of the Packet in byte array
			 *		
			 * Returns:
			 *		Reports status (true = success, false = failure)
			 */

			int			ptr;
			string		str;

			// First verify that it is a LAN Messenger Packet
			str = toStringForm(sizeof(uint), bytePacket, startIndex + sizeof(uint));
			if (str != PACKET_ID)
			{
				// Set Packet Type to NA
				init();
				id = new string(' ', 8);
				type = PacketType.NA;

				// Report Failure
				return (false);
			}

			// Get all properties
			ptr = startIndex;
			size = packetSize;
			reserved = BitConverter.ToUInt32(bytePacket, ptr);
			ptr += sizeof(uint);
			id = str;
			ptr += sizeof(uint);
			type = (PacketType) BitConverter.ToUInt64(bytePacket, ptr);
			ptr += sizeof(uint);
			offsetData = BitConverter.ToUInt32(bytePacket, ptr);
			ptr += sizeof(uint);
			details = toStringForm(bytePacket, ptr);
			ptr += sizeof(uint) + details.Length;
			dataSize = BitConverter.ToUInt32(bytePacket, ptr);
			ptr += sizeof(uint);
			data = new byte[dataSize];
			Array.Copy(bytePacket, ptr, data, 0, (int) dataSize);

			// Report Success
			return (true);
		}

		public bool loadBytePacket (uint packetSize, byte[] bytePacket)
		{
			/*
			 * Summary:
			 *		Gets the Packet information from the given byte Packet.
			 *		
			 * Parameters:
			 *		packetSize	= Size of Packet (in bytes)
			 *		bytePacket	= byte Packet without Packet Size
			 *		(startIndex	= 0; Starting index of the Packet in byte array)
			 *		
			 * Returns:
			 *		Reports status (true = success, false = failure)
			 */

			// Get Packet information and report status
			return (loadBytePacket(packetSize, bytePacket, 0));
		}

		public bool loadBytePacket (byte[] bytePacket, int startIndex)
		{
			/*
			 * Summary:
			 *		Gets the Packet information from the given byte Packet.
			 *		
			 * Parameters:
			 *		bytePacket	= byte Packet with Packet Size
			 *		startIndex	= Starting index of the Packet in byte array
			 *		
			 * Returns:
			 *		Reports status (true = success, false = failure)
			 */

			// Get Packet Size
			uint packetSize = BitConverter.ToUInt32(bytePacket, startIndex);

			// Get Packet information and report status
			return (loadBytePacket(packetSize, bytePacket, startIndex + sizeof(uint)));
		}

		public bool loadBytePacket (byte[] bytePacket)
		{
			/*
			 * Summary:
			 *		Gets the Packet information from the given byte Packet.
			 *		
			 * Parameters:
			 *		bytePacket	= byte Packet with Packet Size
			 *		(startIndex	= 0; Starting index of the Packet in byte array)
			 *		
			 * Returns:
			 *		Reports status (true = success, false = failure)
			 */

			// Get Packet information and report status
			return (loadBytePacket(bytePacket, 0));
		}

		public Packet ()
		{
			/*
			 * Summary:
			 *		Creates a Connection-Test Packet.
			 */

			init();
		}

		public Packet (PacketType packetType, byte[] packetData)
		{
			/*
			 * Summary:
			 *		Creates a Packet of specified Packet Type with no details.
			 *		
			 * Parameters:
			 *		packetType	= Type of Packet
			 *		packetData	= byte array of Packet data
			 */

			init();
			type = packetType;
			data = packetData;
			updateChanges();
		}

		public Packet (PacketType packetType, string packetDetails, byte[] packetData)
		{
			/*
			 * Summary:
			 *		Creates a Packet of specified Packet Type with no details.
			 *		
			 * Parameters:
			 *		packetType		= Type of Packet
			 *		packetDetails	= Details list about the Packet
			 *		packetData		= byte array of Packet data
			 */

			init();
			type = packetType;
			details = packetDetails;
			data = packetData;
			updateChanges();
		}

		public Packet (uint packetSize, byte[] bytePacket, int startIndex)
		{
			/*
			 * Summary:
			 *		Creates a Packet from byte Packet (i.e., Packet recieved from network).
			 *		
			 * Parameters:
			 *		packetSize		= Size of Packet
			 *		bytePacket		= The byte Packet without Packet Size
			 *		startIndex		= Start index of the Packet in the byte array
			 */

			loadBytePacket(packetSize, bytePacket, startIndex);
		}

		public Packet (uint packetSize, byte[] bytePacket)
		{
			/*
			 * Summary:
			 *		Creates a Packet from byte Packet (i.e., Packet recieved from network).
			 *		
			 * Parameters:
			 *		packetSize		= Size of Packet
			 *		bytePacket		= The byte Packet without Packet Size
			 *		(startIndex		= 0; Start index of the Packet in the byte array)
			 */

			loadBytePacket(packetSize, bytePacket);
		}

		public Packet (byte[] bytePacket, int startIndex)
		{
			/*
			 * Summary:
			 *		Creates a Packet from byte Packet (i.e., Packet recieved from network).
			 *		
			 * Parameters:
			 *		bytePacket		= The byte Packet with Packet Size
			 *		startIndex		= Start index of the Packet in the byte array
			 */

			loadBytePacket(bytePacket, startIndex);
		}

		public Packet (byte[] bytePacket)
		{
			/*
			 * Summary:
			 *		Creates a Packet from byte Packet (i.e., Packet recieved from network).
			 *		
			 * Parameters:
			 *		bytePacket		= The byte Packet without Packet Size
			 *		(startIndex		= 0; Start index of the Packet in the byte array)
			 */

			loadBytePacket(bytePacket);
		}

		public byte[] saveBytePacket ()
		{
			/*
			 * Summary:
			 *		Creates a byte Packet from Packet (i.e., Packet sent to network).
			 *		
			 * Returns:
			 *		Byte array of the data to be transmitted.
			 */

			int			ptr;
			byte[]		transfer, temp;

			// Calculate sizes
			updateChanges();

			// Put Packet in Byte Array
			ptr = 0;
			transfer = new byte[size];
			temp = BitConverter.GetBytes(size);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = BitConverter.GetBytes(reserved);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = toByteForm(id, false);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = BitConverter.GetBytes((uint) type);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = BitConverter.GetBytes(offsetData);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = toByteForm(details);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			temp = BitConverter.GetBytes(dataSize);
			Array.Copy(temp, 0, transfer, ptr, sizeof(uint));
			ptr += sizeof(uint);
			Array.Copy(data, 0, transfer, (int) ptr, (int) dataSize);

			// Return the byte array to be transferred
			return (transfer);
		}
	}

	class Host
	{
		// Define Constants
		private const string LOCAL_IP = "127.0.0.1";

		// Static data
		public static int			pendingConnections = 64;
		public static List<Host>	allHosts = new List<Host>();
		public static List<Host>	localHosts = new List<Host>();

		// Host details
		public string			ipAddress;
		public int				port;
		public HostType			type;
		public Socket			socket;
		private	Queue<Packet>	sendPackets;
		private Queue<Packet>	recvPackets;
		public bool				serverMode;
		public bool				autoAcceptConn;
		public List<Host>		connectedHosts;
		private Host			parentHost;

		// Member functions
		private void init ()
		{
			/*
			 * Summary:
			 *		Initializes the Host variables to defaults.
			 *		Use this only when initializing a newly created Host.
			 *		
			 */

			ipAddress = "";
			port = 0;
			type = HostType.NA;
			socket = null;
			sendPackets = null;
			recvPackets = null;
			connectedHosts = null;
		}

		private static void stopSocket (Host remoteHost)
		{
			if (remoteHost.socket != null)
			{
				remoteHost.socket.Shutdown(SocketShutdown.Both);
				remoteHost.socket.Close();
			}
		}

		private static void disconnectCallback (IAsyncResult ar)
		{
			((Socket) ar.AsyncState).EndDisconnect(ar);

		}

		public bool disconnect (Host remoteHost)
		{
			if (remoteHost.type == HostType.RemoteHost)
			{
				remoteHost.socket.BeginDisconnect(false, new AsyncCallback(disconnectCallback), remoteHost);
			}
			return (false);
		}

		public void disconnectAll ()
		{

		}

		public void stop ()
		{
			/*
			 * Summary:
			 *		Stops the Host. All connected hosts get disconnected.
			 *		
			 */

			if (socket != null)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
			init();
		}

		public void update ()
		{
			if (socket.Connected)
			{
				ipAddress = ((IPEndPoint) socket.RemoteEndPoint).Address.ToString();
				port = ((IPEndPoint) socket.RemoteEndPoint).Port;
				type = HostType.RemoteHost;
			}
			else
			{
				if (socket.IsBound)
				{
					ipAddress = ((IPEndPoint) socket.LocalEndPoint).Address.ToString();
					port = ((IPEndPoint) socket.LocalEndPoint).Port;
					type = HostType.LocalHost;
				}
				else
				{
					ipAddress = "";
					port = 0;
					type = HostType.NA;
				}
			}
		}

		public static string[] getLocalIp ()
		{
			IPHostEntry		host;
			Queue<string>	ipAddr = new Queue<string>();

			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					ipAddr.Enqueue(ip.ToString());
				}
			}
			return (ipAddr.ToArray());
		}

		public void setHost (Socket hSocket)
		{
			stop();
			socket = hSocket;
			socket.Blocking = false;
			socket.DontFragment = false;
			socket.EnableBroadcast = true;
			update();
			if (type == HostType.LocalHost) socket.Listen(1);
		}

		public void setHost (AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, string sIpAddr, int nPort)
		{
			Socket hSocket = new Socket(addressFamily, socketType, protocolType);
			hSocket.Bind((EndPoint) new IPEndPoint(IPAddress.Parse(sIpAddr), nPort));
			setHost(hSocket);
		}

		public void setHost (AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, int nPort)
		{
			setHost(addressFamily, socketType, protocolType, LOCAL_IP, nPort);
		}

		public void setHost (string sIpAddr, int nPort)
		{
			Socket hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			hSocket.Bind((EndPoint) new IPEndPoint(IPAddress.Parse(sIpAddr), nPort));
			setHost(hSocket);
		}

		public void setHost (int nPort)
		{
			setHost(LOCAL_IP, nPort);
		}

		public bool setPort (int nPort)
		{
			try
			{
				socket.Bind((EndPoint) new IPEndPoint(IPAddress.Parse(LOCAL_IP), nPort));
				update();
				return (true);
			}
			catch (Exception)
			{
				return (false);
			}
		}

		public void startListen ()
		{
		}

		Host (Socket hSocket)
		{
			socket = null;
			setHost(hSocket);
		}

		Host (AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, string sIpAddr, int nPort)
		{
			socket = null;
			setHost(addressFamily, socketType, protocolType, sIpAddr, nPort);
		}

		Host (AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, int nPort)
		{
			socket = null;
			setHost(addressFamily, socketType, protocolType, nPort);
		}

		Host (string sIpAddr, int nPort)
		{
			socket = null;
			setHost(sIpAddr, nPort);
		}

		Host (int nPort)
		{
			socket = null;
			setHost(nPort);
		}

		Host ()
		{
			socket = null;
			setHost(0);
		}
	}



	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main ()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}
