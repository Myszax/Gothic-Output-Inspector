using System.Windows;

namespace WPFUI.Components;

public static class Messages
{
    public const string CAPTION_AUDIO = "Audio";
    public const string AUDIO_PATH_NOT_SPECIFIED = "Please specify path to audio files.";
    public const string FILE_NOT_FOUND = "File not exists.";
    public const string TITLE = "Gothic Output Inspector";
    public const string CAPTION_SAVING = "Saving";
    public const string SAVE_PROMPT = "Do you want to save project?";

    public static void FileNotFound() => MessageBox.Show(FILE_NOT_FOUND, CAPTION_AUDIO, MessageBoxButton.OK, MessageBoxImage.Warning);
    public static void AudioPathNotSpecified() => MessageBox.Show(AUDIO_PATH_NOT_SPECIFIED, CAPTION_AUDIO, MessageBoxButton.OK, MessageBoxImage.Information);
    public static MessageBoxResult SaveProjectPrompt() => MessageBox.Show(SAVE_PROMPT, CAPTION_SAVING, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
}