# twin-core → Manager / Service / Provider 변환 가이드

울산 WCS 의 계층 구조(Manager → Service → Provider)를 이 연습 프로젝트에 적용하는 계획입니다.
**기존 동작 코드는 그대로 두었고**, 새로 만든 건 "연결 안 된 빈 틀(스켈레톤)"뿐입니다. 변환은 직접 합니다.

## 1. 지금 구조 vs 목표 구조

```
[지금]
Worker ─ Manager(켜고 끄기) ─ 소켓 수신 ─ Handler ──────────────┐
                                           ├ 프로토콜 파싱/응답    │  전부 한 클래스 안에
                                           ├ 업무 흐름(검증/순서)   │
                                           └ _context.XXX.Add(); SaveChanges();  ← EF 를 직접 사용
RFIDControllService ─ 재시도/알람 로직 + _context 직접 사용

[목표]
Worker ─ Manager(켜고 끄기만)
            └ 소켓 수신 ─ Handler (프레임 파싱 → Service 호출 → 응답 프레임 만들기)   ← 프로토콜만
                              └ Service (업무 흐름 + 트랜잭션 경계)                  ← 판단만
                                    └ Provider (EF 로 DB 읽고 쓰기)                  ← DB 만
```

| 계층 | 해야 할 일 | 하면 안 되는 일 |
|---|---|---|
| Manager | `Run()` / `Stop()`, 하위 서비스 생성·시작·종료 | 업무 판단, DB 접근 |
| Handler | 프레임 해석, 응답 프레임 조립 | DB 접근(`_context`), 업무 규칙 판단 |
| Service | 업무 순서, 검증, 트랜잭션 시작/커밋/롤백 | SQL/EF 쿼리 직접 작성, 프레임 조립 |
| Provider | 조회/저장 한 가지씩 | 업무 판단(`if (라인타입이 ...)`), 트랜잭션 결정 |

## 2. 새로 만든 스켈레톤 (아직 어디에도 연결 안 됨)

| 파일 | 역할 | 울산 대응 |
|---|---|---|
| `database/Providers/ITwinTransaction.cs` | 트랜잭션 시작/커밋/롤백 | `WCSMysqlServerDBClient` |
| `database/Providers/ICartProvider.cs` | 대차 마스터 조회 | `ContainerMoveProvider.SelectCartMaster` |
| `database/Providers/IEquipmentStatusProvider.cs` | 상태 이력 저장/조회 | `EquipmentProvider.InsertEquipmentDetailStatusHist` 자리 |
| `database/Providers/IVisionResultProvider.cs` | 비전 결과 저장 | 비전 결과 저장 Provider |
| `eis-socket-service/Services/VisionCartInfoService.cs` | Command 03 업무 흐름 | `VisionDataRequestHandler` 의 업무 부분 |
| `eis-socket-service/Services/VisionInspectionService.cs` | Command 11 업무 흐름 | `VisionProductCompleteHandler` 의 업무 부분 |

각 파일의 `TODO` 주석에 규칙이 적혀 있습니다. 빈 메서드는 `NotImplementedException` 을 던지니 **연결하기 전에 쓰면 바로 알 수 있어요.**

## 3. 단계별 계획 (한 단계씩, 매 단계 끝에 빌드 + 실행 확인)

### Step 0 — 기준선
- 현재 상태를 **커밋**해 둡니다. 변환은 "동작이 안 바뀌는 구조 변경(리팩터링)"이라서, 언제든 돌아올 지점이 필요해요.
- 시뮬레이터를 한 번 돌려서 정상 로그(대차 03 → RFID 읽기 → 11 → 출고)를 눈에 익혀 둡니다. 이 로그가 "변환 후에도 같아야 하는" 기준이에요.

### Step 1 — Provider + 트랜잭션 채우기
- 위 표의 `database/Providers/*` 4개 파일의 TODO 를 채웁니다.
- 완료 기준: `dotnet build` 통과. 아직 아무도 안 쓰니 동작 변화는 없음.

### Step 2 — RFIDControllService 를 Provider 로 전환
- `_context.EquipmentStatusHists.Add(...)` + `SaveChangesAsync` (`SaveStatusAsync`) → `IEquipmentStatusProvider.InsertHistAsync`
- 알람 해제 확인 `AnyAsync(...)` → `IEquipmentStatusProvider.ExistsHistSinceAsync`
- 생성자에서 `WcsTwinContext` 가 사라지는지 확인
- 완료 기준: RFID 재시도/알람 시나리오가 Step 0 과 같은 로그를 낸다

