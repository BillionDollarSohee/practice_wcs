# Kata 9 — 알람 서비스 + 트랜잭션 템플릿

알람 업무 자체는 단순하지만, 이 kata 의 진짜 목적은 **울산 코드의 Service 가 DB 를 쓰는 "모양"을 손에 익히는 것**입니다.

```
BeginTransaction → Provider 호출들 → CommitTransaction
                         └ 예외 → RollbackTransaction → 삼키지 않고 다시 던진다
```

| 원본 | `WCS.Common/BaseServices/AlarmManagement/AlarmManagementService.cs`, `src/WCS.Common/ProviderMethodTemplate.txt` |
|---|---|
| 새로 배우는 것 | Service/Provider 역할 분리, 멱등한 Start/End (이미 있으면 무시), 롤백 후 `throw`, 예외 감싸기 |

## 파일

| 파일 | 역할 |
|---|---|
| `AlarmSupport.cs`, `AlarmProvider.cs` | 주어지는 부품 (수정 X). 가짜 DB 연결 + 알람 표 |
| `AlarmService.cs` | **TODO 1, 2** |
| `Program.cs` | 자동 채점기 (수정 X) |

## 실행

```
dotnet run
dotnet run -- A      # PART A 만
```

## 연습 순서

1. `NOTES.md` 3줄 설명
2. TODO 1 → PART A → TODO 2 → PART B → (PART C 는 두 함수의 예외 처리를 확인)
3. 두 함수가 똑같은 모양이 되면, 그 반복이 **울산 코드에서 수백 번 나오는 모양**입니다. 일부러 복사하지 말고 직접 타이핑하세요.

## 생각해 볼 질문

- 이미 알람이 있어도 `Start` 가 트랜잭션을 열고 닫는 이유는? (일관된 모양 / 읽기도 트랜잭션 안에서)
- `catch` 에서 예외를 삼키고 `return 0` 하면 호출한 쪽에서는 무슨 일이 생기나요?
- 원본의 설비 알람 `SaveWcsEquipmentAlarmStart` 는 알람이 **이미 있으면 종료**시킵니다. 서비스 알람과 다른 이 동작은 의도일까요? 원본을 읽고 본인 의견을 NOTES 에 적어 보세요.
