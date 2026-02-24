using TaskPractice.Model;

namespace TaskPractice.Route
{
    internal class Dijkstra
    {
        private List<Node> _nodes;

        public Dijkstra(List<Node> nodes)
        {
            _nodes = nodes;
        }

        public List<Node> GetShortestPath(string fromId, string toId)
        {
            // 탐색 전 상태 초기화
            foreach (var node in _nodes)
            {
                node.Visited = false;
                node.MinCost = null; // 0으로 초기화했더니 조건에 걸림
                node.NearestToStart = null;
            }

            Node startNode = _nodes.First(n => n.Id == fromId);
            Node endNode = _nodes.First(n => n.Id == toId);

            // 출발 노드 비용 0에서 시작
            startNode.MinCost = 0;

            while (true)
            {
                // 미방문 노드 중 비용이 가장 낮은 노드 선택
                Node? current = _nodes
                    .Where(n => !n.Visited && n.MinCost != null)
                    .OrderBy(n => n.MinCost)
                    .FirstOrDefault();

                if (current == null) break;
                if (current == endNode) break;

                current.Visited = true;

                // 연결된 노드 비용 업데이트
                foreach (var edge in current.Connections)
                {
                    double newCost = current.MinCost.Value + edge.Cost;
                    if (edge.To.MinCost == null || newCost < edge.To.MinCost)
                    {
                        edge.To.MinCost = newCost;
                        edge.To.NearestToStart = current;
                    }
                }
            }

            // 도착지에서 역추적해서 경로 복원
            List<Node> path = new List<Node>();
            Node trace = endNode;
            
            while(trace != null)
            {
                path.Insert(0, trace);
                trace = trace.NearestToStart;
            }

            return path;
        }
    }
}
