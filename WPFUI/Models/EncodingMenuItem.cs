using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace WPFUI.Models;
public partial class EncodingMenuItem : ObservableObject
{
    public Encoding Encoding { get; set; }

    [ObservableProperty]
    private bool _isChecked = false;
}