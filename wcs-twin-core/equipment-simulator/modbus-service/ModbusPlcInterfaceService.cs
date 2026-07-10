using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModbusService.Helper;
using ModbusService.Model;
using ModbusService.Protocol;

namespace ModbusService
{
    // Modbus TCP 마스터(클라이언트) - Mock UHF 리더에 접속해서 태그 Read/Write 명령을 수행
    public class ModbusPlcInterfaceService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogger<ModbusPlcInterfaceService> _logger;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private ushort _transactionId = 0;
        private readonly object _transactionLock = new object();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public ModbusPlcInterfaceService(ILogger<ModbusPlcInterfaceService> logger, string host, int port)
        {
            _logger = logger;
            _host = host;
            _port = port;
        }

        public async Task EnsureConnectedAsync()
        {
            if (_tcpClient != null && _tcpClient.Connected) return;

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port);
            _stream = _tcpClient.GetStream();
            _logger.LogInformation("Modbus 리더 접속 성공 - {Host}:{Port}", _host, _port);
        }

        // Read 명령 (Memory Area 1=EPC, 2=TID, 3=USER)
        public async Task<ParsedRfidStatus> ReadTagAsync(ushort memoryArea = 1, ushort startAddress = 0, ushort length = 24, int channelBase = 0, int pollTimeoutMs = 5000)
        {
            return await ExecuteCommandAsync(2, memoryArea, startAddress, length, channelBase, pollTimeoutMs);
        }

        // Write 명령
        public async Task<ParsedRfidStatus> WriteTagAsync(ushort memoryArea = 1, ushort startAddress = 0, ushort length = 24, int channelBase = 0, int pollTimeoutMs = 5000)
        {
            return await ExecuteCommandAsync(4, memoryArea, startAddress, length, channelBase, pollTimeoutMs);
        }

        private async Task<ParsedRfidStatus> ExecuteCommandAsync(ushort commandCode, ushort memoryArea, ushort startAddress, ushort length, int channelBase, int pollTimeoutMs)
        {
            await EnsureConnectedAsync();

            // 1. Holding Register에 명령 쓰기 (FC 0x10)
            ushort[] commandRegisters = ModbusDataWriteHelper.BuildCommandRegisters(commandCode, memoryArea, startAddress, length, 0);
            byte[] writeRequest = ModbusCodec.BuildWriteMultipleRegistersRequest(NextTransactionId(), 1, (ushort)channelBase, commandRegisters);
            await SendAndReceiveAsync(writeRequest);

            _logger.LogInformation("Modbus Command {Command} 전송 완료 - 결과 폴링 시작", commandCode);

            // 2. Input Register를 Busy가 풀릴 때까지 폴링 (FC 0x04)
            DateTime deadline = DateTime.Now.AddMilliseconds(pollTimeoutMs);
            while (DateTime.Now < deadline)
            {
                await Task.Delay(200);

                byte[] readRequest = ModbusCodec.BuildReadRegistersRequest(ModbusCodec.FC_READ_INPUT_REGISTERS, NextTransactionId(), 1, (ushort)channelBase, 32);
                byte[] response = await SendAndReceiveAsync(readRequest);
                var (_, registers) = ModbusCodec.ParseReadResponse(response);

                ParsedRfidStatus status = ModbusDataParsingHelper.Parse(registers);
                if (!status.IsBusy)
                {
                    _logger.LogInformation("Modbus 명령 완료 - ResponseCode:{Code}, HasError:{Error}", status.ResponseCode, status.HasError);
                    return status;
                }
            }

            _logger.LogWarning("Modbus 명령 폴링 타임아웃 - Command:{Command}", commandCode);
            return new ParsedRfidStatus { HasError = true, ErrorCode = 0x4007 }; // Timeout
        }

        private async Task<byte[]> SendAndReceiveAsync(byte[] request)
        {
            await _sendLock.WaitAsync();
            try
            {
                await _stream.WriteAsync(request, 0, request.Length);

                byte[] header = new byte[6];
                await ReadExactAsync(header, 0, 6);
                ushort bodyLength = (ushort)((header[4] << 8) | header[5]);

                byte[] body = new byte[bodyLength];
                await ReadExactAsync(body, 0, bodyLength);

                byte[] full = new byte[6 + bodyLength];
                Array.Copy(header, 0, full, 0, 6);
                Array.Copy(body, 0, full, 6, bodyLength);
                return full;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReadExactAsync(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0) throw new IOException("Modbus 연결 종료됨");
                totalRead += read;
            }
        }

        private ushort NextTransactionId()
        {
            lock (_transactionLock)
            {
                _transactionId++;
                return _transactionId;
            }
        }
    }
}