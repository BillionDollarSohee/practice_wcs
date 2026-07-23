# Kata 4 — DB 폴링 + 행 단위 트랜잭션 격리

실무 `WcsIfSequenceService.SchedulerSequenceParsing` / `ProcCommandData`가 쓰는
"WAIT 상태 행을 폴링해서, 한 행마다 독립 트랜잭션으로 처리하고, 실패해도 다른 행은 안 막히게"
패턴을 최소 단위로 연습합니다.

## 사전 준비 (완료됨)

`wcs-twin-core/equipment-simulator`의 `WcsTwinContext`에 이 kata 전용 테이블
(`KATA_RAW_ORDER`, `KATA_PARSED_ORDER`)을 이미 만들어뒀습니다 (`wcs_twin` DB,
127.0.0.1:3307 — practice_wcs 안의 로컬 DB). `KATA_RAW_ORDER`에 정상 4건 + 일부러
깨뜨린 2건(수량이 숫자 아님 / 필드 부족)이 `WAIT` 상태로 시드되어 있습니다.

DB 스키마는 `setup.sql`에도 참고용으로 남겨뒀습니다(실제 적용은 `WcsTwinContext`를
통해 이미 했습니다).

## 실행 방법

```
dotnet run
```

## 완성 조건

1. `KATA_RAW_ORDER`의 `WAIT` 행을 전부 조회해서 하나씩 처리
2. 행마다 **독립된 트랜잭션** (한 트랜잭션이 여러 행을 묶지 않음)
3. `RAW_DATA`를 `|`로 쪼개서 정확히 3필드 + 수량이 정수인지 검증
4. 성공 시: `KATA_PARSED_ORDER` INSERT + 원본 행 `PROCESS_STATUS='COMPLETE'` + 커밋
5. 실패 시: 롤백 + (별도로) 원본 행 `PROCESS_STATUS='ERROR'`, `ERROR_MSG` 기록 — 다른 행 처리는 계속
6. 마지막에 "성공 N건 / 실패 N건" 출력 (정상 시나리오면 4/2가 나와야 함)

## 확인 방법

실행 후 DB를 직접 조회해서 결과를 확인해보세요 (SSMS, DBeaver, 또는 아무 MySQL 클라이언트든):

```sql
SELECT * FROM KATA_RAW_ORDER;      -- 4건 COMPLETE, 2건 ERROR(+ERROR_MSG) 여야 함
SELECT * FROM KATA_PARSED_ORDER;   -- 성공한 4건만 들어있어야 함
```

## 생각해볼 것

- 왜 실패한 행의 트랜잭션을 롤백한 뒤, "새 트랜잭션"으로 ERROR 상태를 다시 기록해야 할까요?
  (롤백된 트랜잭션 안에서 이미 했던 UPDATE는 어떻게 될까요?)
- 만약 트랜잭션을 행마다 나누지 않고 전체를 하나로 묶었다면, 2번째 깨진 행에서 무슨 일이
  벌어졌을까요? (실무 코드 주석의 "한 inst 실패가 다른 inst의 commit을 rollback 시키지
  않도록 격리"라는 문장을 다시 읽어보세요.)
- 막히면, 실제 코드(`WcsIfSequenceService.cs`의 `ProcCommandData`)를 베끼지 말고
  "구조만" 참고하세요.
