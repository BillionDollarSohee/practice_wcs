using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ModbusService
{
    // 라인(TR/FL/DR)별로 독립된 ModbusPlcInterfaceService(=RFID 리더 접속)를 관리한다.
    // 트림/파이널/도어는 각자 물리적으로 다른 RFID 리더를 쓰므로 연결도 완전히 분리한다.
    public class ModbusServiceRegistry
    {
        private readonly Dictionary<string, ModbusPlcInterfaceService> _services = new();

        public ModbusServiceRegistry(Dictionary<string, (string Host, int Port)> lineEndpoints, ILogger<ModbusPlcInterfaceService> logger)
        {
            foreach (var (lineType, endpoint) in lineEndpoints)
            {
                _services[lineType] = new ModbusPlcInterfaceService(logger, endpoint.Host, endpoint.Port);
            }
        }

        public bool TryGetService(string lineType, out ModbusPlcInterfaceService service)
        {
            return _services.TryGetValue(lineType, out service);
        }

        public IEnumerable<ModbusPlcInterfaceService> All => _services.Values;
    }
}
