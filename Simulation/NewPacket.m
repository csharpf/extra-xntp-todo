function pkt = NewPacket(ProtocolId, PacketType, Data)
%
% Creates a new Packet structure
%
% Inputs:
% PacketType     =	Type of Packet
% Data           =  Byte Array of Data
%
% Returns:
% pkt	=   Packet Structure of the Packet
%

pkt = struct('PacketSize', 0, 'ProtocolId', ProtocolId, 'PacketType', PacketType, 'Data', Data);
pkt.PacketSize = SizeOfPacket(pkt);
end
