using CommunityToolkit.Mvvm.ComponentModel;

namespace HardwareBench.App.ViewModels;

public partial class MainViewModel(
    DetectionViewModel detection,
    PlaceholderViewModel benchmark,
    PlaceholderViewModel history) : ObservableObject
{
    public IReadOnlyList<NavEntry> Nav { get; } =
    [
        new NavEntry("硬件检测", detection),
        new NavEntry("性能跑分", benchmark),
        new NavEntry("历史结果", history)
    ];

    private ObservableObject? _currentPage;

    public ObservableObject? CurrentPage
    {
        get => _currentPage ??= detection;
        set => SetProperty(ref _currentPage, value);
    }

    public sealed record NavEntry(string Title, ObservableObject ViewModel);
}