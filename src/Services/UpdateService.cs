using System.ComponentModel;
using System.Runtime.CompilerServices;
using Velopack;
using Velopack.Sources;

namespace Breaksy.Services;

/// <summary>
/// Comprueba, descarga e instala actualizaciones publicadas en GitHub Releases mediante Velopack.
/// Solo funciona cuando la app se ha instalado con el instalador (no al ejecutar desde Visual Studio).
/// </summary>
public class UpdateService : INotifyPropertyChanged, IDisposable
{
    public const string RepositoryUrl = "https://github.com/DavidForero22/Breaksy";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly UpdateManager _manager;
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private UpdateInfo? _pendingUpdate;

    /// <summary>Se dispara (una vez por versión) al encontrar una actualización nueva.</summary>
    public event Action<string>? UpdateFound;

    public bool IsInstalled => _manager.IsInstalled;

    public string CurrentVersion => _manager.CurrentVersion?.ToString() ?? LocalizationService.Get("update.dev_version");

    private string? _availableVersion;
    public string? AvailableVersion { get => _availableVersion; private set => SetProperty(ref _availableVersion, value); }

    public bool IsUpdateAvailable => _pendingUpdate != null;

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    private string _status = string.Empty;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    // Se guarda la clave del mensaje para poder traducirlo de nuevo al cambiar de idioma
    private string _statusKey = string.Empty;
    private object[] _statusArgs = [];

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        Status = LocalizationService.Format(key, args);
    }

    private void RefreshLanguage()
    {
        if (_statusKey.Length > 0) Status = LocalizationService.Format(_statusKey, _statusArgs);
        OnPropertyChanged(nameof(CurrentVersion));
    }

    public UpdateService()
    {
        _manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));

        SetStatus(IsInstalled ? "update.press_check" : "update.only_installed");
        LocalizationService.Subscribe(this, u => u.RefreshLanguage());

        _timer = new System.Windows.Threading.DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += async (_, _) => await CheckAsync();
    }

    /// <summary>Comprueba ahora y luego periódicamente.</summary>
    public async void StartPeriodicChecks()
    {
        if (!IsInstalled) return;
        _timer.Start();
        await CheckAsync();
    }

    public async Task CheckAsync()
    {
        if (!IsInstalled || IsBusy) return;

        IsBusy = true;
        SetStatus("update.checking");
        try
        {
            var update = await _manager.CheckForUpdatesAsync();
            var previousVersion = AvailableVersion;

            _pendingUpdate = update;
            AvailableVersion = update?.TargetFullRelease.Version.ToString();
            OnPropertyChanged(nameof(IsUpdateAvailable));

            if (update == null)
            {
                SetStatus("update.up_to_date");
            }
            else
            {
                SetStatus("update.available", AvailableVersion!);
                LogService.Log($"[UpdateService] Actualización disponible: {CurrentVersion} -> {AvailableVersion}");
                if (AvailableVersion != previousVersion)
                    UpdateFound?.Invoke(AvailableVersion!);
            }
        }
        catch (Exception ex)
        {
            // Sin conexión, límite de peticiones de GitHub, etc. No es crítico: se reintenta en la próxima comprobación.
            SetStatus("update.check_failed");
            LogService.Log($"[UpdateService] ERROR al buscar actualizaciones: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Descarga la actualización pendiente, la aplica y reinicia la aplicación.</summary>
    public async Task DownloadAndRestartAsync()
    {
        if (_pendingUpdate == null || IsBusy) return;

        IsBusy = true;
        try
        {
            await _manager.DownloadUpdatesAsync(_pendingUpdate, progress => SetStatus("update.downloading", progress));
            SetStatus("update.installing");
            LogService.Log($"[UpdateService] Aplicando actualización {AvailableVersion} y reiniciando.");
            _manager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            SetStatus("update.download_failed");
            LogService.Log($"[UpdateService] ERROR al descargar/aplicar la actualización: {ex.Message}");
            IsBusy = false;
        }
    }

    public void Dispose() => _timer.Stop();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}
