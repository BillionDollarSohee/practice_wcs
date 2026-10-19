# Kata 6 — ExecutionAgent (스케줄러 폴링 실행기)

울산 WCS 의 모든 서비스가 스케줄러 함수를 돌릴 때 거치는 **"실행해도 되는지 확인하고 돌려주는 심부름꾼"** 입니다.

```
서비스 타이머 루프 (20ms 마다 가볍게)
   └ ExecuteLogicByPollingTime("RFID", "SchedulerA", SchedulerA, ...)   ← 6개를 줄줄이 호출
         └ 조건이 맞으면 Task.Run 으로 따로 실행 (타이머 루프는 기다리지 않음)
```

| 원본 | `WCS.Common/01_BaseUtilities/ExecutionAgent.cs`, `models/LogicPolling.cs` |
|---|---|
| 이미 아는 것 | kata 01(끝난 뒤 다시 예약), kata 02(Task.Run) |
| 새로 배우는 것 | `IsLogicBusy` + `Stopwatch` 로 로직마다 "끝난 뒤 주기 대기", 주기를 DB 설정으로 두기, 기본값 자동 등록 |

## 파일

| 파일 | 역할 |
|---|---|
| `LogicPolling.cs`, `SchedulerSettings.cs` | 주어지는 부품 (수정 X) |
| `ExecutionAgent.cs` | **TODO 1~5** — 여기를 채웁니다 |
| `Program.cs` | 자동 채점기 (수정 X) |
| `NOTES.md` | 코드 치기 전에 3줄 설명부터 |

## 실행

```
dotnet run            # 전체 채점 (타이밍 테스트라 10초쯤)
dotnet run -- A       # PART A 만
```

## 연습 순서

1. **말로 설명** — `NOTES.md` 의 칸을 먼저 채운다. 코드 금지.
2. **TODO 1 → PART A 채점 → TODO 2~4 → PART B 채점 → TODO 5 → PART C 채점**
3. 막히면 *막힌 위치*를 NOTES 하단에 기록하고, 시도한 코드를 가져오세요.
4. 다 통과하면 원본 `ExecutionAgent.cs` 를 열어 **차이점**을 NOTES 에 적으세요.
   (원본은 오버로드 두 개가 거의 똑같은 코드를 반복합니다 — 내 코드는 어떻게 줄였나요?)

## 핵심 질문 (직접 답해 보기)

- 왜 `Task.Run` 으로 실행하고 기다리지 않나요? 기다리면 타이머 루프에서 무슨 일이 생기나요?
- `IsLogicBusy` 를 `finally` 에서 풀지 않고 정상 종료 직전에만 풀면 어떤 사고가 날까요? (PART B 의 예외 테스트)
- 여러 스레드가 동시에 `_pollingList` 를 만지는데, 어디에 `lock` 이 필요한가요?
