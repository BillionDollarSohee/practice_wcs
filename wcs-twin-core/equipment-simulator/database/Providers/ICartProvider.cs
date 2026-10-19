using Database.Models;

namespace Database.Providers;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다)
//
// Provider = "DB 접근만" 하는 계층입니다. 업무 판단(라인타입이 맞는지 등)은 하지 않습니다.
// 울산: IXxxProvider(인터페이스) + XxxMySqlProvider(구현). 여기서는 EF 를 쓰니 XxxProvider.

public interface ICartProvider
{
    /// <summary>대차 마스터 한 건. 없으면 null  (현재 VisionCartInfoRequestHandler 의 CartMasters.FirstOrDefault 자리)</summary>
    Task<CartMaster?> SelectCartAsync(string cartId);
}

public sealed class CartProvider : ICartProvider
{
    private readonly WcsTwinContext _context;

    public CartProvider(WcsTwinContext context)
    {
        _context = context;
    }

    public Task<CartMaster?> SelectCartAsync(string cartId)
    {
        // TODO 1: _context.CartMasters 에서 CartId 로 한 건 조회 (FirstOrDefaultAsync)
        //         읽기 전용이니 AsNoTracking 을 쓸지 생각해 보세요.
        throw new NotImplementedException("TODO 1: SelectCartAsync");
    }
}
