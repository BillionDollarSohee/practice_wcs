// ============================================================
// Kata 1 : Self-Rescheduling Timer Pattern
// ------------------------------------------------------------
// 실무 코드의 SocketVisionInterfaceService.SorketLogic / ThreadStart / ThreadStop
// (그리고 VisionImageMigrationService.SchedulerLogic)가 쓰는 패턴을 그대로 축소한 연습입니다.
//
// 왜 "Period"를 쓰는 일반 Timer가 아니라 "self-reschedule" 패턴을 쓸까?
//   - new Timer(callback, state, dueTime, period) 처럼 period를 주면,
//     콜백이 오래 걸려도 다음 콜백이 그대로 겹쳐서 또 호출된다 (동시 실행 위험).
//   - 그래서 실무 코드는 항상 period를 Timeout.Infinite로 주고,
//     콜백 "시작할 때" 스스로를 멈추고, "끝날 때" 다시 Change()로 다음 실행을 예약한다.
//   - 즉 "한 번 실행 → 끝나면 → 그 다음 대기시간 예약" 이 반복되는 구조.
//
// 목표: 아래 TODO를 채워서, 1초마다 "Tick N" 을 출력하다가
//       아무 키나 누르면 안전하게 멈추는 프로그램을 완성하세요.
//
// 완성 조건 (Acceptance Criteria):
//   1) 프로그램 시작 후 약 1초 간격으로 "Tick 1", "Tick 2", ... 가 출력된다.
//   2) 아무 키나 누르면 "Stopping..." 이 출력되고, 진행 중이던 콜백이 있으면
//      그게 끝난 뒤에 안전하게 종료된다 (콜백 도중에 강제로 끊기지 않는다).
//   3) Timer 필드에 접근하는 부분은 lock으로 감싸서 동시 접근을 막는다.
//   4) 종료 후 Timer 리소스를 Dispose 한다.
//
// 힌트: 실제 코드(SocketVisionInterfaceService.cs)의 SorketLogic / ThreadStart / ThreadStop
//       구조를 참고하되, 절대 그 파일을 복사해서 붙여넣지 마세요 — 직접 짜보는 게 이 kata의 목적입니다.
// ============================================================

using System;
using System.Threading;

class TickCounterService
{
    private Timer? _timer;
    private int _tickCount = 0;
    private volatile bool _stopRequested = false;
    private readonly object _timerLock = new object();

    private const int INTERVAL_MS = 1000;

    /// <summary>
    /// 타이머를 시작한다. 최초 1회는 짧은 지연 후 OnTick이 호출되도록 예약한다.
    /// </summary>
    public void Start()
    {
        // TODO 1: _timer = new Timer(OnTick) 로 타이머를 생성하세요.
        // TODO 2: _timer.Change(?, Timeout.Infinite) 로 최초 실행을 예약하세요.
        //         (Period 자리는 항상 Timeout.Infinite - self-reschedule 패턴이니까)
        throw new NotImplementedException("TODO 1,2: 타이머 생성 및 최초 예약");
    }

    /// <summary>
    /// 타이머 콜백. 매번 호출될 때마다:
    ///   1) 타이머를 잠시 멈추고 (Change(Infinite, Infinite))
    ///   2) 실제 작업(틱 카운트 증가 + 출력)을 하고
    ///   3) 정지 요청이 없으면 다음 실행을 다시 예약한다
    /// </summary>
    private void OnTick(object? state)
    {
        lock (_timerLock)
        {
            if (_timer == null) return;
            // TODO 3: _timer.Change(Timeout.Infinite, Timeout.Infinite) 로 재실행을 잠시 멈추세요.
        }

        try
        {
            // TODO 4: _tickCount 증가시키고 "Tick {N}" 콘솔 출력
            throw new NotImplementedException("TODO 4: 실제 작업(틱 증가 + 출력)");
        }
        finally
        {
            lock (_timerLock)
            {
                if (!_stopRequested && _timer != null)
                {
                    // TODO 5: _timer.Change(INTERVAL_MS, Timeout.Infinite) 로 다음 실행 예약
                }
                else if (_stopRequested && _timer != null)
                {
                    // TODO 6: 타이머 Dispose 하고 _timer = null 로 정리
                    Console.WriteLine("[TickCounterService] 타이머 종료됨.");
                }
            }
        }
    }

    /// <summary>
    /// 정지를 요청한다. 이미 실행 중인 콜백이 있다면 그게 끝난 뒤 OnTick의 finally에서 정리된다.
    /// 여기서는 "그냥 플래그만 켜는" 것 이상을 하지 않는 게 포인트 (강제 종료 X).
    /// </summary>
    public void Stop()
    {
        // TODO 7: _stopRequested = true 로 설정
        throw new NotImplementedException("TODO 7: 정지 플래그 설정");
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Timer Pattern Kata ===");
        Console.WriteLine("아무 키나 누르면 종료합니다.");
        Console.WriteLine();

        var service = new TickCounterService();
        service.Start();

        Console.ReadKey(true);

        Console.WriteLine();
        Console.WriteLine("Stopping...");
        service.Stop();

        // TODO 8 (선택, 검증용): Stop() 호출 직후 바로 프로그램이 끝나버리면
        //         "콜백이 끝난 뒤 안전 종료"를 눈으로 확인하기 어렵습니다.
        //         잠깐 대기(Thread.Sleep 등)를 넣어서 종료 로그가 찍히는 걸 확인해보세요.
    }
}
