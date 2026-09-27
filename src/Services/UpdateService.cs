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
    private const string RepositoryUrl = "https://github.com/DavidForero22/Breaksy";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly UpdateManager _manager;
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private UpdateInfo? _pendingUpdate;

    /// <summary>Se dispara (una vez por versión) al encontrar una actualización nueva.</summary>
    public event Action<string>? UpdateFound;

    public bool IsInstalled => _manager.IsInstalled;

    public string CurrentVersion => _manager.CurrentVersion?.ToString() ?? "desarrollo";

    private string? _availableVersion;
    public string? AvailableVersion { get => _availableVersion; private set => SetProperty(ref _availableVersion, value); }

    public bool IsUpdateAvailable => _pendingUpdate != null;

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    private string _status = string.Empty;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public UpdateService()
    {
        _manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));

        Status = IsInstalled
            ? "Pulsa \"Buscar actualizaciones\" para comprobar si hay una versión nueva."
            : "Las actualizaciones solo están disponibles en la versión instalada.";

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
        Status = "Buscando actualizaciones...";
        try
        {
            var update = await _manager.CheckForUpdatesAsync();
            var previousVersion = AvailableVersion;

            _pendingUpdate = update;
            AvailableVersion = update?.TargetFullRelease.Version.ToString();
            OnPropertyChanged(nameof(IsUpdateAvailable));

            if (update == null)
            {
                Status = "Breaksy está actualizado.";
            }
            else
            {
                Status = $"Hay una nueva versión disponible: {AvailableVersion}.";
                LogService.Log($"[UpdateService] Actualización disponible: {CurrentVersion} -> {AvailableVersion}");
                if (AvailableVersion != previousVersion)
                    UpdateFound?.Invoke(AvailableVersion!);
            }
        }
        catch (Exception ex)
        {
            // Sin conexión, límite de peticiones de GitHub, etc. No es crítico: se reintenta en la próxima comprobación.
            Status = "No se pudo comprobar si hay actualizaciones.";
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
            await _manager.DownloadUpdatesAsync(_pendingUpdate, progress => Status = $"Descargando actualización... {progress}%");
            Status = "Instalando y reiniciando...";
            LogService.Log($"[UpdateService] Aplicando actualización {AvailableVersion} y reiniciando.");
            _manager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            Status = "No se pudo descargar la actualización. Inténtalo de nuevo más tarde.";
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
