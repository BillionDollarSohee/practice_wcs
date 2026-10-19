using EquipmentStatusKata;

// ★ 이 파일은 "채점기" 입니다. 수정하지 않습니다.
//   dotnet run          → 전체 채점
//   dotnet run -- A     → PART A 만 (A, B, C)

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

(EquipmentService svc, InMemoryEquipmentProvider prov, FakeClock clock) New()
{
    var clock = new FakeClock();
    var prov = new InMemoryEquipmentProvider(clock);
    return (new EquipmentService(prov, clock), prov, clock);
}


// ===================================================== PART A
Section("A", "저장 (SaveStatus / SaveStatusCheckExists)", () =>
{
    Check("SaveStatus: 행이 없으면 갱신 안 되지만 이력은 1건 남는다", () =>
    {
        var (svc, prov, _) = New();
        bool ok = svc.SaveStatus("CV.TR.01", "PLC_R_CART_PRESENT", "", "1", "", "", "t");
        Expect(ok, "true 를 반환해야 함");
        Expect(prov.SelectStatus("CV.TR.01", "").Count == 0, "행이 없는데 새 행이 만들어짐 (SaveStatus 는 INSERT 하지 않음)");
        Expect(prov.Hist.Count == 1, $"이력 {prov.Hist.Count}건 (기대 1건)");
    });

    Check("SaveStatusCheckExists: 행이 없으면 만들고 값을 저장 + 이력 1건", () =>
    {
        var (svc, prov, _) = New();
        svc.SaveStatusCheckExists("CV.TR.01", "PLC_C_RFID_DATA", "", "ABC", "", "", "t");
        var rows = prov.SelectStatus("CV.TR.01", "PLC_C_RFID_DATA");
        Expect(rows.Count == 1, $"행 {rows.Count}개 (기대 1개)");
        Expect(rows[0].ResultValue == "ABC", $"값 \"{rows[0].ResultValue}\" (기대 ABC) — INSERT 후 UPDATE 까지 해야 함");
        Expect(prov.Hist.Count == 1, $"이력 {prov.Hist.Count}건 (기대 1건)");
    });

    Check("SaveStatusCheckExists: 행이 이미 있으면 새로 만들지 않고 갱신한다 (행 1개 유지)", () =>
    {
        var (svc, prov, _) = New();
        svc.SaveStatusCheckExists("E1", "T1", "", "1", "", "", "t");
        svc.SaveStatusCheckExists("E1", "T1", "", "2", "", "", "t");
        var rows = prov.SelectStatus("E1", "T1");
        Expect(rows.Count == 1, $"행 {rows.Count}개 (기대 1개)");
        Expect(rows[0].ResultValue == "2", $"값 \"{rows[0].ResultValue}\" (기대 2)");
    });

    Check("같은 값을 다시 저장해도 SaveDttm 이 갱신되고 이력이 쌓인다 (\"바뀔 때만 저장\" 이 아님)", () =>
    {
        var (svc, prov, clock) = New();
        svc.SaveStatusCheckExists("E1", "T1", "", "1", "", "", "t");
        var first = prov.SelectStatus("E1", "T1")[0].SaveDttm;

        clock.Advance(TimeSpan.FromSeconds(10));
        svc.SaveStatusCheckExists("E1", "T1", "", "1", "", "", "t");   // 값은 그대로

        var second = prov.SelectStatus("E1", "T1")[0].SaveDttm;
        Expect(second - first == TimeSpan.FromSeconds(10), $"SaveDttm 차이 {(second - first).TotalSeconds}초 (기대 10초)");
        Expect(prov.Hist.Count == 2, $"이력 {prov.Hist.Count}건 (기대 2건)");
    });
});


