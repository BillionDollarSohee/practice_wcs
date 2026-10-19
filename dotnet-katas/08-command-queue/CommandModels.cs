namespace CommandQueueKata;

// ★ 이 파일은 수정하지 않습니다.
//   실무: ECS_EQP_COMMAND(명령 큐 표) 한 행, CommandStateValues

public static class CommandState
{
    public const string WAIT = "WAIT";          // 등록됨, 아직 PLC 에 쓰지 않음
    public const string COMPLETE = "COMPLETE";  // PLC 에 쓰기 완료
    public const string ERROR = "ERROR";        // 쓰기 실패
    public const string CANCELED = "CANCELED";  // 취소
}

public sealed class EquipmentCommand
{
    public string EqpId { get; set; } = "";
    public string InstNo { get; set; } = "";        // 명령번호 (= PLC_W_CMD_SEQ 값과 같은 번호를 쓴다)
    public string ValuesJson { get; set; } = "";    // {"PLC_W_COMMAND":"13","PLC_W_CMD_SEQ":"7"} 같은 "태그→값" JSON
    public string Status { get; set; } = CommandState.WAIT;
    public string Message { get; set; } = "";

    public EquipmentCommand Clone() => (EquipmentCommand)MemberwiseClone();
}
