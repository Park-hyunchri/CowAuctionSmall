using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;

namespace CowAuctionSmall.Models
{
    public class FlowTextAnimation
    {
        private TextBlock _textBlock;
        private Canvas _canvas;
        private double _speed;
        private DoubleAnimation _animation;

        public FlowTextAnimation(TextBlock textBlock, Canvas canvas, double speed = 20)
        {
            _textBlock = textBlock;
            _canvas = canvas;
            _speed = speed;
        }

        private DoubleAnimation CreateAnimation(double fromValue, double toValue, TimeSpan duration, EventHandler completedHandler = null)
        {
            DoubleAnimation animation = new DoubleAnimation(fromValue, toValue, duration);
            if (completedHandler != null)
            {
                animation.Completed += completedHandler;
            }
            return animation;
        }

        public void Start()
        {
            _textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _textBlock.Arrange(new Rect(new Size(_textBlock.DesiredSize.Width, _textBlock.DesiredSize.Height)));

            double animationDuration = (_textBlock.ActualWidth + _canvas.ActualWidth) / _speed;
            double startPosition = _canvas.ActualWidth;
            double endPosition = -_textBlock.ActualWidth;

            if (_animation == null)
            {
                _animation = CreateAnimation(startPosition, endPosition, TimeSpan.FromSeconds(animationDuration), Animation_Completed);
            }
            else
            {
                _animation.From = startPosition;
                _animation.To = endPosition;
                _animation.Duration = TimeSpan.FromSeconds(animationDuration);
            }

            Canvas.SetLeft(_textBlock, startPosition);
            _textBlock.BeginAnimation(Canvas.LeftProperty, _animation);
        }

        public void Start2()
        {
            _speed = 15;
            Start();
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            Start();
        }
    }
}