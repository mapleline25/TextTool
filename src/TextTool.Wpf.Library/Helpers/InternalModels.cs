using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using TextTool.Library.Utils;

namespace TextTool.Wpf.Library.Helpers;

public static class InternalModels
{
    // System.Windows.Controls.SelectedItemCollection : ObservableCollection<object>
    public static class SelectedItemCollection
    {
        public static Action<ObservableCollection<object>> BeginUpdateSelectedItems => _BeginUpdateSelectedItems;

        public static Action<ObservableCollection<object>> EndUpdateSelectedItems => _EndUpdateSelectedItems;

        private static readonly Type _Type = TypeAccess.GetType("System.Windows.Controls.SelectedItemCollection")!;
        private static readonly Action<ObservableCollection<object>> _BeginUpdateSelectedItems =
            TypeAccess.CreateMethodDelegate<Action<ObservableCollection<object>>>(_Type, null, "BeginUpdateSelectedItems", []);
        private static readonly Action<ObservableCollection<object>> _EndUpdateSelectedItems =
            TypeAccess.CreateMethodDelegate<Action<ObservableCollection<object>>>(_Type, null, "EndUpdateSelectedItems", []);
    }

    public static class SystemXmlHelper
    {
        public static readonly Type Type = TypeAccess.GetType("MS.Internal.SystemXmlHelper")!;
        public delegate IComparer PrepareXmlComparerMethod(IEnumerable collection, SortDescriptionCollection sort, CultureInfo culture);

        public static PrepareXmlComparerMethod PrepareXmlComparer => _PrepareXmlComparer;

        private static readonly PrepareXmlComparerMethod _PrepareXmlComparer =
            TypeAccess.CreateMethodDelegate<PrepareXmlComparerMethod>(
                Type,
                null,
                "PrepareXmlComparer",
                [typeof(IEnumerable), typeof(SortDescriptionCollection), typeof(CultureInfo)]);
    }

    public static class SortFieldComparer
    {
        public static readonly Type Type = TypeAccess.GetType("MS.Internal.Data.SortFieldComparer")!;
        public delegate IComparer Constructor(SortDescriptionCollection sortFields, CultureInfo culture);

        public static Constructor CreateInstance => _CreateInstance;

        public static Action<ArrayList, IComparer> SortHelper => _SortHelper;

        private static readonly Constructor _CreateInstance = TypeAccess.CreateConstuctorDelegate<Constructor>(Type);
        private static readonly Action<ArrayList, IComparer> _SortHelper =
            TypeAccess.CreateMethodDelegate<Action<ArrayList, IComparer>>(Type, null, "SortHelper", [typeof(ArrayList), typeof(IComparer)]);
    }

    public static class NamedObject
    {
        public static readonly Type Type = TypeAccess.GetType("MS.Internal.NamedObject")!;
        public delegate object Constructor(string name);

        public static Constructor CreateInstance => _CreateInstance;

        private static readonly Constructor _CreateInstance = TypeAccess.CreateConstuctorDelegate<Constructor>(Type);
    }
}
