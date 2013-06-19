function x = PutPacket(pkt)
%
% Gives the byte array of the Packet from Packet Structure
%
% Inputs:
% x     =	Byte array form of Packet
%
% Returns:
% pkt	=   Packet Structure of the Packet
%

x = uint8(zeros(1, pkt.PacketSize));
x(1, 1:4) = uint32_to_uint8(pkt.PacketSize);
x(1, 5:8) = uint8(pkt.ProtocolId);
x(1, 9:12) = uint32_to_uint8(pkt.PacketType);
if(isstruct(pkt.Data) == 0)
    x(1, 13:end) = pkt.Data;
else
    ptr = 13;
    for i = 1 : length(pkt.Data)
        sz = pkt.Data(1, i).PacketSize;
        x(1, ptr : (ptr + sz - 1)) = PutPacket(pkt.Data(1, i));
        ptr = ptr + sz;
    end
end
end
