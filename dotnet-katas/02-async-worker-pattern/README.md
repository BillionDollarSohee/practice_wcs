# Kata 2 — ConcurrentQueue + Fixed Worker Task Pattern

실무 WCS 코드(`VisionImageMigrationService.SchedulerVisionImageMigration` / `ProcessWorker`)가 쓰는
"작업을 큐에 담고, 고정된 개수의 워커 Task가 병렬로 큐를 비우는" 패턴을 최소 단위로 연습합니다.

## 실행 방법

```
dotnet run
```

## 완성 조건

1. 워커 3개가 "동시에" 돌면서 큐를 나눠 처리 (로그의 Worker 번호가 뒤섞여 찍혀야 함)
2. 3초 뒤 자동 취소되면, 워커들은 처리 중이던 것만 마무리하고 더 이상 큐에서 안 꺼냄
3. 전체 종료 후 "몇 개 처리했는지" 집계 출력
4. `Task.WhenAll`로 전체 워커 완료를 기다림 (fire-and-forget 금지)

## Program.cs 안의 TODO 0~6을 순서대로 채우면 됩니다

## 생각해볼 것

- `queue.TryDequeue`를 여러 워커가 동시에 호출해도 안전한 이유가 뭘까요? (`ConcurrentQueue`가 왜 필요한지)
- `_processedCount++`를 그냥 쓰면 왜 위험할까요? (`Interlocked.Increment`가 필요한 이유)
- `Task.Delay(ms, token)`에 취소 토큰을 넘기면 취소 시 어떤 예외가 나는지, 그걸 잡아서 뭘 해야 할지
- 막히면, 실제 코드(`VisionImageMigrationService.cs`의 `ProcessWorker`)를 베끼지 말고 "구조만" 참고하세요.
