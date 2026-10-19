namespace AlarmServiceKata;

// ★ 이 파일은 수정하지 않습니다. 알람 표(Provider)를 메모리로 대신한 것입니다.
//   실무: AlarmManagementMySqlProvider.SelectWcsServiceAlarm / InsertWcsServiceAlarm /
//         InsertWcsServiceAlarmHist / DeleteWcsServiceAlarm / UpdateWcsServiceAlarmHist
//
//   "현재 알람 표"에는 지금 울리고 있는 알람만, "이력 표"에는 시작~종료 기록이 모두 남습니다.

public sealed class ServiceAlarm
{
    public string ProgramId { get; set; } = "";
    public string EqpId { get; set; } = "";
    public string AlarmCd { get; set; } = "";
    public string AlarmMsg { get; set; } = "";
    public string PopupYn { get; set; } = "";
    public DateTime StartDttm { get; set; }
    public DateTime? EndDttm { get; set; }
}

public interface IAlarmProvider
{
    /// <summary>(프로그램ID, 설비ID, 알람코드) 로 지금 울리고 있는 알람을 찾는다. 없으면 빈 리스트</summary>
    List<ServiceAlarm> SelectActive(object tx, string programId, string eqpId, string alarmCd);

    /// <summary>현재 알람 표에 추가. 1 반환</summary>
    int Insert(object tx, string programId, string eqpId, string alarmCd, string alarmMsg, string popupYn);

    /// <summary>이력 표에 시작 기록 추가. 1 반환</summary>
    int InsertHist(object tx, string programId, string eqpId, string alarmCd, string alarmMsg, string popupYn);

    /// <summary>현재 알람 표에서 삭제. 지운 건수 반환</summary>
    int Delete(object tx, string programId, string eqpId, string alarmCd);

    /// <summary>이력 표에서 (시작시각이 같은) 해당 알람의 종료시각을 채운다. 갱신한 건수 반환</summary>
    int UpdateHistEnd(object tx, string programId, string eqpId, string alarmCd, DateTime alarmStartDttm);
}

public sealed class InMemoryAlarmProvider : IAlarmProvider
{
    private readonly IClock _clock;
    public List<ServiceAlarm> Active { get; } = new();
    public List<ServiceAlarm> Hist { get; } = new();

    /// <summary>"Insert", "InsertHist", "Delete", "UpdateHistEnd" 중 하나를 적으면 그 메서드가 예외를 던진다 (롤백 테스트용)</summary>
    public string FailOn { get; set; } = "";

    public InMemoryAlarmProvider(IClock clock) => _clock = clock;

    private void MaybeFail(string method)
    {
        if (FailOn == method) throw new InvalidOperationException($"{method} 실패(테스트용)");
    }

    public List<ServiceAlarm> SelectActive(object tx, string programId, string eqpId, string alarmCd) =>
        Active.Where(a => a.ProgramId == programId && a.EqpId == eqpId && a.AlarmCd == alarmCd).ToList();

    public int Insert(object tx, string programId, string eqpId, string alarmCd, string alarmMsg, string popupYn)
    {
        MaybeFail("Insert");
        Active.Add(new ServiceAlarm { ProgramId = programId, EqpId = eqpId, AlarmCd = alarmCd, AlarmMsg = alarmMsg, PopupYn = popupYn, StartDttm = _clock.Now });
        return 1;
    }

    public int InsertHist(object tx, string programId, string eqpId, string alarmCd, string alarmMsg, string popupYn)
    {
        MaybeFail("InsertHist");
        Hist.Add(new ServiceAlarm { ProgramId = programId, EqpId = eqpId, AlarmCd = alarmCd, AlarmMsg = alarmMsg, PopupYn = popupYn, StartDttm = _clock.Now });
        return 1;
    }

    public int Delete(object tx, string programId, string eqpId, string alarmCd)
    {
        MaybeFail("Delete");
        return Active.RemoveAll(a => a.ProgramId == programId && a.EqpId == eqpId && a.AlarmCd == alarmCd);
    }

    public int UpdateHistEnd(object tx, string programId, string eqpId, string alarmCd, DateTime alarmStartDttm)
    {
        MaybeFail("UpdateHistEnd");
        int n = 0;
        foreach (var h in Hist.Where(h => h.ProgramId == programId && h.EqpId == eqpId && h.AlarmCd == alarmCd
                                          && h.StartDttm == alarmStartDttm && h.EndDttm == null))
        {
            h.EndDttm = _clock.Now;
            n++;
        }
        return n;
    }
}
