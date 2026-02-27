using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskPractice.Model;
using TaskPractice.Service;

namespace TaskPractice.WPF.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private OrderService _orderService;
        private SchedulerService _schedulerService;

        // 오더 현황 테이블에 바인딩할 리스트
        public ObservableCollection<OrderMaster> Orders { get; set; }
            = new ObservableCollection<OrderMaster>();

        // 컨베이어 노드 상태 리스트
        public ObservableCollection<ConveyorStatusViewModel> ConveyorNodes { get; set; }
            = new ObservableCollection<ConveyorStatusViewModel>();

        // 로그 리스트
        public ObservableCollection<string> Logs { get; set; }
            = new ObservableCollection<string>();

        // 출발지 선택
        private string _selectedFromId;
        public string SelectedFromId
        {
            get => _selectedFromId;
            set => SetProperty(ref _selectedFromId, value);
        }

        // 도착지 선택
        private string _selectedToId;
        public string SelectedToId
        {
            get => _selectedToId;
            set => SetProperty(ref _selectedToId, value);
        }

        // 선택 가능한 노드 목록
        // 출발지 : RFID가 달린 입고 포인트 노드만 선택 가능
        // 도착지 : 목적지가 될 수 있는 노드만 선택 가능
        public List<string> FromNodeIds { get; set; } = new List<string>();  // IN 노드만
        public List<string> ToNodeIds { get; set; } = new List<string>();    // OUT 노드만

        // 입고 시작 버튼 커맨드
        public ICommand StartCommand { get; }

        public MainWindowViewModel()
        {
            var nodes = NodeInitializer.CreateNodes();

            // 출발지/도착지 분리
            FromNodeIds = NodeInitializer.GetFromNodeIds();
            ToNodeIds = NodeInitializer.GetToNodeIds();

            _orderService = new OrderService(nodes);
            _schedulerService = new SchedulerService(_orderService);

            // 컨베이어 노드 상태 초기화
            foreach (var node in nodes)
            {
                ConveyorNodes.Add(new ConveyorStatusViewModel { NodeId = node.Id });
            }

            // 커맨드 연결
            StartCommand = new RelayCommand(OnStartCommand);

            // 스케줄러 시작
            _schedulerService.Start();

            // 화면 갱신 타이머 (100ms마다 오더 상태 동기화)
            var uiTimer = new System.Timers.Timer(100);
            uiTimer.Elapsed += (s, e) => SyncOrders();
            uiTimer.Start();
        }

        private void OnStartCommand()
        {
            if (string.IsNullOrEmpty(SelectedFromId) || string.IsNullOrEmpty(SelectedToId))
            {
                AddLog("출발지와 도착지를 선택해주세요.");
                return;
            }

            if (SelectedFromId == SelectedToId)
            {
                AddLog("출발지와 도착지가 같습니다.");
                return;
            }

            // 대차 생성
            var cart = new Cart()
            {
                CartId = $"CART-{DateTime.Now:HHmmss}",
                FromEqpId = SelectedFromId,
                ToEqpId = SelectedToId
            };

            var order = _orderService.CreateOrder(cart);
            if (order != null)
            {
                AddLog($"[입고시작] {cart.CartId} {cart.FromEqpId} → {cart.ToEqpId}");
            }
        }

        // 오더 상태 화면 동기화
        private void SyncOrders()
        {
            var currentOrders = _orderService.GetOrders();

            App.Current.Dispatcher.Invoke(() =>
            {
                Orders.Clear();
                foreach (var order in currentOrders)
                {
                    Orders.Add(order);
                }

                // 컨베이어 노드 상태 갱신
                foreach (var node in ConveyorNodes)
                {
                    var isActive = currentOrders
                        .Any(o => o.Details
                            .Any(d => d.Status == DetailStatus.WORKING
                                && d.ToEqpId == node.NodeId));

                    node.IsActive = isActive;
                }
            });
        }

        private void AddLog(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (Logs.Count > 50)
                    Logs.RemoveAt(Logs.Count - 1);
            });
        }

        // 컨베이어 노트 상태 ViewModel
        public class ConveyorStatusViewModel : ViewModelBase
        {
            public string NodeId { get; set; }

            private bool _isActive;
            public bool IsActive
            {
                get => _isActive;
                set => SetProperty(ref _isActive, value);
            }
        }

        // 커멘드 패턴 구현체
        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;  
            }

            public event EventHandler? CanExecuteChanged;
            // 버튼 클릭시 실행할 메소드 담아두는 변수
            public void Execute(object? parameter) => _execute();
            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        }
    }

}
