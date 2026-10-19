namespace ExecutionAgentKata;

// ★ 이 파일은 수정하지 않습니다. DB 설정 표(WCS_CFG_SCHEDULER)를 대신하는 메모리 저장소입니다.
//   실무: MasterService.WCSSchedulerSettings / SaveWCSSchedulerSetting

public sealed record SchedulerSetting(int PollingMs, bool IsUse);

public interface ISchedulerSettingProvider
{
    /// <summary>(프로그램ID, 로직ID) 설정 조회. 없으면 null</summary>
    SchedulerSetting? Select(string programId, string logicId);

    /// <summary>설정이 없을 때 기본값으로 등록한다 (사용 Y)</summary>
    void SaveDefault(string programId, string logicId, int pollingMs, string saveBy);
}

public sealed class InMemorySchedulerSettingProvider : ISchedulerSettingProvider
{
    private readonly Dictionary<(string, string), SchedulerSetting> _rows = new();
    private readonly object _lock = new();

    /// <summary>SaveDefault 가 몇 번 불렸는지 (테스트가 확인한다)</summary>
    public int SaveDefaultCalls { get; private set; }

    public void Set(string programId, string logicId, int pollingMs, bool isUse)
    {
        lock (_lock) _rows[(programId, logicId)] = new SchedulerSetting(pollingMs, isUse);
    }

    public SchedulerSetting? Select(string programId, string logicId)
    {
        lock (_lock) return _rows.TryGetValue((programId, logicId), out var s) ? s : null;
    }

    public void SaveDefault(string programId, string logicId, int pollingMs, string saveBy)
    {
        lock (_lock)
        {
            SaveDefaultCalls++;
            _rows[(programId, logicId)] = new SchedulerSetting(pollingMs, true);
        }
    }
}
