# NOTES — 코드 치기 전에 여기부터 채우기

> 규칙: **각 함수를 3줄로 설명 못 하면 아직 타이핑하지 않습니다.**
> 한국어로, 내 말로, 코드 용어 최소로. (README 의 "연습 순서" 1단계)

## TODO 1~4 (WordBit)

### GetBit
- 입력: 비트배열과 오프셋
- 출력: true / false
- 실패/경계: 7까지는 low byte, 8부터는 high byte

### WordsNeeded
- 입력: 워드 단위인지 (T/F), 시작 오프셋, 길이
- 출력: 필요한 워드 개수
- 실패/경계: 길이가 0이거나 비트총합이 16의 배수일 때

## TODO 5~7 (ChunkedReader.ReadChunked)
- 입력: 메모리 구역, 시작워드, 읽을 바이트 영역
- 출력: 읽은 바이트 배열
- 루프를 돌 때 매번 바뀌는 값 3개는? 시작워드, 값을 쓸 위치, 남은 바이트
- 실패하면: null 반환

## TODO 8~10 (SegmentBuilder.Build)
- 입력: 읽을 항목들(Subscription 목록), 병합해도 되는 최대 빈 워드 수(maxGapWords)
- 출력: 구간(Segment) 목록. 각 구간은 [StartWord, EndWord) 이고 시작 워드 순서로 정렬돼 있다
- 병합 판단 기준 (gap) 을 그림으로 그려보면: 다음.Start - 현재.End 가 maxGapWords 보다 작거나 같으면 병합
- 실패/경계:
  - gap 이 딱 maxGap 이면 병합, maxGap+1 이면 분리
  - 입력이 비어 있으면 빈 목록
  - 입력이 정렬돼 있지 않아도 StartWord 오름차순으로 먼저 정렬한다
  - 다른 구간 안에 들어가는 항목은 gap 이 음수 → 병합, 단 End 는 줄어들지 않게 큰 쪽으로 

## TODO 11~13 (PlcSnapshot.ReadAll)
- 입력:
- 출력:
- 한 구간이 실패하면:

---

## 막혔던 지점 기록 (나중에 가장 값진 자료)
- (날짜) 어디서 / 왜 막혔나 / 어떻게 풀었나
