﻿using NAudio.Utils;
using NAudio.Wave;
using System;
using System.IO;
using WPFUI.NAudioWrapper.Enums;

namespace WPFUI.NAudioWrapper;

public class AudioPlayer
{
    public PlaybackStopType PlaybackStopType { get; set; }

    public double CurrentTime => _waveFileReader!.CurrentTime.TotalSeconds;

    public double GetLengthInSeconds() => _waveFileReader is not null ? _waveFileReader.TotalTime.TotalSeconds : 0d;

    public double GetPositionInSeconds() => _waveFileReader is not null ? _waveFileReader.CurrentTime.TotalSeconds : 0d;

    public PlaybackState GetPlaybackState => _output is not null ? _output.PlaybackState : PlaybackState.Stopped;

    public float GetVolume() => _output is not null ? _output.Volume : 1f;

    public event Action PlaybackPaused;

    public event Action PlaybackResumed;

    public event Action<PlaybackStopType> PlaybackStopped;

    private WaveFileReader? _waveFileReader;

    private WaveOutEvent? _output;

    public AudioPlayer(string filepath, float volume, double position = 0d)
    {
        PlaybackStopType = PlaybackStopType.ByReachingEndOfFile;

        _waveFileReader = new WaveFileReader(filepath);

        var outputStream = new MemoryStream();

        // I have to convert audio to PCM Wave
        using (var reader = WaveFormatConversionStream.CreatePcmStream(_waveFileReader))
        // Then save it to MemoryStream. I choose it instead saving it to temporary file and avoid I/O operation
        // Don't know why but without conversion and saving it to file/stream
        // I couldn't get accurate TotalTime property. It was always different than real
        // As well IgnoreDisposeStream is used because WaveFileWriter can Dispose MemoryStream
        using (var waveFileWriter = new WaveFileWriter(new IgnoreDisposeStream(outputStream), reader.WaveFormat))
        {
            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int readedBytes;
            while ((readedBytes = reader.Read(buffer, 0, buffer.Length - (buffer.Length % waveFileWriter.WaveFormat.BlockAlign))) > 0)
                waveFileWriter.Write(buffer, 0, readedBytes);
        }
        outputStream.Position = 0; // must be reseted so WaveFileReader can read headers
        _waveFileReader = new WaveFileReader(outputStream);

        _output = new WaveOutEvent
        {
            DesiredLatency = 50,
            NumberOfBuffers = 25, // if less latency then more buffers are needed to avoid clipping sound as I observed
            Volume = volume,
        };

        _output.PlaybackStopped += OutputPlaybackStopped;

        if (position > 0)
            SetPosition(position);

        _output.Init(_waveFileReader);
    }

    public void Dispose()
    {
        if (_output is not null)
        {
            if (_output.PlaybackState == PlaybackState.Playing)
                _output.Stop();

            _output.Dispose();
            _output = null;
        }

        if (_waveFileReader is not null)
        {
            _waveFileReader.Dispose();
            _waveFileReader = null;
        }
    }

    public void Pause()
    {
        if (_output is null)
            return;

        _output.Pause();

        PlaybackPaused?.Invoke();
    }

    public void Play(PlaybackState playbackState, double currentVolumeLevel, double desiredPosition)
    {
        if (_waveFileReader is null || _output is null || playbackState == PlaybackState.Playing)
            return;

        if (playbackState == PlaybackState.Paused && GetPositionInSeconds() != desiredPosition)
            _waveFileReader.SetPosition(desiredPosition);

        _output.Play();
        _output.Volume = (float)currentVolumeLevel;
        PlaybackResumed?.Invoke();
    }

    public void SetPosition(double value) => _waveFileReader?.SetPosition(value);

    public void SetVolume(float value)
    {
        if (_output is not null)
            _output.Volume = value;
    }

    public void Stop() => _output?.Stop();

    public void TogglePlayPause(double currentVolumeLevel, double desiredPosition)
    {
        if (_output is null)
        {
            Play(PlaybackState.Stopped, currentVolumeLevel, desiredPosition);

            return;
        }

        if (_output.PlaybackState == PlaybackState.Playing)
            Pause();
        else
            Play(_output.PlaybackState, currentVolumeLevel, desiredPosition);
    }

    private void OutputPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Dispose();
        PlaybackStopped?.Invoke(PlaybackStopType);
    }
}