// ===================================================== PART B
Section("B", "명령번호(instNo) 규칙", () =>
{
    Check("instNo 가 다르고 isNewInstNo=false 이면 갱신되지 않는다 (이력은 남음)", () =>
    {
        var (svc, prov, _) = New();
        svc.SaveStatusCheckExists("E1", "VISION_RFID_WRITE_REQ", "A", "REQUESTED", "", "", "t", isNewInstNo: true);
        svc.SaveStatusCheckExists("E1", "VISION_RFID_WRITE_REQ", "B", "COMPLETED", "", "", "t");  // isNewInstNo=false

        var row = prov.SelectStatus("E1", "VISION_RFID_WRITE_REQ")[0];
        Expect(row.ResultValue == "REQUESTED", $"값 \"{row.ResultValue}\" (기대 REQUESTED — 다른 명령번호의 결과는 반영하지 않음)");
        Expect(prov.Hist.Count == 2, $"이력 {prov.Hist.Count}건 (기대 2건)");
    });

    Check("instNo 가 같으면 갱신된다", () =>
    {
        var (svc, prov, _) = New();
        svc.SaveStatusCheckExists("E1", "T1", "A", "REQUESTED", "", "", "t", isNewInstNo: true);
        svc.SaveStatusCheckExists("E1", "T1", "A", "COMPLETED", "", "", "t");
        Expect(prov.SelectStatus("E1", "T1")[0].ResultValue == "COMPLETED", "같은 명령번호인데 갱신되지 않음");
    });

    Check("isNewInstNo=true 이면 명령번호가 새 값으로 바뀐다", () =>
    {
        var (svc, prov, _) = New();
        svc.SaveStatusCheckExists("E1", "T1", "A", "REQUESTED", "", "", "t", isNewInstNo: true);
        svc.SaveStatusCheckExists("E1", "T1", "B", "REQUESTED", "", "", "t", isNewInstNo: true);
        var row = prov.SelectStatus("E1", "T1")[0];
        Expect(row.InstructionNo == "B", $"InstNo \"{row.InstructionNo}\" (기대 B)");
    });
});


// ===================================================== PART C
Section("C", "조회 / 도우미", () =>
{
    Check("GetStatus: 태그 지정 / 전체 / 없는 설비", () =>
    {
        var (svc, _, _) = New();
        svc.SaveStatusCheckExists("E1", "PLC_R_A", "", "1", "", "", "t");
        svc.SaveStatusCheckExists("E1", "PLC_R_B", "", "2", "", "", "t");
        svc.SaveStatusCheckExists("E2", "PLC_R_A", "", "9", "", "", "t");

        Expect(svc.GetStatus("E1", "").Count == 2, "E1 전체는 2개여야 함");
        Expect(svc.GetStatus("E1", "PLC_R_B").Count == 1, "E1 의 PLC_R_B 는 1개여야 함");
        Expect(svc.GetStatus("NONE", "").Count == 0, "없는 설비는 빈 리스트여야 함");
    });

    Check("GetValue: 있으면 값, 없으면 기본값", () =>
    {
        var (svc, _, _) = New();
        svc.SaveStatusCheckExists("CV", "PLC_R_CART_PRESENT", "", "1", "", "", "t");
        Expect(svc.GetValue("CV", "PLC_R_CART_PRESENT", "0") == "1", "값이 있으면 그 값");
        Expect(svc.GetValue("CV", "PLC_R_FORK_UP", "0") == "0", "행이 없으면 기본값 \"0\"");
        Expect(svc.GetValue("CV", "PLC_C_RFID_DATA", "") == "", "기본값이 빈 문자열이어도 동작");
    });

    Check("ElapsedSecondsSinceSave: 저장 후 흐른 시간, 행이 없으면 null", () =>
    {
        var (svc, _, clock) = New();
        Expect(svc.ElapsedSecondsSinceSave("CV", "PLC_R_CART_PRESENT") == null, "행이 없으면 null 이어야 함");

        svc.SaveStatusCheckExists("CV", "PLC_R_CART_PRESENT", "", "1", "", "", "t");
        clock.Advance(TimeSpan.FromSeconds(4));
        var e = svc.ElapsedSecondsSinceSave("CV", "PLC_R_CART_PRESENT");
        Expect(e.HasValue && Math.Abs(e.Value - 4) < 0.001, $"경과 {e}초 (기대 4초)");

        svc.SaveStatusCheckExists("CV", "PLC_R_CART_PRESENT", "", "1", "", "", "t");   // 다시 저장 → 경과가 0 으로
        e = svc.ElapsedSecondsSinceSave("CV", "PLC_R_CART_PRESENT");
        Expect(e.HasValue && Math.Abs(e.Value) < 0.001, $"다시 저장한 뒤 경과 {e}초 (기대 0초)");
    });

    Check("GetTagKind: R / W / C / 그 외", () =>
    {
        (string tag, TagKind exp)[] cases =
        {
            ("PLC_R_CART_PRESENT", TagKind.Read),
            ("PLC_W_COMMAND", TagKind.Write),
            ("PLC_C_RFID_DATA", TagKind.Common),
            ("RFID_C_READ_VAL", TagKind.Common),
            ("VISION_RFID_READ_REQ", TagKind.Unknown),
            ("PLC_X_SOMETHING", TagKind.Unknown),
            ("SHORT", TagKind.Unknown),
        };
        foreach (var c in cases)
        {
            var got = EquipmentService.GetTagKind(c.tag);
            Expect(got == c.exp, $"GetTagKind(\"{c.tag}\") = {got} (기대 {c.exp})");
        }
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
