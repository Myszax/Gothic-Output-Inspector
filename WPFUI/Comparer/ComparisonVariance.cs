namespace WPFUI.Comparer;

public sealed class ComparisonVariance
{
    public string PropertyName { get; set; } = string.Empty;
    public object? ValA { get; set; }
    public object? ValB { get; set; }
}