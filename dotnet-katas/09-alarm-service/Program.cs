using AlarmServiceKata;

// ★ 이 파일은 "채점기" 입니다. 수정하지 않습니다.
//   dotnet run          → 전체 채점
//   dotnet run -- A     → PART A 만 (A, B)

int pass = 0, todo = 0, fail = 0;
string? only = args.FirstOrDefault()?.ToUpperInvariant();

void Check(string name, Action test)
{
    try { test(); Console.WriteLine($"  PASS  {name}"); pass++; }
    catch (NotImplementedException ex) { Console.WriteLine($"  TODO  {name}  ({ex.Message})"); todo++; }
    catch (Exception ex) { Console.WriteLine($"  FAIL  {name}  → {ex.Message}"); fail++; }
}

void Expect(bool cond, string msg) { if (!cond) throw new Exception(msg); }

void Section(string letter, string title, Action body)
{
    if (only != null && only != letter) return;
    Console.WriteLine($"\n[{letter}] {title}");
    body();
}

(AlarmService svc, InMemoryAlarmProvider prov, FakeDbClient db, FakeClock clock) New()
{
    var clock = new FakeClock();
    var db = new FakeDbClient();
    var prov = new InMemoryAlarmProvider(clock);
    return (new AlarmService(db, prov), prov, db, clock);
}

string Log(FakeDbClient db) => string.Join(",", db.Log);


// ===================================================== PART A
Section("A", "알람 시작 (Start)", () =>
{
    Check("새 알람: 현재 표 1건 + 이력 1건, 반환 2, BEGIN → COMMIT", () =>
    {
        var (svc, prov, db, _) = New();
        int r = svc.Start("P", "CV1", "NOT_FOUND_CART_INFO", "대차 없음", "Y");
        Expect(r == 2, $"반환 {r} (기대 2)");
        Expect(prov.Active.Count == 1, $"현재 알람 {prov.Active.Count}건 (기대 1건)");
        Expect(prov.Hist.Count == 1, $"이력 {prov.Hist.Count}건 (기대 1건)");
        Expect(Log(db) == "BEGIN,COMMIT", $"DB 호출 순서 {Log(db)} (기대 BEGIN,COMMIT)");
    });

    Check("같은 알람을 또 시작해도 늘어나지 않는다 (멱등), 반환 0", () =>
    {
        var (svc, prov, db, _) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        int r = svc.Start("P", "CV1", "A1", "m", "Y");
        Expect(r == 0, $"반환 {r} (기대 0)");
        Expect(prov.Active.Count == 1 && prov.Hist.Count == 1, $"현재 {prov.Active.Count}건 / 이력 {prov.Hist.Count}건 (기대 1 / 1)");
        Expect(Log(db) == "BEGIN,COMMIT,BEGIN,COMMIT", $"DB 호출 순서 {Log(db)} (이미 있어도 트랜잭션은 열고 닫아야 함)");
    });

    Check("알람코드/설비/프로그램 중 하나라도 다르면 별개의 알람이다", () =>
    {
        var (svc, prov, _, _) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        svc.Start("P", "CV1", "A2", "m", "Y");   // 코드 다름
        svc.Start("P", "CV2", "A1", "m", "Y");   // 설비 다름
        svc.Start("Q", "CV1", "A1", "m", "Y");   // 프로그램 다름
        Expect(prov.Active.Count == 4, $"현재 알람 {prov.Active.Count}건 (기대 4건)");
    });
});


