namespace coding.Lessons;

public class L04_Methods : ILesson
{
    public string Title => "메서드";

    public void Run()
    {
        Console.WriteLine(Add(3, 4));
        Console.WriteLine(Greet("철수"));
        Console.WriteLine(Greet("영희", "안녕"));
    }

    static int Add(int a, int b) => a + b;

    // 기본값 매개변수
    static string Greet(string name, string greeting = "Hello") => $"{greeting}, {name}!";
}
