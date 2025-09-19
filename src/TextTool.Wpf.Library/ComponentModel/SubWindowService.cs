using System.Windows;

namespace TextTool.Wpf.Library.ComponentModel;

public static class SubWindowService
{
    public static T GetWindow<T>(Window? owner) where T : Window, new()
    {
        return WindowWrapper<T>.Instance(owner);
    }
    
    private static class WindowWrapper<T> where T : Window, new()
    {
        private static T? _instance;

        public static T Instance(Window? owner)
        {
            if (_instance == null)
            {
                _instance = new();
                _instance.Closed += OnClosed;
            }
            else
            {
                _instance.Focus();
            }

            if (_instance.Owner != owner)
            {
                _instance.Owner = owner;
            }

            return _instance;
        }

        private static void OnClosed(object? sender, EventArgs e)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.Closed -= OnClosed;
            try
            {
                _instance.Owner.Focus();
            }
            catch { }

            _instance = null;
        }
    }
}
