namespace EquipmentStatusKata;

// ★ 이 파일은 수정하지 않습니다. DB 접근 계층(Provider)을 메모리로 대신한 것입니다.
//   실무: EquipmentMySqlProvider.SelectEquipmentDetailStatus / InsertEquipmentDetailStatus /
//         UpdateEquipmentDetailStatus / InsertEquipmentDetailStatusHist

public interface IEquipmentProvider
{
    /// <summary>eqpDetailId 가 빈 문자열이면 해당 설비의 모든 태그</summary>
    List<EquipmentDetailStatus> SelectStatus(string eqpId, string eqpDetailId);

    int InsertStatus(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy);

    /// <summary>
    /// UPDATE ... SET RESULT_VALUE=.., SAVE_DTTM=NOW(6) WHERE EQP_ID=.. AND EQP_DETAIL_ID=..
    ///   - isNewInstNo == false 이고 instNo 가 비어있지 않으면  AND INST_NO = @instNo  조건이 붙는다 (명령번호가 다르면 0건 갱신)
    ///   - isNewInstNo == true 이면 INST_NO 도 새 값으로 바꾼다
    ///   - 행이 없으면 0 을 돌려준다.  SAVE_DTTM 은 "값이 같아도" 항상 갱신된다
    /// </summary>
    int UpdateStatus(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy, bool isNewInstNo);

    /// <summary>이력 테이블에 항상 1건 추가</summary>
    int InsertStatusHist(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy);
}

public sealed class InMemoryEquipmentProvider : IEquipmentProvider
{
    private readonly IClock _clock;
    private readonly List<EquipmentDetailStatus> _rows = new();

    /// <summary>이력 테이블 (테스트가 건수를 확인한다)</summary>
    public List<EquipmentDetailStatus> Hist { get; } = new();

    public InMemoryEquipmentProvider(IClock clock) => _clock = clock;

    public List<EquipmentDetailStatus> SelectStatus(string eqpId, string eqpDetailId) =>
        _rows.Where(r => r.EquipmentId == eqpId && (string.IsNullOrEmpty(eqpDetailId) || r.EquipmentDetailId == eqpDetailId))
             .Select(r => r.Clone()).ToList();

    public int InsertStatus(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy)
    {
        _rows.Add(new EquipmentDetailStatus
        {
            EquipmentId = eqpId, EquipmentDetailId = eqpDetailId, InstructionNo = instNo, ResultValue = value,
            ResultValueJson = json, Remark = remark, SaveBy = saveBy, SaveDttm = _clock.Now
        });
        return 1;
    }

    public int UpdateStatus(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy, bool isNewInstNo)
    {
        var row = _rows.FirstOrDefault(r => r.EquipmentId == eqpId && r.EquipmentDetailId == eqpDetailId);
        if (row == null) return 0;
        if (!isNewInstNo && !string.IsNullOrEmpty(instNo) && row.InstructionNo != instNo) return 0;

        if (isNewInstNo) row.InstructionNo = instNo;
        row.ResultValue = value;
        row.ResultValueJson = json;
        row.Remark = remark;
        row.SaveBy = saveBy;
        row.SaveDttm = _clock.Now;
        return 1;
    }

    public int InsertStatusHist(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy)
    {
        Hist.Add(new EquipmentDetailStatus
        {
            EquipmentId = eqpId, EquipmentDetailId = eqpDetailId, InstructionNo = instNo, ResultValue = value,
            ResultValueJson = json, Remark = remark, SaveBy = saveBy, SaveDttm = _clock.Now
        });
        return 1;
    }
}
