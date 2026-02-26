using MySqlConnector;
using System.Data;
using TaskPractice.Conveyor.Interface;
using TaskPractice.Model;

namespace TaskPractice.Conveyor.Provider
{
    // IConveyorProvider MySQL 구현체
    public class ConveyorMySqlProvider : IConveyorProvider
    {
        private readonly string _connectionString;

        public ConveyorMySqlProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 설비 그룹의 READY 오더 조회 후 CV_COMMAND 생성
        public bool FindConveyorOrderAndCreateCommand(string eqpGroup)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // READY 오더 조회
                string selectSql = @"
SELECT 
    ORDMST.ORDER_ID,
    ORDMST.EQP_CMD_NO,
    ORDMST.CONTAINER_ID,
    ORDDTL.DETAIL_SEQ,
    ORDDTL.FROM_EQP_ID,
    ORDDTL.TO_EQP_ID,
    FROMUNIT.CV_DEST_NO AS FROM_CV_DEST_NO,
    TOUNIT.CV_DEST_NO   AS TO_CV_DEST_NO
FROM ORDER_DETAIL ORDDTL
INNER JOIN ORDER_MASTER ORDMST ON ORDDTL.ORDER_ID = ORDMST.ORDER_ID
LEFT OUTER JOIN CV_UNIT FROMUNIT ON ORDDTL.FROM_EQP_ID = FROMUNIT.EQP_ID
LEFT OUTER JOIN CV_UNIT TOUNIT   ON ORDDTL.TO_EQP_ID   = TOUNIT.EQP_ID
WHERE FROMUNIT.EQP_GROUP  = @EQP_GROUP
AND   ORDDTL.ORDER_STATUS = 'READY'
";
                var selectCmd = new MySqlCommand(selectSql, conn, transaction);
                selectCmd.Parameters.AddWithValue("@EQP_GROUP", eqpGroup);

                var dt = new DataTable();
                new MySqlDataAdapter(selectCmd).Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    string eqpId = row["FROM_EQP_ID"].ToString();
                    string toCvDestNo = row["TO_CV_DEST_NO"].ToString();

                    if (string.IsNullOrEmpty(toCvDestNo))
                        continue;

                    // 이미 명령 존재 확인
                    string checkSql = "SELECT COUNT(*) FROM CV_COMMAND WHERE EQP_ID = @EQP_ID";
                    var checkCmd = new MySqlCommand(checkSql, conn, transaction);
                    checkCmd.Parameters.AddWithValue("@EQP_ID", eqpId);

                    int cmdCount = int.Parse(checkCmd.ExecuteScalar().ToString());
                    if (cmdCount > 0)
                        continue;

