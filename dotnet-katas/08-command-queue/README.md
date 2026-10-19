# Kata 8 — 명령 큐 (WAIT → PLC 쓰기 → COMPLETE)

울산 WCS 는 PLC 에 **직접 쓰지 않습니다.** 명령을 표에 `WAIT` 로 넣어 두면 PLC 서비스가 꺼내서 씁니다.

```
[판단 서비스]  CanIssue? → Enqueue(eqp, {PLC_W_COMMAND:13})            명령 표에 WAIT 로 INSERT
[PLC 서비스]   ProcessWaiting(eqp) → 태그를 PLC 에 WRITE (CMD_SEQ 는 맨 마지막) → COMPLETE → 이력 + 삭제
```

| 원본 | `ContainerMoveService.checkPlcCommandPossible`, `EquipmentService.SaveEquipmentCommandIfStatus`, `FEnetPlcInterfaceService.WriteInstuctionToPLC` |
|---|---|
| 새로 배우는 것 | 서비스끼리 직접 호출하지 않고 **표를 통해 대화**, 명령 등록 전 `CanIssue` 확인, `PLC_W_CMD_SEQ` 를 마지막에 쓰는 이유, 끝난 명령은 이력 + 삭제 |

## 파일

| 파일 | 역할 |
|---|---|
| `CommandModels.cs`, `CommandProvider.cs` | 주어지는 부품 (수정 X). 명령 표와 PLC 를 메모리로 대신 |
| `CommandService.cs` | **TODO 1~4** |
| `Program.cs` | 자동 채점기 (수정 X) |

## 실행

```
dotnet run
dotnet run -- A      # PART A 만
```

## 연습 순서

1. `NOTES.md` 3줄 설명
2. TODO 1,2 → PART A → TODO 4(Finish) → TODO 3(ProcessWaiting) → PART B, C
   (ProcessWaiting 이 Finish 를 부르니 Finish 를 먼저 만드는 게 편합니다)
3. 원본 `WriteInstuctionToPLC` 를 열어서 내 코드와 비교 (원본에는 CMD_SEQ 를 쓰기 전 1초 지연, CV 이동 시각 기록 같은 추가 규칙이 있어요)

## 생각해 볼 질문

- `CanIssue` 의 두 조건("대기 명령 있음" / "PLC 가 처리 중") 중 하나만 있으면 어떤 사고가 날까요?
- 왜 `PLC_W_CMD_SEQ` 를 맨 마지막에 쓸까요? (PLC 가 이 값이 바뀌는 것을 "명령 도착" 신호로 보기 때문)
- 끝난 명령을 삭제하지 않고 상태만 바꿔서 남겨두면 어떤 문제가 생길까요?