### Step 3 — Service 채우기
- `VisionCartInfoService`, `VisionInspectionService` 의 `ProcessAsync` 를 구현합니다 (기존 Handler 코드를 보고 옮기되, `_context` 대신 Provider).
- **질문 (직접 결정해 보세요)**
  1. Command 11 의 "결과 저장 + 이력 저장" 은 한 트랜잭션이어야 합니다. 그럼 RFID 쓰기(오래 걸리고 재시도/알람 대기까지 함)는 트랜잭션 **안**일까요, **밖**일까요? 안에 넣으면 무슨 일이 생길까요?
  2. 트랜잭션 도중 예외가 나면 롤백 후 예외를 다시 던질까요, 삼킬까요? (kata 09 의 규칙)
- 완료 기준: 두 Service 가 `_context` 없이 Provider/Transaction/RFID 서비스만 사용

### Step 4 — Handler 슬림화
- `VisionCartInfoRequestHandler`, `VisionProductCompleteHandler` 에서 `WcsTwinContext` 와 업무 로직을 걷어내고
  `프레임 파싱 → Service.ProcessAsync → 결과로 응답 프레임 조립` 만 남깁니다.
- 완료 기준: Handler 파일 맨 위에서 `using Database;` 가 사라진다 (DB 를 모르는 클래스가 됨)

### Step 5 — DI 등록 (`eis-socket-service/Program.cs`)
- `ITwinTransaction`, 세 Provider, 두 Service 를 `AddScoped` 로 등록합니다. (DbContext 가 scoped 라서 같이 scoped)
- **일부러 한 번 빠뜨려 보세요.** 컴파일은 통과하는데 실행하면 `InvalidOperationException` 으로 죽습니다. 울산 CLAUDE.md 의 "등록을 빼먹으면 컴파일이 아니라 시작할 때 터진다" 가 이거예요.
- 주의: `SocketServiceManager` 는 singleton 인데 Service 는 scoped 입니다. singleton 이 scoped 를 직접 들고 있으면 안 돼요 (현재 코드가 `IServiceProvider` + `CreateScope()` 를 쓰는 이유). 왜 그런지 설명해 보세요.

### Step 6 — 검증
- Service 를 **InMemory Provider 로 바꿔서** 단위로 돌려 봅니다 (kata 07/08 의 Provider 를 만든 방식). Provider 가 인터페이스라서 가능한 일이에요.
- 시뮬레이터 전체 시나리오를 Step 0 과 비교.

### Step 7 (심화) — 울산 패턴 도입
| 지금 | 바꿀 모양 | 쓰는 kata |
|---|---|---|
| `RaiseAlarmAndWaitForClearAsync` 의 `while(true) + Task.Delay` | "알람 해제 감시" 스케줄러 + `ExecutionAgent` 폴링 | kata 06 |
| 서비스마다 제각각 켜고 끄는 `Run/Stop` | `TimerService` 형태의 `ThreadStart/ThreadStop` | kata 10 |
| `EquipmentStatusHist` 이력만 쌓는 구조 | "현재값" 표(태그 → 값) 추가 | kata 07 |
| 서비스가 서로 직접 호출(`RFIDControllService` 호출) | 명령 큐 표를 통한 간접 호출 | kata 08 |
| 오류를 로그만 남김 | 알람 서비스(Start/End) | kata 09 |

## 4. 울산 스타일 체크리스트 (새 코드를 쓸 때마다 확인)

- [ ] 클래스마다 자기 로거를 가진다 (울산: log4net 래퍼 / 여기: `ILogger<T>`)
- [ ] Provider 메서드는 한 가지 일만 한다 (조회 하나 / 저장 하나)
- [ ] Service 의 DB 작업은 `Begin → Provider 호출들 → Commit`, 예외면 `Rollback → throw` (삼키지 않음)
- [ ] 조건에 안 맞으면 일찍 `return` / `continue` (가드 절), `else` 중첩을 줄인다
- [ ] 상태 문자열은 상수로 (`"REQUESTED"`, `"COMPLETED"` 를 흩어 쓰지 않기) — 오타 한 글자는 컴파일이 못 잡아요
- [ ] 시각 비교는 가능하면 한 기준(DB 시각 또는 주입한 시계)으로
- [ ] 주석은 "무엇"보다 "왜" (한국어)

## 5. 일부러 울산과 다르게 두는 것

| 항목 | 울산 | 여기 | 이유 |
|---|---|---|---|
| DB 접근 | 원시 SQL + 명시적 트랜잭션 | EF Core | 이 프로젝트의 학습 초점이 "계층 분리"이지 SQL 이 아님 |
| 비동기 | 동기 + `Task.Run` 폴링 | `async/await` | 현재 코드가 async. 구조(계층)가 같으면 충분 |
| 로깅 | log4net 래퍼 | `ILogger<T>` | 호스팅 기본값 |

## 6. 막히면

- 시도한 코드와 에러/로그를 가져오세요. 정답을 쓰는 대신 어디가 왜 틀렸는지 짚어 드립니다.
- 한 번에 여러 Step 을 하지 마세요. Step 이 하나 끝날 때마다 빌드하고 시뮬레이터를 돌려서 로그를 비교하는 게 가장 안전합니다.
