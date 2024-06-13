using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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

public partial class MainWindowViewModel : ObservableObject
{
public partial class MainWindowViewModel : ObservableObject, ICloseable
    public RangeObservableCollection<Conversation> SelectedConversations { get; } = new();
    public ICollectionView ConversationCollection { get; set; }
    public ICollectionView ConversationDiffCollection { get; set; }
    public List<EncodingMenuItem> Encodings { get; }
    public int LoadedConversationsCount => _dataService.Data.Count;
    public int LoadedConversationsDiffCount => _conversationDiffList.Count;
    public int LoadedNPCsCount => ConversationCollection.Groups.Count;
    public int EditedConversationsCount => _dataService.Data.Where(x => x.IsEdited).Count();
    public int InspectedConversationsCount => _dataService.Data.Where(x => x.IsInspected).Count();
    public int FilteredConversationsCount => ConversationCollection.Cast<object>().Count();
    public int FilteredConversationsDiffCount => ConversationDiffCollection.Cast<object>().Count();
    public int AddedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff.Type == ComparisonResultType.Added).Count();
    public int ChangedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff.Type == ComparisonResultType.Changed).Count();
    public int RemovedConversationsDiffCount => _conversationDiffList.Where(x => x.Diff.Type == ComparisonResultType.Removed).Count();

    [ObservableProperty]
    private ColoredText _propertyColor = new();

    [ObservableProperty]
    private string _filterValue = string.Empty;

    [ObservableProperty]
    private string _filterValueCompareMode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditedConversationsCount))]
    [NotifyPropertyChangedFor(nameof(InspectedConversationsCount))]
    private Conversation _selectedConversation = new();

    [ObservableProperty]
    private ConversationDiff _selectedConversationDiff = new();

    [ObservableProperty]
    private Conversation? _selectedGridRow = new();

    [ObservableProperty]
    private Conversation? _selectedLowerGridRow = new();

    [ObservableProperty]
    private ConversationDiff? _selectedGridRowCompareMode = new();

    [ObservableProperty]
    private string _pathToAudioFiles = string.Empty;

    [ObservableProperty]
    private bool _isEnabledIgnoreInspectedWhileTransfer = true;

    [ObservableProperty]
    private Encoding _selectedEncoding;

    [ObservableProperty]
    private Encoding _usedEncoding;

    [ObservableProperty]
    private StringComparison _selectedComparisonMethod = StringComparison.Ordinal;

    [ObservableProperty]
    private FilterType _selectedFilterType = FilterType.HideAll;

    [ObservableProperty]
    private FilterType _selectedFilterTypeCompareMode = FilterType.HideAll;

    [ObservableProperty]
    private bool _isEnabledFilterName = true;

    [ObservableProperty]
    private bool _isEnabledFilterOriginalText = true;

    [ObservableProperty]
    private bool _isEnabledFilterEditedText = false;

    [ObservableProperty]
    private bool _isEnabledFilterIsEdited = true;

    [ObservableProperty]
    private bool _isEnabledFilterIsInspected = true;

    [ObservableProperty]
    private bool _isEnabledFilterCompareModeIsInspected = true;

    [ObservableProperty]
    private string _pathToSaveFile = string.Empty;

    [ObservableProperty]
    private string _title = TITLE;

    [ObservableProperty]
    private string _titleCompareMode = TITLE;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetPathToAudioFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(CompareOtherFileCommand))]
    private bool _isOuFileImported = false;

    private List<ConversationDiff> _conversationDiffList = new();

    private List<Dialogue> _parsedDialogues = new();

    private string _previousNpcName = string.Empty;

    private bool _projectWasEdited = false;

    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;

    public MainWindowViewModel(IDataService dataService, IDialogService dialogService)
    public MainWindowViewModel(IDataService dataService, IDialogService dialogService, AudioPlayerViewModel audioPlayerViewModel)
    {
        _dataService = dataService;
        _dialogService = dialogService;

        AudioPlayerViewModel = audioPlayerViewModel;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // need to register to be able to use Encoding.GetEncoding()

        Encodings = Encoding.GetEncodings()
            .Select(x => x.GetEncoding())
            .Where(x => x.EncodingName
            .Contains("windows", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.CodePage)
            .Select(x => new EncodingMenuItem { Encoding = x, IsChecked = false })
            .ToList();

        Encodings[1].IsChecked = true;
        SelectedEncoding = Encodings[1].Encoding;

        ConversationCollection = CollectionViewSource.GetDefaultView(_dataService.Data);
        SetGroupingAndSortingOnConversationCollection();
        ConversationCollection.Filter = FilterCollection;
        OnPropertyChanged(nameof(LoadedConversationsCount));
        OnPropertyChanged(nameof(LoadedNPCsCount));
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

    public void OnCompareWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_conversationDiffList.Any())
            return; // don't have to do anything else, program will close itself because CancelEventArgs.Cancel is false

        var result = _dialogService.ShowMessageBox(COMPARE_MODE_EXIT_PROMPT, CAPTION_EXITING_COMPARE_MODE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
            e.Cancel = false;
        else
            e.Cancel = true;
    }

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

    partial void OnFilterValueCompareModeChanged(string value)
    {
        ConversationDiffCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
    }

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

    partial void OnSelectedGridRowCompareModeChanged(ConversationDiff? value)
    {
        if (value is null)
            return;

        SelectedConversationDiff = value;
    }

    partial void OnSelectedConversationDiffChanged(ConversationDiff value)
    {
        if (value.Diff is null)
            return;

        string? soundFileName;
        if (value.Diff.Type == ComparisonResultType.Removed)
            soundFileName = value.Diff.Original?.Sound;
        else
            soundFileName = value.Diff.Compared?.Sound;

        PropertyColor = ColoredText.Create(value.Diff.Variances);
    }

    partial void OnSelectedConversationChanging(Conversation value) => _previousNpcName = SelectedConversation.NpcName;

    partial void OnSelectedConversationChanged(Conversation value)
    {
        FillLowerDataGrid();

        if (!ConversationCollection.Cast<Conversation>().Contains(value))
            SelectedGridRow = null;

        SelectedLowerGridRow = value;
        _dataService.CurrentConversation = value;
    }

    partial void OnPathToAudioFilesChanged(string value) => _dataService.AudioFilesPath = value;

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

    private bool FilterCollectionCompareMode(object obj)
    {
        if (obj is null || obj is not ConversationDiff conversationDiff)
            return false;

        if (conversationDiff.Diff.Compared is not null)
        {
            if (SelectedFilterTypeCompareMode == FilterType.HideAll)
            {
                if (IsEnabledFilterCompareModeIsInspected && conversationDiff.Diff.Variances.Count == 1 && conversationDiff.Diff.Variances.ContainsKey("IsInspected"))
                    return false;
            }
            else
            {
                if (IsEnabledFilterCompareModeIsInspected && conversationDiff.Diff.Variances.ContainsKey("IsInspected"))
                    return false;
            }
        }

        if (FilterValueCompareMode.Length > 0 && !conversationDiff.Name.Contains(FilterValueCompareMode, SelectedComparisonMethod))
            return false;

        return true;
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

    private void SaveFileToDisk(string path)
    {
        var save = new SaveFile()
        {
            Conversations = _dataService.Data,
            OriginalEncoding = UsedEncoding.HeaderName,
            ChosenEncoding = SelectedEncoding.HeaderName,
            AudioPath = PathToAudioFiles,
            ComparisonMethod = SelectedComparisonMethod,
            EnabledFilterName = IsEnabledFilterName,
            EnabledFilterOriginalText = IsEnabledFilterOriginalText,
            EnabledFilterEditedText = IsEnabledFilterEditedText,
            FilterType = SelectedFilterType,
            FilterTypeCompareMode = SelectedFilterTypeCompareMode,
            EnabledFilterIsEdited = IsEnabledFilterIsEdited,
            EnabledFilterIsInspected = IsEnabledFilterIsInspected,
            EnabledFilterCompareModeIsInspected = IsEnabledFilterCompareModeIsInspected,
            EnabledIgnoreInspectedWhileTransfer = IsEnabledIgnoreInspectedWhileTransfer,
            AudioPlayerVolume = AudioPlayerViewModel.Volume,
            AudioPlayerPreviousVolume = AudioPlayerViewModel.PreviousVolume,
            AudioPlayerMuted = AudioPlayerViewModel.IsMuted,
        };

        var opt = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        File.WriteAllText(path, JsonSerializer.Serialize(save, opt));
        _projectWasEdited = false;
    }

    [RelayCommand]
    private void SelectedTreeItemChanged(object value)
    {
        if (value is null || value is not Conversation)
            return;

        SelectedConversation = (Conversation)value;
    }

    [RelayCommand]
    private void RefreshConversationCollection(bool checkForEmptyFilterValue = true)
    {
        if (checkForEmptyFilterValue && string.IsNullOrEmpty(FilterValue))
            return;

        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsCount));
    }

    [RelayCommand]
    private void RefreshConversationDiffCollection(bool checkForEmptyFilterValue = true)
    {
        if (checkForEmptyFilterValue && string.IsNullOrEmpty(FilterValueCompareMode))
            return;

        ConversationDiffCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SetPathToAudioFiles()
    {
        var fbd = new FolderBrowserDialog();

        if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            PathToAudioFiles = fbd.SelectedPath + '\\';
            ProjectFileChanged();
        }
        else
            _dialogService.ShowMessageBox(AUDIO_PATH_NOT_SPECIFIED, CAPTION_AUDIO, MessageBoxButtons.OK, MessageBoxIcon.Information);

        fbd.Dispose();
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

    [RelayCommand]
    private void ChangeEncoding(EncodingMenuItem value)
    {
        foreach (var encodingMenuItem in Encodings)
            encodingMenuItem.IsChecked = false;

        value.IsChecked = true;
        SelectedEncoding = value.Encoding;
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void CompareOtherFile()
    {
        var fileDialogSettings = new FileDialogSettings()
        {
            Filter = "Supported files|*.bin;*.goi",
            Title = "Open file to compare"
        };

        var fileDialogResult = _dialogService.ShowFileDialog(fileDialogSettings, out string filePath, false);

        if (fileDialogResult != DialogResult.OK || string.IsNullOrEmpty(filePath))
            return;

        var loadedConversations = new HashSet<Conversation>();
        try
        {
            loadedConversations = OpenFileToCompare(filePath);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _conversationDiffList = _dataService.Data.CompareTo(loadedConversations, x => x.Name)
            .Select(x => new ConversationDiff() { Diff = x, Name = x.Original?.Name ?? x.Compared.Name }).ToList();

        FilterValueCompareMode = string.Empty;
        ConversationDiffCollection = CollectionViewSource.GetDefaultView(_conversationDiffList);
        ConversationDiffCollection.Filter = FilterCollectionCompareMode;
        OnPropertyChanged(nameof(LoadedConversationsDiffCount));
        OnPropertyChanged(nameof(FilteredConversationsDiffCount));

        // those properties should be reset before `compareWindow.ShowDialog()`
        // to avoid strange bugs when SelectedConversation is selected on Grid and then edited via Compare Mode
        SelectedConversation = new();
        SelectedGridRow = null;

        TitleCompareMode = TITLE + " - " + filePath;
        var compareWindow = new CompareWindow(this);
        compareWindow.ShowDialog();
        compareWindow = null;

        CleanUpOnCompareWindowClose();
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

        var fileDialogSettings = new FileDialogSettings()
        {
            Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*",
        };

        var fileDialogResult = _dialogService.ShowFileDialog(fileDialogSettings, out string filePath, false);

        if (fileDialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(filePath))
            return;

        var parser = new Reader(filePath, SelectedEncoding);

        try
        {
            _parsedDialogues = parser.Parse(false);
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

        foreach (var dialogue in _parsedDialogues)
            conversationList.Add(Conversation.CreateConversationFromDialogue(dialogue));

        _dataService.Data.AddRange(conversationList);

        CleanReloadRefreshConversationCollection();
        IsOuFileImported = true;
        Title = TITLE + " - NewProject";
        ProjectFileChanged();
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SaveProject() => TryToSaveProject();

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SaveProjectAs() => TryToSaveProjectAs();

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

        var fileDialogSettings = new FileDialogSettings()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi"
        };

        var fileDialogResult = _dialogService.ShowFileDialog(fileDialogSettings, out string filePath, false);

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
            return; // TODO: info about it

        try
        {
            if (!string.IsNullOrWhiteSpace(projectFile.ChosenEncoding))
                SelectedEncoding = Encoding.GetEncoding(projectFile.ChosenEncoding);

            if (!string.IsNullOrWhiteSpace(projectFile.OriginalEncoding))
                UsedEncoding = Encoding.GetEncoding(projectFile.OriginalEncoding);
            else
                UsedEncoding = Encoding.GetEncoding(1250);

            if (!string.IsNullOrWhiteSpace(projectFile.AudioPath))
                PathToAudioFiles = projectFile.AudioPath;

            if (Enum.IsDefined(typeof(FilterType), projectFile.FilterType))
                SelectedFilterType = projectFile.FilterType;

            if (Enum.IsDefined(typeof(FilterType), projectFile.FilterType))
                SelectedFilterTypeCompareMode = projectFile.FilterTypeCompareMode;

            if (Enum.IsDefined(typeof(StringComparison), projectFile.ComparisonMethod))
                SelectedComparisonMethod = projectFile.ComparisonMethod;

            if (projectFile.AudioPlayerVolume >= 0f && projectFile.AudioPlayerVolume <= 1f)
                AudioPlayerViewModel.Volume = projectFile.AudioPlayerVolume;

            if (projectFile.AudioPlayerPreviousVolume >= 0f && projectFile.AudioPlayerPreviousVolume <= 1f)
                AudioPlayerViewModel.PreviousVolume = projectFile.AudioPlayerPreviousVolume;

            IsEnabledFilterName = projectFile.EnabledFilterName;
            IsEnabledFilterOriginalText = projectFile.EnabledFilterOriginalText;
            IsEnabledFilterEditedText = projectFile.EnabledFilterEditedText;
            IsEnabledFilterIsInspected = projectFile.EnabledFilterIsInspected;
            IsEnabledFilterIsEdited = projectFile.EnabledFilterIsEdited;
            IsEnabledFilterCompareModeIsInspected = projectFile.EnabledFilterCompareModeIsInspected;
            IsEnabledIgnoreInspectedWhileTransfer = projectFile.EnabledIgnoreInspectedWhileTransfer;
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
        PathToSaveFile = pathToSaveFile;
        Title = TITLE + " - " + pathToSaveFile;
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
        ProjectFileChanged();
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

    [RelayCommand]
    private void StartPlayback() => AudioPlayerViewModel.PlayCommand.Execute(null);

    private bool TryToSaveProject()
    {
        if (string.IsNullOrWhiteSpace(PathToSaveFile))
            return TryToSaveProjectAs();

        try
        {
            SaveFileToDisk(PathToSaveFile);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        Title = TITLE + " - " + PathToSaveFile;
        return true;
    }

    private bool TryToSaveProjectAs()
    {
        var sfd = new SaveFileDialog()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi",
        };

        if (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(sfd.FileName))
        {
            sfd.Dispose();
            return false;
        }
        string path = sfd.FileName;
        sfd.Dispose();

        try
        {
            SaveFileToDisk(path);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessageBox(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        PathToSaveFile = path;
        Title = TITLE + " - " + path;
        return true;
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

    private void CleanUpOnCompareWindowClose()
    {
        SelectedConversationDiff = new();
        SelectedGridRowCompareMode = null;
        FillLowerDataGrid();
        ConversationCollection = CollectionViewSource.GetDefaultView(_dataService.Data);
        OnPropertyChanged(nameof(LoadedConversationsCount));
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(LoadedNPCsCount));
        OnPropertyChanged(nameof(FilteredConversationsCount));
        OnPropertyChanged(nameof(InspectedConversationsCount));
        OnPropertyChanged(nameof(EditedConversationsCount));
    }

    private void MuteSound()
    {
        _previousVolume = CurrentVolume;
        CurrentVolume = 0;
        IsMuted = true;
    }

    private void UnMuteSound()
    {
        CurrentVolume = _previousVolume;
        IsMuted = false;
    }

    private void AudioPlayer_PlaybackResumed() => StateOfPlayback = PlaybackState.Playing;

    private void AudioPlayer_PlaybackPaused() => StateOfPlayback = PlaybackState.Paused;

    private void RefreshAudioPosition()
    {
        // i know it drains unnecessary resources
        // should be running only when `PlaybackState.Playing`
        // eg. in play and resume method and stopped in stop method
        // but for now i am leaving it like that: TODO
        while (true)
        {
            if (_audioPlayer is not null && StateOfPlayback == PlaybackState.Playing)
                CurrentAudioPosition = _audioPlayer.CurrentTime;

            Thread.Sleep(20);
        }
    }
}