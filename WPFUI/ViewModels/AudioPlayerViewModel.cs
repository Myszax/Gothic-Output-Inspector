using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WPFUI.NAudioWrapper;
using WPFUI.NAudioWrapper.Enums;
using WPFUI.Services;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public partial class AudioPlayerViewModel : ObservableObject
{
    public float PreviousVolume { get; set; } = 0f;

    [ObservableProperty]
    private string _currentlyPlayingAudioName = string.Empty;

    [ObservableProperty]
    private double _currentAudioPosition;

    [ObservableProperty]
    private double _currentAudioLength;

    [ObservableProperty]
    private float _volume = 1f;

    [ObservableProperty]
    private bool _isMuted = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private PlaybackState _stateOfPlayback = PlaybackState.Stopped;

    private AudioPlayer? _audioPlayer;
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;

    public AudioPlayerViewModel(IDataService dataService, IDialogService dialogService)
    {
        _dataService = dataService;
        _dialogService = dialogService;

        var refreshAudioPosition = new Task(new Action(RefreshAudioPosition));
        refreshAudioPosition.Start();
    }

    [RelayCommand]
    private void Play()
    {
        var audioFileFullPath = _dataService.AudioFilesPath + _dataService.CurrentConversation.Sound;

        if (string.IsNullOrEmpty(audioFileFullPath))
            return;

        if (string.IsNullOrEmpty(_dataService.AudioFilesPath))
        {
            _dialogService.ShowMessageBox(AUDIO_PATH_NOT_SPECIFIED, CAPTION_AUDIO, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!File.Exists(audioFileFullPath))
        {
            _dialogService.ShowMessageBox(FILE_NOT_FOUND, CAPTION_AUDIO, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!CurrentlyPlayingAudioName.Equals(audioFileFullPath) && _audioPlayer is not null)
        {
            _audioPlayer.PlaybackStopType = PlaybackStopType.BySelectingNewFileWhilePlaying;
            _audioPlayer.Stop();
            CurrentAudioPosition = 0;
        }

        var realState = _audioPlayer?.GetPlaybackState ?? PlaybackState.Stopped;

        if (realState == PlaybackState.Stopped)
        {
            _audioPlayer = new AudioPlayer(audioFileFullPath, Volume, CurrentAudioPosition);
            _audioPlayer.PlaybackPaused += PlaybackPaused;
            _audioPlayer.PlaybackResumed += PlaybackResumed;
            _audioPlayer.PlaybackStopped += PlaybackStopped;
            CurrentAudioLength = _audioPlayer.GetLengthInSeconds();
            CurrentlyPlayingAudioName = audioFileFullPath;
        }
        _audioPlayer?.TogglePlayPause(Volume, CurrentAudioPosition);
    }

    [RelayCommand]
    private void VolumeControlValueChanged()
    {
        _audioPlayer?.SetVolume(Volume);
        IsMuted = false;
    }

    [RelayCommand(CanExecute = nameof(CanStopPlayback))]
    private void Stop()
    {
        if (_audioPlayer is null)
            return;

        _audioPlayer.PlaybackStopType = PlaybackStopType.ByUser;
        _audioPlayer.Stop();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        if (IsMuted)
            UnMuteSound();
        else
            MuteSound();
    }

    private void PlaybackResumed() => StateOfPlayback = PlaybackState.Playing;

    private void PlaybackPaused() => StateOfPlayback = PlaybackState.Paused;

    private void PlaybackStopped(PlaybackStopType playbackStopType)
    {
        // when it occurs I don't have to change UI because it is in same state, just new audio is playing
        if (playbackStopType == PlaybackStopType.BySelectingNewFileWhilePlaying)
            return;

        StateOfPlayback = PlaybackState.Stopped;
        CurrentAudioPosition = 0;
    }

    private void MuteSound()
    {
        PreviousVolume = Volume;
        Volume = 0;
        IsMuted = true;
    }

    private void UnMuteSound()
    {
        Volume = PreviousVolume;
        IsMuted = false;
    }

    private bool CanStopPlayback() => StateOfPlayback == PlaybackState.Playing || StateOfPlayback == PlaybackState.Paused;

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
