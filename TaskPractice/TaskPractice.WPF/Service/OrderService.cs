using TaskPractice.Model;
using TaskPractice.Route;

namespace TaskPractice.Service
{
    public class OrderService
    {
        private List<Node> _allNodes;
        private Dijkstra _dijkstra;

        // 인메모리 오더 저장소
        private List<OrderMaster> _orders = new List<OrderMaster>();

        private int _orderSeq = 0;

        public OrderService(List<Node> allNodes)
        {
            _allNodes = allNodes;
            _dijkstra = new Dijkstra(allNodes);
        }

        // 오더 ID 채번
        private string GetNextOrderId()
        {
            _orderSeq++;
            return $"ORD-{_orderSeq:D3}";
        }

        // 오더 생성 (ECS Serivce.CreateOrder 역할)
        public OrderMaster CreateOrder(Cart cart)
        {
            // 경로 계산
            List<Node> path = _dijkstra.GetShortestPath(cart.FromEqpId, cart.ToEqpId);

            if (path == null || path.Count == 0)
            {
                Console.WriteLine($"[오더생성 실패] 경로를 찾을 수 없습니다. {cart.FromEqpId} → {cart.ToEqpId}");
                return null;
            }

            // 오더 마스터 생성
            OrderMaster order = new OrderMaster()
            {
                OrderId = GetNextOrderId(),
                CartId = cart.CartId,
                FromId = cart.FromEqpId,
                ToEqpId = cart.ToEqpId,
                Status = OrderStatus.INIT
            };

            // 오더 디테일 생성 (구간별)
            for (int i = 1; i < path.Count; i++)
            {
                order.Details.Add(new OrderDetail()
                {
                    OrderId = order.OrderId,
                    Seq = i,
                    FromEqpId = path[i - 1].Id,
                    ToEqpId = path[i].Id,
                    Status = DetailStatus.INIT
                });
            }

            _orders.Add(order);

            Console.WriteLine($"[오더생성] {order.OrderId} 대차:{cart.CartId} {cart.FromEqpId} → {cart.ToEqpId} 구간수:{order.Details.Count}");

            return order;
        }

        // 현재 진행할 구간 반환 (= INIT중 가장 작은 Seq)
        public OrderDetail? GetNextDetail(string orderId)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return null;

            return order.Details
                .Where(d => d.Status == DetailStatus.INIT)
                .OrderBy(d => d.Seq)
                .FirstOrDefault();
        }

        // 구간 출발 처리 (INIT -> WORKING)
        public void DepartDetail(string orderId, int seq)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return;

            var detail = order.Details.FirstOrDefault(d => d.Seq == seq);
            if (detail == null) return;

            detail.Status = DetailStatus.WORKING;

            // 첫 구간 출발 시 마스터 WORKING으로 변경
            if (order.Status == OrderStatus.INIT)
            {
                order.Status = OrderStatus.WORKING;
            }

            Console.WriteLine($"[이동중] {order.CartId} {detail.FromEqpId} -> {detail.ToEqpId}");
        }

        // 구간 도착 처리 (WORKING → COMPLETE)
        public void ArriveDetail(string orderId, int seq)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return;

            var detail = order.Details.FirstOrDefault(d => d.Seq == seq);
            if (detail == null) return;

            detail.Status = DetailStatus.COMPLETE;

            Console.WriteLine($"[도착완료] {order.CartId} {detail.FromEqpId} → {detail.ToEqpId}");

            // 모든 구간 완료 시 마스터 COMPLETE
            if (order.Details.All(d => d.Status == DetailStatus.COMPLETE))
            {
                order.Status = OrderStatus.COMPLETE;
                Console.WriteLine($"[오더완료] {order.OrderId} 대차:{order.CartId} 최종목적지:{order.ToEqpId} 도착");
            }
        }

        public List<OrderMaster> GetOrders() => _orders;
    }
}