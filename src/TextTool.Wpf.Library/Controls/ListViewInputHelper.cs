using System.Windows.Controls;
using System.Windows.Input;
using TextTool.Library.ComponentModel;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf.Library.Controls
{
    public class ListViewInputHelper : IAttachedPropertyControl
    {
        private readonly ListView _target;
        private bool _isEnabled;

        public ListViewInputHelper(ListView target)
        {
            if (!target.IsLoaded)
            {
                throw new InvalidOperationException($"ListView '{nameof(target)}' is not loaded.");
            }

            _target = target;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled)
                {
                    return;
                }

                _isEnabled = value;
                if (_isEnabled)
                {
                    _target.MouseUp += OnListViewMouseUp;
                    _target.KeyDown += OnListViewKeyDown;
                }
                else
                {
                    _target.MouseUp -= OnListViewMouseUp;
                    _target.KeyDown += OnListViewKeyDown;
                }
            }
        }

        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            {
                if (_target.Items.Count == 1)
                {
                    _target.SelectAll();
                }
            }
        }

        // UnselectAll when click at blank area
        private void OnListViewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ListViewService.GetItemsUpdater(_target) is INotifyItemsUpdated && _target.SelectedItems.Count > 0 && e.OriginalSource is ScrollViewer)
            {
                _target.UnselectAll();
            }
        }
    }
}
