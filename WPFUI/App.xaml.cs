﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WPFUI.Services;
using WPFUI.ViewModels;

namespace WPFUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services = new ServiceCollection();

    public App()
    {
        _services.AddSingleton<MainWindowViewModel>();
        _services.AddTransient<CompareWindowViewModel>();
        _services.AddSingleton<AudioPlayerViewModel>();
        _services.AddSingleton<WindowMapper>();
        _services.AddSingleton<ISettingsService, SettingsService>();
        _services.AddSingleton<IDataService, DataService>();
        _services.AddSingleton<IWindowManager, WindowManager>();
        _services.AddSingleton<IDialogService, DialogService>();
        _serviceProvider = _services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var windowManager = _serviceProvider.GetRequiredService<IWindowManager>();
        windowManager.ShowWindow<MainWindowViewModel>();

        base.OnStartup(e);
    }
}