using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Services;
using CowAuctionSmall.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Canvas = System.Windows.Controls.Canvas;


namespace CowAuctionSmall.ViewModels
{
    /// <summary>
    /// 각각의 대기,진행 패널들을 관리하는 곳
    /// </summary>
    public partial class AuctionContPanelViewModel : IDisposable, INotifyPropertyChanged
    {

        private DisplaySelect _displaySelect;
        private gValues _cowInfo;

        public VirtualizingStackPanel _panel; //표출되어줄 패널

        private bool _isRunning; //테두리 깜박이는거 여부

        private int _totalRunningPage; // user.xml에서 정의한 표출 페이지 갯수

        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool HasPaternityMatch => !string.IsNullOrWhiteSpace(CowInfo.PaternityMatch) && CowInfo.PaternityMatch != "-";

        public Thickness NoteMargin => HasPaternityMatch ? new Thickness(32, 110, 0, 1) : new Thickness(2, 110, 0, 1);


        public gValues CowInfo
        {
            get => _cowInfo;
            set
            {
                _cowInfo = value;
                OnPropertyChanged(nameof(CowInfo));

                // ★ 추가
                OnPropertyChanged(nameof(HasPaternityMatch));
                OnPropertyChanged(nameof(NoteMargin));
            }
        }


        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        public String? Note
        {
            get { return _cowInfo?.Note; }
            set
            {
                if (_cowInfo != null)
                {
                    _cowInfo.Note = value;
                    if (value.Length < 9 )
                    {

                    }
                    OnPropertyChanged(nameof(_cowInfo.Note));
                }
            }
        }

        /// <summary>
        /// 새로운 개체 정보로 뷰모델 상태를 갱신한다.
        /// </summary>
        public void UpdateCowInfo(gValues cowinfo)
        {
            var previousNote = _cowInfo?.Note;

            _cowInfo = cowinfo;
            IsRunning = cowinfo.IsRunning;
            UpdateSexDisc(cowinfo);

            OnPropertyChanged(nameof(CowInfo));

            // ★ 추가: PaternityMatch 기반 파생 속성도 갱신 알림
            OnPropertyChanged(nameof(HasPaternityMatch));
            OnPropertyChanged(nameof(NoteMargin));

            if (!string.Equals(previousNote, cowinfo.Note, StringComparison.Ordinal))
            {
                OnPropertyChanged(nameof(Note));
                ResetNotePositions();
            }
        }


        private double _notePosition;
        public double NotePosition
        {
            get { return _notePosition; }
            set
            {
                _notePosition = value;
            }
        }

        // Note 위치 업데이트 로직 추가
        /// <summary>
        /// 노트 스크롤 위치를 갱신한다.
        /// </summary>
        public void UpdateNotePosition(double newPosition)
        {
            NotePosition = newPosition;
        }

        private string _sexDisc =string.Empty;
        public string SexDisc // 성별에 구분자가지 포함
        {
            get { return _sexDisc; }
            set
            {
                _sexDisc = value;
                OnPropertyChanged(nameof(SexDisc));
            }
        }

        private readonly Dictionary<string, double> _notePositions = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _noteTextSnapshot = new Dictionary<string, string>(StringComparer.Ordinal);

        public double? GetNotePosition(string pageKey)
        {
            return _notePositions.TryGetValue(pageKey, out var pos) ? pos : (double?)null;
        }

        public void SetNotePosition(string pageKey, double position)
        {
            _notePositions[pageKey] = position;
        }

        public bool IsSameNoteText(string pageKey, string? noteText)
        {
            noteText ??= string.Empty;
            return _noteTextSnapshot.TryGetValue(pageKey, out var stored) && stored == noteText;
        }

        public void UpdateNoteTextSnapshot(string pageKey, string? noteText)
        {
            _noteTextSnapshot[pageKey] = noteText ?? string.Empty;
        }

        public void ResetNotePositions()
        {
            _notePositions.Clear();
            _noteTextSnapshot.Clear();
        }
        /// <summary>
        /// 패널별 진행 화면 뷰모델을 구성한다.
        /// </summary>
        public AuctionContPanelViewModel( gValues cowinfo, DisplaySelect display, VirtualizingStackPanel panel, List<int> pageTime, int totalRunningPage)
        {
            _displaySelect = display;
            _cowInfo = cowinfo;
            _panel = panel;
            _totalRunningPage = totalRunningPage;

            _isRunning = cowinfo.IsRunning;

            UpdateSexDisc(cowinfo);

            if (_isRunning)
            {
                Debug.WriteLine($"진행중 화면 {cowinfo.SpaceIndex}");
            }

        }

        /// <summary>
        /// 성별/구분 문자열을 계산해 노출용 값을 갱신한다.
        /// </summary>
        private void UpdateSexDisc(gValues cowinfo)
        {
            if (cowinfo.Sex.Length < 2)
            {
                _sexDisc = cowinfo.Sex + " " + cowinfo.StrCowDistinction;
            }
            else
            {
                _sexDisc = cowinfo.StrCowDistinction;
            }
            OnPropertyChanged(nameof(SexDisc));
        }




        // IDisposable 인터페이스 구현
        /// <summary>
        /// 리소스를 정리한다.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // 종료자 정의
        /// <summary>
        /// 정리 루틴을 실행한다.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 원래 있던 정리 코드
                    PropertyChanged = null;
                }

                _disposed = true;
            }
        }

        // PropertyChanged 이벤트 발생 메서드
        /// <summary>
        /// 속성 변경 알림을 전달한다.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
