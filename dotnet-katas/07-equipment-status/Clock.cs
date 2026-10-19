namespace EquipmentStatusKata;

// ★ 이 파일은 수정하지 않습니다.
//   시각을 코드가 직접 DateTime.Now 로 읽으면 테스트가 어려워서, "시계"를 밖에서 넣어주는 방식을 씁니다.
//   (실무는 SQL 안의 NOW(6) 이 이 역할 — DB 서버 시각을 기준으로 삼습니다)

public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

public sealed class FakeClock : IClock
{
    public DateTime Now { get; private set; } = new DateTime(2026, 10, 1, 9, 0, 0);
    public void Advance(TimeSpan span) => Now += span;
}
