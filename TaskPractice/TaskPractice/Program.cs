using TaskPractice.Model;
using TaskPractice.Rfid.Interface;
using TaskPractice.Rfid.Provider;
using TaskPractice.Rfid.Service;
using TaskPractice.Route;
using TaskPractice.Service;


// DB 연결 문자열
string connectionString = "Server=localhost;Port=3306;Database=TaskPractice;User=root;Password=1234;";

// 의존성 주입
IRfidProvider rfidProvider = new RfidMySqlProvider(connectionString);
IRfidService rfidService = new RfidService(rfidProvider);


// 노드 생성
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

// 전체 노드 목록
var allNodes = inNodes
    .Concat(stNodes)
    .Concat(pkNodes)
    .Concat(qcNodes)
    .Concat(waNodes)
    .Concat(outNodes)
    .ToList();

// IN → ST 전체 연결
foreach (var inNode in inNodes)
    foreach (var stNode in stNodes)
    {
        inNode.Connections.Add(new Edge { To = stNode, Cost = 1 });
        stNode.Connections.Add(new Edge { To = inNode, Cost = 1 + 90 }); // 역방향 패널티
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

// 경로 탐색 테스트
var dijkstra = new Dijkstra(allNodes);

Console.WriteLine("=== 경로 탐색 테스트 ===\n");

var testCases = new[]
{
    ("IN1",  "ST2"),
    ("ST3",  "PK1"),
    ("PK2",  "QC2"),
    ("QC1",  "WA3"),
    ("WA2",  "OUT2"),
    ("IN2",  "OUT1"),  // 전체 경로
};

// OrderService 생성
var orderService = new OrderService(allNodes);

// SchedulerService 생성
var schedulerService = new SchedulerService(orderService);

Console.WriteLine("\n=== 오더 생성 ===\n");

// 대차 3개 동시 생성
var cart1 = new Cart { CartId = "CA001", FromEqpId = "IN1", ToEqpId = "OUT1", StockType = StockType.EMPTY, LocationType = LocationType.EXTERNAL, InDttm = DateTime.Now };
var cart2 = new Cart { CartId = "CA002", FromEqpId = "IN2", ToEqpId = "OUT2", StockType = StockType.EMPTY, LocationType = LocationType.EXTERNAL, InDttm = DateTime.Now };
var cart3 = new Cart { CartId = "CA003", FromEqpId = "IN3", ToEqpId = "OUT3", StockType = StockType.EMPTY, LocationType = LocationType.EXTERNAL, InDttm = DateTime.Now };

orderService.CreateOrder(cart1);
orderService.CreateOrder(cart2);
orderService.CreateOrder(cart3);

Console.WriteLine("\n=== 폴링 스케줄러 시작 ===\n");

// 스케줄러 시작
schedulerService.Start();

// 모든 오더 COMPLETE + 스케줄러 안정적으로 종료
while (true)
{
    var orders = orderService.GetOrders();
    if (orders.All(o => o.Status == OrderStatus.COMPLETE))
    {
        Thread.Sleep(200); // 마지막 로그 출력 대기
        break;
    }
    Thread.Sleep(100);
}

schedulerService.Stop();
Console.WriteLine("\n=== 전체 완료 ===");

// RFID TEST
rfidService.RequestRead("RFID_IN1");
Console.WriteLine($"BUSY_STATUS: {rfidService.GetStatus("RFID_IN1", "BUSY_STATUS")}");
