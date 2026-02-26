using TaskPractice.Conveyor.Interface;
using TaskPractice.Model;

namespace TaskPractice.Conveyor.Service
{
    // 컨베이어 인터페이스 / 스케줄러 총괄 관리
    public class ConveyorServiceManager
    {
        private readonly IConveyorProvider _conveyorProvider;

        // Key: PollingGroup
        private Dictionary<string, ConveyorInterface> _interfaceDict = new Dictionary<string, ConveyorInterface>();

        // Key: EqpGroup
        private Dictionary<string, ConveyorScheduler> _schedulerDict = new Dictionary<string, ConveyorScheduler>();

        // 전체 설비 목록 (Repository 역할)
        private List<ConveyorUnit> _conveyorUnits = new List<ConveyorUnit>();

        // PLC 연결 정보 (Key: PollingGroup)
        private Dictionary<string, (string IpAddr, int Port)> _connectionDict = new Dictionary<string, (string, int)>();

        private Timer _serviceManagerThread = null;
        private bool _threadStopSwitch = false;

        // 폴링 주기 (ms)
        private const int POLLING_INTERVAL = 200;

        public ConveyorServiceManager(IConveyorProvider conveyorProvider)
        {
            _conveyorProvider = conveyorProvider;
        }

        public void Run()
        {
            try
            {
                // 설비 목록 로드
                _conveyorUnits = _conveyorProvider.GetConveyorUnitList();

                // 인터페이스 딕셔너리 생성 (PollingGroup 단위)
                foreach (var unit in _conveyorUnits.GroupBy(g => g.PollingGroup))
                {
                    string pollingGroup = unit.Key;

                    if (_interfaceDict.ContainsKey(pollingGroup))
                        continue;

                    // 연결 정보 조회
                    if (!_connectionDict.TryGetValue(pollingGroup, out var connInfo))
                    {
                        Console.WriteLine($"연결 정보 없음 - PollingGroup: {pollingGroup}");
                        continue;
                    }

                    var conveyorInterface = new ConveyorInterface(
                        _conveyorProvider,
                        pollingGroup,
                        connInfo.IpAddr,
                        connInfo.Port,
                        unit.ToList()
                    );

                    _interfaceDict.Add(pollingGroup, conveyorInterface);
                }

                // 스케줄러 딕셔너리 생성 (EqpGroup 단위)
                foreach (var unit in _conveyorUnits.GroupBy(g => g.EqpGroup))
                {
                    string eqpGroup = unit.Key;

                    if (_schedulerDict.ContainsKey(eqpGroup))
                        continue;

                    var scheduler = new ConveyorScheduler(_conveyorProvider, eqpGroup);
                    _schedulerDict.Add(eqpGroup, scheduler);
                }

                // 인터페이스 스레드 시작
                foreach (var iface in _interfaceDict.Values)
                {
                    iface.Start();
                }

                // 스케줄러 스레드 시작
                foreach (var scheduler in _schedulerDict.Values)
                {
                    scheduler.Start();
                }

                // 서비스 매니저 스레드 시작
                if (_serviceManagerThread == null)
                {
                    _threadStopSwitch = false;
                    _serviceManagerThread = new Timer(ServiceThreadScheduler);
                    _serviceManagerThread.Change(0, Timeout.Infinite);

                    Console.WriteLine("ConveyorServiceManager Start");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            _threadStopSwitch = true;

            foreach (var iface in _interfaceDict.Values)
                iface.Stop();

            foreach (var scheduler in _schedulerDict.Values)
                scheduler.Stop();
        }

        // 연결 정보 등록 (Program.cs에서 호출)
        public void AddConnection(string pollingGroup, string ipAddr, int port)
        {
            _connectionDict[pollingGroup] = (ipAddr, port);
        }

        private void ServiceThreadScheduler(object? state)
        {
            // 타이머 중지
            _serviceManagerThread.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // WAIT 명령 조회 → 폴링그룹별 큐에 분배
                GetCommandAndDistribute();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (!_threadStopSwitch)
                {
                    _serviceManagerThread.Change(POLLING_INTERVAL, Timeout.Infinite);
                }
                else
                {
                    _serviceManagerThread.Dispose();
                    _serviceManagerThread = null;

                    Console.WriteLine("ConveyorServiceManager Stop");
                }
            }
        }

        private void GetCommandAndDistribute()
        {
            // WAIT 명령 전체 조회
            var waitCommands = _conveyorProvider.GetWaitCommandList();

            if (waitCommands.Count <= 0)
                return;

            foreach (var command in waitCommands)
            {
                // 해당 설비의 PollingGroup 찾기
                var unit = _conveyorUnits.FirstOrDefault(w => w.EqpId == command.EqpId);
                if (unit == null)
                    continue;

                string pollingGroup = unit.PollingGroup;

                // 해당 폴링그룹 인터페이스 큐에 Enqueue
                if (!_interfaceDict.TryGetValue(pollingGroup, out var iface))
                    continue;

                // 이미 큐에 있으면 중복 방지
                if (iface.CommandQueue.Any(q => q.EqpId == command.EqpId))
                    continue;

                iface.CommandQueue.Enqueue(command);

                Console.WriteLine($"명령 분배 - EqpId: {command.EqpId}, PollingGroup: {pollingGroup}");
            }
        }
    }
}