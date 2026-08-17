namespace HardwareBench.App.ViewModels;

public interface IFileSaveService
{
    string? PickSavePath(string defaultName);
}