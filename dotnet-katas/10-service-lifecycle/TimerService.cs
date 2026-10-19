namespace ServiceLifecycleKata;

// 실무 참고: ECS.RFIDControll.Service/RFIDControllService.cs 의 ThreadStart / ThreadStop / SchedulerLogic
//           (ContainerMoveService, SocketVisionInterfaceService 도 같은 모양)  — 구조만 참고, 베끼지 말 것
//
// kata 01(자기 재예약 Timer)을 "울산 코드의 실제 모양"으로 키운 버전입니다.
// 서비스를 안전하게 "켜고(ThreadStart) 끄는(ThreadStop)" 문 + 타이머 한 바퀴(SchedulerLogic) 를 직접 만들어 봅니다.
//
//   한 바퀴 = ① 타이머 일시정지(겹침 방지) → ② 일 시키기 → ③ finally 에서 "재예약할지 / 정리할지" 결정
//   종료    = 멈춤 스위치를 켜고, 한 바퀴가 끝나 타이머가 정리될 때까지 (최대 N ms) 기다린다
//
// 이 서비스는 요청이 들어올 때 일하는 게 아니라, 타이머가 계속 돌며 스스로 일을 찾는 "폴링 방식"입니다.

public sealed class TimerService
{
    private readonly Func<bool> _work;
    private readonly Action<string> _log;
    private readonly int _intervalMs;
    private readonly int _stopWaitMs;

    private Timer? _timer;
    private volatile bool _stopSwitch;
    private readonly object _timerLock = new();

    /// <param name="work">한 바퀴마다 할 일 (false 를 반환해도 루프는 계속 돈다)</param>
    /// <param name="intervalMs">한 바퀴가 끝난 뒤 다음 바퀴까지 기다리는 시간 (실무: 20)</param>
    /// <param name="stopWaitMs">ThreadStop 이 타이머가 정리되기를 기다리는 최대 시간 (실무: 3000)</param>
    public TimerService(Func<bool> work, Action<string>? log = null, int intervalMs = 20, int stopWaitMs = 3000)
    {
        _work = work;
        _log = log ?? (_ => { });
        _intervalMs = intervalMs;
        _stopWaitMs = stopWaitMs;
    }

    /// <summary>타이머가 있으면(= 가동 중이면) true. 실무의 매니저가 "TimerThread == null 이면 ThreadStart" 로 판단하는 값</summary>
    public bool IsRunning
    {
        get { lock (_timerLock) return _timer != null; }
    }

    /// <summary>
    /// 서비스를 켠다.
    ///   1. 이미 가동 중이면 아무것도 하지 않고 true  (타이머를 두 개 만들면 안 된다)
    ///   2. 타이머를 만들어 SchedulerLogic 을 콜백으로 걸고, _intervalMs 후에 "한 번만" 실행되게 시작한다
    ///        (주기 반복 X — 다음 실행은 SchedulerLogic 이 스스로 예약한다)
    ///   3. 멈춤 스위치를 끈다 (_stopSwitch = false)
    ///   4. 성공하면 true, 예외가 나면 로그를 남기고 false
    /// </summary>
    public bool ThreadStart()
    {
        // TODO 1
        throw new NotImplementedException("TODO 1: ThreadStart");
    }

    /// <summary>
    /// 서비스를 끈다. 일하던 한 바퀴를 끊지 않고 끝나기를 기다린다.
    ///   1. 멈춤 스위치를 켠다
    ///   2. 타이머가 정리되기(_timer == null)를 100ms 간격으로 최대 _stopWaitMs 동안 기다린다
    ///        (정상이면 SchedulerLogic 의 finally 가 정리해서 바로 빠져나온다)
    ///   3. 그래도 타이머가 남아있으면 강제 정리:
    ///        - lock 안에서 _timer 를 지역 변수로 옮기고 _timer = null 로 만든다
    ///        - lock "밖에서" 그 지역 변수를 Dispose() 한다
    ///        (진행 중인 콜백은 계속 돌다가 끝나지만, finally 에서 _timer 가 null 이라 다음 예약을 하지 않는다)
    ///   4. 기다린 시간이 _stopWaitMs 를 넘었으면 로그에 "ThreadStop timeout" 을 남긴다
    ///
    ///   ※ 실무 원본(RFIDControllService.ThreadStop)은 3번을 lock 을 잡은 채로 Dispose(waitHandle) + WaitOne(3초) 로 합니다.
    ///     그런데 콜백의 finally 도 같은 lock 이 필요해서, 일이 오래 걸리면 서로 기다리다가 3초 후 풀려나고
    ///     이미 해제된 waitHandle 에 신호가 가서 예외가 날 수 있습니다. (이 kata 의 테스트 PART C 가 바로 그 상황이에요)
    ///     원본과 내 코드의 차이를 NOTES 에 적어 보세요.
    ///   5. 켜진 적 없어도, 두 번 불러도 예외 없이 true.  예외가 나면 로그 + false
    /// </summary>
    public bool ThreadStop()
    {
        // TODO 2
        throw new NotImplementedException("TODO 2: ThreadStop");
    }

    /// <summary>
    /// 타이머 한 바퀴 (콜백).
    ///   1. lock 안에서: _timer 가 null 이면 즉시 return.  아니면 타이머를 일시정지 (Change(Infinite, Infinite))
    ///        - 이미 해제된 타이머를 건드리면 ObjectDisposedException → 잡아서 return
    ///   2. try { _work() } catch (Exception) { 로그 }   ← 일이 예외를 던져도 루프는 살아야 한다
    ///   3. finally, lock 안에서:
    ///        - 멈춤 스위치가 꺼져 있고 _timer 가 있으면 → _intervalMs 후에 다시 한 번 실행되도록 재예약
    ///        - 멈춤 스위치가 켜져 있고 _timer 가 있으면 → _timer.Dispose(), _timer = null, 로그("종료")
    /// </summary>
    private void SchedulerLogic(object? state)
    {
        // TODO 3
        throw new NotImplementedException("TODO 3: SchedulerLogic");
    }
}
