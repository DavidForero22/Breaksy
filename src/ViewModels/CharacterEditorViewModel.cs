using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Breaksy.Models;
using Breaksy.Services;
using Microsoft.Win32;

namespace Breaksy.ViewModels;

public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Editor de personaje: una entrada por estado, cada una con su lista de imágenes y de sonidos.</summary>
public class CharacterEditorViewModel : ObservableBase
{
    private StateEditorViewModel _selectedState;

    public IReadOnlyList<StateEditorViewModel> States { get; }

    public StateEditorViewModel SelectedState
    {
        get => _selectedState;
        set
        {
            // Un clic sobre el estado ya elegido no debe dejar la lista sin selección
            if (value == null || value == _selectedState) return;
            _selectedState = value;
            OnPropertyChanged();
        }
    }

    private readonly SettingsService _settings;

    public ICommand ResetImagesCommand { get; }
    public ICommand ResetAudioCommand { get; }
    public ICommand ResetAudioTypesCommand { get; }

    public CharacterEditorViewModel(SettingsService settings)
    {
        _settings = settings;

        // Las carpetas coinciden con las que lee VoiceService
        SoundEventOption Event(string labelKey, string relativeDir) => new(labelKey, relativeDir, settings);

        SoundEventOption[] Steps(string folder) =>
        [
            Event("event.start", $@"audio\{folder}\step_1"),
            Event("event.left_40", $@"audio\{folder}\step_2"),
            Event("event.left_20", $@"audio\{folder}\step_3")
        ];

        SoundEventOption[] Blocked(string folder) =>
        [
            Event("event.on_block", $@"audio\{folder}\enter"),
            Event("event.after_1", $@"audio\{folder}\1_min"),
            Event("event.after_5", $@"audio\{folder}\5_min")
        ];

        States =
        [
            new(BreaksyState.Disabled, [Event("event.on_disable", @"audio\disabled\disable")]),
            new(BreaksyState.Idle, []),
            new(BreaksyState.Awaken,
            [
                Event("event.from_idle", @"audio\awaken\from_idle"),
                Event("event.from_disabled", @"audio\awaken\from_disabled"),
                Event("event.from_extension", @"audio\awaken\from_blocked1"),
                Event("event.from_rest", @"audio\awaken\from_waiting")
            ]),
            new(BreaksyState.Paused,
            [
                Event("event.on_pause", @"audio\paused\pause"),
                Event("event.on_resume", @"audio\paused\resume")
            ]),
            new(BreaksyState.Warning, Steps("warning")),
            new(BreaksyState.SeriousWarning, Steps("serious_warning")),
            new(BreaksyState.Blocked1, Blocked("blocked_1")),
            new(BreaksyState.Blocked2, Blocked("blocked_2")),
            new(BreaksyState.Sleeping, []),
            new(BreaksyState.Waiting, [])
        ];

        _selectedState = States[0];

        ResetImagesCommand = new RelayCommand(_ => ResetFiles("editor.confirm_reset_images",
            States.Select(s => s.Images)));
        ResetAudioCommand = new RelayCommand(_ => ResetFiles("editor.confirm_reset_audio",
            States.SelectMany(s => s.SoundEvents).Select(e => e.Assets)));
        ResetAudioTypesCommand = new RelayCommand(_ =>
        {
            if (Confirm("editor.confirm_reset_types")) _settings.ResetAudioSettings();
        });
    }

