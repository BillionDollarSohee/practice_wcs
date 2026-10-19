using System.Reflection;
using coding.Lessons;

// Lessons 폴더의 ILesson 구현 클래스를 자동으로 찾아서 메뉴로 보여줌.
// 새 연습을 추가하려면 Lessons 폴더에 파일만 만들면 됨 (Program.cs 수정 불필요).
var lessons = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(ILesson).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
    .OrderBy(t => t.Name)
    .Select(t => (ILesson)Activator.CreateInstance(t)!)
    .ToList();

while (true)
{
    Console.Clear();
    Console.WriteLine("=== C# 연습장 ===\n");
    for (int i = 0; i < lessons.Count; i++)
        Console.WriteLine($"{i + 1}. {lessons[i].Title}");
    Console.WriteLine("0. 종료");
    Console.Write("\n번호 선택 > ");

    if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > lessons.Count)
        continue;
    if (choice == 0)
        break;

    Console.Clear();
    Console.WriteLine($"--- {lessons[choice - 1].Title} ---\n");
    try
    {
        lessons[choice - 1].Run();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[에러] {ex.Message}");
    }
    Console.WriteLine("\n(Enter를 누르면 메뉴로 돌아갑니다)");
    Console.ReadLine();
}
