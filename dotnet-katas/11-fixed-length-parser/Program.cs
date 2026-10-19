using FixedLengthParserKata;

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

// 서열 정보가 들어 있는 대표 RFID 값 (40글자)
//   "7C  000906" + "7C  " + "TR" + "01" + "0005" + "0012" + "TR" + "000000000000"
const string Full = "7C  0009067C  TR010005" + "0012" + "TR" + "000000000000";


// ===================================================== PART A
Section("A", "읽기 (Parse)", () =>
{
    Check("대표 문자열: 각 칸이 위치대로 잘린다", () =>
    {
        Expect(Full.Length == 40, $"테스트 데이터 길이 {Full.Length} (40 이어야 함 — 채점기 오류)");
        var c = new ParsedCartInfo(Full);
        Expect(c.RfidData == Full, $"RfidData \"{c.RfidData}\"");
        Expect(c.SkidId == "7C  0009067C  TR01", $"SkidId \"{c.SkidId}\" (기대 \"7C  0009067C  TR01\" — 앞 18글자를 Trim)");
        Expect(c.SeqNo == "0005", $"SeqNo \"{c.SeqNo}\" (기대 0005)");
        Expect(c.CartNo == "0012", $"CartNo \"{c.CartNo}\" (기대 0012)");
        Expect(c.LineType == "TR", $"LineType \"{c.LineType}\" (기대 TR)");
        Expect(c.Option == "000000000000", $"Option \"{c.Option}\"");
    });

    Check("CartId 는 EV_ + 라인타입 첫글자 + - + 대차번호", () =>
    {
        Expect(new ParsedCartInfo(Full).CartId == "EV_T-0012", $"CartId \"{new ParsedCartInfo(Full).CartId}\" (기대 EV_T-0012)");
        string fl = "                  " + "    " + "0007" + "FL" + "000000000000";
        Expect(new ParsedCartInfo(fl).CartId == "EV_F-0007", $"FL 라인: \"{new ParsedCartInfo(fl).CartId}\" (기대 EV_F-0007)");
    });

    Check("서열번호가 0005 이면 서열정보 있음", () =>
    {
        Expect(new ParsedCartInfo(Full).IsExistsSeqInfo, "IsExistsSeqInfo 가 true 여야 함");
    });

    Check("서열번호가 \"0000\" 이거나 공백이면 서열정보 없음", () =>
    {
        string zero = "7C  0009067C  TR010000" + "0012" + "TR" + "000000000000";
        string blank = new string(' ', 22) + "0012" + "TR" + "000000000000";
        Expect(!new ParsedCartInfo(zero).IsExistsSeqInfo, "\"0000\" 인데 true");
        Expect(!new ParsedCartInfo(blank).IsExistsSeqInfo, "공백인데 true");
    });

    Check("대차번호가 \"0000\" 이거나 라인타입이 비어 있으면 CartId 가 빈 문자열", () =>
    {
        string noCart = new string(' ', 22) + "0000" + "TR" + "000000000000";
        string noLine = new string(' ', 22) + "0012" + "  " + "000000000000";
        Expect(new ParsedCartInfo(noCart).CartId == "", $"대차번호 0000 → \"{new ParsedCartInfo(noCart).CartId}\"");
        Expect(new ParsedCartInfo(noLine).CartId == "", $"라인타입 없음 → \"{new ParsedCartInfo(noLine).CartId}\"");
    });

    Check("빈 문자열 / 짧은 문자열도 예외 없이 40글자로 맞춰 해석한다", () =>
    {
        var empty = new ParsedCartInfo("");
        Expect(empty.RfidData.Length == 40, $"RfidData 길이 {empty.RfidData.Length} (기대 40)");
        Expect(empty.CartId == "" && !empty.IsExistsSeqInfo, "빈 값은 대차/서열 정보가 없어야 함");

        var shortOne = new ParsedCartInfo("7C  000906");
        Expect(shortOne.SkidId == "7C  000906", $"짧은 문자열 SkidId \"{shortOne.SkidId}\"");
        Expect(shortOne.RfidData.Length == 40, "40글자로 맞춰야 함");
    });
});


