# Kata 1 — Self-Rescheduling Timer Pattern

실무 WCS 코드(`SocketVisionInterfaceService.SorketLogic`, `VisionImageMigrationService.SchedulerLogic`)가
공통으로 쓰는 "타이머 콜백이 끝나면 스스로 다음 실행을 예약하는" 패턴을 최소 단위로 연습합니다.

## 실행 방법

```
dotnet run
```

## 완성 조건

1. 시작 후 약 1초 간격으로 `Tick 1`, `Tick 2`, ... 출력
2. 아무 키나 누르면 `Stopping...` 출력 후, 진행 중이던 콜백이 있다면 그게 끝난 뒤 안전하게 종료
3. 타이머 필드 접근은 `lock`으로 보호
4. 종료 시 `Timer` 리소스 `Dispose`

## Program.cs 안의 TODO 1~8을 순서대로 채우면 됩니다

빌드는 지금도 됩니다 (TODO 자리에 `throw new NotImplementedException()`을 넣어둬서),
실행하면 어느 TODO부터 막히는지 바로 확인할 수 있습니다.

## 막히면

- `Timer.Change(dueTime, period)`의 두 인자가 각각 뭘 뜻하는지부터 다시 확인해보세요.
- "왜 period를 안 쓰고 매번 Change를 다시 호출할까?"에 스스로 답할 수 있으면 이 kata의 절반은 이해한 겁니다.
- 그래도 막히면, 실제 코드(`SocketVisionInterfaceService.cs`의 `SorketLogic`/`ThreadStart`/`ThreadStop`)를
  베끼지 말고 "구조만" 참고하세요.
