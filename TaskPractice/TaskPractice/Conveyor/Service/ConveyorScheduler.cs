using TaskPractice.Conveyor.Interface;

namespace TaskPractice.Conveyor.Service
{
    // 오더 탐색 및 명령 생성 스케줄러
    public class ConveyorScheduler
    {
        private readonly IConveyorProvider _conveyorProvider;

        private readonly string _eqpGroup;

        private Timer _schedulerThread = null;
        private bool _threadStopSwitch = false;

        // 폴링 주기 (ms)
        private const int POLLING_INTERVAL = 500;

        public ConveyorScheduler(IConveyorProvider conveyorProvider, string eqpGroup)
        {
            _conveyorProvider = conveyorProvider;
            _eqpGroup = eqpGroup;
        }

        public void Start()
        {
            if (_schedulerThread == null)
            {
                _threadStopSwitch = false;
                _schedulerThread = new Timer(SchedulerLogic);
                _schedulerThread.Change(0, Timeout.Infinite);

                Console.WriteLine($"ConveyorScheduler Start - EqpGroup: {_eqpGroup}");
            }
        }

        public void Stop()
        {
            _threadStopSwitch = true;
        }

        private void SchedulerLogic(object? state)
        {
            // 타이머 중지
            _schedulerThread.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // READY 오더 탐색 후 CV_COMMAND 생성
                _conveyorProvider.FindConveyorOrderAndCreateCommand(_eqpGroup);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (!_threadStopSwitch)
                {
                    // 다음 폴링 예약
                    _schedulerThread.Change(POLLING_INTERVAL, Timeout.Infinite);
                }
                else
                {
                    _schedulerThread.Dispose();
                    _schedulerThread = null;

                    Console.WriteLine($"ConveyorScheduler Stop - EqpGroup: {_eqpGroup}");
                }
            }
        }
    }
}