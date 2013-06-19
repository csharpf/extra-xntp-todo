function y = uint8_to_uint32(x)
y = x(1, 1) + x(1, 2)*(2^8) + x(1, 3)*(2^16) + x(1, 4)*(2^24);
end
