-- Kata 4 전용 테이블. 기존 트윈 시뮬레이터 테이블(CART_MASTER 등)과는 완전히 별개입니다.
-- wcs_twin DB에 그대로 실행하면 됩니다.

DROP TABLE IF EXISTS KATA_PARSED_ORDER;
DROP TABLE IF EXISTS KATA_RAW_ORDER;

-- 원본(수신) 테이블: 서열의 wcs_if_seq_receive_hist 역할
CREATE TABLE KATA_RAW_ORDER (
    RAW_ID          INT AUTO_INCREMENT PRIMARY KEY,
    RAW_DATA        VARCHAR(200)  NOT NULL,   -- "부품코드|수량|위치" 파이프(|) 구분 원문
    PROCESS_STATUS  VARCHAR(20)   NOT NULL DEFAULT 'WAIT',  -- WAIT / COMPLETE / ERROR
    ERROR_MSG       VARCHAR(500)  NULL,
    CREATE_DTTM     DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

-- 파싱 결과(목적지) 테이블: 서열의 wcs_work_seq_order 역할
CREATE TABLE KATA_PARSED_ORDER (
    PARSED_ID       INT AUTO_INCREMENT PRIMARY KEY,
    RAW_ID          INT           NOT NULL,
    PART_CD         VARCHAR(50)   NOT NULL,
    QTY             INT           NOT NULL,
    LOCATION        VARCHAR(50)   NOT NULL,
    CREATE_DTTM     DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

-- 테스트 데이터: 정상 4건 + 일부러 깨뜨린 2건 (수량이 숫자가 아님 / 필드 개수 모자람)
INSERT INTO KATA_RAW_ORDER (RAW_DATA, PROCESS_STATUS) VALUES
    ('A100|5|BANK1_01',   'WAIT'),
    ('A101|3|BANK1_02',   'WAIT'),
    ('B200|10|BANK2_05',  'WAIT'),   -- 이 다음이 일부러 깨진 행
    ('B201|abc|BANK2_06', 'WAIT'),   -- 수량 자리에 숫자가 아닌 값 → 파싱 실패해야 함
    ('C300|7|BANK3_01',   'WAIT'),
    ('C301|BANK3_02',     'WAIT');   -- 필드가 2개뿐(수량 없음) → 파싱 실패해야 함
