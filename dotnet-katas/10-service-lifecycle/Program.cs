using System.Collections.Concurrent;
using ServiceLifecycleKata;

// ★ 이 파일은 "채점기" 입니다. 수정하지 않습니다.
//   dotnet run          → 전체 채점 (타이밍 테스트라 8초 정도 걸립니다)
//   dotnet run -- A     → PART A 만 (A, B)
//
// 주의: TODO 1(ThreadStart)만 채우고 TODO 3(SchedulerLogic)을 아직 안 채웠다면, 타이머 콜백에서 예외가 나서 프로세스가 종료될 수 있습니다.
//       그래서 TODO 3 → 1 → 2 순서로 채우거나, 세 개를 모두 채운 뒤에 돌려 보세요.

var lockObj = new object();
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


// ===================================================== PART A
Section("A", "켜기 / 끄기", () =>
{
    Check("ThreadStart 하면 일이 반복해서 실행된다", () =>
    {
        int n = 0;
        var svc = new TimerService(() => { Interlocked.Increment(ref n); return true; }, intervalMs: 20);
        Expect(svc.ThreadStart(), "ThreadStart 가 true 를 반환해야 함");
        Expect(WaitUntil(() => n >= 4, 1500), $"일이 {n}번만 실행됨 (기대 4번 이상 — 한 바퀴 끝에서 스스로 재예약해야 함)");
        svc.ThreadStop();
    });

    Check("ThreadStop 하면 더 이상 실행되지 않고 IsRunning 이 false 가 된다", () =>
    {
        int n = 0;
        var svc = new TimerService(() => { Interlocked.Increment(ref n); return true; }, intervalMs: 20);
        svc.ThreadStart();
        WaitUntil(() => n >= 2);
        Expect(svc.ThreadStop(), "ThreadStop 이 true 를 반환해야 함");
        Expect(!svc.IsRunning, "IsRunning 이 false 여야 함");

        int after = n;
        Thread.Sleep(250);
        Expect(n == after, $"정지 후에도 {n - after}번 더 실행됨");
    });

    Check("켜진 적 없는 서비스를 ThreadStop 해도 예외 없이 true", () =>
    {
        var svc = new TimerService(() => true);
        Expect(svc.ThreadStop(), "true 여야 함");
    });

    Check("ThreadStop 을 두 번 불러도 괜찮다", () =>
    {
        var svc = new TimerService(() => true, intervalMs: 20);
        svc.ThreadStart();
        Expect(svc.ThreadStop() && svc.ThreadStop(), "두 번 모두 true 여야 함");
    });

    Check("ThreadStart 를 두 번 불러도 타이머는 하나다 (일이 겹쳐 실행되지 않는다)", () =>
    {
        int running = 0, maxRunning = 0, n = 0;
        var svc = new TimerService(() =>
        {
            int now = Interlocked.Increment(ref running);
            lock (lockObj) maxRunning = Math.Max(maxRunning, now);
            Thread.Sleep(40);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref n);
            return true;
        }, intervalMs: 10);
        svc.ThreadStart();
        Expect(svc.ThreadStart(), "두 번째 ThreadStart 도 true 여야 함");
        WaitUntil(() => n >= 4, 1500);
        svc.ThreadStop();
        Expect(maxRunning == 1, $"동시에 {maxRunning}개가 실행됨 (타이머를 두 개 만들었거나 한 바퀴가 겹침)");
    });

    Check("정지한 뒤 다시 ThreadStart 하면 다시 돈다", () =>
    {
        int n = 0;
        var svc = new TimerService(() => { Interlocked.Increment(ref n); return true; }, intervalMs: 20);
        svc.ThreadStart();
        WaitUntil(() => n >= 2);
        svc.ThreadStop();
        int stopped = n;

        Expect(svc.ThreadStart(), "재시작이 true 여야 함");
        Expect(WaitUntil(() => n >= stopped + 3, 1500), $"재시작 후 일이 늘지 않음 (정지 시 {stopped}, 지금 {n}) — ThreadStart 에서 멈춤 스위치를 꺼야 함");
        svc.ThreadStop();
    });
});


