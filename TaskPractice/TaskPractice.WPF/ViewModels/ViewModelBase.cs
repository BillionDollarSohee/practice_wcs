using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskPractice.WPF.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // 프로퍼티 값이 바뀌면 View에게 알려주는 메소드 -> 화면 다시 그리게끔
        // WPF 바인딩이 이 이벤트를 감지해서 화면을 자동 갱신
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 값 변경 + 알림을 한번에 처리
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}