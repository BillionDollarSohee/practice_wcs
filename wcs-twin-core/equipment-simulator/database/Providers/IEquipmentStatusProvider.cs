using Database.Models;

namespace Database.Providers;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다)
//
// 지금은 Handler 와 RFIDControllService 가 "_context.EquipmentStatusHists.Add(new EquipmentStatusHist { ... }); SaveChanges();"
// 를 열 곳이 넘게 반복합니다. 이 반복을 Provider 한 곳으로 모으는 것이 변환의 첫걸음이에요.
//
// 울산의 EquipmentService.SaveEquipmentDetailStatus... 가 하는 일과 같은 자리입니다 (kata 07 참고).

public interface IEquipmentStatusProvider
{
    /// <summary>상태 이력 한 건 추가 (CreateDttm 은 호출한 쪽이 채워서 넘긴다). 반영된 행 수 반환</summary>
    Task<int> InsertHistAsync(EquipmentStatusHist hist);

    /// <summary>
    /// "since 이후에 (cartId, statusType, status) 이력이 있는가".
    /// RFIDControllService.RaiseAlarmAndWaitForClearAsync 가 알람 해제(CLEARED) 여부를 확인하는 쿼리 자리입니다.
    /// </summary>
    Task<bool> ExistsHistSinceAsync(string cartId, string statusType, string status, DateTime since);
}

public sealed class EquipmentStatusProvider : IEquipmentStatusProvider
{
    private readonly WcsTwinContext _context;

    public EquipmentStatusProvider(WcsTwinContext context)
    {
        _context = context;
    }

    public Task<int> InsertHistAsync(EquipmentStatusHist hist)
    {
        // TODO 1: Add + SaveChangesAsync, 반환값은 SaveChangesAsync 의 결과
        throw new NotImplementedException("TODO 1: InsertHistAsync");
    }

    public Task<bool> ExistsHistSinceAsync(string cartId, string statusType, string status, DateTime since)
    {
        // TODO 2: _context.EquipmentStatusHists.AnyAsync(...)  — 조건은 RFIDControllService 의 기존 쿼리를 그대로 옮기세요
        throw new NotImplementedException("TODO 2: ExistsHistSinceAsync");
    }
}
