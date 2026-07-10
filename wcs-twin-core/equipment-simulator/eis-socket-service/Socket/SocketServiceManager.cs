using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtocolCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EisSocketService.Handlers;

namespace EisSocketService.Socket
{
    // Vision 소켓 통신을 관리하는 매니저
    // 매니저가 늘어나도 Worker에서 Run()/Stop()으로 제어한다.
    public class SocketServiceManager
    {
        private readonly int _port;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SocketServiceManager> _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenTask;

        public SocketServiceManager(IServiceProvider serviceProvider, ILogger<SocketServiceManager> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _port = int.Parse(configuration["SocketServer:Port"] ?? "9000");

        }

        // Worker에서 호출 - 리스너 루프를 백그라운드로 띄우고 즉시 반환
        public void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
            _logger.LogInformation("SocketServiceManager 시작 - 포트{Port}", _port);

        }

        // Worker에서 호출 - 취소 신호만 보내고 실제 종료는 리스너 루프가 알아서 정리
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("SocketServiceManager 정지 요청됨");
        }

        private async Task ListenLoopAsync(CancellationToken stoppingToken)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            _logger.LogInformation("EIS 소켓 서비스 시작 - 포트{PORT} 대기중 (STX/ETX 프로토콜)", _port);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("설비 접속 시작");
                    _ = HandleClientAsync(tcpClient, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            { 
                // Stop() 호출로 인한 정상 종료
            }
            finally
            {
                listener.Stop();
                _logger.LogInformation("SocketServiceManager 리스너 종료됨");
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken stoppingToken)
        {
            using (tcpClient)
            using (NetworkStream stream = tcpClient.GetStream())
            { 
                List<byte> buffer = new List<byte>();
                byte[] readBuffer = new byte[1024];

                while (tcpClient.Connected && !stoppingToken.IsCancellationRequested)
                {
                    int readCount;
                    try
                    {
                        readCount = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("수신 오류 : {Message}, ex.Message");
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
                    _logger.LogWarning("처리가능한 핸들러없음 - COMMAND: {command}", command);
                    return null;
                }

                var handler = factory.GetMessageHandler(command, "RECEIVE");
                return handler.Handle(frame);
            }
            catch (Exception ex)
            {
                _logger.LogError("메세지 처리 오류: Message}", ex.ToString());
                return null;
            }
        }
    }
}
