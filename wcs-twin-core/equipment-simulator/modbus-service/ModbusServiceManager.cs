using Microsoft.Extensions.Logging;

namespace ModbusService
{
    // Modbus 클라이언트 연결을 관리하는 매니저 - Worker가 Run()/Stop()으로 제어
    public class ModbusServiceManager
    {
        private readonly ModbusPlcInterfaceService _plcInterfaceService;
        private readonly ILogger<ModbusServiceManager> _logger;

        public ModbusServiceManager(ModbusPlcInterfaceService plcInterfaceService, ILogger<ModbusServiceManager> logger)
        {
            _plcInterfaceService = plcInterfaceService;
            _logger = logger;
        }

        public void Run()
        {
            _ = _plcInterfaceService.EnsureConnectedAsync();
            _logger.LogInformation("ModbusServiceManager 시작 - 리더 접속 시도");
        }

        public void Stop()
        {
            _logger.LogInformation("ModbusServiceManager 정지 요청됨");
        }
    }
}