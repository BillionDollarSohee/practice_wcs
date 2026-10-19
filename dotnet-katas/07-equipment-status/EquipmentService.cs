namespace EquipmentStatusKata;

// 실무 참고: WCS.Common/SystemInterfaces/Equipment/EquipmentService.cs  (구조만 참고, 베끼지 말 것)
//
// 울산 코드의 거의 모든 로직이 이 서비스를 통해 "태그 이름 → 값" 으로 설비 상태를 읽고 씁니다.
//   string isExistsCart = eqpDetailStatus.FirstOrDefault(s => s.EquipmentDetailId == "PLC_R_CART_PRESENT")?.ResultValue ?? "0";
// 이 한 줄 패턴이 GetValue 입니다.

public sealed class EquipmentService
{
    private readonly IEquipmentProvider _provider;
    private readonly IClock _clock;

    public EquipmentService(IEquipmentProvider provider, IClock clock)
    {
        _provider = provider;
        _clock = clock;
    }

    /// <summary>
    /// 태그 값을 저장한다. (실무: SaveEquipmentDetailStatus)
    ///   1. UpdateStatus 로 갱신한다.  ※ 행이 없으면 갱신은 0건이고, 새 행을 만들지 않는다
    ///   2. 갱신 성공 여부와 상관없이 InsertStatusHist 로 이력을 1건 남긴다
    ///   3. true 반환
    /// </summary>
    public bool SaveStatus(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy, bool isNewInstNo = false)
    {
        // TODO 1
        throw new NotImplementedException("TODO 1: SaveStatus");
    }

    /// <summary>
    /// 태그 값을 저장한다. 행이 없으면 먼저 만든다. (실무: SaveEquipmentDetailStatusCheckExists)
    ///   1. SelectStatus 로 행이 있는지 본다 → 없으면 InsertStatus
    ///   2. UpdateStatus + InsertStatusHist (SaveStatus 와 같다)
    ///   ※ 이름의 "CheckExists" 는 "없으면 INSERT" 라는 뜻이지, "값이 바뀔 때만 저장" 이 아닙니다.
    ///     같은 값을 또 저장해도 SaveDttm 이 갱신되고 이력이 1건 더 쌓입니다.
    /// </summary>
    public bool SaveStatusCheckExists(string eqpId, string eqpDetailId, string instNo, string value, string json, string remark, string saveBy, bool isNewInstNo = false)
    {
        // TODO 2
        throw new NotImplementedException("TODO 2: SaveStatusCheckExists");
    }

    /// <summary>설비의 태그 목록. eqpDetailId 가 빈 문자열이면 전체. 없으면 빈 리스트</summary>
    public List<EquipmentDetailStatus> GetStatus(string eqpId, string eqpDetailId)
    {
        // TODO 3
        throw new NotImplementedException("TODO 3: GetStatus");
    }

    /// <summary>
    /// 태그 하나의 값. 행이 없으면 defaultValue.
    /// 예) GetValue("CV.TR.01", "PLC_R_CART_PRESENT", "0")
    /// </summary>
    public string GetValue(string eqpId, string eqpDetailId, string defaultValue)
    {
        // TODO 4: GetStatus 결과의 첫 행 ?.ResultValue ?? defaultValue  (울산 코드의 FirstOrDefault(...)?.ResultValue ?? "0" 패턴)
        throw new NotImplementedException("TODO 4: GetValue");
    }

    /// <summary>
    /// 태그가 "마지막으로 저장된 지" 몇 초 지났는지. 행이 없으면 null.
    /// 울산 입고 정합성의 "5초 이내", "3초 이내" 같은 조건이 이 값을 씁니다.
    /// </summary>
    public double? ElapsedSecondsSinceSave(string eqpId, string eqpDetailId)
    {
        // TODO 5: SaveDttm 과 _clock.Now 의 차이(TotalSeconds)
        throw new NotImplementedException("TODO 5: ElapsedSecondsSinceSave");
    }

    /// <summary>
    /// 태그 이름의 접두어로 값의 성격을 구분한다. "장치_종류_이름" 형태에서 두 번째 토막이 종류.
    ///   PLC_R_CART_PRESENT → Read,  PLC_W_COMMAND → Write,
    ///   PLC_C_RFID_DATA / RFID_C_READ_VAL → Common,  그 외(VISION_RFID_READ_REQ 등) → Unknown
    /// </summary>
    public static TagKind GetTagKind(string eqpDetailId)
    {
        // TODO 6: '_' 로 쪼개서 [1] 이 "R"/"W"/"C" 인지 (토막이 3개 미만이면 Unknown)
        throw new NotImplementedException("TODO 6: GetTagKind");
    }
}
