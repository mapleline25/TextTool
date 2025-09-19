// Copyright (c) .NET Foundation and Contributors
// Licensed under the MIT license.
//
// Forked from Windows Presentation Foundation (WPF) (dotnet/wpf).
// See: https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ItemsControl.cs.
//
// Summery of changes:
// The file includes the method ItemsControlHelper.EqualsEx() which is copied from the System.Windows.Controls.ItemsControl to be used with SimpleListCollectionView.
// The method is identical to the original and has not been modified. 

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TextTool.Wpf.Helpers;

public static class ItemsControlHelper
{
    // A version of Object.Equals with paranoia for mismatched types, to avoid problems
    // with classes that implement Object.Equals poorly
    internal static bool EqualsEx(object o1, object o2)
    {
        try
        {
            return Equals(o1, o2);
        }
        catch (InvalidCastException)
        {
            // A common programming error: the type of o1 overrides Equals(object o2)
            // but mistakenly assumes that o2 has the same type as o1:
            //     MyType x = (MyType)o2;
            // This throws InvalidCastException when o2 is a sentinel object,
            // e.g. UnsetValue, DisconnectedItem, NewItemPlaceholder, etc.
            // Rather than crash, just return false - the objects are clearly unequal.
            return false;
        }
    }
}
