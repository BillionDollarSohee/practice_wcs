using Microsoft.Extensions.Logging;

namespace ModbusService
{
    // Modbus 클라이언트 연결을 관리하는 매니저 - Worker가 Run()/Stop()으로 제어
    // 라인(TR/FL/DR)별로 독립된 리더에 각각 접속을 시도한다.
    public class ModbusServiceManager
    {
        private readonly ModbusServiceRegistry _registry;
        private readonly ILogger<ModbusServiceManager> _logger;

        public ModbusServiceManager(ModbusServiceRegistry registry, ILogger<ModbusServiceManager> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public void Run()
        {
            foreach (var service in _registry.All)
            {
                _ = service.EnsureConnectedAsync();
            }
            _logger.LogInformation("ModbusServiceManager 시작 - 라인별 리더 접속 시도");
        }

        public void Stop()
        {
            _logger.LogInformation("ModbusServiceManager 정지 요청됨");
        }
    }
}