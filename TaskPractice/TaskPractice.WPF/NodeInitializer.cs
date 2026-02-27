using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskPractice.Model;

namespace TaskPractice.WPF
{
    public static class NodeInitializer
    {
        public static List<Node> CreateNodes()
        {
            var inNodes = new[] { "IN1", "IN2", "IN3" }
                .Select(id => new Node { Id = id, Name = $"입고구{id.Last()}" }).ToList();
            var stNodes = new[] { "ST1", "ST2", "ST3" }
                .Select(id => new Node { Id = id, Name = $"보관{id.Last()}" }).ToList();
            var pkNodes = new[] { "PK1", "PK2", "PK3" }
                .Select(id => new Node { Id = id, Name = $"피킹존{id.Last()}" }).ToList();
            var qcNodes = new[] { "QC1", "QC2", "QC3" }
                .Select(id => new Node { Id = id, Name = $"검수존{id.Last()}" }).ToList();
            var waNodes = new[] { "WA1", "WA2", "WA3" }
                .Select(id => new Node { Id = id, Name = $"대기라인{id.Last()}" }).ToList();
            var outNodes = new[] { "OUT1", "OUT2", "OUT3" }
                .Select(id => new Node { Id = id, Name = $"출고구{id.Last()}" }).ToList();

            // IN → ST 전체 연결
            foreach (var inNode in inNodes)
                foreach (var stNode in stNodes)
                {
                    inNode.Connections.Add(new Edge { To = stNode, Cost = 1 });
                    stNode.Connections.Add(new Edge { To = inNode, Cost = 1 + 90 });
                }

            // ST → PK 전체 연결
            foreach (var stNode in stNodes)
                foreach (var pkNode in pkNodes)
                {
                    stNode.Connections.Add(new Edge { To = pkNode, Cost = 1 });
                    pkNode.Connections.Add(new Edge { To = stNode, Cost = 1 + 90 });
                }

            // PK → QC 1:1 연결
            for (int i = 0; i < 3; i++)
            {
                pkNodes[i].Connections.Add(new Edge { To = qcNodes[i], Cost = 1 });
                qcNodes[i].Connections.Add(new Edge { To = pkNodes[i], Cost = 1 + 90 });
            }

            // QC → WA 전체 연결
            foreach (var qcNode in qcNodes)
                foreach (var waNode in waNodes)
                {
                    qcNode.Connections.Add(new Edge { To = waNode, Cost = 1 });
                    waNode.Connections.Add(new Edge { To = qcNode, Cost = 1 + 90 });
                }

            // WA → OUT 1:1 연결
            for (int i = 0; i < 3; i++)
            {
                waNodes[i].Connections.Add(new Edge { To = outNodes[i], Cost = 1 });
                outNodes[i].Connections.Add(new Edge { To = waNodes[i], Cost = 1 + 90 });
            }

            var allNodes = inNodes
                .Concat(stNodes)
                .Concat(pkNodes)
                .Concat(qcNodes)
                .Concat(waNodes)
                .Concat(outNodes)
                .ToList();

            return allNodes;
        }

        // IN 노드만 반환
        public static List<string> GetFromNodeIds()
        {
            return new[] { "IN1", "IN2", "IN3" }.ToList();
        }

        // OUT 노드만 반환
        public static List<string> GetToNodeIds()
        {
            return new[] { "OUT1", "OUT2", "OUT3" }.ToList();
        }
    }
}
