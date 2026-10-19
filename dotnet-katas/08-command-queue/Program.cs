using CommandQueueKata;

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

(CommandService svc, InMemoryCommandProvider prov, FakePlcWriter plc) New()
{
    var prov = new InMemoryCommandProvider();
    var plc = new FakePlcWriter();
    return (new CommandService(prov, plc), prov, plc);
}

Dictionary<string, string> Move() => new() { ["PLC_W_COMMAND"] = "13" };


// ===================================================== PART A
Section("A", "등록 가능 여부 / 등록", () =>
{
    Check("CanIssue: 대기 명령이 없고 PLC 도 \"0\"(대기) 이면 true", () =>
    {
        var (svc, _, _) = New();
        Expect(svc.CanIssue("CV1", "0"), "true 여야 함");
    });

    Check("CanIssue: 이 설비에 WAIT 명령이 있으면 false", () =>
    {
        var (svc, _, _) = New();
        svc.Enqueue("CV1", Move());
        Expect(!svc.CanIssue("CV1", "0"), "대기 명령이 있는데 true");
    });

    Check("CanIssue: PLC 가 아직 명령 처리 중(\"13\")이면 false", () =>
    {
        var (svc, _, _) = New();
        Expect(!svc.CanIssue("CV1", "13"), "PLC_W_COMMAND 가 0 이 아닌데 true");
    });

    Check("CanIssue: 다른 설비의 대기 명령은 영향을 주지 않는다", () =>
    {
        var (svc, _, _) = New();
        svc.Enqueue("CV2", Move());
        Expect(svc.CanIssue("CV1", "0"), "CV2 의 명령 때문에 CV1 이 막힘");
    });

    Check("Enqueue: WAIT 로 1건 등록되고 명령번호가 1, 2 로 올라간다", () =>
    {
        var (svc, prov, _) = New();
        string a = svc.Enqueue("CV1", Move());
        prov.UpdateStatus("CV1", a, CommandState.COMPLETE, "");   // 다음 등록을 위해 정리
        string b = svc.Enqueue("CV1", Move());
        Expect(a == "1" && b == "2", $"명령번호 {a}, {b} (기대 1, 2)");
        Expect(prov.Find("CV1", b)?.Status == CommandState.WAIT, "상태가 WAIT 여야 함");
    });

    Check("Enqueue: PLC_W_COMMAND 가 있으면 PLC_W_CMD_SEQ = 명령번호 가 자동으로 들어간다", () =>
    {
        var (svc, prov, _) = New();
        string seq = svc.Enqueue("CV1", Move());
        string json = prov.Find("CV1", seq)!.ValuesJson;
        Expect(json.Contains("\"PLC_W_CMD_SEQ\":\"" + seq + "\""), $"JSON 에 PLC_W_CMD_SEQ 가 없음: {json}");
    });

    Check("Enqueue: 하트비트처럼 PLC_W_COMMAND 가 없으면 CMD_SEQ 를 넣지 않는다", () =>
    {
        var (svc, prov, _) = New();
        string seq = svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_HEART_BIT"] = "1" });
        string json = prov.Find("CV1", seq)!.ValuesJson;
        Expect(!json.Contains("PLC_W_CMD_SEQ"), $"하트비트에 CMD_SEQ 가 들어감: {json}");
    });
});


