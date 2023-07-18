using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using WPFUI.Components;
using WPFUI.Enums;
using WPFUI.Extensions;
using WPFUI.Models;
using WPFUI.NAudioWrapper;
using WPFUI.NAudioWrapper.Enums;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public RangeObservableCollection<Conversation> SelectedConversations { get; } = new();
    public ICollectionView ConversationCollection { get; set; }
    public ICollectionView ConversationDiffCollection { get; set; }
    public List<EncodingMenuItem> Encodings { get; }
    public int LoadedConversationsCount => _conversationList.Count;
    public int LoadedConversationsDiffCount => _conversationDiffList.Count;
    public int LoadedNPCsCount => ConversationCollection.Groups.Count;
    public int EditedConversationsCount => _conversationList.Where(x => x.IsEdited).Count();
    public int InspectedConversationsCount => _conversationList.Where(x => x.IsInspected).Count();
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
    private ConversationDiff? _selectedGridRowCompareMode = new();

    [ObservableProperty]
    private double _currentAudioLength;

    [ObservableProperty]
    private double _currentAudioPosition;

    [ObservableProperty]
    private float _currentVolume = 1f;

    [ObservableProperty]
    private string _currentlyPlayingAudioName = string.Empty;

    [ObservableProperty]
    private string _currentlySelectedAudioName = string.Empty;

    [ObservableProperty]
    private string _pathToAudioFiles = string.Empty;

    [ObservableProperty]
    private bool _isMuted = false;

    [ObservableProperty]
    private bool _isEnabledIgnoreInspected = true;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    private PlaybackState _stateOfPlayback = PlaybackState.Stopped;

    private AudioPlayer? _audioPlayer;

    private readonly List<Conversation> _conversationList = new();

    private List<ConversationDiff> _conversationDiffList = new();

    private List<Dialogue> _parsedDialogues = new();

    private string _previousNpcName = string.Empty;

    private bool _projectWasEdited = false;

    private float _previousVolume = 0f;

    public MainWindowViewModel()
    {
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

        ConversationCollection = CollectionViewSource.GetDefaultView(_conversationList);
        SetGroupingAndSortingOnConversationCollection();
        ConversationCollection.Filter = FilterCollection;
        OnPropertyChanged(nameof(LoadedConversationsCount));
        OnPropertyChanged(nameof(LoadedNPCsCount));

        var refreshAudioPosition = new Task(new Action(RefreshAudioPosition));
        refreshAudioPosition.Start();
    }

    public void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_projectWasEdited)
            return; // don't have to do anything else, program will close itself because CancelEventArgs.Cancel is false

        var result = SaveProjectPrompt();

        if (result == System.Windows.MessageBoxResult.Yes)
            e.Cancel = !TryToSaveProject();
        else if (result == System.Windows.MessageBoxResult.Cancel)
            e.Cancel = true;
    }

    public void OnCompareWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_conversationDiffList.Any())
            return; // don't have to do anything else, program will close itself because CancelEventArgs.Cancel is false

        var result = ConversationDiffListNonEmpty();

        if (result == System.Windows.MessageBoxResult.Yes)
            e.Cancel = false;
        else
            e.Cancel = true;
    }

    partial void OnFilterValueChanged(string value)
    {
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsCount));
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

        CurrentlySelectedAudioName = PathToAudioFiles + (soundFileName ?? string.Empty);
        PropertyColor = ColoredText.Create(value.Diff.Variances);
    }

    partial void OnSelectedConversationChanging(Conversation value) => _previousNpcName = SelectedConversation.NpcName;

    partial void OnSelectedConversationChanged(Conversation value)
    {
        CurrentlySelectedAudioName = PathToAudioFiles + SelectedConversation.Sound;
        FillLowerDataGrid();
    }

    partial void OnPathToAudioFilesChanged(string value) => CurrentlySelectedAudioName = value + SelectedConversation.Sound;

    private void FillLowerDataGrid()
    {
        if (SelectedConversation.NpcName.Equals(_previousNpcName))
            return;

        SelectedConversations.Clear();
        SelectedConversations.AddRange(_conversationList.Where(x => x.NpcName.Equals(SelectedConversation.NpcName)));
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
        StopPlayback();
        _audioPlayer = null;
        CurrentlyPlayingAudioName = string.Empty;
        _filterValue = string.Empty; // _filterValue has to accessed directly to avoid unnecessary Refresh() on ConversationCollection        
        OnPropertyChanged(nameof(FilterValue)); // then call OnPropertyChanged on FilterValue, it speed ups loading/importing
        SelectedGridRow = null;
        SelectedConversation = new();
        FillLowerDataGrid();
        ConversationCollection = CollectionViewSource.GetDefaultView(_conversationList);
        OnPropertyChanged(nameof(LoadedConversationsCount));
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(LoadedNPCsCount));
        OnPropertyChanged(nameof(FilteredConversationsCount));
        _projectWasEdited = false;
    }

    private void SaveFileToDisk(string path)
    {
        var save = new SaveFile()
        {
            Conversations = _conversationList,
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
            EnabledIgnoreInspectedWhileTransfer = IsEnabledIgnoreInspected,
            AudioPlayerVolume = CurrentVolume,
            AudioPlayerPreviousVolume = _previousVolume,
            AudioPlayerMuted = IsMuted,
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
            AudioPathNotSpecified();

        fbd.Dispose();
    }

    [RelayCommand]
    private void CompareOriginalAndEditedText()
    {
        var edited = !SelectedConversation.EditedText.Equals(SelectedConversation.OriginalText);
        SelectedConversation.IsEdited = edited;

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
        var odf = new OpenFileDialog()
        {
            Filter = "Supported files|*.bin;*.goi",
            Title = "Open file to compare"
        };

        if (odf.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(odf.FileName))
        {
            odf.Dispose();
            return;
        }

        var loadedConversations = new HashSet<Conversation>();
        var filePath = odf.FileName;
        try
        {
            loadedConversations = OpenFileToCompare(filePath);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            odf.Dispose();
        }

        _conversationDiffList = _conversationList.CompareTo(loadedConversations, x => x.Name)
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
            var result = SaveProjectPrompt();
            if (result == System.Windows.MessageBoxResult.Yes)
                SaveProject();
            else if (result == System.Windows.MessageBoxResult.Cancel)
                return;
        }

        var odf = new OpenFileDialog()
        {
            Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*",
        };

        if (odf.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(odf.FileName))
        {
            odf.Dispose();
            return;
        }

        var parser = new Reader(odf.FileName, SelectedEncoding);

        try
        {
            _parsedDialogues = parser.Parse(false);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            odf.Dispose();
        }

        UsedEncoding = SelectedEncoding;

        if (_conversationList.Any())
            _conversationList.Clear();

        foreach (var dialogue in _parsedDialogues)
        {
            _conversationList.Add(Conversation.CreateConversationFromDialogue(dialogue));
        }

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
            var result = SaveProjectPrompt();
            if (result == System.Windows.MessageBoxResult.Yes)
                SaveProject();
            else if (result == System.Windows.MessageBoxResult.Cancel)
                return;
        }

        var odf = new OpenFileDialog()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi",
        };

        if (odf.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(odf.FileName))
        {
            odf.Dispose();
            return;
        }

        SaveFile? projectFile;
        string pathToSaveFile;

        try
        {
            var saveFile = File.ReadAllText(odf.FileName);
            projectFile = JsonSerializer.Deserialize<SaveFile>(saveFile);
            pathToSaveFile = odf.FileName;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            odf.Dispose();
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
                CurrentVolume = projectFile.AudioPlayerVolume;

            if (projectFile.AudioPlayerPreviousVolume >= 0f && projectFile.AudioPlayerPreviousVolume <= 1f)
                _previousVolume = projectFile.AudioPlayerPreviousVolume;

            IsEnabledFilterName = projectFile.EnabledFilterName;
            IsEnabledFilterOriginalText = projectFile.EnabledFilterOriginalText;
            IsEnabledFilterEditedText = projectFile.EnabledFilterEditedText;
            IsEnabledFilterIsInspected = projectFile.EnabledFilterIsInspected;
            IsEnabledFilterIsEdited = projectFile.EnabledFilterIsEdited;
            IsEnabledFilterCompareModeIsInspected = projectFile.EnabledFilterCompareModeIsInspected;
            IsEnabledIgnoreInspected = projectFile.EnabledIgnoreInspectedWhileTransfer;
            IsMuted = projectFile.AudioPlayerMuted;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_conversationList.Any())
            _conversationList.Clear();

        if (projectFile.Conversations is not null)
            _conversationList.AddRange(projectFile.Conversations);

        CleanReloadRefreshConversationCollection();
        IsOuFileImported = true;
        PathToSaveFile = pathToSaveFile;
        Title = TITLE + " - " + pathToSaveFile;
    }

    [RelayCommand]
    private void ProjectFileChanged()
    {
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

        var result = SaveProjectPrompt();
        bool shouldExit = false;

        if (result == System.Windows.MessageBoxResult.No)
            shouldExit = true;
        else if (result == System.Windows.MessageBoxResult.Yes)
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
                    _conversationList.Remove(SelectedConversationDiff.Diff.Original);
                break;
            case ComparisonResultType.Added:
                if (SelectedConversationDiff.Diff.Compared is not null)
                    _conversationList.Add(SelectedConversationDiff.Diff.Compared);
                break;
            case ComparisonResultType.Changed:
                if (IsEnabledIgnoreInspected && SelectedConversationDiff.Diff.Variances.ContainsKey("IsInspected"))
                    SelectedConversationDiff.Diff.Variances.Remove("IsInspected");

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
    private void StartPlayback()
    {
        if (string.IsNullOrEmpty(CurrentlySelectedAudioName))
            return;

        if (string.IsNullOrEmpty(PathToAudioFiles))
        {
            AudioPathNotSpecified();
            return;
        }

        if (!File.Exists(CurrentlySelectedAudioName))
        {
            FileNotFound();
            return;
        }

        if (!CurrentlyPlayingAudioName.Equals(CurrentlySelectedAudioName) && _audioPlayer is not null)
        {
            _audioPlayer.PlaybackStopType = PlaybackStopType.BySelectingNewFileWhilePlaying;
            _audioPlayer.Stop();
            CurrentAudioPosition = 0;
        }

        var realState = _audioPlayer?.GetPlaybackState ?? PlaybackState.Stopped;

        if (realState == PlaybackState.Stopped)
        {
            _audioPlayer = new AudioPlayer(CurrentlySelectedAudioName, CurrentVolume, CurrentAudioPosition);
            _audioPlayer.PlaybackPaused += AudioPlayer_PlaybackPaused;
            _audioPlayer.PlaybackResumed += AudioPlayer_PlaybackResumed;
            _audioPlayer.PlaybackStopped += AudioPlayer_PlaybackStopped;
            CurrentAudioLength = _audioPlayer.GetLengthInSeconds();
            CurrentlyPlayingAudioName = CurrentlySelectedAudioName;
        }
        _audioPlayer?.TogglePlayPause(CurrentVolume, CurrentAudioPosition);
    }

    [RelayCommand(CanExecute = nameof(CanStopPlayback))]
    private void StopPlayback()
    {
        if (_audioPlayer is null)
            return;

        _audioPlayer.PlaybackStopType = PlaybackStopType.ByUser;
        _audioPlayer.Stop();
    }
    private bool CanStopPlayback() => StateOfPlayback == PlaybackState.Playing || StateOfPlayback == PlaybackState.Paused;

    [RelayCommand]
    private void TrackControlMouseDown()
    {
        if (_audioPlayer is null)
            return;

        if (StateOfPlayback == PlaybackState.Playing)
            _audioPlayer.Pause();
    }


    [RelayCommand]
    private void TrackControlMouseUp()
    {
        if (_audioPlayer is null)
            return;

        _audioPlayer.Play(PlaybackState.Paused, CurrentVolume, CurrentAudioPosition);
    }

    [RelayCommand]
    private void VolumeControlValueChanged()
    {
        _audioPlayer?.SetVolume(CurrentVolume);
        IsMuted = false;
    }

    private void AudioPlayer_PlaybackStopped(PlaybackStopType playbackStopType)
    {
        // when it occurs I don't have to change UI because it is in same state, just new audio is playing
        if (playbackStopType == PlaybackStopType.BySelectingNewFileWhilePlaying)
            return;

        StateOfPlayback = PlaybackState.Stopped;
        CurrentAudioPosition = 0;
    }
    [RelayCommand]
    private void ToggleMute()
    {
        if (IsMuted)
            UnMuteSound();
        else
            MuteSound();
    }

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
            MessageBox.Show(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        PathToSaveFile = path;
        Title = TITLE + " - " + path;
        return true;
    }

    private void SelectNextConversationDiffGridItem()
    {
        var index = _conversationDiffList.IndexOf(SelectedConversationDiff);
        _conversationDiffList.Remove(SelectedConversationDiff);

        var count = _conversationDiffList.Count;

        if (count == 0)
        {
            SelectedGridRowCompareMode = null;
            SelectedConversationDiff = new();
        }
        else if (index < _conversationDiffList.Count)
            SelectedGridRowCompareMode = _conversationDiffList[index];
        else
            SelectedGridRowCompareMode = _conversationDiffList.Last();
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
        ConversationCollection = CollectionViewSource.GetDefaultView(_conversationList);
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