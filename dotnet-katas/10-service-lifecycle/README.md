# Kata 10 — 서비스 켜고 끄기 (ThreadStart / ThreadStop / SchedulerLogic)

kata 01(자기 재예약 Timer)을 **울산 코드의 실제 모양**으로 키운 버전입니다.
`RFIDControllService`, `ContainerMoveService` 등 모든 서비스가 이 구조예요.

```
ThreadStart : 타이머를 만들어 20ms 후 "한 번만" 실행 + 멈춤 스위치 OFF
SchedulerLogic (한 바퀴) : ① 타이머 일시정지 → ② 일 시키기 → ③ finally 에서 재예약 / 정리 결정
ThreadStop  : 멈춤 스위치 ON → 한 바퀴가 끝나 타이머가 정리되길 (최대 N ms) 기다림 → 안 되면 강제 정리
```

| 원본 | `ECS.RFIDControll.Service/RFIDControllService.cs` 의 `ThreadStart` / `ThreadStop` / `SchedulerLogic` |
|---|---|
| 이미 아는 것 | kata 01, 이 구조를 직접 한 줄씩 해석해 본 경험 |
| 새로 배우는 것 | 멈춤 스위치(volatile), `lock` + `null` / `ObjectDisposedException` 방어, `Dispose(waitHandle)`, "끝나면 재예약" 이 곧 종료의 문 |

## 파일

| 파일 | 역할 |
|---|---|
| `TimerService.cs` | **TODO 1~3** |
| `Program.cs` | 자동 채점기 (수정 X, 타이밍 테스트라 8초쯤) |

## 실행

```
dotnet run
dotnet run -- A
```

> ⚠️ TODO 1(ThreadStart)만 채우고 TODO 3(SchedulerLogic)을 안 채우면 타이머 콜백에서 예외가 나서 프로세스가 종료될 수 있어요.
> 권장 순서는 **TODO 3 → TODO 1 → TODO 2** 입니다.

## 연습 순서

1. `NOTES.md` 3줄 설명 (특히 "종료가 일어나는 곳은 어디인가?")
2. TODO 3 → 1 → 2 순서로 채우고 PART A, B, C 채점
3. 끝나면 원본 `RFIDControllService.cs` 의 같은 함수 세 개와 비교

## 생각해 볼 질문

- `ThreadStop` 에서 타이머를 바로 `Dispose` 하지 않고 스위치부터 켜는 이유는?
- `SchedulerLogic` 맨 앞에서 타이머를 일시정지하는 이유는? 안 하면 어떤 상황에서 겹칠까요?
- 한 바퀴가 `stopWaitMs` 보다 오래 걸리면 어떻게 되나요? (PART C)
- 원본에서는 일을 `Task.Run` 으로 따로 돌려서 `ThreadStop` 이 그 일의 끝을 기다리지 않습니다. 이 kata 는 일이 같은 스레드에서 도는 단순화예요. 차이가 어떤 결과를 낳을지 생각해 보세요.
