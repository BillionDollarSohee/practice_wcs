using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RfidControllService
{
    // RFID 요청 주기를 관리하는 매니저 - Worker가 Run()/Stop()으로 제어
    //
    // 실제 프로젝트는 대차 도착 같은 다른 이벤트가 트리거가 되지만,
    // 미니 프로젝트에서는 데모 목적으로 일정 주기마다 순환 대차ID에 대해 Read를 요청한다.
    //
    // RFIDControllService는 Scoped(내부적으로 DB 컨텍스트에 의존)라서,
    // Singleton인 이 매니저에서는 반복마다 스코프를 새로 만들어 사용한다.
    public class EcsRFIDControllManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EcsRFIDControllManager> _logger;

        private CancellationTokenSource _cts;
        private readonly string[] _demoCartIds = { "CART_0001", "CART_0002", "CART_0003" };

        public EcsRFIDControllManager(IServiceProvider serviceProvider, ILogger<EcsRFIDControllManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Run()
        {
            _cts = new CancellationTokenSource();
            _ = DemoLoopAsync(_cts.Token);
            _logger.LogInformation("EcsRFIDControllManager 시작 - 데모 주기 Read 요청 루프 실행");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _logger.LogInformation("EcsRFIDControllManager 정지 요청됨");
        }

        private async Task DemoLoopAsync(CancellationToken stoppingToken)
        {
            int index = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                string cartId = _demoCartIds[index % _demoCartIds.Length];
                index++;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var rfidControllService = scope.ServiceProvider.GetRequiredService<RFIDControllService>();
                    await rfidControllService.RequestReadTagAsync(cartId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("RFID Read 요청 처리 오류: {Message}", ex.Message);
                }

                try
                {
                    await Task.Delay(8000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}