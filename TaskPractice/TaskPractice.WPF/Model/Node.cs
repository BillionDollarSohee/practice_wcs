namespace TaskPractice.Model
{
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Edge> Connections { get; set; } = new List<Edge>();

        // 다익스트라 탐색용 임시 필드
        public bool Visited { get; set; } = false;
        public double? MinCost { get; set; } = null;
        public Node? NearestToStart { get; set; } = null;

        public override string ToString() => $"{Id}({Name})";
    }
}
