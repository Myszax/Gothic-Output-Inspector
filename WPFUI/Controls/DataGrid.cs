using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace WPFUI.Controls;

// There is a bug in PresentationFramework
// Occasionally appears when selecting item on DataGrid
// Critical issue that can crash application
// This code disable Automation Peers for DataGrid
// So I have to use other derived class with same exact name
// Code by `vasiliy-vdovichenko` from https://github.com/dotnet/wpf/issues/4279#issuecomment-1068216694
// Thanks!
public sealed class DataGrid : System.Windows.Controls.DataGrid
{
    /// <summary>
    /// Turn off UI Automation
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new CustomDataGridExAutomationPeer(this);

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        var dataGrid = e.Source as DataGrid;

        if (dataGrid is not null && dataGrid.SelectedItem is not null)
        {
            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
            dataGrid.UpdateLayout();
        }

        base.OnSelectionChanged(e);
    }
}

public sealed class CustomDataGridExAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetNameCore() => "DataGridExAutomationPeer";

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

    protected override List<AutomationPeer> GetChildrenCore() => [];
}
