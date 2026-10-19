namespace AlarmServiceKata;

// ★ 이 파일은 수정하지 않습니다. DB 연결(트랜잭션)과 시계를 가짜로 대신한 것입니다.
//   실무: WCSMysqlServerDBClient (BeginTransaction / CommitTransaction / RollbackTransaction)

public interface IDbClient
{
    object BeginTransaction();
    void CommitTransaction(object transaction);
    void RollbackTransaction(object transaction);
}

/// <summary>호출 순서를 기록하는 가짜 DB 연결. 테스트가 "BEGIN → COMMIT" 같은 순서를 확인한다.</summary>
public sealed class FakeDbClient : IDbClient
{
    public List<string> Log { get; } = new();
    private int _txSeq;

    public object BeginTransaction() { Log.Add("BEGIN"); return ++_txSeq; }
    public void CommitTransaction(object transaction) => Log.Add("COMMIT");
    public void RollbackTransaction(object transaction) => Log.Add("ROLLBACK");
}

public interface IClock { DateTime Now { get; } }

public sealed class FakeClock : IClock
{
    public DateTime Now { get; private set; } = new DateTime(2026, 10, 1, 9, 0, 0);
    public void Advance(TimeSpan span) => Now += span;
}
