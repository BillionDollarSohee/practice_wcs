namespace coding.Lessons;

// 연습문제 2: 반복문과 메서드
public class P02_LoopsAndMethods : ILesson
{
    public string Title => "[문제] 2. 반복문과 메서드";

    public void Run()
    {
        Problem1();
        Problem2();
        Problem3();
        Problem4();
        Problem5();
    }

    // 문제 1) 1부터 10까지의 합을 for문으로 구해서 출력하세요. (정답: 55)
    static void Problem1()
    {
        Console.WriteLine("[문제 1]");
        int sum = 0;
        for (int i = 1; i <= 10; i++)
        {
            sum += i;
        }

        Console.WriteLine(sum);

    }

    // 문제 2) 구구단 중 원하는 단을 입력받아 출력하세요.
    //   입력 3 → 3 x 1 = 3 ... 3 x 9 = 27
    static void Problem2()
    {
        Console.WriteLine("\n[문제 2]");
        int dan = int.Parse(Console.ReadLine()!);

        for (int i= 1; i <= 9; i++)
        {
            Console.WriteLine($"{dan} x {i} = {dan * i}");
        }


    }

    // 문제 3) 1부터 30까지 출력하되, 3의 배수는 숫자 대신 "짝", 5의 배수는 "뽕"
    //   (3과 5의 공배수는 "짝뽕")을 출력하세요. (FizzBuzz 입니다)
    //   힌트: 공배수 조건을 가장 먼저 검사하세요.
    static void Problem3()
    {
        Console.WriteLine("\n[문제 3]");
        for (int i = 1; i <= 30; i++)
        {
            if (i % 3 == 0 && i % 5 == 0)
                Console.WriteLine("짝뽕");
            else if (i % 3 == 0)
                Console.WriteLine("짝");
            else if (i % 5 == 0)
                Console.WriteLine("뽕");
            else
                Console.WriteLine(i);
        }
    }

    // 문제 4) 아래 메서드를 완성하세요: 정수 배열에서 가장 큰 값을 반환
    //   힌트: foreach 로 돌면서 지금까지의 최대값과 비교하세요. (Max() 같은 기존 기능은 쓰지 않기)
    //   출력 확인: 9
    static void Problem4()
    {
        Console.WriteLine("\n[문제 4]");
        int[] numbers = { 3, 9, 2, 7, 5 };
        Console.WriteLine(FindMax(numbers));
    }

    static int FindMax(int[] numbers)
    {
        int max = numbers[0];
        foreach (var item in numbers)
        {
            if (max < item) 
            { 
                max = item;
            }
        }
        return max;
    }

    // 문제 5) 문자열을 거꾸로 뒤집는 메서드 Reverse 를 직접 만들고 호출하세요.
    //   Reverse("hello") → "olleh"
    //   힌트: 문자열은 text[i] 로 한 글자씩 접근 가능, text.Length 로 길이 확인
    static void Problem5()
    {
        Console.WriteLine("\n[문제 5]");
        Console.WriteLine(Reverse("hello"));
    }

    static string Reverse(string origin)
    { 
        string reversed = "";
        for (int i = origin.Length - 1; i >= 0; i--)
        {
            reversed += origin[i];
        }
        return reversed;
    }
}