    private static bool Confirm(string messageKey) =>
        System.Windows.MessageBox.Show(LocalizationService.Get(messageKey), "Breaksy",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;

    // Borra los archivos propios del usuario: vuelven a usarse los de serie
    private static void ResetFiles(string confirmKey, IEnumerable<AssetListViewModel> lists)
    {
        if (!Confirm(confirmKey)) return;

        int failed = lists.Sum(list => list.ClearUserFiles());
        if (failed > 0)
            System.Windows.MessageBox.Show(LocalizationService.Format("editor.reset_failed", failed), "Breaksy",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }
}

/// <summary>Opción del selector Voz / Sonido de un evento de audio.</summary>
public class AudioTypeOption : ObservableBase
{
    public static IReadOnlyList<AudioTypeOption> All { get; } =
    [
        new(AudioType.Voice, "editor.type_voice"),
        new(AudioType.Sound, "editor.type_sound")
    ];

    private readonly string _labelKey;

    public AudioType Type { get; }
    public string Label => LocalizationService.Get(_labelKey);

    private AudioTypeOption(AudioType type, string labelKey)
    {
        Type = type;
        _labelKey = labelKey;
        LocalizationService.Subscribe(this, o => o.OnPropertyChanged(nameof(Label)));
    }
}

public class SoundEventOption : ObservableBase
{
    private readonly string _labelKey;
    private readonly SettingsService _settings;

    public string Label => LocalizationService.Get(_labelKey);
    public AssetListViewModel Assets { get; }
    public IReadOnlyList<AudioTypeOption> AudioTypes => AudioTypeOption.All;

    /// <summary>Identifica el evento en la configuración: su carpeta dentro de audio\.</summary>
    public string Key { get; }

    /// <summary>Tipo del audio (voz o sonido); por defecto, el que tiene de serie.</summary>
    public AudioTypeOption SelectedType
    {
        get => AudioTypeOption.All.First(o => o.Type == _settings.GetAudioType(Key));
        set { if (value != null) _settings.SetAudioType(Key, value.Type); }
    }

    public bool IsMuted
    {
        get => _settings.IsAudioMuted(Key);
        set => _settings.SetAudioMuted(Key, value);
    }

    /// <summary>Silenciar un audio solo se puede cambiar si su tipo está activo en Aplicación.</summary>
    public bool CanMute => _settings.GetAudioType(Key) == AudioType.Voice ? !_settings.IsMuted : !_settings.IsSoundMuted;

    public SoundEventOption(string labelKey, string relativeDir, SettingsService settings)
    {
        _labelKey = labelKey;
        _settings = settings;
        Key = relativeDir["audio\\".Length..];
        Assets = new AssetListViewModel(relativeDir, AssetKind.Sound);

        LocalizationService.Subscribe(this, o => o.OnPropertyChanged(nameof(Label)));
        // Enlace débil: el servicio de ajustes vive toda la sesión y este objeto, solo mientras esté abierta la ventana
        PropertyChangedEventManager.AddHandler(settings, OnSettingsChanged, string.Empty);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(SettingsService.IsMuted) or nameof(SettingsService.IsSoundMuted)
            or SettingsService.AudioSettingsProperty)) return;

        OnPropertyChanged(nameof(SelectedType));
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(CanMute));
    }
}

public class StateEditorViewModel : ObservableBase
{
    private enum EditorMode { None, Image, Sound }

    private EditorMode _mode = EditorMode.Image;
    private SoundEventOption? _selectedSoundEvent;

    private readonly BreaksyState _state;

    public string Name => LocalizationService.StateName(_state);
    public AssetListViewModel Images { get; }
    public IReadOnlyList<SoundEventOption> SoundEvents { get; }

    public bool HasSounds => SoundEvents.Count > 0;
    public bool IsImageMode => _mode == EditorMode.Image;
    public bool IsSoundMode => _mode == EditorMode.Sound;

    public SoundEventOption? SelectedSoundEvent
    {
        get => _selectedSoundEvent;
        set
        {
            if (value == null || value == _selectedSoundEvent) return;
            _selectedSoundEvent = value;
            OnPropertyChanged();
        }
    }

    public ICommand ToggleImageCommand { get; }
    public ICommand ToggleSoundCommand { get; }

    public StateEditorViewModel(BreaksyState state, IReadOnlyList<SoundEventOption> soundEvents)
    {
        _state = state;
        LocalizationService.Subscribe(this, o => o.OnPropertyChanged(nameof(Name)));
        Images = new AssetListViewModel($@"character\{CharacterImageService.GetStateKey(state)}", AssetKind.Image);
        SoundEvents = soundEvents;
        _selectedSoundEvent = soundEvents.FirstOrDefault();

        ToggleImageCommand = new RelayCommand(_ => SetMode(EditorMode.Image));
        ToggleSoundCommand = new RelayCommand(_ => { if (HasSounds) SetMode(EditorMode.Sound); });
    }

    private void SetMode(EditorMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;
        OnPropertyChanged(nameof(IsImageMode));
        OnPropertyChanged(nameof(IsSoundMode));
    }
}

public enum AssetKind { Image, Sound }

/// <summary>
/// Lista dinámica de archivos de una carpeta de personalización del usuario. Siempre termina con un hueco
/// vacío para añadir otro archivo, hasta un máximo de <see cref="MaxSlots"/>.
/// </summary>
public class AssetListViewModel
{
    public const int MaxSlots = 30;

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"];
    private static readonly string[] SoundExtensions = [".mp3", ".wav"];

    private readonly string _relativeDir;
    private readonly AssetKind _kind;

    public ObservableCollection<AssetSlotViewModel> Slots { get; } = [];

    public string FormatsText => _kind == AssetKind.Image ? ".png, .jpg, .jpeg, .gif" : ".mp3, .wav";

    private string Folder => Path.Combine(AssetPaths.UserRoot, _relativeDir);

    public AssetListViewModel(string relativeDir, AssetKind kind)
    {
        _relativeDir = relativeDir;
        _kind = kind;
        Refresh();
    }

    public bool IsImageList => _kind == AssetKind.Image;

