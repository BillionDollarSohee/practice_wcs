using System.Text;

namespace FixedLengthParserKata;

// 실무 참고: WCS.Common/SystemInterfaces/ECS/RFIDControll/Models/ParsedCartInfo.cs  (구조만 참고, 베끼지 말 것)
//
// 대차 RFID 태그에는 40글자 고정길이 문자열이 들어 있습니다. 칸(위치)마다 의미가 정해져 있어요.
//
//   위치  0~ 9 (10)  차체번호      ─┐
//   위치 10~13 ( 4)  차종          ─┤ 스키드ID (18)
//   위치 14~15 ( 2)  라인타입      ─┤  (서열이 WRITE 될 때만 채워짐)
//   위치 16~17 ( 2)  라인코드      ─┘
//   위치 18~21 ( 4)  서열번호      ← 스키드ID + 서열번호 = "서열정보 22글자"
//   위치 22~25 ( 4)  대차번호      ┐ 대차 고유 정보 (처음부터 들어 있음, 서열이 지워져도 유지)
//   위치 26~27 ( 2)  라인타입      ┘
//   위치 28~39 (12)  옵션
//
// 입고 때는 "서열정보 22글자"만 공백으로 덮어써서 지우고(대차 정보는 유지),
// 비전 OK / 출고 CV 에서는 서열정보 22글자만 새로 씁니다.

public sealed class ParsedCartInfo
{
    public const int TotalLength = 40;
    public const int SeqInfoLength = 22;
    public const string DefaultOption = "000000000000";

    public string RfidData { get; private set; } = "";      // 40글자로 맞춰진 전체 문자열
    public string SkidId { get; private set; } = "";        // [0,18)  Trim
    public string SeqNo { get; private set; } = "";         // [18,22) Trim
    public string CartNo { get; private set; } = "";        // [22,26) Trim
    public string LineType { get; private set; } = "";      // [26,28) Trim
    public string Option { get; private set; } = "";        // [28,40) Trim
    public string CartId { get; private set; } = "";        // "EV_{라인타입 첫글자}-{대차번호}" 또는 ""
    public bool IsExistsSeqInfo { get; private set; }       // 서열정보가 들어 있는가

    /// <summary>RFID 에서 읽은 문자열로 만든다. 40글자보다 짧으면 오른쪽을 공백으로 채워서 해석한다.</summary>
    public ParsedCartInfo(string rfidData)
    {
        Parse(rfidData);
    }

    /// <summary>
    /// 서열 정보로 "WRITE 할 RFID 문자열"을 만든다. (비전 OK / 출고 CV 에서 쓰는 형태)
    ///   차체번호(10, 오른쪽 공백채움) + 차종(4, 오른쪽 공백) + 라인타입(2, 대문자, 오른쪽 공백) + 라인코드(2, 왼쪽 '0')
    ///   + 서열번호(4, 왼쪽 '0') + 대차번호(4, 왼쪽 '0') + 라인타입(2, 대문자) + 옵션(DefaultOption)
    /// 대차번호는 cartId 에서 '-' 뒤쪽 부분입니다. 예) "EV_T-12" → "12" → "0012"
    /// 만든 문자열을 Parse 해서 속성을 채운다.
    /// </summary>
    public ParsedCartInfo(string bodyNo, string carTypeCd, string lineCd, string seqNo, string cartId, string lineType)
    {
        // TODO 1: StringBuilder 로 조립 → Parse
        throw new NotImplementedException("TODO 1: 서열정보로 RFID 문자열 만들기");
    }

    /// <summary>
    /// 대차번호와 라인타입만으로 만든다. (서열정보 22글자는 전부 공백/0 으로 비어 있는 상태)
    ///   차체번호 공백 10 + 차종 공백 4 + 라인타입(2, 대문자) + 라인코드 "00" + 서열번호 "0000"
    ///   + 대차번호(4, 왼쪽 '0') + 라인타입(2, 대문자) + 옵션
    /// </summary>
    public ParsedCartInfo(string cartNo, string lineType)
    {
        // TODO 2
        throw new NotImplementedException("TODO 2: 대차번호/라인타입으로 RFID 문자열 만들기");
    }

    private void Parse(string rfidData)
    {
        // TODO 3: 아래 규칙대로 속성을 채운다
        //   - RfidData = rfidData.PadRight(40, ' ')        (짧으면 공백으로 채움)
        //   - SkidId / SeqNo / CartNo / LineType / Option 은 위 표의 위치대로 Substring 후 Trim
        //   - CartId : CartNo 가 "0000" 이거나 비었거나 LineType 이 비었으면 "" (대차 정보 없음)
        //              그 외 "EV_" + LineType 첫 글자 + "-" + CartNo      예) LineType "TR", CartNo "0012" → "EV_T-0012"
        //   - IsExistsSeqInfo : SeqNo 가 비어있지 않고 "0000" 도 아니면 true
        throw new NotImplementedException("TODO 3: Parse");
    }

    /// <summary>
    /// 서열정보 22글자(스키드ID 18 + 서열번호 4)를 그대로 돌려준다. RFID 에 서열정보를 WRITE 할 때 쓰는 값.
    /// 22글자보다 짧으면 오른쪽을 공백으로 채워서 항상 22글자.
    /// </summary>
    public string GetSeqInfoForWrite()
    {
        // TODO 4
        throw new NotImplementedException("TODO 4: GetSeqInfoForWrite");
    }

    /// <summary>
    /// "서열정보를 지운" 새 RFID 문자열을 돌려준다. 앞 22글자를 전부 공백으로 바꾸고 나머지(대차번호, 라인타입, 옵션)는 유지.
    /// 입고 때 이전 서열 데이터를 클리어하는 동작입니다. 원본 객체는 바꾸지 않는다.
    /// </summary>
    public string ClearSeqInfo()
    {
        // TODO 5
        throw new NotImplementedException("TODO 5: ClearSeqInfo");
    }
}
