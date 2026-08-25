using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;

    public bool IsMuted
    {
        get => _settings.IsMuted;
        set => _settings.IsMuted = value;
    }

    public bool IsDebugConsoleEnabled
    {
        get => _settings.IsDebugConsoleEnabled;
        set => _settings.IsDebugConsoleEnabled = value;
    }

    public ICommand OpenTestMenuCommand { get; }

    public SettingsViewModel(SettingsService settings, Action openTestMenuAction)
    {
        _settings = settings;
        OpenTestMenuCommand = new RelayCommand(_ => openTestMenuAction());

        _settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsService.IsMuted))
                OnPropertyChanged(nameof(IsMuted));

            if (e.PropertyName == nameof(SettingsService.IsDebugConsoleEnabled))
                OnPropertyChanged(nameof(IsDebugConsoleEnabled));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}