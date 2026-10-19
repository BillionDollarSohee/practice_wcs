namespace coding.Lessons;

// 모든 연습 파일이 구현하는 규칙. 이걸 구현한 클래스는 메뉴에 자동으로 나타남.
public interface ILesson
{
    string Title { get; }   // 메뉴에 보일 이름
    void Run();             // 선택되면 실행될 코드
}
