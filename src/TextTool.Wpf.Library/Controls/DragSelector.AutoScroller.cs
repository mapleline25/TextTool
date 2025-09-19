using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TextTool.Library.Extensions;

namespace TextTool.Library.Wpf.Controls;

public partial class DragSelector
{
    private class AutoScroller
    {
        private const double OffsetWidth = 16;
        private const double OffsetHeight = 1;
        private readonly ScrollViewer _scrollViewer = null!;
        private readonly UIElement _content = null!;
        private readonly ScrollState _stateX = null!;
        private readonly ScrollState _stateY = null!;
        private bool _isEnabled;

        public AutoScroller(ScrollViewer scrollViewer)
        {
            if (!scrollViewer.IsLoaded)
            {
                throw new InvalidOperationException($"ScrollViewer '{nameof(scrollViewer)}' is not loaded.");
            }

            if (scrollViewer.Content is not UIElement content)
            {
                throw new ArgumentException("'ScrollViewer.Content' must be 'UIElement'");
            }

            _scrollViewer = scrollViewer;
            _content = content;
            _stateX = new ScrollState(HorizontalScroll);
            _stateY = new ScrollState(VerticalScroll);
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                    {
                        _scrollViewer.PreviewMouseMove += OnMouseMove;
                        _scrollViewer.LostMouseCapture += OnLostMouseCapture;
                    }
                    else
                    {
                        _stateX.StopScroll();
                        _stateY.StopScroll();
                        _scrollViewer.PreviewMouseMove -= OnMouseMove;
                        _scrollViewer.LostMouseCapture -= OnLostMouseCapture;
                    }
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource == _scrollViewer && Mouse.Captured == _scrollViewer
                && (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed))
            {
                AutoScroll();
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            _stateX.StopScroll();
            _stateY.StopScroll();
        }

        private void AutoScroll()
        {
            Point position = Mouse.GetPosition(_content);
            Rect bounds = new Rect(new Point(), _content.RenderSize);

            double leftOffset = 0;
            double rightOffset = _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth;
            if (rightOffset > leftOffset)
            {
                double leftDelta = bounds.Left - position.X;
                double rightDelta = position.X - bounds.Right;

                if (leftDelta > 0 || leftDelta.EqualsEx(0))
                {
                    _stateX.StartScroll(leftDelta, _scrollViewer.HorizontalOffset, leftOffset, OffsetWidth);
                }
                else if (rightDelta > 0 || rightDelta.EqualsEx(0))
                {
                    _stateX.StartScroll(rightDelta, _scrollViewer.HorizontalOffset, rightOffset, OffsetWidth);
                }
                else
                {
                    _stateX.StopScroll();
                }
            }
            else
            {
                _stateX.StopScroll();
            }

            double topOffset = 0;
            double bottomOffset = _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight;
            if (bottomOffset > topOffset)
            {
                double topDelta = bounds.Top - position.Y;
                double bottomDelta = position.Y - bounds.Bottom;

                if (topDelta > 0 || topDelta.EqualsEx(0))
                {
                    _stateY.StartScroll(topDelta, _scrollViewer.VerticalOffset, topOffset, OffsetHeight);
                }
                else if (bottomDelta > 0 || bottomDelta.EqualsEx(0))
                {
                    _stateY.StartScroll(bottomDelta, _scrollViewer.VerticalOffset, bottomOffset, OffsetHeight);
                }
                else
                {
                    _stateY.StopScroll();
                }
            }
            else
            {
                _stateY.StopScroll();
            }
        }

        private void HorizontalScroll(double offset)
        {
            _scrollViewer.ScrollToHorizontalOffset(offset);
        }

        private void VerticalScroll(double offset)
        {
            _scrollViewer.ScrollToVerticalOffset(offset);
        }
    }

    private class ScrollState : DispatcherObject
    {
        private const double MinCountStep = 0.1;
        private const double CountStepCoefficient = 0.023;
        private const double BaseInterval = 20;
        private const double BaseCountStep = 1;
        private readonly System.Timers.Timer _timer = null!;
        private readonly Action<double> _runScroll;
        private bool _active;
        private bool _positiveScroll;
        private double _startOffset;
        private double _endOffset;
        private double _offset;
        private double _offsetStep;
        private double _actualOffsetStep;
        private double _interval;
        private double _count;
        private double _countStep;

        public ScrollState(Action<double> scrollAction)
        {
            _timer = new();
            _timer.Elapsed += OnTimerElapsed;
            _runScroll = scrollAction;
            _positiveScroll = true;
            _offsetStep = 1;
            _countStep = 1;
            _actualOffsetStep = _offsetStep * _countStep;
        }

        public void StartScroll(double distance, double startOffset, double endOffset, double offsetStep)
        {
            if (offsetStep < 0 || offsetStep.EqualsEx(0) || startOffset.EqualsEx(endOffset))
            {
                return;
            }

            SetCounter(distance);
            bool positiveScroll = startOffset < endOffset;
            if (_positiveScroll != positiveScroll)
            {
                StopScroll();
            }
            _positiveScroll = positiveScroll;
            _offsetStep = _positiveScroll ? offsetStep : -offsetStep;
            _actualOffsetStep = _offsetStep * _countStep;

            if (!_active)
            {
                _active = true;
                _startOffset = startOffset;
                _endOffset = endOffset;
                _offset = _startOffset;
                _count = 0;
                Scroll();
                _timer.Enabled = true;
            }
        }

        public void StopScroll()
        {
            if (_active)
            {
                _active = false;
                _timer.Enabled = false;
            }
        }

        private void SetCounter(double distance)
        {
            double countStep = (distance > 0 ? distance * CountStepCoefficient : 0) + MinCountStep;
            if (countStep < BaseCountStep)
            {
                _interval = BaseInterval / countStep;
                _countStep = BaseCountStep;
            }
            else
            {
                _interval = BaseInterval;
                _countStep = countStep;
            }
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(Scroll);
            }
            catch { }
        }

        private void Scroll()
        {
            if (!_active)
            {
                return;
            }

            if (!_timer.Interval.EqualsEx(_interval))
            {
                _timer.Interval = _interval;
            }

            double delta = _endOffset - _offset;
            if (_positiveScroll ? delta > _actualOffsetStep : delta < _actualOffsetStep)
            {
                _count += _countStep;
                _offset = _startOffset + _offsetStep * _count;
                _runScroll(_offset);
            }
            else if (delta.EqualsEx(0))
            {
                StopScroll();
            }
            else
            {
                _offset = _endOffset;
                _runScroll(_offset);
                StopScroll();
            }
        }
    }
}
