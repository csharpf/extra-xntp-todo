function pkt = GetPacket(x, nest)
%
% Gives the Packet Structure from byte array of the Packet
%
% Inputs:
% x     =	Byte array form of Packet
%
% Returns:
% pkt	=   Packet Structure of the Packet
%

PacketSize = uint8_to_uint32(x(1, 1:4));
ProtocolId = char(x(1, 5:8));
PacketType = uint8_to_uint32(x(1, 9:12));
if(PacketType ~= nest)
    Data = x(1, 13:end);
else
    ptr = 13;
    dataLeft = PacketSize - 12;
    Data = [];
    while(dataLeft > 0)
        sz = uint8_to_uint32(x(1, ptr:(ptr+3)));
        Data = [Data GetPacket(x(1, ptr:(ptr+sz-1)), nest)];
        ptr = ptr + sz;
        dataLeft = dataLeft - sz;
    end
end
pkt = struct('PacketSize', PacketSize, 'ProtocolId', ProtocolId, 'PacketType', PacketType, 'Data', Data);
end
