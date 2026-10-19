# Kata 7 — 설비 상태 표 (태그 이름 → 값)

울산 WCS 가 설비(PLC, RFID, 비전)의 현재 상태를 보관하는 방식입니다. **모든 로직이 이 표를 통해 서로 대화합니다.**

```
CV.TR.01 | PLC_R_CART_PRESENT | "1" | SaveDttm ...     ← PLC 가 올려준 값 (R)
CV.TR.01 | PLC_W_COMMAND      | "13"                    ← WCS 가 PLC 에 쓰는 값 (W)
CV.TR.01 | PLC_C_RFID_DATA    | "7C  000906..."         ← WCS 내부 공유값 (C, PLC 주소 없음)
```

| 원본 | `WCS.Common/SystemInterfaces/Equipment/EquipmentService.cs`, `EquipmentMySqlProvider` |
|---|---|
| 새로 배우는 것 | "이름으로 읽고 쓰기", `SaveStatus` vs `SaveStatusCheckExists`, `instNo` 규칙, `SaveDttm` 은 **저장할 때마다** 갱신, R/W/C 접두어 |

## 꼭 알아둘 사실 (코드로 확인된 것)

- `SaveStatusCheckExists` 의 "CheckExists" 는 **"행이 없으면 INSERT"** 입니다. 값이 바뀔 때만 저장하는 게 아니에요.
  같은 값을 또 저장해도 `SaveDttm` 이 갱신되고 이력이 1건 쌓입니다.
- 그래서 울산 입고 정합성의 "PLC_C_RFID_DATA 저장 후 5초" 는 **"값이 바뀐 지"가 아니라 "마지막으로 저장된 지"** 입니다.

## 파일

| 파일 | 역할 |
|---|---|
| `Clock.cs`, `EquipmentModels.cs`, `EquipmentProvider.cs` | 주어지는 부품 (수정 X). Provider 는 DB 대신 메모리 |
| `EquipmentService.cs` | **TODO 1~6** |
| `Program.cs` | 자동 채점기 (수정 X) |

## 실행

```
dotnet run
dotnet run -- A      # PART A 만
```

## 연습 순서

1. `NOTES.md` 3줄 설명
2. TODO 1,2 → PART A → TODO (instNo 규칙은 Provider 가 처리, PART B 로 확인) → TODO 3~6 → PART C
3. 끝나면 원본 `EquipmentService.cs` 의 `SaveEquipmentDetailStatusCheckExists` 와 비교

## 생각해 볼 질문

- `Provider` 와 `Service` 를 나눈 이유는? Service 에서 `provider.UpdateStatus` 의 반환값(0/1)을 쓰나요, 안 쓰나요? 왜 안 써도 되나요?
- `GetValue(eqp, tag, "0")` 의 기본값 `"0"` 이 의미하는 안전 방향은? (값이 없으면 "감지 안 됨" 으로 본다는 뜻)
