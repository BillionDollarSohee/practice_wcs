using System.Collections.Concurrent;
using ExecutionAgentKata;

// ★ 이 파일은 "채점기" 입니다. 수정하지 않습니다.
//   dotnet run          → 전체 채점 (타이밍 테스트라 10초 정도 걸립니다)
//   dotnet run -- A     → PART A 만 (A, B)
//
//   PASS = 통과,  TODO = 아직 안 채움,  FAIL = 틀림 (이유가 → 뒤에 나옵니다)

int pass = 0, todo = 0, fail = 0;
string? only = args.FirstOrDefault()?.ToUpperInvariant();

void Check(string name, Action test)
{
    try { test(); Console.WriteLine($"  PASS  {name}"); pass++; }
    catch (NotImplementedException ex) { Console.WriteLine($"  TODO  {name}  ({ex.Message})"); todo++; }
    catch (Exception ex) { Console.WriteLine($"  FAIL  {name}  → {ex.Message}"); fail++; }
}

void Expect(bool cond, string msg) { if (!cond) throw new Exception(msg); }

void Section(string letter, string title, Action body)
{
    if (only != null && only != letter) return;
    Console.WriteLine($"\n[{letter}] {title}");
    body();
}

bool WaitUntil(Func<bool> cond, int timeoutMs = 2000)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    while (sw.ElapsedMilliseconds < timeoutMs)
    {
        if (cond()) return true;
        Thread.Sleep(5);
    }
    return cond();
}

// 타이머 루프 흉내: durationMs 동안 10ms 마다 호출
void Spin(int durationMs, Action call)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    while (sw.ElapsedMilliseconds < durationMs)
    {
        call();
        Thread.Sleep(10);
    }
}

(ExecutionAgent agent, InMemorySchedulerSettingProvider settings, ConcurrentQueue<string> logs) NewAgent(int pollingMs = 100, bool isUse = true, string logicId = "L")
{
    var settings = new InMemorySchedulerSettingProvider();
    settings.Set("P", logicId, pollingMs, isUse);
    var logs = new ConcurrentQueue<string>();
    return (new ExecutionAgent(settings, logs.Enqueue), settings, logs);
}


// ===================================================== PART A
Section("A", "설정 조회 / 기본값 등록", () =>
{
    Check("설정이 있으면 그 값을 돌려준다", () =>
    {
        var (agent, _, _) = NewAgent(pollingMs: 250, isUse: false);
        var r = agent.GetLogicPollingTime("P", "L", "t");
        Expect(r.PollingMs == 250 && r.IsUse == false, $"결과 ({r.PollingMs}, {r.IsUse}) (기대 (250, False))");
    });

    Check("설정이 없으면 기본값(500, true)을 돌려주고, 저장소에 1번 등록한다", () =>
    {
        var settings = new InMemorySchedulerSettingProvider();
        var agent = new ExecutionAgent(settings);
        var r = agent.GetLogicPollingTime("P", "NEW", "t");
        Expect(r.PollingMs == 500 && r.IsUse, $"결과 ({r.PollingMs}, {r.IsUse}) (기대 (500, True))");
        Expect(settings.SaveDefaultCalls == 1, $"SaveDefault {settings.SaveDefaultCalls}번 (기대 1번)");
        agent.GetLogicPollingTime("P", "NEW", "t");
        Expect(settings.SaveDefaultCalls == 1, "두 번째 조회에서는 다시 등록하면 안 됨 (이미 설정이 생겼으니까)");
    });
});


