using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;
using CowAuctionSmall.Views;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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


        public gValues CowInfo
        {
            get { return _cowInfo; }
            set
            {
                _cowInfo = value;
                OnPropertyChanged(nameof(CowInfo));
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(_isRunning));
            }
        }

        public String Note
        {
            get { return _cowInfo.Note; }
            set
            {
                OnPropertyChanged(nameof(_cowInfo.Note));
            }
        }


        public AuctionContPanelViewModel(gValues cowinfo, DisplaySelect display, VirtualizingStackPanel panel, List<int> pageTime, int totalRunningPage)
        {
            _displaySelect = display;
            _cowInfo = cowinfo;
            _panel = panel;
            _totalRunningPage = totalRunningPage; //보여줄 진행 정보패널

            _isRunning = cowinfo.IsRunning;

            if (_isRunning ==true)
            {
                Debug.WriteLine($"진행중 화면 {cowinfo.SpaceIndex} ");
            }
            //NotePosition = 300; // 초기 위치
            //StartAnimation();

            //Debug.WriteLine("진행중 화면");

            if (_totalRunningPage == 1)
            {
                // Total running page가 1이면 로테이션을 하지 않고 첫 번째 페이지만 표시
                _displaySelect.DisplayRunningPageNum(_panel, _cowInfo, 1);
            }
            else
            {

            }
        }


        // IDisposable 인터페이스 구현
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // 종료자 정의
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 관리되는 리소스 해제
                    _displaySelect = null;
                    _cowInfo = null;
                    _panel = null;
                    PropertyChanged = null; // 이벤트 핸들러 참조 해제
                }

                // 비관리 리소스를 여기에서 해제

                _disposed = true;
            }
        }
        // PropertyChanged 이벤트 발생 메서드
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _notePosition;
        public double NotePosition
        {
            get => _notePosition;
            set
            {
                _notePosition = value;
                OnPropertyChanged(nameof(_notePosition));
            }
        }

    }

}
