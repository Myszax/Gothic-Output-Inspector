using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Forms;
using WPFUI.Components;
using WPFUI.Enums;
using WPFUI.Interfaces;
using WPFUI.Models;
using WPFUI.Services;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, ICloseable
{
    public AudioPlayerViewModel AudioPlayerViewModel { get; }
    public List<EncodingMenuItem> Encodings { get; }
    public RangeObservableCollection<Conversation> SelectedConversations { get; } = [];

    public ICollectionView ConversationCollection { get; private set; }

    public int EditedConversationsCount => _dataService.Data.Count(x => x.IsEdited);
    public int FilteredConversationsCount => ConversationCollection.Cast<object>().Count();
    public int InspectedConversationsCount => _dataService.Data.Count(x => x.IsInspected);
    public int LoadedConversationsCount => _dataService.Data.Count;
    public int LoadedNPCsCount => ConversationCollection.Groups.Count;

    [ObservableProperty]
    private string _filterValue = string.Empty;

    [ObservableProperty]
    private bool _isEnabledFilterEditedText;

    [ObservableProperty]
    private bool _isEnabledFilterIsEdited;

    [ObservableProperty]
    private bool _isEnabledFilterIsInspected;

    [ObservableProperty]
    private bool _isEnabledFilterName;

    [ObservableProperty]
    private bool _isEnabledFilterOriginalText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareOtherFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetPathToAudioFilesCommand))]
    private bool _isOuFileImported = false;

    [ObservableProperty]
    private Conversation _selectedConversation = new();

    [ObservableProperty]
    private StringComparison _selectedComparisonMethod;

    [ObservableProperty]
    private Encoding _selectedEncoding;

    [ObservableProperty]
    private FilterType _selectedFilterType;

    [ObservableProperty]
    private Conversation? _selectedGridRow = new();

    [ObservableProperty]
    private Conversation? _selectedLowerGridRow = new();

    [ObservableProperty]
    private string _title = TITLE;

    [ObservableProperty]
    private Encoding _usedEncoding;

    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManager _windowManager;

    private string _previousNpcName = string.Empty;
    private bool _projectWasEdited = false;

    public MainWindowViewModel(ISettingsService settingsService, IDataService dataService, IDialogService dialogService,
        IWindowManager windowManager, AudioPlayerViewModel audioPlayerViewModel)
    {
        _settingsService = settingsService;
        _windowManager = windowManager;
        _dataService = dataService;
        _dialogService = dialogService;

        AudioPlayerViewModel = audioPlayerViewModel;

        Encodings = Encoding.GetEncodings()
            .Select(x => x.GetEncoding())
            .Where(x => x.EncodingName
            .Contains("windows", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.CodePage)
            .Select(x => new EncodingMenuItem { Encoding = x, IsChecked = false })
            .ToList();

        var selectedEncodingIndex = Encodings.FindIndex(x => x.Encoding == _settingsService.MainCurrentEncoding);
        if (selectedEncodingIndex > -1)
        {
            SelectedEncoding = _settingsService.MainCurrentEncoding;
            Encodings[selectedEncodingIndex].IsChecked = true;
        }
        else
        {
            Encodings[1].IsChecked = true;
            SelectedEncoding = Encodings[1].Encoding;
            _settingsService.MainCurrentEncoding = SelectedEncoding;
        }
        UsedEncoding = _settingsService.MainOriginalEncoding;

        SelectedFilterType = _settingsService.MainFilterType;
        SelectedComparisonMethod = _settingsService.MainComparisonMethod;
        IsEnabledFilterIsEdited = _settingsService.MainIsEnabledFilterIsEdited;
        IsEnabledFilterIsInspected = _settingsService.MainIsEnabledFilterIsInspected;
        IsEnabledFilterEditedText = _settingsService.MainIsEnabledFilterEditedText;
        IsEnabledFilterName = _settingsService.MainIsEnabledFilterName;
        IsEnabledFilterOriginalText = _settingsService.MainIsEnabledFilterOriginalText;

        ConversationCollection = CollectionViewSource.GetDefaultView(_dataService.Data);
        SetGroupingAndSortingOnConversationCollection();
        ConversationCollection.Filter = FilterCollection;
    }

    public bool CanClose()
    {
        if (!_projectWasEdited)
            return true;

        var result = _dialogService.ShowMessageBox(SAVE_PROMPT, CAPTION_SAVING, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
            return TryToSaveProject();
        else if (result == DialogResult.Cancel)
            return false;

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator",
    "MVVMTK0034:Direct field reference to [ObservableProperty] backing field", Justification = "Avoid call to OnFilterValueChanged() in this case.")]
    private void CleanReloadRefreshConversationCollection()
    {
        AudioPlayerViewModel.StopCommand.Execute(null);
        _filterValue = string.Empty; // _filterValue has to accessed directly to avoid unnecessary Refresh() on ConversationCollection        
        OnPropertyChanged(nameof(FilterValue)); // then call OnPropertyChanged on FilterValue, it speed ups loading/importing
        SelectedGridRow = null;
        SelectedConversation = new();
        FillLowerDataGrid();
        ConversationCollection = CollectionViewSource.GetDefaultView(_dataService.Data);
        OnPropertyChanged(nameof(LoadedConversationsCount));
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(LoadedNPCsCount));
        OnPropertyChanged(nameof(FilteredConversationsCount));
        OnPropertyChanged(nameof(EditedConversationsCount));
        OnPropertyChanged(nameof(InspectedConversationsCount));
        _projectWasEdited = false;
    }

    private void CleanUpOnCompareWindowClose()
    {
        ConversationCollection = CollectionViewSource.GetDefaultView(_dataService.Data);
        OnPropertyChanged(nameof(LoadedConversationsCount));
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(LoadedNPCsCount));
        OnPropertyChanged(nameof(FilteredConversationsCount));
        OnPropertyChanged(nameof(InspectedConversationsCount));
        OnPropertyChanged(nameof(EditedConversationsCount));
    }

    private void FillLowerDataGrid()
    {
        if (SelectedConversation.NpcName.Equals(_previousNpcName))
            return;

        SelectedConversations.Clear();
        SelectedConversations.AddRange(_dataService.Data.Where(x => x.NpcName.Equals(SelectedConversation.NpcName)));
    }

    private bool FilterCollection(object obj)
    {
        if (obj is not Conversation)
            return false;

        var conversation = (Conversation)obj;

        if (SelectedFilterType == FilterType.HideAll)
        {
            if (IsEnabledFilterIsInspected && conversation.IsInspected)
                return false;
            if (IsEnabledFilterIsEdited && conversation.IsEdited)
                return false;
        }
        else
        {
            if (IsEnabledFilterIsInspected && !conversation.IsInspected)
                return false;
            if (IsEnabledFilterIsEdited && !conversation.IsEdited)
                return false;
        }

        if (IsEnabledFilterName && conversation.Name.Contains(FilterValue, SelectedComparisonMethod))
            return true;
        if (IsEnabledFilterOriginalText && conversation.OriginalText.Contains(FilterValue, SelectedComparisonMethod))
            return true;
        if (IsEnabledFilterEditedText && conversation.EditedText.Contains(FilterValue, SelectedComparisonMethod))
            return true;

        // this check prevents returning false if every search checkbox is unchecked
        return !IsEnabledFilterName && !IsEnabledFilterOriginalText && !IsEnabledFilterEditedText;
    }

    private HashSet<Conversation> OpenFileToCompare(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new Exception("File name is NULL or WhiteSpace");

        var loadedList = Enumerable.Empty<Conversation>();

        if (fileName.EndsWith(".goi", StringComparison.OrdinalIgnoreCase))
        {
            var saveFile = File.ReadAllText(fileName);
            var projectFile = JsonSerializer.Deserialize<SaveFile>(saveFile)
                ?? throw new Exception("Error while reading save file. NULL");

            loadedList = projectFile.Conversations;
        }
        else if (fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            var parser = new Reader(fileName, SelectedEncoding);
            var parsedDialogues = parser.Parse(false);

            loadedList = parsedDialogues.Select(Conversation.CreateConversationFromDialogue);
        }
        else
            throw new Exception($"File '{fileName}' cannot be opened.");

        return loadedList.ToHashSet();
    }

    private void SaveFileToDisk(string path)
    {
        var save = new SaveFile()
        {
            Conversations = _dataService.Data,
            OriginalEncoding = _settingsService.MainOriginalEncoding.HeaderName,
            ChosenEncoding = _settingsService.MainCurrentEncoding.HeaderName,
            AudioPath = _settingsService.AudioPlayerPathToFiles,
            ComparisonMethod = _settingsService.MainComparisonMethod,
            ComparisonMethodCompareMode = _settingsService.CompareModeComparisonMethod,
            EnabledFilterName = _settingsService.MainIsEnabledFilterName,
            EnabledFilterOriginalText = _settingsService.MainIsEnabledFilterOriginalText,
            EnabledFilterEditedText = _settingsService.MainIsEnabledFilterEditedText,
            FilterType = _settingsService.MainFilterType,
            FilterTypeCompareMode = _settingsService.CompareModeSelectedFilterType,
            EnabledFilterIsEdited = _settingsService.MainIsEnabledFilterIsEdited,
            EnabledFilterIsInspected = _settingsService.MainIsEnabledFilterIsInspected,
            EnabledFilterCompareModeIsInspected = _settingsService.CompareModeIsEnabledFilterIsInspected,
            EnabledIgnoreInspectedWhileTransfer = _settingsService.CompareModeIsEnabledIgnoreInspectedWhileTransfer,
            AudioPlayerVolume = _settingsService.AudioPlayerVolume,
            AudioPlayerPreviousVolume = _settingsService.AudioPlayerPreviousVolume,
            AudioPlayerMuted = _settingsService.AudioPlayerIsMuted,
        };

        File.WriteAllText(path, JsonSerializer.Serialize(save, SaveFile.SerializerOptions));
        _projectWasEdited = false;
    }

    private void SelectFirstFilteredConversation()
    {
        if (FilteredConversationsCount > 0)
            ConversationCollection.MoveCurrentToNext();
    }

    private void SetGroupingAndSortingOnConversationCollection()
    {
        var pgd = new PropertyGroupDescription(nameof(Conversation.NpcName));
        pgd.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        ConversationCollection.GroupDescriptions.Add(pgd);

        pgd = new PropertyGroupDescription(nameof(Conversation.Context));
        pgd.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        ConversationCollection.GroupDescriptions.Add(pgd);

        ConversationCollection.SortDescriptions.Add(new SortDescription(nameof(Conversation.Number), ListSortDirection.Ascending));
    }

    private bool TryToSaveProject()
    {
        if (string.IsNullOrWhiteSpace(_settingsService.MainPathToSaveFile))
            return TryToSaveProjectAs();

        try
        {
            SaveFileToDisk(_settingsService.MainPathToSaveFile);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        Title = TITLE + " - " + _settingsService.MainPathToSaveFile;
        return true;
    }

    private bool TryToSaveProjectAs()
    {
        var saveFileDialogSettings = new SaveFileDialogSettings()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi"
        };

        var result = _dialogService.ShowSaveFileDialog(saveFileDialogSettings, out string path);

        if (result != DialogResult.OK || string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            SaveFileToDisk(path);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        _settingsService.MainPathToSaveFile = path;
        Title = TITLE + " - " + path;
        return true;
    }

    [RelayCommand]
    private static void CopyToClipboard(object value)
    {
        if (value is string text && !string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    [RelayCommand]
    private void ChangeEncoding(EncodingMenuItem value)
    {
        foreach (var encodingMenuItem in Encodings)
            encodingMenuItem.IsChecked = false;

        value.IsChecked = true;
        SelectedEncoding = value.Encoding;
    }

    [RelayCommand]
    private void CompareOriginalAndEditedText()
    {
        var edited = !SelectedConversation.EditedText.Equals(SelectedConversation.OriginalText);
        SelectedConversation.IsEdited = edited;
        OnPropertyChanged(nameof(EditedConversationsCount));

        if (edited)
            ProjectFileChanged();
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void CompareOtherFile()
    {
        var fileDialogSettings = new OpenFileDialogSettings()
        {
            Filter = "Supported files|*.bin;*.goi",
            Title = "Open file to compare"
        };

        var fileDialogResult = _dialogService.ShowOpenFileDialog(fileDialogSettings, out string filePath, false);

        if (fileDialogResult != DialogResult.OK || string.IsNullOrEmpty(filePath))
            return;

        try
        {
            _dataService.ConversationsToCompare = OpenFileToCompare(filePath);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _dataService.CompareWindowTitle = TITLE + " - " + filePath;
        var previousConversation = _dataService.CurrentConversation;
        _dataService.CurrentConversation = new();
        AudioPlayerViewModel.StopCommand.Execute(null);
        _windowManager.ShowDialogWindow<CompareWindowViewModel>();

        CleanUpOnCompareWindowClose();
        if (_dataService.Data.Contains(previousConversation))
        {
            SelectedGridRow = previousConversation;
            _dataService.CurrentConversation = previousConversation;
        }
        else
        {
            SelectedGridRow = null;
            SelectedConversation = _dataService.CurrentConversation = new();
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        if (!_projectWasEdited)
            System.Windows.Application.Current.Shutdown();

        var result = _dialogService.ShowMessageBox(SAVE_PROMPT, CAPTION_SAVING, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        bool shouldExit = false;

        if (result == DialogResult.No)
            shouldExit = true;
        else if (result == DialogResult.Yes)
            shouldExit = TryToSaveProject();

        if (shouldExit)
        {
            _projectWasEdited = false; // to avoid another SaveProjectPrompt() in Closing event
            System.Windows.Application.Current.Shutdown();
        }
    }

    [RelayCommand]
    private void ImportFile()
    {
        if (_projectWasEdited)
        {
            var result = _dialogService.ShowMessageBox(SAVE_PROMPT, CAPTION_SAVING, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
                SaveProject();
            else if (result == DialogResult.Cancel)
                return;
        }

        var fileDialogSettings = new OpenFileDialogSettings()
        {
            Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*",
        };

        var fileDialogResult = _dialogService.ShowOpenFileDialog(fileDialogSettings, out string filePath, false);

        if (fileDialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(filePath))
            return;

        var parser = new Reader(filePath, SelectedEncoding);
        List<Dialogue> parsedDialogues;

        try
        {
            parsedDialogues = parser.Parse(false);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UsedEncoding = SelectedEncoding;

        if (_dataService.Data.Count > 0)
            _dataService.Data.Clear();

        var conversationList = new List<Conversation>();

        foreach (var dialogue in parsedDialogues)
            conversationList.Add(Conversation.CreateConversationFromDialogue(dialogue));

        _dataService.Data.AddRange(conversationList);

        CleanReloadRefreshConversationCollection();
        IsOuFileImported = true;
        Title = TITLE + " - NewProject";
        _settingsService.MainPathToSaveFile = string.Empty;
        ProjectFileChanged();
        SelectFirstFilteredConversation();
    }

    [RelayCommand]
    private void OpenProject()
    {
        if (_projectWasEdited)
        {
            var result = _dialogService.ShowMessageBox(SAVE_PROMPT, CAPTION_SAVING, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
                SaveProject();
            else if (result == DialogResult.Cancel)
                return;
        }

        var fileDialogSettings = new OpenFileDialogSettings()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi"
        };

        var fileDialogResult = _dialogService.ShowOpenFileDialog(fileDialogSettings, out string filePath, false);

        if (fileDialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(filePath))
            return;

        SaveFile? projectFile;
        string pathToSaveFile;

        try
        {
            var saveFile = File.ReadAllText(filePath);
            projectFile = JsonSerializer.Deserialize<SaveFile>(saveFile);
            pathToSaveFile = filePath;
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (projectFile is null)
        {
            _dialogService.ShowMessageBox(SAVE_PROJECT_NULL, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(projectFile.ChosenEncoding))
                SelectedEncoding = Encoding.GetEncoding(projectFile.ChosenEncoding);

            if (!string.IsNullOrWhiteSpace(projectFile.OriginalEncoding))
                UsedEncoding = Encoding.GetEncoding(projectFile.OriginalEncoding);

            if (!string.IsNullOrWhiteSpace(projectFile.AudioPath))
                _settingsService.AudioPlayerPathToFiles = projectFile.AudioPath;

            if (Enum.IsDefined(typeof(FilterType), projectFile.FilterType))
                SelectedFilterType = projectFile.FilterType;

            if (Enum.IsDefined(typeof(FilterType), projectFile.FilterType))
                _settingsService.CompareModeSelectedFilterType = projectFile.FilterTypeCompareMode;

            if (Enum.IsDefined(typeof(StringComparison), projectFile.ComparisonMethod))
                SelectedComparisonMethod = projectFile.ComparisonMethod;

            if (Enum.IsDefined(typeof(StringComparison), projectFile.ComparisonMethodCompareMode))
                _settingsService.CompareModeComparisonMethod = projectFile.ComparisonMethodCompareMode;

            if (projectFile.AudioPlayerVolume >= 0f && projectFile.AudioPlayerVolume <= 1f)
                AudioPlayerViewModel.Volume = projectFile.AudioPlayerVolume;

            if (projectFile.AudioPlayerPreviousVolume >= 0f && projectFile.AudioPlayerPreviousVolume <= 1f)
                _settingsService.AudioPlayerPreviousVolume = projectFile.AudioPlayerPreviousVolume;

            IsEnabledFilterName = projectFile.EnabledFilterName;
            IsEnabledFilterOriginalText = projectFile.EnabledFilterOriginalText;
            IsEnabledFilterEditedText = projectFile.EnabledFilterEditedText;
            IsEnabledFilterIsInspected = projectFile.EnabledFilterIsInspected;
            IsEnabledFilterIsEdited = projectFile.EnabledFilterIsEdited;
            _settingsService.CompareModeIsEnabledFilterIsInspected = projectFile.EnabledFilterCompareModeIsInspected;
            _settingsService.CompareModeIsEnabledIgnoreInspectedWhileTransfer = projectFile.EnabledIgnoreInspectedWhileTransfer;
            AudioPlayerViewModel.IsMuted = projectFile.AudioPlayerMuted;
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_dataService.Data.Count > 0)
            _dataService.Data.Clear();

        if (projectFile.Conversations is not null)
            _dataService.Data.AddRange(projectFile.Conversations);

        CleanReloadRefreshConversationCollection();
        IsOuFileImported = true;
        _settingsService.MainPathToSaveFile = pathToSaveFile;
        Title = TITLE + " - " + pathToSaveFile;
        SelectFirstFilteredConversation();
    }

    [RelayCommand]
    private void ProjectFileChanged()
    {
        OnPropertyChanged(nameof(InspectedConversationsCount));
        if (_projectWasEdited)
            return;

        Title += '*';
        _projectWasEdited = true;
    }

    [RelayCommand]
    private void RefreshConversationCollection(bool checkForEmptyFilterValue = true)
    {
        if (checkForEmptyFilterValue && string.IsNullOrEmpty(FilterValue))
            return;

        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsCount));
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SaveProject() => TryToSaveProject();

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SaveProjectAs() => TryToSaveProjectAs();

    [RelayCommand]
    private void SelectedTreeItemChanged(object value)
    {
        if (value is null || value is not Conversation)
            return;

        SelectedConversation = (Conversation)value;
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SetPathToAudioFiles()
    {
        var result = _dialogService.ShowFolderBrowserDialog(new FolderBrowserDialogSettings(), out string path);

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(path))
        {
            _settingsService.AudioPlayerPathToFiles = path + '\\';
            ProjectFileChanged();
        }
        else
            _dialogService.ShowMessageBox(AUDIO_PATH_NOT_SPECIFIED, CAPTION_AUDIO, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    [RelayCommand]
    private void StartPlayback() => AudioPlayerViewModel.PlayCommand.Execute(null);

    partial void OnFilterValueChanged(string value)
    {
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsCount));

        if (FilteredConversationsCount > 0)
        {
            var enumerator = ConversationCollection.GetEnumerator();
            if (enumerator.MoveNext())
                SelectedGridRow = (Conversation)enumerator.Current;
        }
    }

    partial void OnIsEnabledFilterEditedTextChanged(bool value) => _settingsService.MainIsEnabledFilterEditedText = value;
    partial void OnIsEnabledFilterIsEditedChanged(bool value) => _settingsService.MainIsEnabledFilterIsEdited = value;
    partial void OnIsEnabledFilterIsInspectedChanged(bool value) => _settingsService.MainIsEnabledFilterIsInspected = value;
    partial void OnIsEnabledFilterNameChanged(bool value) => _settingsService.MainIsEnabledFilterName = value;
    partial void OnIsEnabledFilterOriginalTextChanged(bool value) => _settingsService.MainIsEnabledFilterOriginalText = value;
    partial void OnSelectedComparisonMethodChanged(StringComparison value) => _settingsService.MainComparisonMethod = value;

    partial void OnSelectedConversationChanged(Conversation value)
    {
        FillLowerDataGrid();

        if (!ConversationCollection.Cast<Conversation>().Contains(value))
            SelectedGridRow = null;

        SelectedLowerGridRow = value;
        _dataService.CurrentConversation = value;
    }

    partial void OnSelectedConversationChanging(Conversation value) => _previousNpcName = SelectedConversation.NpcName;
    partial void OnSelectedEncodingChanged(Encoding value) => _settingsService.MainCurrentEncoding = value;
    partial void OnSelectedFilterTypeChanged(FilterType value) => _settingsService.MainFilterType = value;

    partial void OnSelectedGridRowChanged(Conversation? value)
    {
        if (value is null)
            return;

        SelectedConversation = value;
    }

    partial void OnSelectedLowerGridRowChanged(Conversation? value)
    {
        if (value is null)
            return;

        SelectedGridRow = value;
    }

    partial void OnUsedEncodingChanged(Encoding value) => _settingsService.MainOriginalEncoding = value;
}