// ===================================================== PART B
Section("B", "주기 실행 (타이밍)", () =>
{
    Check("첫 호출은 실행되지 않는다 (스톱워치만 시작), 주기가 지난 뒤 호출하면 실행된다", () =>
    {
        var (agent, _, _) = NewAgent(100);
        int n = 0;
        Func<bool> logic = () => { Interlocked.Increment(ref n); return true; };

        agent.ExecuteLogicByPollingTime("P", "L", logic, "t");
        Thread.Sleep(30);
        agent.ExecuteLogicByPollingTime("P", "L", logic, "t");
        Thread.Sleep(30);
        Expect(n == 0, $"주기(100ms)가 지나기 전에 {n}번 실행됨 (기대 0번)");

        Thread.Sleep(120);
        agent.ExecuteLogicByPollingTime("P", "L", logic, "t");
        Expect(WaitUntil(() => n == 1), $"주기가 지났는데 실행되지 않음 (n={n})");
    });

    Check("실행 중에는 겹쳐 실행되지 않는다 (동시 실행 최대 1)", () =>
    {
        var (agent, _, _) = NewAgent(50);
        int running = 0, maxRunning = 0, total = 0;
        Func<bool> logic = () =>
        {
            int now = Interlocked.Increment(ref running);
            lock (agent) maxRunning = Math.Max(maxRunning, now);
            Thread.Sleep(300);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref total);
            return true;
        };

        Spin(900, () => agent.ExecuteLogicByPollingTime("P", "L", logic, "t"));
        WaitUntil(() => Volatile.Read(ref running) == 0);
        Expect(total >= 1, "한 번도 실행되지 않음");
        Expect(maxRunning == 1, $"동시에 {maxRunning}개가 실행됨 (IsLogicBusy 로 막아야 함)");
    });

    Check("끝난 뒤에 주기를 다시 잰다 (1초 동안 4~10번 실행)", () =>
    {
        var (agent, _, _) = NewAgent(100);
        int n = 0;
        Spin(1000, () => agent.ExecuteLogicByPollingTime("P", "L", () => { Interlocked.Increment(ref n); return true; }, "t"));
        Expect(n >= 4 && n <= 10, $"1초 동안 {n}번 실행됨 (기대 4~10번: 실행이 끝난 시점부터 100ms 를 다시 세야 함)");
    });

    Check("IsUse = false 이면 절대 실행하지 않는다", () =>
    {
        var (agent, _, _) = NewAgent(50, isUse: false);
        int n = 0;
        Spin(400, () => agent.ExecuteLogicByPollingTime("P", "L", () => { Interlocked.Increment(ref n); return true; }, "t"));
        Expect(n == 0, $"사용 안 함인데 {n}번 실행됨");
    });

    Check("로직이 예외를 던져도 삼키고, busy 가 풀려서 다음에 또 실행된다", () =>
    {
        var (agent, _, logs) = NewAgent(50);
        int n = 0;
        Func<bool> logic = () =>
        {
            int c = Interlocked.Increment(ref n);
            if (c == 1) throw new InvalidOperationException("boom");
            return true;
        };
        Spin(800, () => agent.ExecuteLogicByPollingTime("P", "L", logic, "t"));
        Expect(n >= 2, $"예외 이후 다시 실행되지 않음 (n={n}) — finally 에서 IsLogicBusy 를 풀어야 함");
        Expect(logs.Any(l => l.Contains("예외")), "예외 로그(\"실행 중 예외\")가 없음");
    });

    Check("로직이 false 를 반환하면 \"실행 중 실패\" 로그를 남기고 계속 돈다", () =>
    {
        var (agent, _, logs) = NewAgent(50);
        int n = 0;
        Spin(600, () => agent.ExecuteLogicByPollingTime("P", "L", () => { Interlocked.Increment(ref n); return false; }, "t"));
        Expect(n >= 2, $"false 반환 후 계속 실행되지 않음 (n={n})");
        Expect(logs.Any(l => l.Contains("실패")), "\"실행 중 실패\" 로그가 없음");
    });

    Check("서로 다른 logicId 는 독립적이다 (A 가 오래 걸려도 B 는 계속 돈다)", () =>
    {
        var settings = new InMemorySchedulerSettingProvider();
        settings.Set("P", "A", 50, true);
        settings.Set("P", "B", 50, true);
        var agent = new ExecutionAgent(settings);
        int a = 0, b = 0;
        Func<bool> slow = () => { Interlocked.Increment(ref a); Thread.Sleep(400); return true; };
        Func<bool> fast = () => { Interlocked.Increment(ref b); return true; };

        Spin(700, () =>
        {
            agent.ExecuteLogicByPollingTime("P", "A", slow, "t");
            agent.ExecuteLogicByPollingTime("P", "B", fast, "t");
        });
        Expect(a >= 1 && a <= 2, $"A 실행 {a}번 (기대 1~2번)");
        Expect(b >= 3, $"B 실행 {b}번 (기대 3번 이상 — A 때문에 막히면 안 됨)");
    });
});


// ===================================================== PART C
Section("C", "설비그룹 오버로드", () =>
{
    Check("같은 logicId 라도 설비그룹이 다르면 따로 실행되고, 로직에 그룹이 전달된다", () =>
    {
        var (agent, _, _) = NewAgent(50);
        var seen = new ConcurrentDictionary<string, int>();
        Func<string, bool> logic = g => { seen.AddOrUpdate(g, 1, (_, c) => c + 1); return true; };

        Spin(500, () =>
        {
            agent.ExecuteLogicByPollingTime("P", "L", logic, "S1", "t");
            agent.ExecuteLogicByPollingTime("P", "L", logic, "S2", "t");
        });
        Expect(seen.GetValueOrDefault("S1") >= 2, $"S1 실행 {seen.GetValueOrDefault("S1")}번 (기대 2번 이상)");
        Expect(seen.GetValueOrDefault("S2") >= 2, $"S2 실행 {seen.GetValueOrDefault("S2")}번 (기대 2번 이상) — 그룹별로 별개 상태여야 함");
    });

    Check("한 그룹이 실행 중이어도 다른 그룹은 막히지 않는다", () =>
    {
        var (agent, _, _) = NewAgent(50);
        int s1 = 0, s2 = 0;
        Func<string, bool> logic = g =>
        {
            if (g == "S1") { Interlocked.Increment(ref s1); Thread.Sleep(500); }
            else Interlocked.Increment(ref s2);
            return true;
        };
        Spin(600, () =>
        {
            agent.ExecuteLogicByPollingTime("P", "L", logic, "S1", "t");
            agent.ExecuteLogicByPollingTime("P", "L", logic, "S2", "t");
        });
        Expect(s1 == 1, $"S1 {s1}번 (기대 1번)");
        Expect(s2 >= 3, $"S2 {s2}번 (기대 3번 이상)");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
