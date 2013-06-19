function sz = SizeOfPacket(pkt)
%
% Gives the size of a Fully Structured Packet
%a
% Inputs:
% pkt   =	Packet Structure of the Packet
%
% Returns:
% sz	=   Size of the entire Packet
%

sz = 12;
if(isstruct(pkt.Data) == 0)
    sz = sz + length(pkt.Data);
else
    for i = 1 : length(pkt.Data)
        sz = sz + SizeOfPacket(pkt.Data(1, i));
    end
end
end