    public bool IsSupported(string file) =>
        (_kind == AssetKind.Image ? ImageExtensions : SoundExtensions)
            .Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

    /// <summary>Vuelve a leer la carpeta del usuario.</summary>
    public void Refresh()
    {
        Slots.Clear();

        try
        {
            if (Directory.Exists(Folder))
            {
                foreach (var file in Directory.GetFiles(Folder).Where(IsSupported)
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Take(MaxSlots))
                    Slots.Add(new AssetSlotViewModel(this, file));
            }
        }
        catch (Exception ex)
        {
            LogService.Log($"[CharacterEditor] ERROR al leer '{Folder}': {ex.Message}");
        }

        if (Slots.Count < MaxSlots)
            Slots.Add(new AssetSlotViewModel(this, null));
    }

    /// <summary>Abre el explorador de archivos y añade lo que se elija.</summary>
    public void Browse()
    {
        var patterns = (_kind == AssetKind.Image ? ImageExtensions : SoundExtensions).Select(e => "*" + e);
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Get(_kind == AssetKind.Image ? "editor.choose_image" : "editor.choose_sound"),
            Filter = $"{LocalizationService.Get(_kind == AssetKind.Image ? "editor.images_filter" : "editor.sounds_filter")} ({FormatsText})|{string.Join(";", patterns)}",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
            AddFiles(dialog.FileNames);
    }

    /// <summary>Copia los archivos válidos a la carpeta del usuario. Devuelve cuántos se han rechazado.</summary>
    public int AddFiles(IEnumerable<string> files)
    {
        int rejected = 0;

        foreach (var source in files)
        {
            if (!File.Exists(source) || !IsSupported(source) || Slots.Count(s => !s.IsEmpty) >= MaxSlots)
            {
                rejected++;
                continue;
            }

            try
            {
                Directory.CreateDirectory(Folder);
                File.Copy(source, UniqueDestination(source));
                Refresh();
            }
            catch (Exception ex)
            {
                rejected++;
                LogService.Log($"[CharacterEditor] ERROR al copiar '{source}': {ex.Message}");
            }
        }

        return rejected;
    }

    // Si ya existe un archivo con ese nombre se añade un número, para no pisar los del usuario
    private string UniqueDestination(string source)
    {
        var name = Path.GetFileNameWithoutExtension(source);
        var ext = Path.GetExtension(source);
        var path = Path.Combine(Folder, name + ext);

        for (int i = 2; File.Exists(path); i++)
            path = Path.Combine(Folder, $"{name} ({i}){ext}");

        return path;
    }

    /// <summary>Borra todos los archivos del usuario de esta lista. Devuelve cuántos no se han podido borrar.</summary>
    public int ClearUserFiles()
    {
        int failed = 0;

        foreach (var slot in Slots.Where(s => !s.IsEmpty).ToList())
        {
            try
            {
                File.Delete(slot.FilePath!);
            }
            catch (Exception ex)
            {
                failed++;
                LogService.Log($"[CharacterEditor] ERROR al borrar '{slot.FilePath}': {ex.Message}");
            }
        }

        Refresh();
        return failed;
    }

    public void Remove(AssetSlotViewModel slot)
    {
        if (slot.FilePath == null) return;

        try
        {
            File.Delete(slot.FilePath);
        }
        catch (Exception ex)
        {
            LogService.Log($"[CharacterEditor] ERROR al borrar '{slot.FilePath}': {ex.Message}");
        }

        Refresh();
    }
}

public class AssetSlotViewModel
{
    private readonly AssetListViewModel _owner;

    public string? FilePath { get; }
    public bool IsEmpty => FilePath == null;
    public string FileName => Path.GetFileName(FilePath) ?? string.Empty;
    public string FormatsText => _owner.FormatsText;
    public bool IsImage => _owner.IsImageList;

    /// <summary>Miniatura de la imagen. Se carga en memoria para no dejar el archivo bloqueado.</summary>
    public ImageSource? Thumbnail { get; }

    public ICommand RemoveCommand { get; }
    public ICommand BrowseCommand { get; }

    public AssetSlotViewModel(AssetListViewModel owner, string? filePath)
    {
        _owner = owner;
        FilePath = filePath;

        if (filePath != null && owner.IsImageList)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 160;
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                bitmap.Freeze();
                Thumbnail = bitmap;
            }
            catch (Exception ex)
            {
                LogService.Log($"[CharacterEditor] ERROR al cargar la miniatura de '{filePath}': {ex.Message}");
            }
        }

        RemoveCommand = new RelayCommand(_ => _owner.Remove(this));
        BrowseCommand = new RelayCommand(_ => _owner.Browse());
    }

    public int AddFiles(IEnumerable<string> files) => _owner.AddFiles(files);
}
