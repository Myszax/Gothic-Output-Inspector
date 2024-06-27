using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Forms;
using WPFUI.Comparer;
using WPFUI.Components;
using WPFUI.Enums;
using WPFUI.Extensions;
using WPFUI.Interfaces;
using WPFUI.Models;
using WPFUI.Services;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public sealed partial class CompareWindowViewModel : ObservableObject, ICloseable
{
    public AudioPlayerViewModel AudioPlayerViewModel { get; }
    public MainWindowViewModel MainWindowViewModel { get; }

    public ICollectionView ConversationDiffCollection { get; private set; }

    public int AddedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff?.Type == ComparisonResultType.Added).Count();
    public int ChangedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff?.Type == ComparisonResultType.Changed).Count();
    public int FilteredConversationsDiffCount => ConversationDiffCollection.Cast<object>().Count();
    public int LoadedConversationsDiffCount => _conversationDiffList.Count;
    public int RemovedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff?.Type == ComparisonResultType.Removed).Count();

    [ObservableProperty]
    private string _filterValueCompareMode = string.Empty;

    [ObservableProperty]
    private bool _isEnabledFilterIsInspected;

    [ObservableProperty]
    private bool _isEnabledIgnoreInspectedWhileTransfer;

    [ObservableProperty]
    private ColoredText _propertyColor = new();

    [ObservableProperty]
    private StringComparison _selectedComparisonMethod;

    [ObservableProperty]
    private ConversationDiff _selectedConversationDiff = new();

    [ObservableProperty]
    private FilterType _selectedFilterType;

    [ObservableProperty]
    private ConversationDiff? _selectedGridRow = new();

    [ObservableProperty]
    private string _title = string.Empty;

    private readonly List<ConversationDiff> _conversationDiffList;
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    public CompareWindowViewModel(ISettingsService settingsService, IDataService dataService, IDialogService dialogService, AudioPlayerViewModel audioPlayerViewModel, MainWindowViewModel mainWindowViewModel)
    {
        _settingsService = settingsService;
        _dataService = dataService;
        _dialogService = dialogService;
        AudioPlayerViewModel = audioPlayerViewModel;
        MainWindowViewModel = mainWindowViewModel;

        _conversationDiffList = _dataService.Data.CompareTo(_dataService.ConversationsToCompare, x => x.Name)
            .Select(x => new ConversationDiff() { Diff = x, Name = x.Original?.Name ?? x.Compared!.Name }).ToList();

        FilterValueCompareMode = string.Empty;
        ConversationDiffCollection = CollectionViewSource.GetDefaultView(_conversationDiffList);
        ConversationDiffCollection.Filter = FilterConversationDiffCollection;
        OnPropertyChanged(nameof(LoadedConversationsDiffCount));
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
        Title = _dataService.CompareWindowTitle;

        SelectedComparisonMethod = _settingsService.CompareModeComparisonMethod;
        SelectedFilterType = _settingsService.CompareModeSelectedFilterType;
        IsEnabledFilterIsInspected = _settingsService.CompareModeIsEnabledFilterIsInspected;
        IsEnabledIgnoreInspectedWhileTransfer = _settingsService.CompareModeIsEnabledIgnoreInspectedWhileTransfer;
    }

    public bool CanClose()
    {
        if (LoadedConversationsDiffCount == 0)
        {
            CleanUpOnClose();
            return true;
        }

        var result = _dialogService.ShowMessageBox(COMPARE_MODE_EXIT_PROMPT, CAPTION_EXITING_COMPARE_MODE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            CleanUpOnClose();
            return true;
        }

        return false;
    }

    private void CleanUpOnClose()
    {
        _dataService.ConversationsToCompare.Clear();
        AudioPlayerViewModel.StopCommand.Execute(null);

        _settingsService.CompareModeComparisonMethod = SelectedComparisonMethod;
        _settingsService.CompareModeSelectedFilterType = SelectedFilterType;
        _settingsService.CompareModeIsEnabledFilterIsInspected = IsEnabledFilterIsInspected;
        _settingsService.CompareModeIsEnabledIgnoreInspectedWhileTransfer = IsEnabledIgnoreInspectedWhileTransfer;
    }

    private void SelectNextConversationDiffGridItem()
    {
        _conversationDiffList.Remove(SelectedConversationDiff);

        if (FilteredConversationsDiffCount > 0)
        {
            if (FilteredConversationsDiffCount == ConversationDiffCollection.CurrentPosition + 1)
                ConversationDiffCollection.MoveCurrentToPrevious();
            else
                ConversationDiffCollection.MoveCurrentToNext();
        }
        else
            SelectedConversationDiff = new();
    }

    [RelayCommand]
    private void AcceptComparedChanges()
    {
        if (SelectedConversationDiff.Diff is null)
            return;

        switch (SelectedConversationDiff.Diff.Type)
        {
            case ComparisonResultType.Removed:
                if (SelectedConversationDiff.Diff.Original is not null)
                    _dataService.Data.Remove(SelectedConversationDiff.Diff.Original);
                break;
            case ComparisonResultType.Added:
                if (SelectedConversationDiff.Diff.Compared is not null)
                    _dataService.Data.Add(SelectedConversationDiff.Diff.Compared);
                break;
            case ComparisonResultType.Changed:
                if (IsEnabledIgnoreInspectedWhileTransfer)
                {
                    if (!SelectedConversationDiff.Diff.Variances.TryGetValue(nameof(Conversation.IsInspected), out ComparisonVariance? value))
                    {
                        SelectedConversationDiff.Diff.Variances
                            .Add(nameof(Conversation.IsInspected), new ComparisonVariance { PropertyName = nameof(Conversation.IsInspected), ValA = true, ValB = true });
                    }
                    else
                    {
                        value.ValB = true;
                    }
                    SelectedConversationDiff.Diff.Compared!.IsInspected = true;
                }

                SelectedConversationDiff.Diff.Compared.TransferTo(SelectedConversationDiff.Diff.Original, SelectedConversationDiff.Diff.Variances);
                break;
        }

        SelectNextConversationDiffGridItem();
        ConversationDiffCollection.Refresh();
        MainWindowViewModel.ProjectFileChangedCommand.Execute(null);
        OnPropertyChanged(nameof(LoadedConversationsDiffCount));
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
        OnPropertyChanged(nameof(AddedConversationsDiffCount));
        OnPropertyChanged(nameof(ChangedConversationsDiffCount));
        OnPropertyChanged(nameof(RemovedConversationsDiffCount));
    }

    [RelayCommand]
    private void DiscardComparedChanges()
    {
        if (SelectedConversationDiff.Diff is null)
            return;

        SelectNextConversationDiffGridItem();
        ConversationDiffCollection.Refresh();
        OnPropertyChanged(nameof(LoadedConversationsDiffCount));
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
        OnPropertyChanged(nameof(AddedConversationsDiffCount));
        OnPropertyChanged(nameof(ChangedConversationsDiffCount));
        OnPropertyChanged(nameof(RemovedConversationsDiffCount));
    }

    private bool FilterConversationDiffCollection(object obj)
    {
        if (obj is null || obj is not ConversationDiff conversationDiff)
            return false;

        if (conversationDiff.Diff?.Compared is not null)
        {
            if (SelectedFilterType == FilterType.HideAll)
            {
                if (IsEnabledFilterIsInspected && conversationDiff.Diff.Variances.Count == 1 && conversationDiff.Diff.Variances.ContainsKey("IsInspected"))
                    return false;
            }
            else
            {
                if (IsEnabledFilterIsInspected && conversationDiff.Diff.Variances.ContainsKey("IsInspected"))
                    return false;
            }
        }

        if (FilterValueCompareMode.Length > 0 && !conversationDiff.Name.Contains(FilterValueCompareMode, SelectedComparisonMethod))
            return false;

        return true;
    }

    [RelayCommand]
    private void RefreshConversationDiffCollection(bool checkForEmptyFilterValue = true)
    {
        if (checkForEmptyFilterValue && string.IsNullOrEmpty(FilterValueCompareMode))
            return;

        ConversationDiffCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
    }

    partial void OnFilterValueCompareModeChanged(string value)
    {
        ConversationDiffCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
    }

    partial void OnSelectedConversationDiffChanged(ConversationDiff value)
    {
        if (value.Diff is null)
            return;

        if (value.Diff.Type == ComparisonResultType.Removed && value.Diff.Original is not null)
            _dataService.CurrentConversation = value.Diff.Original;
        else if (value.Diff.Compared is not null)
            _dataService.CurrentConversation = value.Diff.Compared;

        PropertyColor = ColoredText.Create(value.Diff.Variances);
    }

    partial void OnSelectedGridRowChanged(ConversationDiff? value)
    {
        if (value is null)
            return;

        SelectedConversationDiff = value;
    }
}
