using Database.Models;
using Database.Providers;
using RfidControllService;

namespace EisSocketService.Services;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다 — 가이드 Step 3 에서 사용)
//
// Command 11(비전 검사 완료)의 업무 흐름입니다. 기존 VisionProductCompleteHandler 의 업무 부분.
//
// 울산 대응: VisionProductCompleteHandler.Handle / SaveVisionResultInfo
//   (울산은 "결과 저장 + RFID Write 요청 + 대차상태 변경" 을 beforeCommit 콜백으로 한 트랜잭션에 묶었습니다)

public sealed class VisionInspectionService
{
    private readonly ITwinTransaction _transaction;
    private readonly IVisionResultProvider _visionResultProvider;
    private readonly IEquipmentStatusProvider _statusProvider;
    private readonly RFIDControllService _rfidControllService;

    public VisionInspectionService(
        ITwinTransaction transaction,
        IVisionResultProvider visionResultProvider,
        IEquipmentStatusProvider statusProvider,
        RFIDControllService rfidControllService)
    {
        _transaction = transaction;
        _visionResultProvider = visionResultProvider;
        _statusProvider = statusProvider;
        _rfidControllService = rfidControllService;
    }

    /// <summary>
    /// 검사 완료를 처리하고, 설비에 ACK 를 보내도 되는지(true) 를 돌려준다.
    ///   1. result = undercarJudgment == "1" ? "OK" : "NG"
    ///   2. VisionResult(OverallResult=result) + "VISION_PRODUCT_COMPLETE / COMPLETED" 이력을 "한 트랜잭션"으로 저장
    ///        - 둘 중 하나만 저장되면 안 됩니다 (원래는 SaveChanges 한 번이었어요)
    ///   3. result 가 OK 면: _rfidControllService.RequestWriteTagAsync(cartId, lineType) → 결과로 "DISCHARGED" 이력(COMPLETED / FAILED)
    ///      NG 면 RFID/출고 생략
    ///   4. true 반환 (RFID 가 실패해도 ACK 는 보냅니다 — 기존 동작 그대로)
    ///
    /// 생각해 볼 점: 2번과 3번 사이에서 프로세스가 죽으면 어떤 상태가 남나요? 울산은 이 문제를 어떻게 풀었나요?
    /// </summary>
    public Task<bool> ProcessAsync(string cartId, string lineType, string undercarJudgment, string rawRequest)
    {
        // TODO: 위 순서대로
        throw new NotImplementedException("TODO: VisionInspectionService.ProcessAsync");
    }
}
