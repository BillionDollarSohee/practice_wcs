# dotnet-katas 로드맵

울산 WCS 코드를 "읽고 고칠 수 있는" 수준까지 가는 연습 순서입니다. 각 kata 는 **작은 개념 하나 + 자동 채점기**로 되어 있어요.

```
기초        00 ─ 01 ─ 02 ─ 03 ─ 04
PLC/설비    05 ─ 11                       (바이트·워드·비트 / 고정길이 문자열)
서비스 뼈대  06 ─ 10                       (폴링 실행기 / 켜고 끄기)
설비 데이터  07 ─ 08 ─ 09                  (태그 표 / 명령 큐 / 알람 + 트랜잭션)
통합        wcs-twin-core MSP 변환          (wcs-twin-core/MSP_변환_가이드.md)
```

| # | 폴더 | 주제 | 울산 원본 | 선행 |
|---|---|---|---|---|
| 00 | `00-basics-lessons` | C# 기초 연습장 | — | |
| 01 | `01-timer-pattern` | 자기 재예약 Timer | `SorketLogic` | |
| 02 | `02-async-worker-pattern` | 큐 + Task 워커 | `VisionImageMigrationService` | 01 |
| 03 | `03-tcp-echo` | TCP, STX/ETX 프레임 | `SocketVisionInterfaceService` | |
| 04 | `04-db-parsing-pipeline` | DB 폴링 + 트랜잭션 | 서열 파싱 | 02 |
| 05 | `05-plc-read-pipeline` | 워드/비트, 청크 읽기, 구간 병합 | `FEnetPlcInterfaceService` | |
| 06 | `06-execution-agent` | 폴링 실행기 (`IsLogicBusy`+Stopwatch+Task.Run) | `ExecutionAgent` | 01, 02 |
| 07 | `07-equipment-status` | 설비 상태 표 (태그 → 값, R/W/C) | `EquipmentService` | 05 |
| 08 | `08-command-queue` | 명령 큐 (WAIT→쓰기→COMPLETE) | `ContainerMoveService`, `WriteInstuctionToPLC` | 07 |
| 09 | `09-alarm-service` | 알람 Start/End + 트랜잭션 템플릿 | `AlarmManagementService` | 04 |
| 10 | `10-service-lifecycle` | ThreadStart/ThreadStop/SchedulerLogic | `RFIDControllService` | 01, 06 |
| 11 | `11-fixed-length-parser` | RFID 40글자 읽기/만들기 | `ParsedCartInfo` | 05 |

## 권장 순서

1. **05** 마저 (PART C, D)
2. **11** — 바이트/문자열을 다루는 감각을 이어서 (짧음)
3. **06 → 10** — 서비스 뼈대. 이미 `SchedulerLogic` 을 한 줄씩 읽어 봤으니 연결이 쉽습니다
4. **07 → 08 → 09** — 설비 데이터. 07 의 표 위에 08, 09 가 올라가요
5. **wcs-twin-core 변환** — 위에서 만든 부품으로 Step 7 까지

## 모든 kata 공통 규칙

1. **`NOTES.md` 에 3줄 설명을 먼저** — 설명 못 하면 아직 타이핑하지 않는다
2. 원본 코드·인터넷 없이 구현 → 채점(`dotnet run -- A` 처럼 PART 별로)
3. `FAIL` 메시지를 읽고 **코드를 보기 전에 이유를 먼저 추측**
4. 다 끝나면 울산 원본과 비교해서 차이를 NOTES 에 기록
5. 며칠 뒤, 한 함수를 지우고 **자동완성 없이** 백지에서 다시 쓰기 (이게 손코딩 훈련의 본체)

## 채점기 읽는 법

`PASS` 통과 / `TODO` 아직 안 채움 / `FAIL` 틀림 (`→` 뒤에 이유). 타이밍 테스트(06, 10)는 10초쯤 걸립니다.
