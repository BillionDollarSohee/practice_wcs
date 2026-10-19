namespace coding.Lessons;

public class L02_Conditions : ILesson
{
    public string Title => "조건문 (if / switch)";

    public void Run()
    {
        Console.Write("점수 입력 > ");
        int score = int.Parse(Console.ReadLine()!);

        if (score >= 90) Console.WriteLine("A");
        else if (score >= 80) Console.WriteLine("B");
        else Console.WriteLine("C 이하");

        // switch 식
        string result = score switch
        {
            >= 60 => "합격",
            _ => "불합격"
        };
        Console.WriteLine(result);
    }
}
