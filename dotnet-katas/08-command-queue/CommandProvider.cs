namespace CommandQueueKata;

// ★ 이 파일은 수정하지 않습니다. 명령 큐 표(Provider)와 PLC 를 메모리로 대신한 것입니다.
//   실무: EquipmentMySqlProvider.InsertEquipmentCommand / SelectEquipmentCommand /
//         UpdateEquipmentCommandIfStatus / InsertEquipmentCommandHist / DeleteEquipmentCommand,
//         WCSOrderProvider.SelectNextPlcCmdSeq

public interface ICommandProvider
{
    /// <summary>PLC 명령 순번 채번. "1", "2", "3" ... (실무: seq_plc_cmd 시퀀스)</summary>
    string NextCmdSeq();

    /// <summary>상태 WAIT 로 등록</summary>
    int Insert(string eqpId, string instNo, string valuesJson);

    /// <summary>해당 설비의 WAIT 명령들, 등록 순서대로</summary>
    List<EquipmentCommand> SelectWaiting(string eqpId);

    int UpdateStatus(string eqpId, string instNo, string status, string message);

    /// <summary>명령 한 건을 상태와 무관하게 조회. 없으면 null</summary>
    EquipmentCommand? SelectOne(string eqpId, string instNo);

    /// <summary>이력 테이블에 1건 추가 (현재 상태를 그대로 복사)</summary>
    int InsertHist(EquipmentCommand command);

    int Delete(string eqpId, string instNo);

    /// <summary>해당 설비에 아직 처리되지 않은(WAIT) 명령 수</summary>
    int CountPending(string eqpId);
}

public sealed class InMemoryCommandProvider : ICommandProvider
{
    private readonly List<EquipmentCommand> _rows = new();
    private int _seq;

    public List<EquipmentCommand> Hist { get; } = new();
    public int RowCount => _rows.Count;
    public EquipmentCommand? Find(string eqpId, string instNo) => _rows.FirstOrDefault(r => r.EqpId == eqpId && r.InstNo == instNo);

    public string NextCmdSeq() => (++_seq).ToString();

    public int Insert(string eqpId, string instNo, string valuesJson)
    {
        _rows.Add(new EquipmentCommand { EqpId = eqpId, InstNo = instNo, ValuesJson = valuesJson, Status = CommandState.WAIT });
        return 1;
    }

    public List<EquipmentCommand> SelectWaiting(string eqpId) =>
        _rows.Where(r => r.EqpId == eqpId && r.Status == CommandState.WAIT).Select(r => r.Clone()).ToList();

    public int UpdateStatus(string eqpId, string instNo, string status, string message)
    {
        var row = Find(eqpId, instNo);
        if (row == null) return 0;
        row.Status = status;
        row.Message = message;
        return 1;
    }

    public EquipmentCommand? SelectOne(string eqpId, string instNo) => Find(eqpId, instNo)?.Clone();

    public int InsertHist(EquipmentCommand command)
    {
        Hist.Add(command.Clone());
        return 1;
    }

    public int Delete(string eqpId, string instNo) => _rows.RemoveAll(r => r.EqpId == eqpId && r.InstNo == instNo);

    public int CountPending(string eqpId) => _rows.Count(r => r.EqpId == eqpId && r.Status == CommandState.WAIT);
}

/// <summary>PLC 에 "태그 하나"를 쓰는 통로. 실무: FEnet 서비스가 PLC 소켓으로 실제로 씁니다.</summary>
public interface IPlcWriter
{
    /// <summary>성공하면 true</summary>
    bool WriteTag(string eqpId, string tagName, string value);
}

public sealed class FakePlcWriter : IPlcWriter
{
    public List<(string EqpId, string Tag, string Value)> Writes { get; } = new();

    /// <summary>이 태그를 쓰려고 하면 실패(false)한다. 비어 있으면 실패 없음</summary>
    public string FailOnTag { get; set; } = "";

    public bool WriteTag(string eqpId, string tagName, string value)
    {
        if (tagName == FailOnTag) return false;
        Writes.Add((eqpId, tagName, value));
        return true;
    }
}