// ===================================================== PART B
Section("B", "PLC 에 쓰기 (ProcessWaiting)", () =>
{
    Check("태그를 모두 쓰되 PLC_W_CMD_SEQ 는 맨 마지막에 쓴다", () =>
    {
        var (svc, _, plc) = New();
        svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_RFID_STATUS"] = "0", ["PLC_W_COMMAND"] = "12" });
        int n = svc.ProcessWaiting("CV1");
        Expect(n == 1, $"처리 {n}건 (기대 1건)");
        string order = string.Join(",", plc.Writes.Select(w => w.Tag));
        Expect(order == "PLC_W_RFID_STATUS,PLC_W_COMMAND,PLC_W_CMD_SEQ", $"쓰기 순서 {order}");
    });

    Check("성공하면 큐에서 사라지고(0행) 이력에 COMPLETE 1건이 남는다", () =>
    {
        var (svc, prov, _) = New();
        svc.Enqueue("CV1", Move());
        svc.ProcessWaiting("CV1");
        Expect(prov.RowCount == 0, $"큐에 {prov.RowCount}건 남음 (끝난 명령은 삭제해야 함)");
        Expect(prov.Hist.Count == 1, $"이력 {prov.Hist.Count}건 (기대 1건)");
        Expect(prov.Hist[0].Status == CommandState.COMPLETE, $"이력 상태 {prov.Hist[0].Status} (기대 COMPLETE — 상태를 바꾼 뒤에 이력을 남겨야 함)");
    });

    Check("쓰기에 실패하면 거기서 멈추고 ERROR + 실패한 태그 이름을 이력에 남긴다", () =>
    {
        var (svc, prov, plc) = New();
        plc.FailOnTag = "PLC_W_COMMAND";
        svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_RFID_STATUS"] = "0", ["PLC_W_COMMAND"] = "12" });
        int n = svc.ProcessWaiting("CV1");
        Expect(n == 1, $"처리 {n}건 (실패도 \"처리한 것\"으로 셈 — 기대 1건)");
        Expect(!plc.Writes.Any(w => w.Tag == "PLC_W_CMD_SEQ"), "실패했는데 CMD_SEQ 까지 써버림 (실패한 시점에 멈춰야 함)");
        Expect(prov.Hist.Count == 1 && prov.Hist[0].Status == CommandState.ERROR, "이력에 ERROR 1건이 있어야 함");
        Expect(prov.Hist[0].Message.Contains("PLC_W_COMMAND"), $"메시지 \"{prov.Hist[0].Message}\" 에 실패 태그가 없음");
        Expect(prov.RowCount == 0, "실패한 명령도 큐에서 삭제해야 함");
    });

    Check("하트비트 명령은 이력에 남기지 않는다 (그래도 큐에서는 사라진다)", () =>
    {
        var (svc, prov, plc) = New();
        svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_HEART_BIT"] = "1" });
        svc.ProcessWaiting("CV1");
        Expect(plc.Writes.Count == 1, "하트비트 태그 1개를 써야 함");
        Expect(prov.Hist.Count == 0, $"이력 {prov.Hist.Count}건 (기대 0건)");
        Expect(prov.RowCount == 0, "큐에서 삭제되어야 함");
    });

    Check("대기 명령이 여러 개면 등록 순서대로 처리한다", () =>
    {
        var (svc, _, plc) = New();
        svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_COMMAND"] = "10" });
        svc.Enqueue("CV1", new Dictionary<string, string> { ["PLC_W_COMMAND"] = "11" });
        int n = svc.ProcessWaiting("CV1");
        Expect(n == 2, $"처리 {n}건 (기대 2건)");
        var commands = plc.Writes.Where(w => w.Tag == "PLC_W_COMMAND").Select(w => w.Value).ToArray();
        Expect(string.Join(",", commands) == "10,11", $"명령 순서 {string.Join(",", commands)} (기대 10,11)");
    });

    Check("다른 설비의 명령은 건드리지 않는다", () =>
    {
        var (svc, prov, _) = New();
        svc.Enqueue("CV1", Move());
        svc.Enqueue("CV2", Move());
        svc.ProcessWaiting("CV1");
        Expect(prov.CountPending("CV2") == 1, "CV2 의 명령이 처리되어 버림");
    });

    Check("처리할 명령이 없으면 0 을 돌려준다", () =>
    {
        var (svc, _, _) = New();
        Expect(svc.ProcessWaiting("CV1") == 0, "0 이어야 함");
    });
});


// ===================================================== PART C
Section("C", "마무리 (Finish)", () =>
{
    Check("CANCELED 도 이력 1건 + 큐 삭제", () =>
    {
        var (svc, prov, _) = New();
        string seq = svc.Enqueue("CV1", Move());
        int r = svc.Finish("CV1", seq, CommandState.CANCELED, "작업자 취소");
        Expect(r == 1, $"반환 {r} (UpdateStatus 의 반환값 1 이어야 함)");
        Expect(prov.Hist.Count == 1 && prov.Hist[0].Status == CommandState.CANCELED, "이력에 CANCELED 1건");
        Expect(prov.RowCount == 0, "큐에서 삭제되어야 함");
    });

    Check("WAIT 같은 \"끝나지 않은\" 상태로 바꾸면 이력/삭제를 하지 않는다", () =>
    {
        var (svc, prov, _) = New();
        string seq = svc.Enqueue("CV1", Move());
        svc.Finish("CV1", seq, CommandState.WAIT, "재시도 대기");
        Expect(prov.Hist.Count == 0 && prov.RowCount == 1, "끝나지 않은 명령은 큐에 남아 있어야 함");
    });

    Check("skipHistory = true 이면 이력 없이 삭제만 한다", () =>
    {
        var (svc, prov, _) = New();
        string seq = svc.Enqueue("CV1", Move());
        svc.Finish("CV1", seq, CommandState.COMPLETE, "", skipHistory: true);
        Expect(prov.Hist.Count == 0, "이력이 남음");
        Expect(prov.RowCount == 0, "삭제되지 않음");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
