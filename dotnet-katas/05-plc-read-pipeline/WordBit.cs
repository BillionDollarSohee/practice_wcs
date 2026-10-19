using System.Text;

namespace PlcReadKata;

// PART A — Word / Bit 해석
//
// PLC 는 데이터를 "워드(2바이트)" 단위 주소로 관리합니다.
// 읽어온 byte[] 에서 "워드 N번의 비트 M번" 같은 값을 꺼내는 가장 밑바닥 도구들입니다.
//
// 실무 참고: FEnetPlcInterfaceService 가 읽은 byte[] 를 new BitArray(byte[]) 로 바꿔서 쓰는데,
//           BitArray 의 인덱스 규칙(byte 0 의 bit0 이 인덱스 0)이 곧 이 PART 의 규칙입니다.
//
// 규칙
//   - 워드 w 는 data[w*2](low byte), data[w*2+1](high byte) 로 구성된다 (little-endian)
//   - 워드 안 비트 번호 b 는 0~15. bit0 이 low byte 의 최하위 비트, bit15 가 high byte 의 최상위 비트

public static class WordBit
{
    /// <summary>
    /// data 에서 wordOffset 번째 워드를 ushort 로 꺼낸다.
    /// 예) data = {0x34, 0x12}, wordOffset = 0  →  0x1234
    /// </summary>
    public static ushort GetWord(byte[] data, int wordOffset)
    {
        // TODO 1: low byte(data[wordOffset*2]) 와 high byte 를 합쳐서 ushort 로 반환
        int lowByte = data[wordOffset * 2];
        int highByte = data[wordOffset * 2 + 1];
        return (ushort)(lowByte | (highByte << 8));
    }

    /// <summary>
    /// wordOffset 번째 워드의 bitIndex(0~15) 번 비트가 켜져 있으면 true.
    /// 예) data = {0x01, 0x80}  →  (word 0, bit 0) = true, (word 0, bit 15) = true, (word 0, bit 1) = false
    /// </summary>
    public static bool GetBit(byte[] data, int wordOffset, int bitIndex)
    {
        // TODO 2: bitIndex 가 low byte 소속인지 high byte 소속인지 판단 → 해당 byte 에서 (bitIndex % 8) 비트를 검사
        //         힌트: 쉬프트(>>) 와 & 1,  또는  1 << n  과 &
        int byteIndex = wordOffset * 2 + (bitIndex / 8);    // 어느 바이트인가
        int bitInByte = bitIndex % 8;                       // 바이트 안에서 몇 번째 비트인가
        int shifted = data[byteIndex] >> bitInByte;         // 확인할 비트를 맨 오른쪽으로 쉬프트
        bool isBitSet = (shifted & 1) == 1;                 // 맨 오른쪽 비트가 1이면 true, 0이면 false
        return isBitSet;
    }

    /// <summary>
    /// wordOffset 부터 wordLength 워드(= wordLength*2 바이트)를 ASCII 문자열로 해석한다.
    /// 끝의 '\0' 과 공백은 제거한다 (PLC 문자열은 남는 칸이 0 으로 채워진다).
    /// 예) data = "ABCD\0\0", GetAscii(data, 0, 3)  →  "ABCD"
    ///     GetAscii(data, 1, 1)                     →  "CD"
    /// (실제 PLC 는 문자 순서가 워드 안에서 뒤집히는 경우도 있는데, 이 kata 는 순서 그대로 읽는다)
    /// </summary>
    public static string GetAscii(byte[] data, int wordOffset, int wordLength)
    {
        // TODO 3: Encoding.ASCII.GetString(data, 시작byte, 길이byte) 후 TrimEnd
        int startByte = wordOffset * 2;
        int lengthBytes = wordLength * 2;
        return Encoding.ASCII.GetString(data, startByte, lengthBytes).TrimEnd('\0', ' ');
    }

    /// <summary>
    /// 구독 항목 하나가 차지하는 워드 수를 계산한다. (최소 1)
    ///   useWordLength == true  : length 가 "워드 수" → length 그대로 (단, 최소 1)
    ///   useWordLength == false : length 가 "비트 수" → startBit ~ (startBit+length) 를 덮는 워드 수
    ///                            (startBit + length 비트를 16비트 단위로 올림)
    /// 예) (false, startBit 14, length 1) → 1,   (false, 15, 2) → 2,   (false, 0, 16) → 1,   (false, 0, 17) → 2
    ///     (true,  0, 5) → 5,   (true, 0, 0) → 1
    /// 실무: FEnetPlcInterfaceService.BuildOneCakeReadSegments 안의 wordLen 계산 (UseWordLengthYn 분기)
    /// </summary>
    public static int WordsNeeded(bool useWordLength, int startBit, int length)
    {
        // useWordLength 가 true 인가 false 인가로 갈라진다
        if (useWordLength) 
        {
            // true  → 길이가 이미 워드 수. 단 0이 와도 최소 1
            return Math.Max(1, length);
        } 
        else
        {
            // false → 총 비트 수(startBit + length)를 올림 나눗셈으로 워드 수로 바꾸고, 최소 1
            int totalBits = startBit + length;
            return Math.Max(1, (totalBits + 15) / 16);
        }
    }
}
