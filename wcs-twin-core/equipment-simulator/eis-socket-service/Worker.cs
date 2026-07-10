using EisSocketService.Socket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EisSocketService.Host
{
    // 여러 서비스 매니저의 시작과 종료를 한 곳에서 관리
    public class Worker : BackgroundService
    {
        private readonly SocketServiceManager _socketServiceManager;
        private readonly ILogger<Worker> _logger;
        //private readonly ModbusServiceManager _modbusServiceManager;

        public Worker(
            SocketServiceManager socketServiceManager,
            // ModbusServiceManager modbusServiceManager,
            ILogger<Worker> logger)
        {
            _socketServiceManager = socketServiceManager;
            // _modbusServiceManager = modbusServiceManager;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _socketServiceManager.Run();
            // _modbusServiceManager.Run();

            _logger.LogInformation("Worker 시작 - 등록된 서비스 매니저 실행됨");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _socketServiceManager.Stop();
            // _modbusServiceManager.Stop();

            _logger.LogInformation("Worker 종료 - 서비스 매니저 정지됨");
            return base.StopAsync(cancellationToken);
        }

        // 실제 작업은 각 매니저 내부 루프가 수행하므로, 살아있는지만 확인
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
