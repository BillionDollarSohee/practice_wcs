namespace EquipmentStatusKata;

// ★ 이 파일은 수정하지 않습니다. 실무: ECS_EQP_DETAIL_STATUS 한 행(설비의 "태그 하나"의 현재값)

public sealed class EquipmentDetailStatus
{
    public string EquipmentId { get; set; } = "";        // EQP_ID          예) CV.TR.01
    public string EquipmentDetailId { get; set; } = "";  // EQP_DETAIL_ID   예) PLC_R_CART_PRESENT  ← "태그 이름"
    public string InstructionNo { get; set; } = "";      // INST_NO         어떤 명령에 대한 값인지
    public string ResultValue { get; set; } = "";        // RESULT_VALUE    "0" / "1" / 값 문자열
    public string ResultValueJson { get; set; } = "";    // RESULT_VALUE_JSON
    public string Remark { get; set; } = "";
    public DateTime SaveDttm { get; set; }               // 마지막으로 "저장된" 시각
    public string SaveBy { get; set; } = "";

    public EquipmentDetailStatus Clone() => (EquipmentDetailStatus)MemberwiseClone();
}

/// <summary>태그 이름의 접두어가 알려주는 값의 성격</summary>
public enum TagKind
{
    Unknown,
    Read,    // PLC_R_xxx   PLC 가 올려준 값 (WCS 는 읽기만)
    Write,   // PLC_W_xxx   WCS 가 PLC 에 쓰는 값
    Common   // PLC_C_xxx / RFID_C_xxx   PLC 주소가 없는, WCS 내부에서 공유하는 값
}
