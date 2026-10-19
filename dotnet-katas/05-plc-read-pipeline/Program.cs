using System.Collections;
using PlcReadKata;

// ★ 이 파일은 "채점기" 입니다. 수정하지 않습니다.
//   dotnet run              → 전체 채점
//   dotnet run -- A         → PART A 만 (A, B, C, D)
//
//   PASS = 통과,  TODO = 아직 안 채움,  FAIL = 틀림 (이유가 → 뒤에 나옵니다)

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

// FakePlc "D" 메모리 패턴: i번째 바이트 = i % 251
byte[] Expected(int startWord, int bytes) =>
    Enumerable.Range(startWord * 2, bytes).Select(i => (byte)(i % 251)).ToArray();

string Fmt(byte[]? b) => b == null ? "null" : $"byte[{b.Length}]";


// ===================================================== PART A
Section("A", "WordBit — 워드/비트 해석", () =>
{
    Check("GetWord: {0x34,0x12} → 0x1234, {0xFF,0x00} → 0x00FF", () =>
    {
        var data = new byte[] { 0x34, 0x12, 0xFF, 0x00 };
        Expect(WordBit.GetWord(data, 0) == 0x1234, $"word0 = 0x{WordBit.GetWord(data, 0):X} (기대 0x1234)");
        Expect(WordBit.GetWord(data, 1) == 0x00FF, $"word1 = 0x{WordBit.GetWord(data, 1):X} (기대 0x00FF)");
    });

    Check("GetBit: bit0 / bit1 / bit15 (high byte 소속 확인)", () =>
    {
        var data = new byte[] { 0x01, 0x80, 0x00, 0x00 };
        Expect(WordBit.GetBit(data, 0, 0), "word0 bit0 은 true 여야 함");
        Expect(!WordBit.GetBit(data, 0, 1), "word0 bit1 은 false 여야 함");
        Expect(WordBit.GetBit(data, 0, 15), "word0 bit15 는 true 여야 함 (high byte 0x80 의 최상위 비트)");
        Expect(!WordBit.GetBit(data, 1, 0), "word1 bit0 은 false 여야 함");
    });

    Check("GetBit: 모든 (word, bit) 가 BitArray 와 일치", () =>
    {
        var rnd = new Random(7);
        var data = new byte[40];
        rnd.NextBytes(data);
        var bits = new BitArray(data);
        for (int w = 0; w < 20; w++)
            for (int b = 0; b < 16; b++)
                Expect(WordBit.GetBit(data, w, b) == bits[w * 16 + b], $"word {w} bit {b} 가 BitArray 와 다름");
    });

    Check("GetAscii: 끝의 \\0 제거, 부분 구간", () =>
    {
        var data = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', 0, 0 };
        string s1 = WordBit.GetAscii(data, 0, 3);
        string s2 = WordBit.GetAscii(data, 1, 1);
        Expect(s1 == "ABCD", $"GetAscii(0,3) = \"{s1}\" (기대 \"ABCD\")");
        Expect(s2 == "CD", $"GetAscii(1,1) = \"{s2}\" (기대 \"CD\")");
    });

    Check("WordsNeeded: 비트 길이 (경계값)", () =>
    {
        (int sb, int len, int exp)[] cases = { (0, 1, 1), (14, 1, 1), (15, 2, 2), (0, 16, 1), (0, 17, 2), (0, 0, 1) };
        foreach (var c in cases)
        {
            int got = WordBit.WordsNeeded(false, c.sb, c.len);
            Expect(got == c.exp, $"WordsNeeded(false, startBit={c.sb}, length={c.len}) = {got} (기대 {c.exp})");
        }
    });

    Check("WordsNeeded: 워드 길이 (최소 1)", () =>
    {
        Expect(WordBit.WordsNeeded(true, 0, 5) == 5, "length 5 → 5");
        Expect(WordBit.WordsNeeded(true, 0, 0) == 1, "length 0 → 최소 1");
    });
});