// ===================================================== PART B
Section("B", "한 바퀴의 안전성", () =>
{
    Check("한 바퀴가 오래 걸려도 겹치지 않고, 끝난 뒤 간격을 다시 잰다", () =>
    {
        int running = 0, maxRunning = 0, n = 0;
        var svc = new TimerService(() =>
        {
            int now = Interlocked.Increment(ref running);
            lock (lockObj) maxRunning = Math.Max(maxRunning, now);
            Thread.Sleep(100);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref n);
            return true;
        }, intervalMs: 20);
        svc.ThreadStart();
        Thread.Sleep(700);
        svc.ThreadStop();
        Expect(maxRunning == 1, $"동시에 {maxRunning}개 실행됨 (한 바퀴 시작 때 타이머를 일시정지해야 함)");
        Expect(n >= 3 && n <= 7, $"700ms 동안 {n}번 (기대 3~7번: 100ms 일 + 20ms 간격)");
    });

    Check("ThreadStop 은 진행 중인 한 바퀴가 끝나기를 기다린다", () =>
    {
        bool inWork = false;
        bool started = false;
        var svc = new TimerService(() =>
        {
            inWork = true; started = true;
            Thread.Sleep(300);
            inWork = false;
            return true;
        }, intervalMs: 10, stopWaitMs: 3000);
        svc.ThreadStart();
        Expect(WaitUntil(() => started, 1000), "일이 시작되지 않음");
        svc.ThreadStop();
        Expect(!inWork, "ThreadStop 이 돌아왔는데 일이 아직 진행 중임 (끝나기를 기다려야 함)");
        Expect(!svc.IsRunning, "IsRunning 이 false 여야 함");
    });

    Check("일이 예외를 던져도 루프는 계속 돈다 (로그만 남김)", () =>
    {
        int n = 0;
        var logs = new ConcurrentQueue<string>();
        var svc = new TimerService(() =>
        {
            int c = Interlocked.Increment(ref n);
            if (c <= 2) throw new InvalidOperationException("boom");
            return true;
        }, logs.Enqueue, intervalMs: 20);
        svc.ThreadStart();
        Expect(WaitUntil(() => n >= 5, 1500), $"예외 이후 루프가 멈춤 (n={n}) — try/catch 로 삼키고 finally 에서 재예약해야 함");
        svc.ThreadStop();
        Expect(logs.Count >= 2, "예외가 로그로 남지 않음");
    });

    Check("일이 false 를 반환해도 루프는 계속 돈다", () =>
    {
        int n = 0;
        var svc = new TimerService(() => { Interlocked.Increment(ref n); return false; }, intervalMs: 20);
        svc.ThreadStart();
        Expect(WaitUntil(() => n >= 4, 1500), $"false 반환 후 루프가 멈춤 (n={n})");
        svc.ThreadStop();
    });
});


// ===================================================== PART C
Section("C", "정지 시간 초과", () =>
{
    Check("정지 대기 시간(stopWaitMs)보다 일이 오래 걸리면 \"ThreadStop timeout\" 로그를 남기고도 정지한다", () =>
    {
        var logs = new ConcurrentQueue<string>();
        int n = 0;
        bool started = false;
        var svc = new TimerService(() =>
        {
            started = true;
            Interlocked.Increment(ref n);
            Thread.Sleep(700);
            return true;
        }, logs.Enqueue, intervalMs: 10, stopWaitMs: 200);
        svc.ThreadStart();
        WaitUntil(() => started, 1000);
        bool stopped = svc.ThreadStop();

        Expect(stopped, "ThreadStop 이 true 여야 함");
        Expect(logs.Any(l => l.Contains("ThreadStop timeout")), $"\"ThreadStop timeout\" 로그가 없음 (로그: {string.Join(" | ", logs)})");

        int after = n;
        Thread.Sleep(900);
        Expect(n == after, $"정지 후에도 일이 {n - after}번 더 실행됨");
        Expect(!svc.IsRunning, "IsRunning 이 false 여야 함");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");

