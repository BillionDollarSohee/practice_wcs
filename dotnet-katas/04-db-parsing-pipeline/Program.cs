// ============================================================
// Kata 4 : DB 폴링 + 행 단위 트랜잭션 격리 (Raw → Parsed 파싱 파이프라인)
// ------------------------------------------------------------
// 실무 코드의 WcsIfSequenceService.SchedulerSequenceParsing / ProcCommandData 가
// 쓰는 패턴을 그대로 축소한 연습입니다.
//
// 실무 코드가 하던 일: wcs_if_seq_receive_hist 에서 PROCESS_STATUS='WAIT' 인 행들을
// 조회한 다음, "한 행마다 별도 트랜잭션을 열어서" 파싱 → 결과 테이블 INSERT →
// 원본 행 상태 갱신 → 커밋. 한 행이 파싱 실패해도 그 행만 ERROR 로 남기고
// 트랜잭션을 롤백할 뿐, 다른 행들 처리는 전혀 영향을 안 받아야 합니다.
//
// 이 kata에서는 "부품코드|수량|위치" 형식의 원문(RAW_DATA)을 파싱해서
// KATA_PARSED_ORDER 테이블에 PART_CD/QTY/LOCATION 으로 나눠 저장합니다.
//
// 사전 준비: 이 폴더의 setup.sql 을 wcs_twin DB에 먼저 실행해서 테이블을 만들어두세요.
//   예) mysql -h127.0.0.1 -P3307 -uroot -p1234 wcs_twin < setup.sql
//
//완성 조건(Acceptance Criteria):
//   1) KATA_RAW_ORDER 에서 PROCESS_STATUS='WAIT' 인 행을 전부 조회해서 하나씩 처리한다.
//   2) 각 행은 "자기 자신만의" 트랜잭션 안에서 처리된다 (한 트랜잭션이 여러 행을 같이 묶지 않는다).
//   3) RAW_DATA를 '|' 로 쪼개서 정확히 3개 필드(PartCd, Qty, Location)가 아니거나
//      Qty가 정수로 안 바뀌면 "파싱 실패"로 간주한다.
//   4) 파싱 성공: KATA_PARSED_ORDER에 INSERT +그 행의 KATA_RAW_ORDER.PROCESS_STATUS를
//      'COMPLETE'로 갱신하고 커밋한다.
//   5) 파싱 실패: 방금 연 트랜잭션은 롤백하고, (새 트랜잭션으로) 그 행의 PROCESS_STATUS를
//      'ERROR', ERROR_MSG에 실패 이유를 남긴다. — 이 행 때문에 다른 행 처리가 멈추면 안 된다.
//   6) 전부 처리한 뒤 "성공 N건 / 실패 N건"을 콘솔에 출력한다.
//
// 힌트: setup.sql에 일부러 깨진 데이터 2건(수량이 숫자 아님, 필드 개수 부족)을 넣어뒀습니다.
//       완성 후 실행하면 4건은 COMPLETE, 2건은 ERROR로 남아야 정상입니다.
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

class Program
{
    // 트윈 시뮬레이터(eis-socket-service/appsettings.json)와 같은 DB를 씁니다.
    private const string CONNECTION_STRING =
        "Server=127.0.0.1;Port=3307;Database=wcs_twin;User=root;Password=1234;CharSet=utf8mb4;SslMode=None;";

    static async Task Main()
    {
        Console.WriteLine("=== DB Parsing Pipeline Kata ===");

        // TODO 1: MySqlConnection 을 만들고 OpenAsync() 하세요.
        MySqlConnection connection = new MySqlConnection(CONNECTION_STRING);
        await connection.OpenAsync();

        // TODO 2: KATA_RAW_ORDER 에서 PROCESS_STATUS='WAIT' 인 행을 전부 조회하세요.
        //         (RAW_ID, RAW_DATA 두 컬럼만 있으면 됩니다)
        //         조회는 트랜잭션 없이 그냥 SELECT 해도 됩니다 - 실제 처리(파싱/갱신)만
        //         행별로 독립 트랜잭션을 쓰면 됩니다.
        List<(int RawId, string RawData)> waitList = await SelectWaitListAsync(connection);

        static async Task<List<(int RawId, string RawData)>> SelectWaitListAsync(MySqlConnection connection)
        {
            var result = new List<(int, string)>();

            string sql = "" +
                "SELECT RAW_ID, RAW_DATA " +
                "FROM KATA_RAW_ORDER " +
                "WHERE PROCESS_STATUS = 'WAIT'";

            using MySqlCommand command = new MySqlCommand(sql, connection);
            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int rawId = reader.GetInt32("RAW_ID");
                string rawData = reader.GetString("RAW_DATA");
                result.Add((rawId, rawData));
            }

            return result;
        }

        int completeCount = 0;
        int errorCount = 0;