                    // CV_COMMAND INSERT
                    string insertSql = @"
INSERT INTO CV_COMMAND
    (EQP_ID, EQP_CMD_NO, DEST_PLC_EQP_NO, SEND_STATUS, SAVE_DTTM, SAVE_BY)
VALUES
    (@EQP_ID, @EQP_CMD_NO, @DEST_PLC_EQP_NO, 'WAIT', NOW(), 'ConveyorScheduler')
";
                    var insertCmd = new MySqlCommand(insertSql, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@EQP_ID", eqpId);
                    insertCmd.Parameters.AddWithValue("@EQP_CMD_NO", row["EQP_CMD_NO"].ToString());
                    insertCmd.Parameters.AddWithValue("@DEST_PLC_EQP_NO", int.Parse(toCvDestNo));

                    if (insertCmd.ExecuteNonQuery() <= 0)
                        continue;

                    // ORDER_DETAIL 상태 WORKING으로 변경
                    string updateSql = @"
UPDATE ORDER_DETAIL 
SET ORDER_STATUS = 'WORKING'
WHERE ORDER_ID   = @ORDER_ID
AND   DETAIL_SEQ = @DETAIL_SEQ
";
                    var updateCmd = new MySqlCommand(updateSql, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@ORDER_ID", row["ORDER_ID"].ToString());
                    updateCmd.Parameters.AddWithValue("@DETAIL_SEQ", row["DETAIL_SEQ"].ToString());
                    updateCmd.ExecuteNonQuery();

                    // IS_CMD_READY_YN N으로 변경
                    UpdateConveyorStatusIsCmdReady(eqpId, "N");
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        // CV_COMMAND WAIT 목록 조회
        public List<ConveyorCommand> GetWaitCommandList()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
SELECT 
    CMD.EQP_ID,
    CMD.EQP_CMD_NO,
    CMD.DEST_PLC_EQP_NO,
    CMD.SEND_STATUS,
    CMD.SAVE_DTTM,
    CMD.SAVE_BY
FROM CV_COMMAND CMD
WHERE CMD.SEND_STATUS = 'WAIT'
";
            var cmd = new MySqlCommand(sql, conn);
            var dt = new DataTable();
            new MySqlDataAdapter(cmd).Fill(dt);

            var list = new List<ConveyorCommand>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ConveyorCommand
                {
                    EqpId = row["EQP_ID"].ToString(),
                    EqpCmdNo = row["EQP_CMD_NO"].ToString(),
                    DestPlcEqpNo = int.Parse(row["DEST_PLC_EQP_NO"].ToString()),
                    SendStatus = row["SEND_STATUS"].ToString(),
                    SaveDttm = DateTime.Parse(row["SAVE_DTTM"].ToString()),
                    SaveBy = row["SAVE_BY"].ToString()
                });
            }
            return list;
        }

        // CV_COMMAND 전송 상태 업데이트
        public bool UpdateCommandSendStatus(string eqpId, string sendStatus)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
UPDATE CV_COMMAND
SET SEND_STATUS = @SEND_STATUS
WHERE EQP_ID    = @EQP_ID
";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SEND_STATUS", sendStatus);
            cmd.Parameters.AddWithValue("@EQP_ID", eqpId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // CV_STATUS 업데이트 (PLC 읽기 결과)
        public bool UpdateConveyorStatus(string eqpId, string alarmCode, string sensorStatus)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
UPDATE CV_STATUS
SET ALARM_CODE    = @ALARM_CODE,
    SENSOR_STATUS = @SENSOR_STATUS,
    UPDATE_DTTM   = NOW()
WHERE EQP_ID      = @EQP_ID
";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ALARM_CODE", alarmCode);
            cmd.Parameters.AddWithValue("@SENSOR_STATUS", sensorStatus);
            cmd.Parameters.AddWithValue("@EQP_ID", eqpId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // CV_STATUS IS_CMD_READY_YN 업데이트
        public bool UpdateConveyorStatusIsCmdReady(string eqpId, string isCmdReadyYn)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
UPDATE CV_STATUS
SET IS_CMD_READY_YN = @IS_CMD_READY_YN
WHERE EQP_ID        = @EQP_ID
";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IS_CMD_READY_YN", isCmdReadyYn);
            cmd.Parameters.AddWithValue("@EQP_ID", eqpId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // CV_UNIT 전체 조회
        public List<ConveyorUnit> GetConveyorUnitList()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = "SELECT EQP_ID, EQP_GROUP, POLLING_GROUP, CV_TRACK_NO, CV_DEST_NO FROM CV_UNIT";
            var cmd = new MySqlCommand(sql, conn);
            var dt = new DataTable();
            new MySqlDataAdapter(cmd).Fill(dt);

            var list = new List<ConveyorUnit>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ConveyorUnit
                {
                    EqpId = row["EQP_ID"].ToString(),
                    EqpGroup = row["EQP_GROUP"].ToString(),
                    PollingGroup = row["POLLING_GROUP"].ToString(),
                    CvTrackNo = int.Parse(row["CV_TRACK_NO"].ToString()),
                    CvDestNo = int.Parse(row["CV_DEST_NO"].ToString())
                });
            }
            return list;
        }
    }
}