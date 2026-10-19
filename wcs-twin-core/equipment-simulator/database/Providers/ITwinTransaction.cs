using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Database.Providers;

// ★ MSP 변환 스켈레톤 (아직 어디에도 연결되지 않았습니다 — wcs-twin-core/MSP_변환_가이드.md 의 Step 1 에서 사용)
//
// 울산 코드의 Service 는 이렇게 DB 를 씁니다:
//   object transaction = _wcsDbClient.BeginTransaction();
//   try { Provider 호출들...; _wcsDbClient.CommitTransaction(transaction); }
//   catch { _wcsDbClient.RollbackTransaction(transaction); throw; }
//
// 이 프로젝트는 EF Core(WcsTwinContext)를 쓰니, 같은 역할을 EF 의 트랜잭션으로 맡깁니다.
// 같은 scope 의 DbContext 를 쓰는 Provider 들은 이 트랜잭션 안에서 함께 커밋/롤백됩니다.

public interface ITwinTransaction
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public sealed class TwinTransaction : ITwinTransaction
{
    private readonly WcsTwinContext _context;
    private IDbContextTransaction? _transaction;

    public TwinTransaction(WcsTwinContext context)
    {
        _context = context;
    }

    public Task BeginAsync()
    {
        // TODO 1: _context.Database.BeginTransactionAsync() 결과를 _transaction 에 보관
        //         (이미 시작된 트랜잭션이 있으면 어떻게 할지 정해 보세요 — 예외? 무시?)
        throw new NotImplementedException("TODO 1: BeginAsync");
    }

    public Task CommitAsync()
    {
        // TODO 2: 트랜잭션이 없으면 InvalidOperationException, 있으면 Commit 후 정리(Dispose, null)
        throw new NotImplementedException("TODO 2: CommitAsync");
    }

    public Task RollbackAsync()
    {
        // TODO 3: 트랜잭션이 없으면 그냥 return (이미 롤백된 경우에도 안전해야 함), 있으면 Rollback 후 정리
        throw new NotImplementedException("TODO 3: RollbackAsync");
    }
}
