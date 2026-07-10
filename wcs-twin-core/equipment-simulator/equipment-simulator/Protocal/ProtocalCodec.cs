using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolCore
{
    // STX/ETX 프레임 기반 고정길이 프로토콜 코덱
    // STX(0x02) + Command(2) + (구분자(0x60) + 필드) + ETX(0x03)
    public class ProtocolCodec
    {
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte SEP = 0x60;

        // 필드값을 지정된 바이트 길이도 오른쪽 공백 패딩 (길이 초과시 자름)
        public static string PadField(string value, int width)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value.Length >= width)
            {
                return value.Substring(0, width);
            }
            return value.PadRight(width, ' ');
        }

        // Command + 필드들을 STX~ETX 프레임 바이트로 조립
        public static byte[] BuildFrame(string command, params string[] fields)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(command);

            foreach (var field in fields)
            {
                stringBuilder.Append((char)SEP);
                stringBuilder.Append(field);
            }

            byte[] body = Encoding.ASCII.GetBytes(stringBuilder.ToString());
            byte[] fream = new byte[body.Length + 2];
            fream[0] = STX;
            Array.Copy(body, 0, fream, 1, body.Length);
            fream[fream.Length - 1] = ETX;
            return fream;
        }

        // 누적 버퍼에서 STX~ETX 프레임 하나를 꺼낸다. 아직 미완이면 null
        // 꺼낸 만큼은 buffer에서 제거한다.
        public static byte[] ExtractFrame(List<byte> buffer)
        {
            int stxIndex = buffer.IndexOf(STX);
            if (stxIndex < 0)
            {
                buffer.Clear(); //STX가 없는 잡음데이터는 버림
                return null;
            }

            int etxIndex = buffer.IndexOf(ETX, stxIndex + 1);
            if (etxIndex < 0)
            {
                // 아직 etx가 없으면 STX 이전 잡음만 제거하고 다음 수신 대기
                if (stxIndex > 0)
                {
                    buffer.RemoveRange(0, stxIndex);
                }
                return null;
            }

            int frameLength = etxIndex - stxIndex + 1;
            byte[] frame = buffer.GetRange(stxIndex, frameLength).ToArray();
            buffer.RemoveRange(0, etxIndex + 1);
            return frame;
        }

        // STX~ETX 프레임을 Command + 필드 배열로 분해
        public static (string Command, string[] Fields) ParseFrame(byte[] frame)
        {
            string body = Encoding.ASCII.GetString(frame, 1, frame.Length - 2);
            string[] parts = body.Split((char)SEP);
            string command = parts[0];
            string[] fields = parts.Skip(1).ToArray();
            return (command, fields);
        }
    }
}