// ===================================================== PART B
Section("B", "알람 종료 (End)", () =>
{
    Check("울리던 알람 종료: 현재 표에서 삭제 + 이력에 종료시각, 반환 2", () =>
    {
        var (svc, prov, db, clock) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        var started = clock.Now;
        clock.Advance(TimeSpan.FromSeconds(90));

        int r = svc.End("P", "CV1", "A1");
        Expect(r == 2, $"반환 {r} (기대 2)");
        Expect(prov.Active.Count == 0, "현재 알람이 남아 있음");
        Expect(prov.Hist.Count == 1, $"이력 {prov.Hist.Count}건 (이력은 지우지 않고 종료시각만 채움 — 기대 1건)");
        Expect(prov.Hist[0].EndDttm == started.AddSeconds(90), $"종료시각 {prov.Hist[0].EndDttm} (기대 {started.AddSeconds(90)}) — UpdateHistEnd 에 시작시각을 넘겨야 함");
        Expect(Log(db) == "BEGIN,COMMIT,BEGIN,COMMIT", $"DB 호출 순서 {Log(db)}");
    });

    Check("울리지 않는 알람 종료: 아무 일도 없고 반환 0 (그래도 트랜잭션은 커밋)", () =>
    {
        var (svc, prov, db, _) = New();
        int r = svc.End("P", "CV1", "NONE");
        Expect(r == 0, $"반환 {r} (기대 0)");
        Expect(prov.Active.Count == 0 && prov.Hist.Count == 0, "변화가 없어야 함");
        Expect(Log(db) == "BEGIN,COMMIT", $"DB 호출 순서 {Log(db)} (기대 BEGIN,COMMIT)");
    });

    Check("다른 알람은 건드리지 않는다", () =>
    {
        var (svc, prov, _, _) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        svc.Start("P", "CV1", "A2", "m", "Y");
        svc.End("P", "CV1", "A1");
        Expect(prov.Active.Count == 1 && prov.Active[0].AlarmCd == "A2", "A2 가 남아 있어야 함");
    });

    Check("종료한 알람을 다시 시작하면 이력이 새로 1건 더 생긴다", () =>
    {
        var (svc, prov, _, clock) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        clock.Advance(TimeSpan.FromSeconds(10));
        svc.End("P", "CV1", "A1");
        clock.Advance(TimeSpan.FromSeconds(10));
        svc.Start("P", "CV1", "A1", "m", "Y");
        Expect(prov.Active.Count == 1, "현재 알람 1건이어야 함");
        Expect(prov.Hist.Count == 2, $"이력 {prov.Hist.Count}건 (기대 2건)");
        Expect(prov.Hist.Count(h => h.EndDttm == null) == 1, "종료되지 않은 이력은 1건이어야 함");
    });
});


// ===================================================== PART C
Section("C", "실패하면 롤백하고 다시 던진다 (트랜잭션 템플릿)", () =>
{
    Check("Start 중 Insert 가 실패하면 ROLLBACK 하고 예외를 던진다 (COMMIT 은 없다)", () =>
    {
        var (svc, prov, db, _) = New();
        prov.FailOn = "Insert";
        bool threw = false;
        try { svc.Start("P", "CV1", "A1", "m", "Y"); } catch (NotImplementedException) { throw; } catch { threw = true; }
        Expect(threw, "예외를 삼켜버림 (throw 해야 함 — 호출한 쪽이 실패를 알아야 함)");
        Expect(Log(db) == "BEGIN,ROLLBACK", $"DB 호출 순서 {Log(db)} (기대 BEGIN,ROLLBACK)");
    });

    Check("Start 중 InsertHist 가 실패해도 ROLLBACK + 예외", () =>
    {
        var (svc, prov, db, _) = New();
        prov.FailOn = "InsertHist";
        bool threw = false;
        try { svc.Start("P", "CV1", "A1", "m", "Y"); } catch (NotImplementedException) { throw; } catch { threw = true; }
        Expect(threw, "예외를 던져야 함");
        Expect(Log(db) == "BEGIN,ROLLBACK", $"DB 호출 순서 {Log(db)}");
    });

    Check("End 중 UpdateHistEnd 가 실패하면 ROLLBACK + 예외", () =>
    {
        var (svc, prov, db, _) = New();
        svc.Start("P", "CV1", "A1", "m", "Y");
        db.Log.Clear();
        prov.FailOn = "UpdateHistEnd";
        bool threw = false;
        try { svc.End("P", "CV1", "A1"); } catch (NotImplementedException) { throw; } catch { threw = true; }
        Expect(threw, "예외를 던져야 함");
        Expect(Log(db) == "BEGIN,ROLLBACK", $"DB 호출 순서 {Log(db)}");
    });

    Check("던진 예외에 원래 예외가 담겨 있다 (InnerException)", () =>
    {
        var (svc, prov, _, _) = New();
        prov.FailOn = "Insert";
        Exception? caught = null;
        try { svc.Start("P", "CV1", "A1", "m", "Y"); } catch (NotImplementedException) { throw; } catch (Exception ex) { caught = ex; }
        Expect(caught != null && caught.InnerException is InvalidOperationException,
            "new Exception(ex.ToString(), ex) 처럼 원래 예외를 InnerException 으로 감싸서 던져야 함");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
