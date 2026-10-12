using CommunityToolkit.Mvvm.ComponentModel;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CowAuctionSmall.ViewModels
{
    /// <summary>메인 하단의 표시 상태. 수신·회전 동작은 각 담당 서비스가 결정한다.</summary>
    public partial class MainStatusViewModel : ObservableObject
    {
        [ObservableProperty] private string _serverText = "응찰서버 확인 중";
        [ObservableProperty] private Brush _serverBrush = Brushes.DimGray;
        [ObservableProperty] private string _roleText = "모드 확인 중";
        [ObservableProperty] private string _auctionText = "경매 확인 중";
        [ObservableProperty] private string _rootFarmText = "뿌리농가 미확인";
        [ObservableProperty] private string _pageText = "페이지 확인 중";
        [ObservableProperty] private string _rotationText = "확인 중";
        [ObservableProperty] private string _rotationDetail = "페이지 상태 수신 대기";
        [ObservableProperty] private Brush _rotationBrush = Brushes.DimGray;
        [ObservableProperty] private string _versionText = "버전: -";

        public ObservableCollection<Brush> PageDots { get; } = new();
        private bool _acceptAuctionState = true;
        private string _lastConnectionResult = string.Empty;

        public void SetConnection(string resultCode)
        {
            // 중복 접속으로 서버가 채널을 닫아도 원인이 사라지지 않도록 유지한다.
            if (resultCode == "disconnected" && _lastConnectionResult == "2002") resultCode = "2002";
            _lastConnectionResult = resultCode;
            _acceptAuctionState = resultCode == "2000";
            ServerText = resultCode switch
            {
                "2000" => "응찰서버 연결",
                "2001" => "응찰서버 접속 실패",
                "2002" => "응찰서버 중복 접속",
                "disconnected" => "응찰서버 연결 종료",
                _ => "응찰서버 확인 중"
            };
            ServerBrush = resultCode switch
            {
                "2000" => Brushes.LimeGreen,
                "2002" => Brushes.Gold,
                "2001" or "disconnected" => Brushes.OrangeRed,
                _ => Brushes.DimGray
            };
            if (resultCode != "2000") SetAuction(AuctionDisplayState.Empty);
        }

        public void SetAuction(AuctionDisplayState state)
        {
            if (!_acceptAuctionState && state != AuctionDisplayState.Empty) return;
            string method = state.Method switch { 10 => "일괄경매", 20 => "단일경매", _ => "경매" };
            string phase = state.Phase switch
            {
                AuctionDisplayPhase.Waiting => "대기",
                AuctionDisplayPhase.Running => "진행",
                AuctionDisplayPhase.Finished => "종료",
                _ => "확인 중"
            };
            string item = state.Method == 20 && state.SipNumber is > 0
                ? $" · {state.SipNumber}번" : string.Empty;
            AuctionText = $"{method} · {phase}{item}";
        }

        public void SetRootFarm(string? value)
        {
            string setting = value?.Trim().ToUpperInvariant() ?? string.Empty;
            RootFarmText = $"뿌리농가 {(setting is "Y" or "N" or "X" ? setting : "미확인")}";
        }

        public void SetPage(PageIndicatorStateMessage state)
        {
            int total = Math.Clamp(state.TotalPages, 1, 4);
            int current = Math.Clamp(state.CurrentPage, 1, total);
            RoleText = state.IsMaster ? "마스터 모드" : "서브 모드";
            PageText = $"페이지 {current}/{total}";
            string reason = state.FreezeReason switch
            {
                "StopTimer" => "서버 신호로 페이지 회전 정지",
                "UntilRefresh" => "새로고침 신호까지 페이지 고정",
                "Running" => "경매 진행 중 페이지 고정",
                _ => "페이지 고정"
            };
            if (state.IsFrozen)
            {
                RotationText = state.IsSubFallbackActive ? "로컬 복구 · 고정" : "고정";
                RotationDetail = reason;
                RotationBrush = Brushes.Gold;
            }
            else if (total == 1)
            {
                RotationText = state.IsSubFallbackActive ? "로컬 복구 · 단일 페이지" : "단일 페이지";
                RotationDetail = "표시할 페이지가 1개입니다.";
                RotationBrush = state.IsSubFallbackActive ? Brushes.DeepSkyBlue : Brushes.DimGray;
            }
            else if (!state.IsMaster && !state.IsSubFallbackActive)
            {
                RotationText = "동기화 수신 모드";
                RotationDetail = "마스터의 페이지 신호를 따릅니다. 원격 타이머 실행 여부를 뜻하지 않습니다.";
                RotationBrush = Brushes.DeepSkyBlue;
            }
            else
            {
                string running = state.IsTimerRunning ? "전환 중" : "정지";
                RotationText = state.IsSubFallbackActive ? $"로컬 복구 · {running}" : running;
                RotationDetail = state.IsTimerRunning ? "로컬 페이지 회전 타이머 실행 중" : "로컬 페이지 회전 타이머 정지";
                RotationBrush = state.IsSubFallbackActive ? Brushes.DeepSkyBlue
                    : state.IsTimerRunning ? Brushes.LimeGreen : Brushes.Gold;
            }

            while (PageDots.Count > total) PageDots.RemoveAt(PageDots.Count - 1);
            while (PageDots.Count < total) PageDots.Add(Brushes.DimGray);
            for (int i = 0; i < total; i++)
            {
                Brush brush = i + 1 == current ? RotationBrush : Brushes.DimGray;
                if (!ReferenceEquals(PageDots[i], brush)) PageDots[i] = brush;
            }
        }
    }
}
