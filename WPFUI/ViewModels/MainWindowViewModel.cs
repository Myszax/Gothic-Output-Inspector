using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Data;
using WPFUI.Models;
using WPFUI.NAudioWrapper;
using WPFUI.NAudioWrapper.Enums;
using System.IO;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<Conversation> SelectedConversations { get; } = new();
    public ICollectionView ConversationCollection { get; set; }

    [ObservableProperty]
    private object _selectedTreeItem;

    [ObservableProperty]
    private string _filterValue = string.Empty;

    [ObservableProperty]
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
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    private PlaybackState _stateOfPlayback = PlaybackState.Stopped;

    private AudioPlayer? _audioPlayer;

    private List<Conversation> _conversationList = new();

    private List<Dialogue> _parsedDialogues = new();

    private string _previousNpcName = string.Empty;

    private float _previousVolume = 0f;

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

        foreach (var dialogue in _parsedDialogues)
        {
            _conversationList.Add(Conversation.CreateConversationFromDialogue(dialogue));
        }

        ConversationCollection = CollectionViewSource.GetDefaultView(_conversationList);
        SetGroupingAndSortingOnConversationCollection();
        ConversationCollection.Filter = FilterCollection;

        var refreshAudioPosition = new Task(new Action(RefreshAudioPosition));
        refreshAudioPosition.Start();
    }

    partial void OnFilterValueChanged(string value) => ConversationCollection.Refresh();

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

        var filteredByNpcName = _conversationList.Where(x => x.NpcName.Equals(SelectedConversation.NpcName));
        SelectedConversations.Clear();
        foreach (var conversation in filteredByNpcName)
        {
            SelectedConversations.Add(conversation);
            // TODO: AddRange to avoid firing INotifyCollectionChanged on every Add method
        }
    }

    private bool FilterCollection(object obj)
    {
        if (obj is not Conversation)
            return false;

        var conversation = (Conversation)obj;

        return conversation.Name.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
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

    [RelayCommand]
    private void SetPathToAudioFiles()
    {
        var fbd = new System.Windows.Forms.FolderBrowserDialog();

        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            PathToAudioFiles = fbd.SelectedPath + '\\';
        else
            AudioPathNotSpecified();

        fbd.Dispose();
    }

    [RelayCommand]
    private void CompareOriginalAndEditedText()
    {
        SelectedConversation.IsEdited = !SelectedConversation.EditedText.Equals(SelectedConversation.OriginalText);
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