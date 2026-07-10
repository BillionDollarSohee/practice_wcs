using System.Threading;
using System.Threading.Tasks;
using EisSocketService.Socket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusService;
using RfidControllService;

namespace EisSocketService.Host
{
    // 여러 서비스 매니저(Socket, Modbus, RFID)의 시작/종료를 한 곳에서 관리
    public class Worker : BackgroundService
    {
        private readonly SocketServiceManager _socketServiceManager;
        private readonly ModbusServiceManager _modbusServiceManager;
        private readonly EcsRFIDControllManager _ecsRfidControllManager;
        private readonly ILogger<Worker> _logger;

        public Worker(
            SocketServiceManager socketServiceManager,
            ModbusServiceManager modbusServiceManager,
            EcsRFIDControllManager ecsRfidControllManager,
            ILogger<Worker> logger)
        {
            _socketServiceManager = socketServiceManager;
            _modbusServiceManager = modbusServiceManager;
            _ecsRfidControllManager = ecsRfidControllManager;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _socketServiceManager.Run();
            _modbusServiceManager.Run();
            _ecsRfidControllManager.Run();

            _logger.LogInformation("Worker 시작 - 등록된 서비스 매니저 실행됨");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _socketServiceManager.Stop();
            _modbusServiceManager.Stop();
            _ecsRfidControllManager.Stop();

            _logger.LogInformation("Worker 종료 - 서비스 매니저 정지됨");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}