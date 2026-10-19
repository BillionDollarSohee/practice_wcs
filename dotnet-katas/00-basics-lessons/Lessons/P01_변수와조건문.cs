namespace coding.Lessons;

// 연습문제 1: 변수와 조건문
// 각 문제의 TODO 자리에 직접 코드를 작성하고, 메뉴에서 실행해서 결과를 확인하세요.
public class P01_Basics : ILesson
{
    public string Title => "[문제] 1. 변수와 조건문";

    public void Run()
    {
        Problem1();
        Problem2();
        Problem3();
        Problem4();
        Problem5();
    }

    // 문제 1) 이름(string), 나이(int), 키(double) 변수를 만들고 아래처럼 출력하세요.
    //   출력 예: 이름: 철수 / 나이: 20 / 키: 175.5
    static void Problem1()
    {
        Console.WriteLine("[문제 1]");
        string name = "혁기";
        int age = 29;
        double height = 180.0;

        Console.WriteLine($"이름: {name} / 나이: {age} / 키: {height}");
    }

    // 문제 2) 두 정수 a=17, b=5 로 + - * / % 결과를 각각 출력하세요.
    //   힌트: 17 / 5 와 17 % 5 의 결과가 뭔지 먼저 예상해보고 실행해보세요.
    static void Problem2()
    {
        Console.WriteLine("\n[문제 2]");
        int a = 17;
        int b = 5;

        Console.WriteLine($"{a} + {b} = {a + b}");
        Console.WriteLine($"{a} - {b} = {a - b}");
        Console.WriteLine($"{a} * {b} = {a * b}");
        Console.WriteLine($"{a} / {b} = {a / b}");
        Console.WriteLine($"{a} % {b} = {a % b}");
    }

    // 문제 3) 숫자를 입력받아 짝수면 "짝수", 홀수면 "홀수"를 출력하세요.
    //   힌트: int.Parse(Console.ReadLine()!) 로 입력받고, % 연산자를 쓰세요.
    static void Problem3()
    {
        if (!int.TryParse(Console.ReadLine(), out int input))
        {
            Console.WriteLine("정수를 입력하세요.");
            return;
        }

        if (input % 2 == 0)
        {
            Console.WriteLine("짝수");
        }
        else
        {
            Console.WriteLine("홀수");
        }
    }
    
    // 문제 4) 점수를 입력받아 등급을 출력하세요.
    //   90 이상 A / 80 이상 B / 70 이상 C / 그 외 F
    //   if - else if - else 로 먼저 풀고, 익숙해지면 switch 식으로도 풀어보세요.
    static void Problem4()
    {
        Console.WriteLine("\n[문제 4]");
        int score = int.TryParse(Console.ReadLine()!, out int parsedScore) ? parsedScore : 0;

        char grade = score switch
        {
            >= 90 => 'A',
            >= 80 => 'B',
            >= 70 => 'C',
            _ => 'F'
        };

        Console.WriteLine($"등급: {grade}");

    }

    // 문제 5) 연도를 입력받아 윤년인지 출력하세요.
    //   윤년: 4로 나누어떨어지고 100으로 나누어떨어지지 않거나, 400으로 나누어떨어지는 해
    //   확인: 2024 → 윤년 / 1900 → 평년 / 2000 → 윤년 / 2023 → 평년
    //   힌트: && 와 || 를 함께 쓰세요.
    static void Problem5()
    {
        Console.WriteLine("\n[문제 5]");
        int year = int.TryParse(Console.ReadLine()!, out int parsedYear) ? parsedYear : 0;

        if ((year % 4 == 0 && year % 100 != 0) || year % 400 == 0)
        {
            Console.WriteLine("윤년");
        }
        else
        {
            Console.WriteLine("평년");
        }
    }
}
