using System.IO;
using System.Windows.Forms;
using Breaksy.ViewModels;

namespace Breaksy.Services;

/// <summary>
/// Icono en la bandeja del sistema con acceso rápido a las acciones del personaje,
/// útil cuando la ventana está minimizada o fuera de alcance.
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly MainViewModel _viewModel;
    private readonly NotifyIcon _notifyIcon;

    private readonly ToolStripMenuItem _toggleItem = new();
    private readonly ToolStripMenuItem _pauseItem = new();
    private readonly ToolStripMenuItem _updateItem = new() { Visible = false };

    private readonly UpdateService _updates;

    public TrayIconService(MainViewModel viewModel, UpdateService updates, Action showCharacterAction)
    {
        _viewModel = viewModel;
        _updates = updates;

        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon", "icon.ico");
        var menu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Mostrar personaje", null, (_, _) => showCharacterAction());
        showItem.Font = new System.Drawing.Font(showItem.Font, System.Drawing.FontStyle.Bold);

        _toggleItem.Click += (_, _) => ToggleActive();
        _pauseItem.Click += (_, _) => _viewModel.PauseCommand.Execute(null);
        _updateItem.Click += async (_, _) => await _updates.DownloadAndRestartAsync();

        menu.Items.Add(_updateItem);
        menu.Items.Add(showItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Configuración", null, (_, _) => _viewModel.OpenSettingsCommand.Execute(null));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Cerrar Breaksy", null, (_, _) => _viewModel.CloseCommand.Execute(null));

        // Refrescar textos y disponibilidad justo antes de mostrar el menú
        menu.Opening += (_, _) => UpdateMenuItems();

        _notifyIcon = new NotifyIcon
        {
            Icon = File.Exists(iconPath) ? new System.Drawing.Icon(iconPath) : System.Drawing.SystemIcons.Application,
            Text = "Breaksy",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) showCharacterAction();
        };

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _updates.UpdateFound += OnUpdateFound;
        // Al pulsar la notificación de actualización se abre la configuración (sección General)
        _notifyIcon.BalloonTipClicked += (_, _) =>
        {
            if (_updates.IsUpdateAvailable) _viewModel.OpenSettingsCommand.Execute(null);
        };
        UpdateTooltip();
    }

    private void ToggleActive()
    {
        if (_viewModel.CanStart)
            _viewModel.StartCommand.Execute(null);
        else if (_viewModel.CanDisable)
            _viewModel.DisableCommand.Execute(null);
    }

    private void UpdateMenuItems()
    {
        // Durante bloqueos y descanso no se puede desactivar (CanStart y CanDisable son falsos)
        _toggleItem.Text = _viewModel.CanStart ? "Activar" : "Desactivar";
        _toggleItem.Enabled = _viewModel.CanStart || _viewModel.CanDisable;

        _pauseItem.Text = _viewModel.PauseMenuHeader;
        _pauseItem.Enabled = _viewModel.CanPause;

        _updateItem.Visible = _updates.IsUpdateAvailable;
        _updateItem.Enabled = !_updates.IsBusy;
        _updateItem.Text = $"Actualizar a la versión {_updates.AvailableVersion}";
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.StateText))
            UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        // NotifyIcon.Text admite como máximo 63 caracteres
        var text = $"Breaksy - {_viewModel.StateText}";
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    private void OnUpdateFound(string version)
    {
        _notifyIcon.ShowBalloonTip(8000, "Breaksy: actualización disponible",
            $"La versión {version} está lista para instalarse. Pulsa aquí para ver los detalles.",
            ToolTipIcon.Info);
    }

    public void ShowWarning(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Warning);
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _updates.UpdateFound -= OnUpdateFound;
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
    }
}
