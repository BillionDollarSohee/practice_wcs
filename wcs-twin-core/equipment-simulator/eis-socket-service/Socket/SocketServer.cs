using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EisSocketService.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtocolCore;

namespace EisSocketService.Socket
{
    // TCP 서버 - 설비 접속을 받아 MessageHandlerFactory로 라우팅
    // BackgroundService로 호스트 시작 시 자동 실행
    public class SocketServer : BackgroundService
    {
        private readonly int _port;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SocketServer> _logger;

        public SocketServer(IServiceProvider serviceProvider, ILogger<SocketServer> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _port = int.Parse(configuration["SocketServer:Port"] ?? "9000");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            _logger.LogInformation("EIS 소켓 서비스 시작 - 포트{Port} 대기중 (STX/ETX 프로토콜)", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                _logger.LogInformation("설비 접속시작");
                _ = HandleClientAsync(client, stoppingToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                List<byte> buffer = new List<byte>();
                byte[] readBuffer = new byte[1024];

                while (client.Connected && !stoppingToken.IsCancellationRequested)
                {
                    int readCount;
                    try
                    {
                        readCount = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("수신 오류: {Message}", ex.Message);
                        break;
                    }

                    if (readCount == 0)
                    {
                        _logger.LogInformation("설비 접속 종료");
                        break;
                    }

                    buffer.AddRange(new ArraySegment<byte>(readBuffer, 0, readCount));

                    byte[] frame;
                    while ((frame = ProtocolCodec.ExtractFrame(buffer)) != null)
                    {
                        byte[] response = ProcessFrame(frame);
                        if (response != null)
                        {
                            await stream.WriteAsync(response, 0, response.Length, stoppingToken);
                            _logger.LogInformation("응답 전송 (bytes: {Length})", response.Length);
                        }
                    }
                }
            }
        }

        // MessageHandlerFactory에서 Command_RECEIVE 키로 핸들러를 찾아 위임
        private byte[] ProcessFrame(byte[] frame)
        {
            try
            {
                var (command, _) = ProtocolCodec.ParseFrame(frame);
                _logger.LogInformation("수신 - Command: {Command}", command);

                using var scope = _serviceProvider.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<MessageHandlerFactory>();

                if (!factory.HasHandler(command, "RECEIVE"))
                {
                    _logger.LogWarning("처리 가능한 핸들러 없음 - COMMAND: {Command}", command);
                    return null;
                }

                var handler = factory.GetMessageHandler(command, "RECEIVE");
                return handler.Handle(frame);
            }
            catch (Exception ex)
            {
                _logger.LogError("메시지 처리 오류: {Message}", ex.ToString());
                return null;
            }
        }
    }
}