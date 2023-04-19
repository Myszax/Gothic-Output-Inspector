using NAudio.Wave;
using System;

namespace WPFUI.NAudioWrapper;

public static class WaveStreamExtensions
{
    // Set position of WaveStream to nearest block to supplied position
    public static void SetPosition(this WaveStream stream, long position)
    {
        // distance from block boundary (may be 0)
        long adj = position % stream.WaveFormat.BlockAlign;

        // adjust position to boundary and clamp to valid range
        long newPos = Math.Max(0, Math.Min(stream.Length, position - adj));

        // set playback position
        stream.Position = newPos;
    }

    public static void SetPosition(this WaveStream stream, double seconds) => stream.SetPosition((long)(seconds * stream.WaveFormat.AverageBytesPerSecond));

    public static void SetPosition(this WaveStream stream, TimeSpan time) => stream.SetPosition(time.TotalSeconds);

    public static void Seek(this WaveStream stream, double offset) => stream.SetPosition(stream.Position + (long)(offset * stream.WaveFormat.AverageBytesPerSecond));
}