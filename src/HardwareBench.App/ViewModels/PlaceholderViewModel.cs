using CommunityToolkit.Mvvm.ComponentModel;

namespace HardwareBench.App.ViewModels;

public partial class PlaceholderViewModel(string text) : ObservableObject
{
    public string Text { get; } = text;
}