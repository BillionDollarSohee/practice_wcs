namespace AlarmServiceKata;

// 실무 참고: WCS.Common/BaseServices/AlarmManagement/AlarmManagementService.cs
//           (SaveWcsServiceAlarmStart / SaveWcsServiceAlarmEnd),  ProviderMethodTemplate.txt
//
// 이 kata 는 알람 업무 규칙보다 "서비스가 DB 를 쓰는 모양"을 익히는 것이 핵심입니다.
//
//   object transaction = _wcsDbClient.BeginTransaction();
//   try
//   {
//       ... Provider 호출들 ...
//       _wcsDbClient.CommitTransaction(transaction);
//   }
//   catch (Exception ex)
//   {
//       _wcsDbClient.RollbackTransaction(transaction);
//       throw ...;       ← 삼키지 않고 다시 던진다
//   }
//
// 울산 WCS 의 Service 는 이 모양이 수백 번 반복됩니다. 손이 기억할 때까지 직접 써 보세요.

public sealed class AlarmService
{
    private readonly IDbClient _db;
    private readonly IAlarmProvider _provider;

    public AlarmService(IDbClient db, IAlarmProvider provider)
    {
        _db = db;
        _provider = provider;
    }

    /// <summary>
    /// 알람을 시작한다. 같은 알람(프로그램ID+설비ID+알람코드)이 이미 울리고 있으면 아무것도 하지 않는다. (멱등)
    ///
    ///   1. 트랜잭션 시작
    ///   2. SelectActive 로 이미 있는지 확인
    ///   3. 없으면 Insert → 성공(1 이상)이면 InsertHist 까지.  반환값은 두 호출의 건수 합 (정상이면 2)
    ///   4. 이미 있으면 반환값 0
    ///   5. 커밋.  중간에 예외가 나면 롤백하고 예외를 "다시 던진다" (new Exception(ex.ToString(), ex) 형태)
    /// </summary>
    public int Start(string programId, string eqpId, string alarmCd, string alarmMsg, string popupYn)
    {
        // TODO 1
        throw new NotImplementedException("TODO 1: Start");
    }

    /// <summary>
    /// 알람을 종료한다. 울리고 있지 않으면 아무것도 하지 않는다.
    ///
    ///   1. 트랜잭션 시작
    ///   2. SelectActive 로 찾기
    ///   3. 있으면 Delete → 성공이면 UpdateHistEnd(시작시각은 찾은 알람의 StartDttm) 까지.  반환값은 두 호출의 건수 합 (정상이면 2)
    ///   4. 없으면 반환값 0
    ///   5. 커밋 / 예외 시 롤백 후 다시 던짐
    /// </summary>
    public int End(string programId, string eqpId, string alarmCd)
    {
        // TODO 2
        throw new NotImplementedException("TODO 2: End");
    }
}
