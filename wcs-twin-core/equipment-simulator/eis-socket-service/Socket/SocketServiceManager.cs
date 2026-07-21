using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EisSocketService.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtocolCore;

namespace EisSocketService.Socket
{
    // Vision 소켓 통신을 관리하는 매니저 - Worker가 Run()/Stop()으로 제어
    public class SocketServiceManager
    {
        private readonly int _port;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SocketServiceManager> _logger;

        private CancellationTokenSource _cts;
        private Task _listenTask;

        public SocketServiceManager(IServiceProvider serviceProvider, ILogger<SocketServiceManager> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _port = int.Parse(configuration["SocketServer:Port"] ?? "9000");
        }

        public void Run()
        {
            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _logger.LogInformation("SocketServiceManager 시작 - 포트{Port}", _port);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _logger.LogInformation("SocketServiceManager 정지 요청됨");
        }

        private async Task ListenLoopAsync(CancellationToken stoppingToken)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            _logger.LogInformation("EIS 소켓 서비스 시작 - 포트{Port} 대기중 (STX/ETX 프로토콜)", _port);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("설비 접속시작");
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                listener.Stop();
                _logger.LogInformation("SocketServiceManager 리스너 종료됨");
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
                        // RFID 확인/기록 때문에 처리 시간이 길어질 수 있어서 await로 기다림
                        byte[] response = await ProcessFrameAsync(frame);
                        if (response != null)
                        {
                            await stream.WriteAsync(response, 0, response.Length, stoppingToken);
                            _logger.LogInformation("응답 전송 (bytes: {Length})", response.Length);
                        }
                    }
                }
            }
        }

        private async Task<byte[]> ProcessFrameAsync(byte[] frame)
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

                var handler = factory.GetHandler(command, "RECEIVE");
                return await handler.Handle(frame);
            }
            catch (Exception ex)
            {
                _logger.LogError("메시지 처리 오류: {Message}", ex.ToString());
                return null;
            }
        }
    }
}