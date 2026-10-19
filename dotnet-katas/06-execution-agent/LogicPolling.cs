using System.Diagnostics;

namespace ExecutionAgentKata;

// ★ 이 파일은 수정하지 않습니다 (실무 WCS.Common/01_BaseUtilities/models/LogicPolling.cs 의 축소판).
//
// "스케줄러 함수 하나"의 실행 상태를 들고 있는 객체입니다.
// ExecutionAgent 는 (프로그램ID, 로직ID[, 설비그룹]) 조합마다 이 객체를 하나씩 만들어서 재사용합니다.

public sealed class LogicPolling
{
    public string ProgramId { get; init; } = "";
    public string LogicId { get; init; } = "";
    public string EquipGroup { get; init; } = "";

    /// <summary>지금 이 로직이 Task 안에서 돌고 있으면 true (겹쳐 실행되는 것을 막는 표시)</summary>
    public volatile bool IsLogicBusy;

    /// <summary>마지막 실행이 "끝난 시점"부터 흐른 시간을 잰다 (실행이 끝나면 Restart)</summary>
    public Stopwatch LogicStopWatch { get; } = new Stopwatch();

    /// <summary>다시 실행하기까지 기다릴 시간(ms) - DB 설정값</summary>
    public int PollingMs { get; set; } = 500;

    /// <summary>사용 여부 - DB 설정값 (false 면 절대 실행하지 않음)</summary>
    public bool IsUse { get; set; } = true;

    /// <summary>실로직. 설비그룹 없는 형태 / 있는 형태 중 하나만 채워진다</summary>
    public Func<bool>? Logic { get; init; }
}
