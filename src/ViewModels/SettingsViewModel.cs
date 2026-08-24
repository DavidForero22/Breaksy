using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        _settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsService.IsMuted))
                OnPropertyChanged(nameof(IsMuted));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}