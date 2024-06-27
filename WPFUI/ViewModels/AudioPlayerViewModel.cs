using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WPFUI.NAudioWrapper;
using WPFUI.NAudioWrapper.Enums;
using WPFUI.Services;
using static WPFUI.Components.Messages;

namespace WPFUI.ViewModels;

public sealed partial class AudioPlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private double _currentAudioLength;

    [ObservableProperty]
    private double _currentAudioPosition;

    [ObservableProperty]
    private string _currentlyPlayingAudioName = string.Empty;

    [ObservableProperty]
    private bool _isMuted = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private PlaybackState _stateOfPlayback = PlaybackState.Stopped;

    [ObservableProperty]
    private float _volume = 1f;

    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    private AudioPlayer? _audioPlayer;

    public AudioPlayerViewModel(ISettingsService settingsService, IDataService dataService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dataService = dataService;
        _dialogService = dialogService;

        IsMuted = _settingsService.AudioPlayerIsMuted;
        Volume = _settingsService.AudioPlayerVolume;
    }

    private bool CanStopPlayback() => StateOfPlayback == PlaybackState.Playing || StateOfPlayback == PlaybackState.Paused;

    private void MuteSound()
    {
        _settingsService.AudioPlayerPreviousVolume = Volume;
        Volume = 0;
        IsMuted = true;
    }

    private void PlaybackPaused() => StateOfPlayback = PlaybackState.Paused;
    private void PlaybackResumed() => StateOfPlayback = PlaybackState.Playing;

    private void PlaybackStopped(PlaybackStopType playbackStopType)
    {
        // when it occurs I don't have to change UI because it is in same state, just new audio is playing
        if (playbackStopType == PlaybackStopType.BySelectingNewFileWhilePlaying)
            return;

        if (_audioPlayer is not null)
        {
            _audioPlayer.PlaybackPaused -= PlaybackPaused;
            _audioPlayer.PlaybackResumed -= PlaybackResumed;
            _audioPlayer.PlaybackStopped -= PlaybackStopped;
            _audioPlayer.Dispose();
        }

        StateOfPlayback = PlaybackState.Stopped;
        CurrentAudioPosition = 0;
    }

    private async Task RefreshAudioPosition()
    {
        while (_audioPlayer is not null && StateOfPlayback == PlaybackState.Playing)
        {
            CurrentAudioPosition = _audioPlayer.CurrentTime;
            await Task.Delay(5);
        }
    }

    private void UnMuteSound()
    {
        Volume = _settingsService.AudioPlayerPreviousVolume;
        IsMuted = false;
    }

    [RelayCommand]
    private void Play()
    {
        var audioFileFullPath = _settingsService.AudioPlayerPathToFiles + _dataService.CurrentConversation.Sound;

        if (string.IsNullOrEmpty(audioFileFullPath))
            return;

        if (string.IsNullOrEmpty(_settingsService.AudioPlayerPathToFiles))
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

        realState = _audioPlayer?.GetPlaybackState ?? PlaybackState.Stopped;
        if (realState == PlaybackState.Playing)
            Task.Run(RefreshAudioPosition);
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

    [RelayCommand]
    private void VolumeControlValueChanged()
    {
        _audioPlayer?.SetVolume(Volume);
        IsMuted = false;
    }

    partial void OnIsMutedChanged(bool value) => _settingsService.AudioPlayerIsMuted = value;
    partial void OnVolumeChanged(float value) => _settingsService.AudioPlayerVolume = value;
}
