namespace PlcReadKata;

// PART C — 읽기 구간 병합
//
// 장비마다 "워드 100번 비트 3", "워드 104번부터 4워드", "워드 900번" 처럼 읽을 항목(구독)이 흩어져 있습니다.
// 항목마다 PLC 를 호출하면 너무 느리고, 전체를 통째로 읽으면 쓸데없이 큽니다.
// 그래서 "가까운 항목끼리는 한 번에, 멀리 떨어진 건 따로" 읽도록 구간(Segment)으로 묶습니다.
//
// 실무 참고: FEnetPlcInterfaceService.BuildOneCakeReadSegments 의 앞부분 (spans 계산 + 병합 foreach)

public enum SubKind { Bit, Word, Ascii }

/// <summary>
/// 읽을 항목 하나.
///   Bit   : StartWord 의 StartBit 부터 Length 비트   (Length 는 "비트 수")
///   Word  : StartWord 부터 Length 워드               (Length 는 "워드 수")
///   Ascii : StartWord 부터 Length 워드를 문자열로    (Length 는 "워드 수")
/// </summary>
public sealed record Subscription(string Name, SubKind Kind, int StartWord, int StartBit, int Length);

public sealed class Segment
{
    public int StartWord { get; set; }
    public int EndWord { get; set; }      // 끝 워드 (포함하지 않음).  [StartWord, EndWord)
    public byte[]? Data { get; set; }     // 읽은 결과 (PART D 에서 채움). 읽기 실패 시 null
}

public static class SegmentBuilder
{
    /// <summary>
    /// 구독 목록을 읽기 구간 목록으로 묶는다.
    ///
    /// 규칙
    ///   1. 각 항목의 구간 = [StartWord, StartWord + 필요한 워드 수)   (필요한 워드 수는 WordBit.WordsNeeded 재사용)
    ///        - Kind 가 Bit 이면 useWordLength = false, 아니면 true
    ///   2. 입력은 정렬되어 있지 않다 → StartWord 오름차순으로 정렬 후 처리
    ///   3. 앞 구간 끝과 다음 구간 시작 사이의 빈 워드 수 (= 다음.Start - 현재.End) 가 maxGapWords 이하면 병합,
    ///      초과하면 새 구간으로 분리
    ///        - gap 이 정확히 maxGapWords 면 "병합"
    ///        - 겹치거나 다른 구간 안에 완전히 포함되는 항목은 gap 이 음수 → 병합. 이때 끝은 줄어들면 안 됨 (큰 쪽 유지)
    ///   4. 입력이 비어있으면 빈 리스트
    /// </summary>
    public static List<Segment> Build(IEnumerable<Subscription> subs, int maxGapWords)
    {
        // TODO 8: 각 구독을 (Start, End) 로 변환해서 Start 기준 정렬  (LINQ Select / OrderBy, 또는 직접 반복문)
        // TODO 9: 현재 구간(current)을 들고 가며 foreach — 병합할지 / 끊고 새로 시작할지 결정
        // TODO 10: 루프 끝난 뒤 마지막 current 를 리스트에 추가하는 것 잊지 말기 (실무 코드에서도 foreach 밖에 있는 부분)
        throw new NotImplementedException("TODO 8~10: SegmentBuilder.Build");
    }
}