        foreach (var (rawId, rawData) in waitList)
        {
            // TODO 3: 이 행 전용 트랜잭션을 시작하세요.
            //         (힌트: await connection.BeginTransactionAsync())
            MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                // TODO 4: rawData를 '|' 로 Split 해서 정확히 3개 필드인지, 두 번째 필드가
                //         int로 파싱되는지 확인하세요. 조건에 안 맞으면 예외를 던지세요
                //         (예: throw new FormatException("필드 개수 또는 수량 형식이 올바르지 않습니다");)
                string[] fields = rawData.Split('|');
                if (fields.Length != 3)
                    throw new FormatException("필드 개수 또는 수량 형식이 올바르지 않습니다");

                if (!int.TryParse(fields[1], out int qty))
                    throw new FormatException("필드 개수 또는 수량 형식이 올바르지 않습니다");

                // TODO 5: KATA_PARSED_ORDER 에 INSERT 하세요.
                //         INSERT INTO KATA_PARSED_ORDER (RAW_ID, PART_CD, QTY, LOCATION)
                //         VALUES (@RawId, @PartCd, @Qty, @Location)
                //         MySqlCommand의 Parameters.AddWithValue(...)로 파라미터 바인딩하세요
                //         (SQL Injection 방지 - 문자열 그대로 이어붙이면 안 됩니다!)
                //         Command.Transaction = transaction 도 꼭 설정하세요.
                string insertSql = "" +
                    "INSERT INTO KATA_PARSED_ORDER (RAW_ID, PART_CD, QTY, LOCATION) " +
                   "VALUES (@RawId, @PartCd, @Qty, @Location)";

                using MySqlCommand insertCommand = new MySqlCommand(insertSql, connection);
                insertCommand.Transaction = transaction;   // ← 이걸 빼먹으면 "이 트랜잭션 안에서" 실행이 안 됩니다!

                insertCommand.Parameters.AddWithValue("@RawId", rawId);
                insertCommand.Parameters.AddWithValue("@PartCd", fields[0]);
                insertCommand.Parameters.AddWithValue("@Qty", qty);
                insertCommand.Parameters.AddWithValue("@Location", fields[2]);

                await insertCommand.ExecuteNonQueryAsync();


                // TODO 6: KATA_RAW_ORDER 의 이 행 PROCESS_STATUS를 'COMPLETE'로 UPDATE 하세요.
                //         UPDATE KATA_RAW_ORDER SET PROCESS_STATUS='COMPLETE' WHERE RAW_ID=@RawId
                string updateSql = "" +
                    "UPDATE KATA_RAW_ORDER SET PROCESS_STATUS='COMPLETE' WHERE RAW_ID=@RawId";

                using MySqlCommand updateCommand = new MySqlCommand(updateSql, connection);
                updateCommand.Transaction = transaction;
                updateCommand.Parameters.AddWithValue("@RawId", rawId);
                await updateCommand.ExecuteNonQueryAsync();

                // TODO 7: transaction.CommitAsync() 로 커밋하고, completeCount++ 하세요.
                await transaction.CommitAsync();
                completeCount++;
            }
            catch (Exception ex)
            {
                // TODO 8: transaction을 롤백하세요 (transaction이 null이 아닐 때만).
                //         그 다음, 새로운 별도 UPDATE(트랜잭션 없이 혹은 새 트랜잭션으로)로
                //         KATA_RAW_ORDER.PROCESS_STATUS='ERROR', ERROR_MSG=ex.Message 를 저장하고,
                //         errorCount++ 하세요.
                //
                //         왜 "새로 하나 더" 해야 할까요? 방금 롤백한 트랜잭션 안에서 했던
                //         UPDATE(만약 있었다면)도 다 같이 롤백돼서 사라졌기 때문입니다.
                //         ERROR 상태 기록은 롤백과 무관하게 반드시 남아야 하니 별도로 처리해야 합니다.
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                // TODO 8-2: 새 명령으로 ERROR 상태 기록 - .Transaction을 안 걸어야 함(롤백된 트랜잭션은 이미 끝났으니까)
                string errorSql = "" +
                    "UPDATE KATA_RAW_ORDER " +
                    "SET PROCESS_STATUS='ERROR', " +
                    "ERROR_MSG=@" +
                    "ErrorMsg " +
                    "WHERE RAW_ID=@RawId";
                using MySqlCommand errorCommand = new MySqlCommand(errorSql, connection);
                errorCommand.Parameters.AddWithValue("@ErrorMsg", ex.Message);
                errorCommand.Parameters.AddWithValue("@RawId", rawId);
                await errorCommand.ExecuteNonQueryAsync();

                errorCount++;

                Console.WriteLine($"[RawId {rawId}] 파싱 실패: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"처리 완료: 성공 {completeCount}건 / 실패 {errorCount}건");

        await connection.CloseAsync();
    }
}
