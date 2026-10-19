using Database.Providers;
using RfidControllService;

namespace EisSocketService.Services;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다 — wcs-twin-core/MSP_변환_가이드.md 의 Step 3 에서 사용)
//
// Command 03(대차정보 요청)의 "업무 흐름"을 담는 Service 입니다.
// 지금은 VisionCartInfoRequestHandler 안에 프로토콜 처리(프레임 파싱/응답 만들기)와 업무 흐름이 섞여 있어요.
// 변환 후의 역할 분담
//   Handler : 프레임을 파싱해서 cartId 를 꺼내고, Service 를 부르고, 결과(CartInfoResult)로 응답 프레임을 만든다
//   Service : 요청 이력 기록 → 대차 확인 → 라인타입 확인 → 대기줄 진입 기록 → RFID 도착확인 → 검사진행 기록  (DB 는 Provider 로)
//
// 울산 대응: VisionDataRequestHandler(+ SocketVisionInterfaceService.ProcCommandData) 의 업무 부분

/// <summary>응답 프레임에 담길 결과. ErrorCode 는 기존 Handler 가 쓰던 값 그대로("00" 정상 / "01" 미등록 대차 / "03" 알 수 없는 라인타입)</summary>
public sealed record CartInfoResult(bool IsOk, string ErrorCode, string LineType);

public sealed class VisionCartInfoService
{
    private static readonly string[] LineTypes = { "TR", "FL", "DR" };

    private readonly ITwinTransaction _transaction;
    private readonly ICartProvider _cartProvider;
    private readonly IEquipmentStatusProvider _statusProvider;
    private readonly RFIDControllService _rfidControllService;

    public VisionCartInfoService(
        ITwinTransaction transaction,
        ICartProvider cartProvider,
        IEquipmentStatusProvider statusProvider,
        RFIDControllService rfidControllService)
    {
        _transaction = transaction;
        _cartProvider = cartProvider;
        _statusProvider = statusProvider;
        _rfidControllService = rfidControllService;
    }

    /// <summary>
    /// 대차정보 요청을 처리한다. 순서는 기존 VisionCartInfoRequestHandler.Handle 과 같습니다.
    ///   1. "VISION_CART_INFO_REQ / REQUESTED" 이력 기록 (원문 요청 프레임을 ResultJson 에)
    ///   2. 대차 마스터 조회 → 없으면 CartInfoResult(false, "01", "")
    ///   3. LineTypes 에 없는 라인타입이면 CartInfoResult(false, "03", "")
    ///   4. "INSPECTION_QUEUE / WAITING" 이력 기록
    ///   5. _rfidControllService.RequestReadTagAsync(cartId, lineType)  — 실패하면 내부에서 재시도/알람 대기까지 하고 성공한 뒤에 리턴
    ///   6. "VISION_INSPECTING / IN_PROGRESS" 이력 기록
    ///   7. CartInfoResult(true, "00", lineType)
    ///
    /// 생각해 볼 점
    ///   - 1~4 와 6 의 이력 기록을 트랜잭션 하나로 묶어야 할까요? 5번(RFID, 오래 걸림)은 트랜잭션 "밖"이어야 하는 이유는?
    ///   - 이력의 EqpId("VISION01.EQP.01", $"VISION.{lineType}")는 상수/헬퍼로 뺄 수 있을까요?
    /// </summary>
    public Task<CartInfoResult> ProcessAsync(string cartId, string rawRequest)
    {
        // TODO 1~7: 위 순서대로. 트랜잭션 범위는 직접 정해 보세요 (가이드 Step 3 의 질문 참고)
        throw new NotImplementedException("TODO: VisionCartInfoService.ProcessAsync");
    }
}
