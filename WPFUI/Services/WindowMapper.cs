using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Windows;
using WPFUI.ViewModels;

namespace WPFUI.Services;

public sealed class WindowMapper
{
    private readonly Dictionary<Type, Type> _mappings = [];

    public WindowMapper()
    {
        RegisterMapping<MainWindowViewModel, MainWindow>();
    }

    public void RegisterMapping<TViewModel, TWindow>() where TViewModel : ObservableObject where TWindow : Window
    {
        _mappings.Add(typeof(TViewModel), typeof(TWindow));
    }

    public Type? GetWindowTypeForViewModel<TViewModel>()
    {
        _mappings.TryGetValue(typeof(TViewModel), out var windowType);
        return windowType;
    }

    public Type? GetWindowTypeForViewModel(Type viewModelType)
    {
        _mappings.TryGetValue(viewModelType, out var type);
        return type;
    }
}
