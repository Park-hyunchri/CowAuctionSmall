using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace CowAuctionSmall.Presentation.Display
{
    internal enum PanelDisplayMode
    {
        None,
        Logo,
        Running,
        Sold,
        UnSold
    }

    internal sealed class PanelDisplayState
    {
        public PanelDisplayMode Mode;
        public AuctionContPanelViewModel? RunningViewModel;
        public string? RunningKey;
        public List<UserControl>? RunningPages;
        public RunningNoteHost128? RunningNoteHost;
        public bool UseRunningNoteHost;
        public UserControl? SoldView;
        public string? SoldKey;
        public UserControl? UnSoldView;
        public string? UnSoldKey;
        public Image? LogoView;
        public string? LogoPath;
        public gValues? LastData;
    }

    /// <summary>Owns cached panel state and releases running view models and note hosts.</summary>
    internal sealed class DisplayPanelStore : IDisposable
    {
        private readonly Dictionary<VirtualizingStackPanel, PanelDisplayState> _panelStates = new();
        private readonly ObservableCollection<AuctionContPanelViewModel> _runningItems;

        internal DisplayPanelStore(ObservableCollection<AuctionContPanelViewModel> runningItems)
        {
            _runningItems = runningItems;
        }

        /// <summary>
        /// 패널별 상태 객체를 조회하거나 생성한다.
        /// </summary>
        internal PanelDisplayState Get(VirtualizingStackPanel panel)
        {
            if (_panelStates.TryGetValue(panel, out var state))
            {
                return state;
            }

            state = new PanelDisplayState
            {
                Mode = PanelDisplayMode.None
            };
            _panelStates[panel] = state;
            return state;
        }

        /// <summary>
        /// 진행 상태의 뷰모델을 해제하고 상태를 초기화한다.
        /// </summary>
        internal void ClearRunning(PanelDisplayState state, VirtualizingStackPanel panel)
        {
            if (state.RunningViewModel != null)
            {
                _runningItems.Remove(state.RunningViewModel);
                state.RunningViewModel.Dispose();
                state.RunningViewModel = null;
            }

            // 💡 런닝 노트 호스트 및 바인딩 완전히 초기화 (친자 뱃지 잔재 제거)
            if (state.RunningNoteHost != null)
            {
                state.RunningNoteHost.PageContent = null;
                BindingOperations.ClearBinding(state.RunningNoteHost, RunningNoteHost128.ItemDisplayProperty);
                state.RunningNoteHost.ItemDisplay = null;
                state.RunningNoteHost.ScrollOwner = null;
                state.RunningNoteHost.DataContext = null;
                state.RunningNoteHost = null;
            }

            state.RunningKey = null;
            state.RunningPages = null;
            state.UseRunningNoteHost = false;
            state.LastData = null;
            state.SoldView = null;
            state.SoldKey = null;
            state.UnSoldView = null;
            state.UnSoldKey = null;

            panel.DataContext = null;
        }

        public void Dispose()
        {
            foreach (var pair in _panelStates.ToArray())
            {
                ClearRunning(pair.Value, pair.Key);
                pair.Key.Children.Clear();
            }
            _panelStates.Clear();
            _runningItems.Clear();
        }
    }
}
