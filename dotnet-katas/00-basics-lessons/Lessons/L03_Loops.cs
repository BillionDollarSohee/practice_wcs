namespace coding.Lessons;

public class L03_Loops : ILesson
{
    public string Title => "반복문 (for / foreach / while)";

    public void Run()
    {
        for (int i = 1; i <= 5; i++)
        Console.Write($"{i} ");
        Console.WriteLine();

        string[] fruits = { "사과", "바나나", "포도" };
        foreach (var fruit in fruits)
            Console.WriteLine(fruit);

        int n = 3;
        while (n > 0)
        {
            Console.WriteLine($"카운트다운: {n}");
            n--;
        }
    }
}
