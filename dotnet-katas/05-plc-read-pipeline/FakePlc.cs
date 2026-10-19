namespace PlcReadKata;

// ★ 이 파일은 수정하지 않습니다 (연습 대상 아님). 가짜 PLC 입니다.
//   실무의 _fEnetPlcClient.Request(FEnetReadContinuousRequest) 자리를 대신합니다.

public interface IPlcClient
{
    /// <summary>
    /// deviceCode(예: "D")의 startWord 워드 주소부터 byteCount 바이트를 연속으로 읽는다.
    /// 통신 실패 시 null.
    /// </summary>
    byte[]? Read(string deviceCode, int startWord, int byteCount);
}

public sealed class FakePlc : IPlcClient
{
    public const int MemoryWords = 2000;

    private readonly Dictionary<string, byte[]> _memory = new();

    /// <summary>Read 호출 기록 (몇 번, 어떤 구간으로 요청했는지 테스트가 검사한다)</summary>
    public List<(string Device, int StartWord, int Bytes)> Calls { get; } = new();

    /// <summary>n번째(1부터) Read 호출은 null 반환(통신 실패). 0이면 사용 안 함.</summary>
    public int FailAtCall { get; set; }

    /// <summary>n번째(1부터) Read 호출은 요청보다 1바이트 짧게 응답. 0이면 사용 안 함.</summary>
    public int ShortAtCall { get; set; }

    public FakePlc()
    {
        // "D" 디바이스 메모리: i번째 바이트 = i % 251  (구간이 맞게 이어붙었는지 눈으로 검증하기 좋은 패턴)
        var d = new byte[MemoryWords * 2];
        for (int i = 0; i < d.Length; i++) d[i] = (byte)(i % 251);
        _memory["D"] = d;
    }

    public byte[]? Read(string deviceCode, int startWord, int byteCount)
    {
        Calls.Add((deviceCode, startWord, byteCount));
        int callNo = Calls.Count;

        if (callNo == FailAtCall) return null;
        if (!_memory.TryGetValue(deviceCode, out var mem)) return null;

        int from = startWord * 2;
        if (from < 0 || from + byteCount > mem.Length) return null;

        int len = callNo == ShortAtCall ? byteCount - 1 : byteCount;
        var result = new byte[len];
        Array.Copy(mem, from, result, 0, len);
        return result;
    }

    // ---- 테스트용 메모리 세팅 도우미 ----

    public void SetWord(string device, int word, ushort value)
    {
        var mem = _memory[device];
        mem[word * 2] = (byte)(value & 0xFF);       // low byte 먼저
        mem[word * 2 + 1] = (byte)(value >> 8);
    }

    public void SetAscii(string device, int word, string text)
    {
        var mem = _memory[device];
        for (int i = 0; i < text.Length; i++) mem[word * 2 + i] = (byte)text[i];
    }
}
