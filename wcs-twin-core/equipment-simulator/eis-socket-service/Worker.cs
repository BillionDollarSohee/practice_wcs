using System.Threading;
using System.Threading.Tasks;
using EisSocketService.Socket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusService;

namespace EisSocketService.Host
{
    // 여러 서비스 매니저의 시작/종료를 한 곳에서 관리
    //
    // EcsRFIDControllManager(독립 RFID 데모 루프)는 더 이상 여기서 실행하지 않는다.
    // RFID는 이제 Vision 핸들러(VisionCartInfoRequestHandler, VisionProductCompleteHandler)
    // 안에서 해당 대차에 대해서만 트리거되므로, 별도 주기 실행이 필요 없다.
    public class Worker : BackgroundService
    {
        private readonly SocketServiceManager _socketServiceManager;
        private readonly ModbusServiceManager _modbusServiceManager;
        private readonly ILogger<Worker> _logger;

        public Worker(
            SocketServiceManager socketServiceManager,
            ModbusServiceManager modbusServiceManager,
            ILogger<Worker> logger)
        {
            _socketServiceManager = socketServiceManager;
            _modbusServiceManager = modbusServiceManager;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _modbusServiceManager.Run(); // 리더 접속을 먼저 준비
            _socketServiceManager.Run(); // Vision 접속 수신 시작 (RFID 확인/기록은 이 안에서 트리거됨)

            _logger.LogInformation("Worker 시작 - 등록된 서비스 매니저 실행됨");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _socketServiceManager.Stop();
            _modbusServiceManager.Stop();

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