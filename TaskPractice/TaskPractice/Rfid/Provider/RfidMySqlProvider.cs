using MySqlConnector;
using System.Data;
using System.Security.Cryptography;
using TaskPractice.Rfid.Interface;

namespace TaskPractice.Rfid.Provider
{
    public class RfidMySqlProvider : IRfidProvider
    {
        private readonly string _connectionString;

        public RfidMySqlProvider(string connectionString    )
        {
            _connectionString = connectionString;
        }

        // DB 연결 생성 헬퍼
        private MySqlConnection CreateConnection()
        { 
            return new MySqlConnection( _connectionString );

        }

        // 명령 등록
        public int InsertRfidCommand(string eqpId, string instNo, string instValuesJsonStr, string commandType)
        {
            int result = 0;
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
INSERT INTO rfid_command (
    EQP_ID,
    INST_NO,
    INST_VALUES_JSON_STR,
    COMMAND_TYPE,
    IF_STATUS,
    IF_MESSAGE,
    IF_CNT,
    SAVE_DTTM,
    SAVE_BY
) VALUES (
    @EQP_ID,
    @INST_NO,
    @INST_VALUES_JSON_STR,
    @COMMAND_TYPE,
    'WAIT',
    NULL,
    0,
    NOW(6),
    'SYSTEM'
)
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                cmd.Parameters.AddWithValue("@INST_NO", instNo);
                cmd.Parameters.AddWithValue("@INST_VALUES_JSON_STR", instValuesJsonStr);
                cmd.Parameters.AddWithValue("@COMMAND_TYPE", commandType);

                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] InsertRfidCommand: {ex.Message}");
                throw;
            }
            return result;
        }

        // 명령 조회
        public DataTable SelectRfidCommand(string eqpId, string ifStatus)
        {
            DataTable dt = new DataTable();

            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
SELECT
    EQP_ID,
    INST_NO,
    INST_VALUES_JSON_STR,
    COMMAND_TYPE,
    IF_STATUS,
    IF_MESSAGE,
    IF_CNT,
    SAVE_DTTM,
    SAVE_BY
FROM rfid_command
WHERE EQP_ID = @EQP_ID
AND IF_STATUS = @IF_STATUS
ORDER BY SAVE_DTTM
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                cmd.Parameters.AddWithValue("@IF_STATUS", ifStatus);

                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SelectRfidCommand : {ex.Message}");
                throw;
            }
            return dt;
        }

        // 장비 상태 조회
        public DataTable SelectRfidEqpStatus(string eqpId)
        {
            DataTable dt = new DataTable();
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
SELECT
    EQP_ID,
    EQP_DETAIL_ID,
    RESULT_VALUE,
    UPDATE_DTTM
FROM rfid_eqp_status
WHERE EQP_ID = @EQP_ID
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EQP_ID", eqpId);

                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SelectRfidEqpStatus : {ex.Message}");
                throw;
            }
            return dt;
        }

        // 명령 상태 업데이트
        public int UpdateRfidCommandStatus(string eqpId, string instNo, string ifStatus, string ifMessage)
        {
            int result = 0;
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
UPDATE rfid_command
SET
    IF_STATUS = @IF_STATUS,
    IF_MESSAGE = @IF_MESSAGE,
    IF_CNT = IF_CNT + 1,
    IF_START_DTTM = CASE WHEN @IF_STATUS = 'PROCESSING' THEN NOW(6) ELSE IF_START_DTTM END
WHERE EQP_ID = @EQP_ID
AND INST_NO = @INST_NO
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                cmd.Parameters.AddWithValue("@INST_NO", instNo);
                cmd.Parameters.AddWithValue("@IF_STATUS", ifStatus);
                cmd.Parameters.AddWithValue("@IF_MESSAGE", ifMessage);

                result = cmd.ExecuteNonQuery(); // SQL 영향 받는 행의 수
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateRfidCommandStatus : {ex.Message}");
                throw;
            }
            return result;
        }

        // 장비 상태 저장
        public int UpdateRfidEqpStatus(string eqpId, string detailId, string resultValue)
        {
            int result = 0;
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
UPDATE rfid_eqp_status
SET
    RESULT_VALUE = @RESULT_VALUE,
    UPDATE_DTTM = NOW(6)
WHERE EQP_ID = @EQP_ID
AND EQP_DETAIL_ID = @EQP_DETAIL_ID
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@EQP_Id", eqpId);
                cmd.Parameters.AddWithValue("@EQP_DETAIL_ID", detailId);
                cmd.Parameters.AddWithValue("@RESULT_VALUE", resultValue);

                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateRfidEqpStatus : {ex.Message}");
                throw;
            }
            return result;
        }


        // RRPCESSING 타임아웃 명령 조회
        public DataTable SelectRfidCommandTimeout(string eqpId, int timeoutSeconds)
        {
            DataTable dt = new DataTable();

            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
SELECT
    EQP_ID,
    INST_NO,
    INST_VALUES_JSON_STR,
    COMMAND_TYPE,
    IF_STATUS,
    IF_START_DTTM
FROM rfid_command
WHERE IF_STATUS = 'PROCESSING'
AND IF_START_DTTM IS NOT NULL
AND TIMESTAMPDIFF(SECOND, IF_START_DTTM, NOW()) >= @TIMEOUT_SEC 
";
                // eqpId가 있으면 조건을 추가
                if (!string.IsNullOrEmpty(eqpId))
                {
                    sql += " AND EQP_ID = @EQP_ID";
                }

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TIMEOUT_SEC", timeoutSeconds);
                if (!string.IsNullOrEmpty(eqpId))
                {
                    cmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                }

                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SelectRfidCommandTimeout: {ex.Message}");
                throw;
            }
            return dt;
        
        }

        // WAIT 상태 명령을 타입별로 조회
        public DataTable SelectRfidCommandByType(string eqpId, string ifStatus, string commandType)
        {
            DataTable dt = new DataTable();
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                string sql = @"
SELECT
    EQP_ID,
    INST_NO,
    INST_VALUES_JSON_STR,
    COMMAND_TYPE,
    IF_STATUS,
    SAVE_DTTM
FROM rfid_command
WHERE IF_STATUS   = @IF_STATUS
AND   COMMAND_TYPE = @COMMAND_TYPE
ORDER BY SAVE_DTTM
";
                if (!string.IsNullOrEmpty(eqpId))
                {
                    sql += " AND EQP_ID = @EQP_ID";
                }

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@IF_STATUS", ifStatus);
                cmd.Parameters.AddWithValue("@COMMAND_TYPE", commandType);
                if (!string.IsNullOrEmpty(eqpId))
                {
                    cmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                }

                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SelectRfidCommandByType: {ex.Message}");
                throw;
            }
            return dt;
        }


    }
}
