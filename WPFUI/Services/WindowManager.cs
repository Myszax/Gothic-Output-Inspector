using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Windows;

namespace WPFUI.Services;

public interface IWindowManager
{
    void ShowWindow(ObservableObject viewModel, Func<bool> canCloseWindow);
}

public sealed class WindowManager : IWindowManager
{
    private readonly WindowMapper _windowMapper;

    public WindowManager(WindowMapper windowMapper)
    {
        _windowMapper = windowMapper;
    }

    public void ShowWindow(ObservableObject viewModel, Func<bool> canCloseWindow)
    {
        var windowType = _windowMapper.GetWindowTypeForViewModel(viewModel.GetType());
        if (windowType is null)
            return;

        if (Activator.CreateInstance(windowType) is not Window window)
            return;

        window.DataContext = viewModel;

        void closeEventHandler(object? s, CancelEventArgs e)
        {
            if (canCloseWindow())
                window.Closing -= closeEventHandler;
            else
                e.Cancel = true;
        }

        window.Closing += closeEventHandler;
        window.Show();
    }
}
