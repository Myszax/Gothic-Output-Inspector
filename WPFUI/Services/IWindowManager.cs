using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Windows;
using WPFUI.Interfaces;

namespace WPFUI.Services;

public interface IWindowManager
{
    public void ShowWindow<TViewModel>();
    public bool? ShowDialogWindow<TViewModel>();
}

public sealed class WindowManager(WindowMapper windowMapper, IServiceProvider serviceProvider) : IWindowManager
{
    private readonly WindowMapper _windowMapper = windowMapper;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public bool? ShowDialogWindow<TViewModel>()
    {
        var window = CreateWindow<TViewModel>();
        return window.ShowDialog();
    }

    public void ShowWindow<TViewModel>()
    {
        var window = CreateWindow<TViewModel>();
        window.Show();
    }

    private Window CreateWindow<TViewModel>()
    {
        var windowType = _windowMapper.GetWindowTypeForViewModel<TViewModel>()
            ?? throw new ArgumentNullException();

        var viewModel = (TViewModel)_serviceProvider.GetRequiredService(typeof(TViewModel));

        if (Activator.CreateInstance(windowType) is not Window window)
            throw new Exception("Created window isn't type of `Window`");

        window.DataContext = viewModel;

        if (viewModel is ICloseable closeable)
        {
            void closeEventHandler(object? s, CancelEventArgs e)
            {
                if (closeable.CanClose())
                    window.Closing -= closeEventHandler;
                else
                    e.Cancel = true;
            }

            window.Closing += closeEventHandler;
        }

        return window;
    }
}
