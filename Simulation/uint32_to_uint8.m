function y = uint32_to_uint8(x)
y = [mod(x, 256) mod(uint8(x / (2^8)), 256) mod(uint8(x / (2^16)), 256) mod(uint8(x / (2^24)), 256)];
end
