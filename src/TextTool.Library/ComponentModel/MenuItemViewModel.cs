using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace TextTool.Library.ComponentModel;

public class MenuItemViewModel : ObservableObject
{
    private static readonly RelayCommand<object> _EmptyCommand = new(static (obj) => { }, static (obj) => true);
    private readonly string _name;
    private readonly ICommand _command;
    private readonly Predicate<object?> _canEnable;
    private readonly BulkObservableCollection<MenuItemViewModel> _items;

    public MenuItemViewModel(string name, ICommand command)
    {
        _name = name;
        _command = command;
        _canEnable = _command.CanExecute;
        _items = [];
    }

    public MenuItemViewModel(string name, IEnumerable<MenuItemViewModel> items)
    {
        _name = name;
        _command = _EmptyCommand;
        _canEnable = _command.CanExecute;
        _items = new(items);
    }

    public MenuItemViewModel(string name, IEnumerable<MenuItemViewModel> items, Predicate<object?> canEnable)
    {
        _name = name;
        _canEnable = canEnable;
        _command = new RelayCommand<object>(static (obj) => { }, canEnable);
        _items = new(items);
    }

    public string Name => _name;

    public ICommand Command => _command;

    public BulkObservableCollection<MenuItemViewModel> Items => _items;

    public Predicate<object?> CanEnable => _canEnable;
}
