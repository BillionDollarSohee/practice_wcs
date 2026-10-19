namespace PlcReadKata;

// PART D — 합치기 (A + B + C 를 이어서 "장비 한 대 분량 읽기" 완성)
//
// 구독 목록 → 구간으로 병합(C) → 구간별 읽기(B, 필요하면 청크 분할) → 각 항목 값 추출(A)
//
// 실무 참고: OneCakeRead 가 BuildOneCakeReadSegments 로 구간을 읽고,
//           그 ReadData / ReadDataBitArray 에서 각 디테일의 값을 꺼내 DB 에 반영하는 흐름의 축소판.

public static class PlcSnapshot
{
    /// <summary>
    /// 구독 항목들을 PLC 에서 읽어 { 항목 이름 → 문자열 값 } 으로 돌려준다.
    ///
    /// 규칙
    ///   1. SegmentBuilder.Build 로 구간을 만들고, 구간마다 reader.ReadChunked 로 읽어 segment.Data 에 저장
    ///      (구간은 StartWord 순으로 읽는다)
    ///   2. 구간 읽기가 실패(null)해도 예외를 던지지 말 것. 그 구간에 속한 항목은 결과에서 "빠진다".
    ///      다른 구간의 항목은 정상적으로 결과에 들어간다.
    ///   3. 값 추출 (항목이 속한 구간의 Data 기준, 오프셋 = 항목.StartWord - 구간.StartWord)
    ///        Bit   → "1" 또는 "0"   (StartBit 한 비트만 본다)
    ///        Word  → 첫 워드의 숫자 문자열  (예: "1234")
    ///        Ascii → WordBit.GetAscii(Length 워드)
    ///   4. 항목이 어느 구간에 속하는지는 "구간.Start <= 항목.StartWord" 이고 "항목 끝 <= 구간.End" 로 찾는다
    /// </summary>
    public static Dictionary<string, string> ReadAll(
        ChunkedReader reader, string deviceCode, IReadOnlyList<Subscription> subs, int maxGapWords)
    {
        // TODO 11: 구간 만들고 → 구간별로 읽어서 Data 채우기 (+ 실패 시 로그/무시)
        // TODO 12: 각 구독에 대해 자기 구간 찾기 → 데이터 없으면 건너뜀
        // TODO 13: Kind 에 따라 값 추출해서 결과 Dictionary 에 넣기
        throw new NotImplementedException("TODO 11~13: PlcSnapshot.ReadAll");
    }
}
