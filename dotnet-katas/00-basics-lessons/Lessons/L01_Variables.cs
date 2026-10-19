namespace coding.Lessons;

public class L01_Variables : ILesson
{
    public string Title => "변수와 자료형";

    public void Run()
    {
        int age = 25;
        double height = 175.5;
        string name = "홍길동";
        bool isStudent = true;
        char grade = 'A';

        Console.WriteLine($"이름: {name}, 나이: {age}, 키: {height}");
        Console.WriteLine($"학생 여부: {isStudent}, 등급: {grade}");

        // var: 타입을 컴파일러가 추론
        var count = 10;
        Console.WriteLine($"count의 타입: {count.GetType()}");
    }
}
