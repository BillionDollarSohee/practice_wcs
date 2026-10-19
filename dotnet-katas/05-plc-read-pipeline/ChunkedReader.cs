namespace PlcReadKata;

// PART B — 청크 분할 읽기
//
// FEnet 모듈은 한 번에 읽을 수 있는 양이 정해져 있습니다 (실무: 500 word = 1000 byte).
// 그보다 많이 읽어야 하면 "나눠서 여러 번 읽고, 결과를 이어붙여서" 한 덩어리처럼 돌려줘야 합니다.
//
// 실무 참고: FEnetPlcInterfaceService.ReadPlcContinuousChunked  (구조만 참고, 베끼지 말 것)

public sealed class ChunkedReader
{
    public const int WordBytes = 2;

    private readonly IPlcClient _plc;
    private readonly int _maxWordsPerRequest;

    public ChunkedReader(IPlcClient plc, int maxWordsPerRequest = 500)
    {
        _plc = plc;
        _maxWordsPerRequest = maxWordsPerRequest;
    }

    /// <summary>
    /// deviceCode 의 startWord 부터 totalBytes 바이트를 읽어서 하나의 byte[] 로 돌려준다.
    ///
    /// 규칙
    ///   1. totalBytes <= 0 이면 PLC 호출 없이 빈 배열 반환
    ///   2. totalBytes 가 maxWordsPerRequest*2 바이트 이하면 PLC 를 정확히 1번만 호출하고 그 응답을 그대로 반환
    ///   3. 초과하면 maxWordsPerRequest 워드씩 잘라서 여러 번 호출, 순서대로 이어붙여 반환
    ///        예) max=500워드, totalBytes=2500, startWord=0  →  호출 3번
    ///            (word 0, 1000B) → (word 500, 1000B) → (word 1000, 500B)
    ///   4. 어느 한 번이라도 PLC 가 null 을 주면 → 전체 null 반환 (일부만 채워진 결과를 돌려주면 안 됨)
    ///   5. 응답 길이가 요청한 길이와 다르면 → 전체 null 반환
    /// </summary>
    public byte[]? ReadChunked(string deviceCode, int startWord, int totalBytes)
    {
        // TODO 5: 규칙 1, 2 (조기 반환)
        if (totalBytes <= 0)
        {
            return Array.Empty<byte>();
        }

        if (totalBytes <= _maxWordsPerRequest * 2)
        {  
            return _plc.Read(deviceCode, startWord, totalBytes);
        }

        // TODO 6: 결과를 담을 byte[totalBytes] 준비
        byte[] result = new byte[totalBytes];

        // TODO 7: while (남은 바이트 > 0) 루프
        //         - 이번 청크 크기 = Math.Min(남은 바이트, 한 번 최대 바이트)
        //         - _plc.Read(deviceCode, 현재 시작 워드, 청크 크기)
        //         - 규칙 4, 5 검사 → 실패면 return null
        //         - Array.Copy 로 result 의 알맞은 위치에 붙이기
        //         - 다음 청크를 위해 "현재 시작 워드 / 쓰기 위치 / 남은 바이트" 세 개를 모두 갱신
        //           (워드 단위와 바이트 단위가 섞이는 곳 — 여기서 버그가 제일 많이 납니다)
        // (선택) 실무는 두 번째 청크부터 호출 사이에 Thread.Sleep 을 넣는다. 이 kata 에서는 생략.
        int remainingBytes = totalBytes;
        int currentWord = startWord;
        int writePosition = 0;

        while (remainingBytes > 0)
        { 
            int chunkSize = Math.Min(remainingBytes, _maxWordsPerRequest * 2);
            byte[]? chunk = _plc.Read(deviceCode, currentWord, chunkSize);

            // 규칙 4 검사
            if (chunk == null)
            {
                return null;
            }

            // 규칙 5 검사
            if (chunk.Length != chunkSize)
            {
                return null;
            }

            Array.Copy(chunk, 0, result, writePosition, chunkSize);
            remainingBytes -= chunkSize;
            currentWord += chunkSize / 2; // 워드 단위로 이동
            writePosition += chunkSize;
        }
        return result;
    }
}