// ===================================================== PART B
Section("B", "ChunkedReader — 청크 분할 읽기", () =>
{
    Check("totalBytes 0 → 빈 배열, PLC 호출 0번", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 0);
        Expect(r != null && r.Length == 0, $"결과 {Fmt(r)} (기대 byte[0])");
        Expect(plc.Calls.Count == 0, $"PLC 호출 {plc.Calls.Count}번 (기대 0번)");
    });

    Check("작은 요청 → 1번 호출, 내용 일치", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc).ReadChunked("D", 10, 100);
        Expect(r != null && r.SequenceEqual(Expected(10, 100)), "읽은 내용이 PLC 메모리와 다름");
        Expect(plc.Calls.Count == 1, $"PLC 호출 {plc.Calls.Count}번 (기대 1번)");
        Expect(plc.Calls[0] == ("D", 10, 100), $"요청 = {plc.Calls[0]} (기대 (D, 10, 100))");
    });

    Check("정확히 한계(500워드=1000B) → 1번 호출 (경계값)", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 1000);
        Expect(plc.Calls.Count == 1, $"PLC 호출 {plc.Calls.Count}번 (기대 1번 — 한계 '이하'는 분할하지 않음)");
        Expect(r != null && r.SequenceEqual(Expected(0, 1000)), "내용 불일치");
    });

    Check("2500B → 3번 호출 (0,1000)(500,1000)(1000,500), 이어붙인 내용 일치", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 2500);
        Expect(plc.Calls.Count == 3, $"PLC 호출 {plc.Calls.Count}번 (기대 3번)");
        Expect(plc.Calls[0] == ("D", 0, 1000), $"1번째 = {plc.Calls[0]}");
        Expect(plc.Calls[1] == ("D", 500, 1000), $"2번째 = {plc.Calls[1]} (기대 (D, 500, 1000) — 시작 '워드' 갱신 확인)");
        Expect(plc.Calls[2] == ("D", 1000, 500), $"3번째 = {plc.Calls[2]} (기대 (D, 1000, 500))");
        Expect(r != null && r.SequenceEqual(Expected(0, 2500)), "이어붙인 내용이 PLC 메모리와 다름 (쓰기 위치 갱신 확인)");
    });

    Check("0이 아닌 시작 주소에서 분할 (word 100 부터 1500B)", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc).ReadChunked("D", 100, 1500);
        Expect(plc.Calls.Count == 2, $"PLC 호출 {plc.Calls.Count}번 (기대 2번)");
        Expect(plc.Calls[1] == ("D", 600, 500), $"2번째 = {plc.Calls[1]} (기대 (D, 600, 500))");
        Expect(r != null && r.SequenceEqual(Expected(100, 1500)), "내용 불일치");
    });

    Check("작은 한계값(maxWords=3)으로도 동작: 20B 시작 word 5", () =>
    {
        var plc = new FakePlc();
        var r = new ChunkedReader(plc, maxWordsPerRequest: 3).ReadChunked("D", 5, 20);
        // 한 번 최대 6B → 6,6,6,2 = 4번
        Expect(plc.Calls.Count == 4, $"PLC 호출 {plc.Calls.Count}번 (기대 4번)");
        Expect(r != null && r.SequenceEqual(Expected(5, 20)), "내용 불일치 (500 같은 숫자를 코드에 박아놓지 않았는지 확인)");
    });

    Check("2번째 청크에서 PLC null → 전체 null", () =>
    {
        var plc = new FakePlc { FailAtCall = 2 };
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 2500);
        Expect(r == null, $"결과 {Fmt(r)} (기대 null)");
    });

    Check("2번째 청크 응답이 1바이트 짧음 → 전체 null", () =>
    {
        var plc = new FakePlc { ShortAtCall = 2 };
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 2500);
        Expect(r == null, $"결과 {Fmt(r)} (기대 null)");
    });

    Check("단일 요청 경로에서 PLC null → null", () =>
    {
        var plc = new FakePlc { FailAtCall = 1 };
        var r = new ChunkedReader(plc).ReadChunked("D", 0, 100);
        Expect(r == null, $"결과 {Fmt(r)} (기대 null)");
    });
});