// ===================================================== PART B
Section("B", "만들기 (Build)", () =>
{
    Check("서열정보로 WRITE 문자열 만들기: 대표 값과 정확히 같다", () =>
    {
        var c = new ParsedCartInfo("7C  000906", "7C", "1", "5", "EV_T-12", "tr");
        Expect(c.RfidData == Full, $"\n    만든 값  \"{c.RfidData}\"\n    기대 값  \"{Full}\"");
        Expect(c.CartId == "EV_T-0012" && c.IsExistsSeqInfo, "만든 뒤 해석된 속성도 맞아야 함");
    });

    Check("짧은 차체번호는 오른쪽 공백, 서열번호/라인코드는 왼쪽 0", () =>
    {
        var c = new ParsedCartInfo("AB1", "7C", "3", "42", "EV_D-7", "dr");
        Expect(c.RfidData.Length == 40, $"길이 {c.RfidData.Length} (기대 40)");
        Expect(c.RfidData.Substring(0, 10) == "AB1       ", $"차체번호 칸 \"{c.RfidData.Substring(0, 10)}\"");
        Expect(c.RfidData.Substring(16, 2) == "03", $"라인코드 칸 \"{c.RfidData.Substring(16, 2)}\" (기대 03)");
        Expect(c.RfidData.Substring(18, 4) == "0042", $"서열번호 칸 \"{c.RfidData.Substring(18, 4)}\" (기대 0042)");
        Expect(c.RfidData.Substring(22, 4) == "0007", $"대차번호 칸 \"{c.RfidData.Substring(22, 4)}\" (기대 0007)");
        Expect(c.LineType == "DR", $"LineType \"{c.LineType}\" (대문자로 들어가야 함)");
    });

    Check("대차번호/라인타입만으로 만들면 서열정보 22칸은 비어 있다", () =>
    {
        var c = new ParsedCartInfo("12", "tr");
        Expect(c.RfidData.Length == 40, $"길이 {c.RfidData.Length} (기대 40)");
        Expect(c.CartId == "EV_T-0012", $"CartId \"{c.CartId}\" (기대 EV_T-0012)");
        Expect(!c.IsExistsSeqInfo, "서열정보가 없어야 함");
        Expect(c.RfidData.Substring(14, 2) == "TR", $"앞쪽 라인타입 칸 \"{c.RfidData.Substring(14, 2)}\" (기대 TR)");
    });
});


// ===================================================== PART C
Section("C", "서열정보 쓰기 / 지우기", () =>
{
    Check("GetSeqInfoForWrite: 앞 22글자", () =>
    {
        var c = new ParsedCartInfo(Full);
        string s = c.GetSeqInfoForWrite();
        Expect(s.Length == 22, $"길이 {s.Length} (기대 22)");
        Expect(s == Full.Substring(0, 22), $"\"{s}\" (기대 \"{Full.Substring(0, 22)}\")");
    });

    Check("ClearSeqInfo: 앞 22글자만 공백, 대차 정보는 유지, 원본은 그대로", () =>
    {
        var c = new ParsedCartInfo(Full);
        string cleared = c.ClearSeqInfo();
        Expect(cleared.Length == 40, $"길이 {cleared.Length} (기대 40)");
        Expect(cleared.Substring(0, 22) == new string(' ', 22), "앞 22글자가 모두 공백이어야 함");
        Expect(cleared.Substring(22) == Full.Substring(22), "뒤 18글자(대차번호/라인타입/옵션)는 그대로여야 함");
        Expect(c.RfidData == Full, "원본 객체가 바뀌었음");

        var after = new ParsedCartInfo(cleared);
        Expect(!after.IsExistsSeqInfo, "지운 뒤에는 서열정보가 없어야 함");
        Expect(after.CartId == "EV_T-0012", $"대차 정보는 유지되어야 함 (CartId \"{after.CartId}\")");
    });

    Check("읽기 ↔ 만들기 왕복: 만든 값을 다시 읽어도 같은 속성", () =>
    {
        var built = new ParsedCartInfo("7C  000906", "7C", "1", "5", "EV_T-12", "TR");
        var reread = new ParsedCartInfo(built.RfidData);
        Expect(reread.SkidId == built.SkidId && reread.SeqNo == built.SeqNo && reread.CartId == built.CartId, "다시 읽은 값이 다름");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
