function FileTransfer(src_file, dest_file, chunkSize, show)
%
%   File Transfer using XNTP Protocol
%

% Open Files
fsrc = fopen(src_file, 'r');
fdest = fopen(dest_file, 'w');
disp('Files Opened.');

% Read and write till end of file
chunkNo = 0;
dataAmt = 0;
dataSz = chunkSize;
while(dataSz == chunkSize)
    % Get binary data from source file
    [data dataSz] = fread(fsrc, [1 chunkSize]);
    
    % Update Information
    chunkNo = chunkNo + 1;
    dataAmt = dataAmt + dataSz;
    
    % Convert binary data to Packet Format
    Packet = NewPacket('XNTP', 1, data(1, 1 : dataSz));
    pkt0 = PutPacket(Packet);
    
    % Display essential info, if required
    if(show == 1)
        fprintf(1, 'Packet Number:     %d\n', chunkNo);
        fprintf(1, 'Data transferring: %d\n', dataAmt);
        display(Packet);
        disp(' ');
        disp(' ');
    end
    
    % Get back binary form of the Packet
    pkt1 = GetPacket(pkt0, 2);
    
    % Save binary data to source file
    fwrite(fdest, pkt1.Data);
end

% Close the files
fclose(fsrc);
fclose(fdest);
disp('File Transfer Complete.');
disp(' ');
end

