using System.Text.Json;

namespace CommandQueueKata;

// 실무 참고: ContainerMoveService.checkPlcCommandPossible / InsertEquipmentCommand 호출부,
//           EquipmentService.SaveEquipmentCommandIfStatus,
//           FEnetPlcInterfaceService.WriteInstuctionToPLC   (구조만 참고, 베끼지 말 것)
//
// 울산 WCS 는 PLC 에 직접 쓰지 않습니다.
//   [판단 서비스]  "CV 이동 명령 필요" → 명령 큐 표에 WAIT 로 INSERT           ← Enqueue
//   [PLC 서비스]   WAIT 명령을 꺼내 PLC 에 태그별로 WRITE → COMPLETE 로 표시   ← ProcessWaiting
// 소켓 접근이 한 스레드(PLC 서비스)로 좁혀져서 충돌이 없고, 명령이 표에 남아서 추적할 수 있습니다.

public sealed class CommandService
{
    /// <summary>이 태그만 담긴 명령은 이력 표에 쌓지 않는다 (5초마다 오는 하트비트가 이력을 채우지 않게)</summary>
    public static readonly string[] HistSkipTags = { "PLC_W_HEART_BIT" };

    public const string CmdSeqTag = "PLC_W_CMD_SEQ";
    public const string CommandTag = "PLC_W_COMMAND";

    private readonly ICommandProvider _provider;
    private readonly IPlcWriter _plc;

    public CommandService(ICommandProvider provider, IPlcWriter plc)
    {
        _provider = provider;
        _plc = plc;
    }

    /// <summary>
    /// 지금 새 명령을 등록해도 되는지.
    ///   - 이 설비에 처리 대기(WAIT) 명령이 있으면 false
    ///   - PLC 가 아직 이전 명령을 처리 중이면 (plcWCommand 가 "0" 이 아니면) false
    ///   - 그 외 true
    /// 실무: checkPlcCommandPossible. plcWCommand 는 PLC_W_COMMAND 의 현재값이고 "0" 이 "대기(NONE)" 입니다.
    /// </summary>
    public bool CanIssue(string eqpId, string plcWCommand)
    {
        // TODO 1
        throw new NotImplementedException("TODO 1: CanIssue");
    }

    /// <summary>
    /// 명령을 큐에 WAIT 로 등록하고 명령번호(instNo)를 돌려준다.
    ///   1. instNo = _provider.NextCmdSeq()
    ///   2. values 에 PLC_W_COMMAND 가 있고 PLC_W_CMD_SEQ 가 없으면 PLC_W_CMD_SEQ = instNo 를 추가한다
    ///      (하트비트처럼 PLC_W_COMMAND 가 없는 명령에는 추가하지 않는다)
    ///   3. values 를 JSON 으로 만들어 _provider.Insert
    /// </summary>
    public string Enqueue(string eqpId, IDictionary<string, string> values)
    {
        // TODO 2: Dictionary → JSON 은 JsonSerializer.Serialize
        throw new NotImplementedException("TODO 2: Enqueue");
    }

    /// <summary>
    /// 이 설비의 WAIT 명령을 등록 순서대로 PLC 에 쓴다. 처리한(COMPLETE 또는 ERROR 로 끝낸) 명령 수를 돌려준다.
    ///
    /// 한 명령의 처리 규칙
    ///   1. JSON 을 태그→값 목록으로 되돌린다
    ///   2. 태그를 하나씩 _plc.WriteTag 로 쓴다. 단 PLC_W_CMD_SEQ 는 "맨 마지막"에 쓴다
    ///        (PLC 는 CMD_SEQ 가 바뀌는 것을 "명령 도착"의 신호로 보기 때문 — 다른 값이 다 써진 뒤여야 한다)
    ///   3. 모두 성공하면 Finish(COMPLETE)  — 값 태그가 전부 HistSkipTags 에 속하면 skipHistory = true
    ///   4. 하나라도 실패하면 거기서 멈추고(나머지는 쓰지 않음) Finish(ERROR, "쓰기 실패: 태그이름")
    /// </summary>
    public int ProcessWaiting(string eqpId)
    {
        // TODO 3: SelectWaiting → foreach 명령 처리
        throw new NotImplementedException("TODO 3: ProcessWaiting");
    }

    /// <summary>
    /// 명령의 상태를 바꾸고 마무리한다. (실무: SaveEquipmentCommandIfStatus)
    ///   1. UpdateStatus
    ///   2. status 가 COMPLETE / ERROR / CANCELED 이면 (끝난 명령)
    ///        - skipHistory 가 false 일 때만 이력에 1건 남긴다 (SelectOne 으로 현재 행을 읽어서 InsertHist)
    ///        - 큐에서 Delete (끝난 명령은 큐에 남기지 않는다)
    ///   3. UpdateStatus 의 반환값을 돌려준다
    /// 힌트: 이력에는 "상태가 바뀐 뒤의" 행이 들어가야 하므로 UpdateStatus 를 먼저 하고, 그다음 SelectOne 합니다.
    /// </summary>
    public int Finish(string eqpId, string instNo, string status, string message, bool skipHistory = false)
    {
        // TODO 4
        throw new NotImplementedException("TODO 4: Finish");
    }
}
