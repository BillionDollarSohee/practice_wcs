namespace ExecutionAgentKata;

// 실무 참고: WCS.Common/01_BaseUtilities/ExecutionAgent.cs  (구조만 참고, 베끼지 말 것)
//
// 서비스의 타이머 루프(SchedulerLogic)는 20ms 마다 가볍게 돕니다.
// 그 루프가 스케줄러 함수 6개를 "실행해도 되는지 물어보는" 상대가 이 ExecutionAgent 입니다.
//
//   타이머 루프 ──(20ms 마다)──> ExecuteLogicByPollingTime("RFID","SchedulerA", SchedulerA, ...)
//                                   └ 조건이 맞으면 Task.Run 으로 따로 돌린다 (루프는 기다리지 않는다)
//
// 같은 아이디어("끝난 뒤에 주기를 기다린다")를 로직마다 한 번 더 적용하는 층입니다.

public sealed class ExecutionAgent
{
    public const int DefaultPollingMs = 500;

    private readonly ISchedulerSettingProvider _settings;
    private readonly Action<string> _log;
    private readonly List<LogicPolling> _pollingList = new();

    public ExecutionAgent(ISchedulerSettingProvider settings, Action<string>? log = null)
    {
        _settings = settings;
        _log = log ?? (_ => { });
    }

    /// <summary>
    /// (프로그램ID, 로직ID) 의 폴링 주기와 사용여부를 돌려준다.
    ///   - 설정이 있으면 그 값
    ///   - 설정이 없으면 기본값(500ms, 사용 Y) 으로 "설정 저장소에 등록"하고 그 값을 돌려준다
    /// 실무: ExecutionAgent.GetLogicPollingTime
    /// </summary>
    public (int PollingMs, bool IsUse) GetLogicPollingTime(string programId, string logicId, string saveBy)
    {
        // TODO 1: _settings.Select 로 조회 → 있으면 반환, 없으면 SaveDefault 후 (DefaultPollingMs, true) 반환
        throw new NotImplementedException("TODO 1: GetLogicPollingTime");
    }

    /// <summary>
    /// 설비그룹이 없는 로직(Func&lt;bool&gt;)을 "주기가 됐으면" 실행한다.
    ///
    /// 규칙
    ///   1. 같은 (programId, logicId) 는 하나의 LogicPolling 객체를 재사용한다. 처음이면 만든다.
    ///        - 만들 때 GetLogicPollingTime 으로 PollingMs/IsUse 를 채우고, 스톱워치를 시작한다
    ///   2. 아래 세 조건이 "모두" 참일 때만 실행한다
    ///        - 지금 돌고 있지 않다 (IsLogicBusy == false)
    ///        - 스톱워치 경과시간 > PollingMs        ※ 처음 호출은 0ms 이므로 실행되지 않는다
    ///        - IsUse == true
    ///   3. 실행은 Task.Run 으로 한다. 호출한 쪽은 기다리지 않고 바로 돌아간다
    ///        - 실행 직전에 IsLogicBusy = true
    ///        - 로직이 false 를 반환하면 로그("... 실행 중 실패") 를 남긴다
    ///        - 예외가 나면 로그("... 실행 중 예외") 를 남기고 삼킨다 (프로그램이 죽으면 안 됨)
    ///        - 끝나면(성공/실패/예외 모두) finally 에서 IsLogicBusy = false, 스톱워치 Restart
    /// </summary>
    public void ExecuteLogicByPollingTime(string programId, string logicId, Func<bool> logic, string saveBy)
    {
        // TODO 2: _pollingList 에서 객체 찾기/만들기 (lock 필요 — 여러 스레드가 동시에 부른다)
        // TODO 3: 규칙 2 의 조건 검사
        // TODO 4: Task.Run + try/catch/finally (규칙 3)
        // 힌트: 설비그룹 있는 오버로드와 코드를 중복하지 않도록 private 메서드로 공통 부분을 빼 보세요.
        throw new NotImplementedException("TODO 2~4: ExecuteLogicByPollingTime");
    }

    /// <summary>
    /// 설비그룹(서버ID 등)이 있는 로직. 같은 logicId 라도 eqpGroup 이 다르면 "별개의" 실행 상태를 갖는다.
    /// 로직에는 eqpGroup 문자열이 그대로 전달된다.
    /// 실무: 서버 3대가 같은 스케줄러 함수를 각자 주기로 돌려야 해서 이 형태를 씁니다.
    /// </summary>
    public void ExecuteLogicByPollingTime(string programId, string logicId, Func<string, bool> logic, string eqpGroup, string saveBy)
    {
        // TODO 5: 위와 같은 흐름. 단 객체를 찾는 키에 eqpGroup 이 추가되고, 실행은 logic(eqpGroup)
        throw new NotImplementedException("TODO 5: 설비그룹 오버로드");
    }
}
