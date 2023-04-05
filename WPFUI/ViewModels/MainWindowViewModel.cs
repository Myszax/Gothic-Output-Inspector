using CommunityToolkit.Mvvm.ComponentModel;
using Parser;
using System;
using System.Collections.Generic;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object _selectedTreeItem;

    private List<Dialogue> _parsedDialogues = new();

    public MainWindowViewModel()
    {
        var path = @"../../../../Parser/g2nk_ou.bin";
        var parser = new Reader(path, 1250);

        try
        {
            _parsedDialogues = parser.Parse();
        }
        catch (Exception)
        {
        }
    }
}