// ===================================================== PART C
Section("C", "SegmentBuilder — 구간 병합", () =>
{
    Subscription W(int start, int len) => new($"W{start}", SubKind.Word, start, 0, len);

    string Dump(List<Segment> s) => "[" + string.Join(", ", s.Select(x => $"[{x.StartWord},{x.EndWord})")) + "]";

    void ExpectSegs(List<Segment> got, params (int s, int e)[] exp)
    {
        bool ok = got.Count == exp.Length && got.Zip(exp).All(p => p.First.StartWord == p.Second.s && p.First.EndWord == p.Second.e);
        Expect(ok, $"결과 {Dump(got)} (기대 [{string.Join(", ", exp.Select(x => $"[{x.s},{x.e})"))}])");
    }

    Check("빈 입력 → 빈 리스트", () =>
        ExpectSegs(SegmentBuilder.Build(Array.Empty<Subscription>(), 10)));

    Check("항목 1개 → 구간 1개", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(100, 4) }, 10), (100, 104)));

    Check("정렬 안 된 입력, 멀리 떨어진 2개 → 2구간(정렬됨)", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(200, 2), W(100, 2) }, 10), (100, 102), (200, 202)));

    Check("gap 8 (<= 10) → 병합", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(100, 2), W(110, 2) }, 10), (100, 112)));

    Check("gap 정확히 10 → 병합 (경계값)", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(100, 2), W(112, 2) }, 10), (100, 114)));

    Check("gap 11 → 분리 (경계값)", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(100, 2), W(113, 2) }, 10), (100, 102), (113, 115)));

    Check("다른 구간 안에 완전히 포함 → 끝이 줄어들면 안 됨", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(100, 20), W(105, 2) }, 10), (100, 120)));

    Check("Bit 항목: word 50, startBit 15, length 2 → 2워드", () =>
    {
        var sub = new Subscription("B", SubKind.Bit, 50, 15, 2);
        ExpectSegs(SegmentBuilder.Build(new[] { sub }, 10), (50, 52));
    });

    Check("연쇄 병합 + 멀리 있는 것 분리", () =>
        ExpectSegs(SegmentBuilder.Build(new[] { W(118, 2), W(100, 2), W(200, 1), W(108, 2) }, 10),
            (100, 120), (200, 201)));
});


// ===================================================== PART D
Section("D", "PlcSnapshot — 합치기", () =>
{
    var subs = new List<Subscription>
    {
        new("RUN",   SubKind.Bit,   100, 0, 1),
        new("ALARM", SubKind.Bit,   100, 1, 1),
        new("COUNT", SubKind.Word,  101, 0, 1),
        new("CARID", SubKind.Ascii, 102, 0, 4),
        new("FAR",   SubKind.Word,  900, 0, 1),
    };

    FakePlc Prepare()
    {
        var plc = new FakePlc();
        plc.SetWord("D", 100, 0x0005);          // bit0 = 1, bit1 = 0, bit2 = 1
        plc.SetWord("D", 101, 1234);
        plc.SetAscii("D", 102, "ABCDEFGH");     // 4워드 = 8문자
        plc.SetWord("D", 900, 77);
        return plc;
    }

    Check("값 추출 5개 + PLC 호출 2번(구간 2개)", () =>
    {
        var plc = Prepare();
        var res = PlcSnapshot.ReadAll(new ChunkedReader(plc), "D", subs, 10);
        Expect(plc.Calls.Count == 2, $"PLC 호출 {plc.Calls.Count}번 (기대 2번: [100,106) 과 [900,901))");
        Expect(res.GetValueOrDefault("RUN") == "1", $"RUN = {res.GetValueOrDefault("RUN") ?? "없음"} (기대 1)");
        Expect(res.GetValueOrDefault("ALARM") == "0", $"ALARM = {res.GetValueOrDefault("ALARM") ?? "없음"} (기대 0)");
        Expect(res.GetValueOrDefault("COUNT") == "1234", $"COUNT = {res.GetValueOrDefault("COUNT") ?? "없음"} (기대 1234)");
        Expect(res.GetValueOrDefault("CARID") == "ABCDEFGH", $"CARID = {res.GetValueOrDefault("CARID") ?? "없음"} (기대 ABCDEFGH)");
        Expect(res.GetValueOrDefault("FAR") == "77", $"FAR = {res.GetValueOrDefault("FAR") ?? "없음"} (기대 77)");
    });

    Check("2번째 구간 읽기 실패 → 예외 없이, FAR 만 빠짐", () =>
    {
        var plc = Prepare();
        plc.FailAtCall = 2;
        var res = PlcSnapshot.ReadAll(new ChunkedReader(plc), "D", subs, 10);
        Expect(res.Count == 4, $"결과 항목 {res.Count}개 (기대 4개)");
        Expect(!res.ContainsKey("FAR"), "FAR 는 결과에 없어야 함");
        Expect(res.GetValueOrDefault("COUNT") == "1234", "첫 구간 항목은 정상이어야 함");
    });

    Check("1번째 구간 읽기 실패 → 그 구간 항목 전부 빠지고 FAR 만 남음", () =>
    {
        var plc = Prepare();
        plc.FailAtCall = 1;
        var res = PlcSnapshot.ReadAll(new ChunkedReader(plc), "D", subs, 10);
        Expect(res.Count == 1 && res.GetValueOrDefault("FAR") == "77", $"결과 항목 {res.Count}개, 키: {string.Join(",", res.Keys)} (기대 FAR 하나)");
    });
});


Console.WriteLine($"\n===== PASS {pass}   TODO {todo}   FAIL {fail} =====");
