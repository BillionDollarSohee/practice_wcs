# Kata 11 — 고정길이 파서 (RFID 40글자)

대차 RFID 태그의 40글자 문자열을 해석하고 만드는 연습입니다. 울산 WCS 의 입고/출고/비전 로직이 모두 이 값을 기준으로 판단해요.

```
 0~ 9 차체번호 │10~13 차종 │14~15 라인 │16~17 라인코드 │18~21 서열번호 │22~25 대차번호 │26~27 라인 │28~39 옵션
 └──────────── 스키드ID (18) ───────────┘ └ 서열정보 22글자 ┘
```

| 원본 | `WCS.Common/SystemInterfaces/ECS/RFIDControll/Models/ParsedCartInfo.cs` |
|---|---|
| 이미 아는 것 | kata 04(DB 파싱), kata 05 PART A(`Substring`/`Trim`/패딩) |
| 새로 배우는 것 | 자리수 고정 포맷의 "읽기 ↔ 만들기" 왕복, `PadRight` / `PadLeft`, 경계(짧은 입력/빈 값), 서열정보만 지우기 |

## 파일

| 파일 | 역할 |
|---|---|
| `ParsedCartInfo.cs` | **TODO 1~5** |
| `Program.cs` | 자동 채점기 (수정 X) |

## 실행

```
dotnet run
dotnet run -- A
```

## 연습 순서

1. `NOTES.md` — **종이에 40칸 표를 직접 그려 보기** (어느 칸이 몇 글자인지 말로 못 하면 코드를 치지 않기)
2. TODO 3(Parse) → PART A → TODO 1, 2 → PART B → TODO 4, 5 → PART C
3. 끝나면 원본 `ParsedCartInfo.cs` 와 비교. 특히 `CartId` 를 만드는 부분

## 생각해 볼 질문

- `ClearSeqInfo` 가 전체를 지우지 않고 **앞 22글자만** 지우는 이유는? (대차번호가 지워지면 RFID 만 보고 어느 대차인지 알 수 없어요 — 입고 사고의 원인)
- 길이가 40이 안 되는 값이 읽혀 오면 원본의 입고 로직은 어떻게 처리하나요? (`RFID_EQP_DETAIL.DEFAULT_LENGTH_BYTE` 를 검색해 보세요)
- 읽기와 만들기 규칙이 서로 어긋나면 어떤 테스트가 먼저 실패할까요?
