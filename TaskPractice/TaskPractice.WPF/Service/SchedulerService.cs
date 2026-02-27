using TaskPractice.Model;

namespace TaskPractice.Service
{
    public class SchedulerService
    {
        private OrderService _orderService;
        private Timer _timer;
        private bool _stopSwitch = false;

        // 구간별 이동 시간 추적 (오더ID_Seq -> 이동 시작 시간)
        private Dictionary<string, DateTime> _workingStartTime
            = new Dictionary<string, DateTime>();

        // 이동 소요 시간(ms)
        private const int MOVE_DURATION_MS = 2000;

        public SchedulerService(OrderService orderService)
        {
            _orderService = orderService;
        }

        public void Start()
        {
            _stopSwitch = false;
            _timer = new Timer(SchedulerLogic);
            _timer.Change(20, Timeout.Infinite);
            Console.WriteLine("[스케줄러 관리 시작]");
        }

        public void Stop()
        {
            _stopSwitch = true;
            Console.WriteLine("[스케줄러] 종료");
        }

        private void SchedulerLogic(object? state)
        {
            // 타이머 중지 (처리 중 중복 실행 방지)
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // 전체 오더 순회
                var orders = _orderService.GetOrders()
                    .Where(o => o.Status != OrderStatus.COMPLETE)
                    .ToList();

                foreach (var order in orders)
                {
                    // WORKING 구간 확인하고 이동 시간이 지났으면 도착 처리
                    // 추후에 도착 태그로 도착처리하게 바꾸면 좋을듯
                    var workingDetail = order.Details
                        .FirstOrDefault(d => d.Status == DetailStatus.WORKING);

                    if (workingDetail != null)
                    {
                        string key = $"{order.OrderId}_{workingDetail.Seq}";

                        // 로그
                        // Console.WriteLine($"[디버그] {order.CartId} Seq:{workingDetail.Seq} key존재:{_workingStartTime.ContainsKey(key)}");

                        if (_workingStartTime.ContainsKey(key))
                        {
                            var elapsed = (DateTime.Now - _workingStartTime[key]).TotalMilliseconds;

                            // 로그
                            // Console.WriteLine($"[디버그] {order.CartId} Seq:{workingDetail.Seq} elapsed:{elapsed:F0}ms MOVE_DURATION:{MOVE_DURATION_MS}ms");
                            if (elapsed >= MOVE_DURATION_MS)
                            {
                                // 이동 시간 초과시 도착 처리
                                _orderService.ArriveDetail(order.OrderId, workingDetail.Seq);
                                _workingStartTime.Remove(key);
                            }
                        }
                        else
                        {
                            // 이동 시작 시간 기록
                            _workingStartTime[key] = DateTime.Now;
                        }
                        // WORKING 중인 구간 있으면 다음 구간 시작 안함
                        continue;
                    }

                    // INIT 구간 확인 -> 출발처리
                    var nextDetail = _orderService.GetNextDetail(order.OrderId);
                    if (nextDetail != null)
                    {
                        string key = $"{order.OrderId}_{nextDetail.Seq}";
                        _orderService.DepartDetail(order.OrderId, nextDetail.Seq);
                        _workingStartTime[key] = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[스케줄러 오류] {ex.Message}");
            }
            finally
            {
                // 종료 스위치 확인 후 타이머 재등록
                if (!_stopSwitch)
                {
                    _timer.Change(20, Timeout.Infinite);
                }
                else 
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }
    }
}
