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
using WPFUI.Models;
using WPFUI.NAudioWrapper;
using WPFUI.NAudioWrapper.Enums;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public RangeObservableCollection<Conversation> SelectedConversations { get; } = new();
    public ICollectionView ConversationCollection { get; set; }
    public List<EncodingMenuItem> Encodings { get; }
    public int LoadedConversationsCount => _conversationList.Count;
    public int LoadedNPCsCount => ConversationCollection.Groups.Count;
    public int EditedConversationsCount => _conversationList.Where(x => x.IsEdited).Count();
    public int InspectedConversationsCount => _conversationList.Where(x => x.IsInspected).Count();
    public int FilteredConversationsCount => ConversationCollection.Cast<object>().Count();

    [ObservableProperty]
    private object _selectedTreeItem;

    [ObservableProperty]
    private string _filterValue = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditedConversationsCount))]
    [NotifyPropertyChangedFor(nameof(InspectedConversationsCount))]
    private Conversation _selectedConversation = new();

    [ObservableProperty]
    private Conversation? _selectedGridRow = new();

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
    private Encoding _selectedEncoding;

    [ObservableProperty]
    private Encoding _usedEncoding;

    [ObservableProperty]
    private StringComparison _selectedComparisonMethod = StringComparison.Ordinal;

    [ObservableProperty]
    private FilterType _selectedFilterType = FilterType.HideAll;

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
    private string _pathToSaveFile = string.Empty;

    [ObservableProperty]
    private string _title = TITLE;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetPathToAudioFilesCommand))]
    private bool _isOuFileImported = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    private PlaybackState _stateOfPlayback = PlaybackState.Stopped;

    private AudioPlayer? _audioPlayer;

    private readonly List<Conversation> _conversationList = new();

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

    partial void OnFilterValueChanged(string value)
    {
        ConversationCollection.Refresh();
        OnPropertyChanged(nameof(FilteredConversationsCount));
    }

    partial void OnSelectedTreeItemChanged(object value)
    {
        if (value is null || value is not Conversation)
            return;

        SelectedConversation = (Conversation)value;
    }

    partial void OnSelectedGridRowChanged(Conversation? value)
    {
        if (value is null)
            return;

        SelectedConversation = value;
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
            if (IsEnabledFilterIsInspected && conversation.IsInspected) return false;
            if (IsEnabledFilterIsEdited && conversation.IsEdited) return false;
        }
        else
        {
            if (IsEnabledFilterIsInspected && !conversation.IsInspected) return false;
            if (IsEnabledFilterIsEdited && !conversation.IsEdited) return false;
        }

        if (IsEnabledFilterName && conversation.Name.Contains(FilterValue, SelectedComparisonMethod)) return true;
        if (IsEnabledFilterOriginalText && conversation.OriginalText.Contains(FilterValue, SelectedComparisonMethod)) return true;
        if (IsEnabledFilterEditedText && conversation.EditedText.Contains(FilterValue, SelectedComparisonMethod)) return true;

        // this check prevents returning false if every search checkbox is unchecked
        return !IsEnabledFilterName && !IsEnabledFilterOriginalText && !IsEnabledFilterEditedText;
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
            EnabledFilterIsEdited = IsEnabledFilterIsEdited,
            EnabledFilterIsInspected = IsEnabledFilterIsInspected,
            AudioPlayerVolume = CurrentVolume,
            AudioPlayerPreviousVolume = _previousVolume,
            AudioPlayerMuted = IsMuted,
        };

        var opt = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        File.WriteAllText(path, JsonSerializer.Serialize(save, opt));
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

    [RelayCommand]
    private void ImportFile()
    {
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
            _parsedDialogues = parser.Parse();
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
    private void SaveProject()
    {
        if (string.IsNullOrWhiteSpace(PathToSaveFile))
        {
            SaveProjectAs();
            return;
        }

        try
        {
            SaveFileToDisk(PathToSaveFile);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(IsOuFileImported))]
    private void SaveProjectAs()
    {
        var sfd = new SaveFileDialog()
        {
            Filter = "Gothic Output Inspector (*.goi)|*.goi",
        };

        if (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(sfd.FileName))
        {
            sfd.Dispose();
            return;
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
            return;
        }

        PathToSaveFile = path;
        Title = TITLE + " - " + path;
    }

    [RelayCommand]
    private void OpenProject()
    {
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
        if (IsMuted) UnMuteSound();
        else MuteSound();
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