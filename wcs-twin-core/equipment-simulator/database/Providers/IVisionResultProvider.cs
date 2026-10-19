using Database.Models;

namespace Database.Providers;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다)

public interface IVisionResultProvider
{
    /// <summary>비전 검사 결과 한 건 추가. 반영된 행 수 반환</summary>
    Task<int> InsertAsync(VisionResult result);
}

public sealed class VisionResultProvider : IVisionResultProvider
{
    private readonly WcsTwinContext _context;

    public VisionResultProvider(WcsTwinContext context)
    {
        _context = context;
    }

    public Task<int> InsertAsync(VisionResult result)
    {
        // TODO 1: Add + SaveChangesAsync
        // 생각해 볼 점: 원래 VisionProductCompleteHandler 는 VisionResult 와 EquipmentStatusHist 를 "한 번의 SaveChanges" 로 저장했습니다.
        //              Provider 를 나누면 저장이 두 번으로 갈라져요 → 그 사이에 실패하면? → ITwinTransaction 이 필요한 이유.
        throw new NotImplementedException("TODO 1: InsertAsync");
    }
}
