using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Gestiona la reproducción de audios en la carpeta assets/audio.
/// Las voces (carpeta audio/ de cada estado) se silencian con "Reproducir voces"; los sonidos
/// (alarma, pausa, reanudar, desactivar, vuelta desde desactivado) con "Reproducir sonidos".
/// Reacciona a los eventos de la máquina de estados y a los ticks de tiempo.
/// </summary>
public class VoiceService : IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly SettingsService _settings;
    private readonly MediaPlayer _mediaPlayer;
    private readonly MediaPlayer _soundPlayer;
    private readonly DispatcherTimer _blockedTimer;
    private readonly Random _random;
    private readonly MediaPlayer _alarmPlayer;
    private bool _alarmActive;

    private int _blockedSeconds = 0;

    public VoiceService(BreaksyStateMachine stateMachine, SettingsService settings)
    {
        _stateMachine = stateMachine;
        _settings = settings;
        _mediaPlayer = new MediaPlayer();
        _soundPlayer = new MediaPlayer();
        _random = new Random();
        _alarmPlayer = new MediaPlayer();
        _alarmPlayer.MediaEnded += OnAlarmEnded;

        // Temporizador secundario exclusivo para contar el tiempo DENTRO de los bloqueos
        _blockedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _blockedTimer.Tick += OnBlockedTimerTick;

        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.Tick += OnStateMachineTick;
        _settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        // La alarma suena mientras el personaje está en espera
        if (e.OldState == BreaksyState.Waiting) StopWakeupAlarm();
        if (e.NewState == BreaksyState.Waiting) StartWakeupAlarm();

        // Manejar temporizador de bloqueos
        if (e.NewState is BreaksyState.Blocked1 or BreaksyState.Blocked2)
        {
            _blockedSeconds = 0;
            _blockedTimer.Start();
        }
        else
        {
            _blockedTimer.Stop();
        }

        // Sonidos de pausa, reanudación y desactivación
        if (e.NewState == BreaksyState.Paused) PlayRandomSound(@"paused\pause");
        if (e.OldState == BreaksyState.Paused
            && e.NewState is BreaksyState.Awaken or BreaksyState.Warning or BreaksyState.SeriousWarning)
            PlayRandomSound(@"paused\resume");
        if (e.NewState == BreaksyState.Disabled) PlayRandomSound(@"disabled\disable");

        // Disparar audios de transición
        switch (e.NewState)
        {
            case BreaksyState.Awaken:
                HandleAwakenVoices(e.OldState);
                break;
            case BreaksyState.Warning:
                PlayRandomVoice(@"warning\step_1");
                break;
            case BreaksyState.SeriousWarning:
                PlayRandomVoice(@"serious_warning\step_1");
                break;
            case BreaksyState.Blocked1:
                PlayRandomVoice(@"blocked_1\enter");
                break;
            case BreaksyState.Blocked2:
                PlayRandomVoice(@"blocked_2\enter");
                break;
        }
    }

    private void HandleAwakenVoices(BreaksyState oldState)
    {
        switch (oldState)
        {
            case BreaksyState.Idle: PlayRandomVoice(@"awaken\from_idle"); break;
            case BreaksyState.Disabled: PlayRandomSound(@"awaken\from_disabled"); break;
            case BreaksyState.Blocked1: PlayRandomVoice(@"awaken\from_blocked1"); break;
            case BreaksyState.Waiting: PlayRandomVoice(@"awaken\from_waiting"); break;
        }
    }

    private void OnStateMachineTick(object? sender, TimeSpan remaining)
    {
        // Warning y SeriousWarning duran 1 minuto (60s).
        // El tiempo remaining cuenta hacia atrás.
        double secondsRemaining = remaining.TotalSeconds;

        if (_stateMachine.CurrentState == BreaksyState.Warning)
        {
            if (secondsRemaining == 40) PlayRandomVoice(@"warning\step_2");
            if (secondsRemaining == 20) PlayRandomVoice(@"warning\step_3");
        }
        else if (_stateMachine.CurrentState == BreaksyState.SeriousWarning)
        {
            if (secondsRemaining == 40) PlayRandomVoice(@"serious_warning\step_2");
            if (secondsRemaining == 20) PlayRandomVoice(@"serious_warning\step_3");
        }
    }

    private void OnBlockedTimerTick(object? sender, EventArgs e)
    {
        _blockedSeconds++;

        if (_stateMachine.CurrentState == BreaksyState.Blocked1)
        {
            if (_blockedSeconds == 60) PlayRandomVoice(@"blocked_1\1_min");
            if (_blockedSeconds == 300) PlayRandomVoice(@"blocked_1\5_min");
        }
        else if (_stateMachine.CurrentState == BreaksyState.Blocked2)
        {
            if (_blockedSeconds == 60) PlayRandomVoice(@"blocked_2\1_min");
            if (_blockedSeconds == 300) PlayRandomVoice(@"blocked_2\5_min");
        }
    }

    // Si se silencian los sonidos con la alarma sonando, se pausa; al reactivarlos continúa
    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsService.IsSoundMuted) || !_alarmActive) return;

        if (_settings.IsSoundMuted) _alarmPlayer.Pause();
        else _alarmPlayer.Play();
    }

    private void StartWakeupAlarm()
    {
        if (_settings.IsSoundMuted) return;

        var files = AssetPaths.FindFiles(Path.Combine("audio", "system"), "*.*",
            f => Path.GetFileName(f).Equals("wakeup_alarm.wav", StringComparison.OrdinalIgnoreCase));

        if (files.Length == 0)
        {
            LogService.Log("[VoiceService] OMITIDO: No se encontró 'wakeup_alarm.wav' en assets/audio/system/.");
            return;
        }

        _alarmActive = true;
        LogService.Log($"[VoiceService] ALARMA -> {Path.GetFileName(files[0])}");
        _alarmPlayer.Open(new Uri(files[0]));
        _alarmPlayer.Play();
    }

    private void StopWakeupAlarm()
    {
        if (!_alarmActive) return;
        _alarmActive = false;
        _alarmPlayer.Stop();
        _alarmPlayer.Close();
    }

    // La alarma se repite hasta que el usuario reinicie el ciclo (sale de Waiting)
    private void OnAlarmEnded(object? sender, EventArgs e)
    {
        if (!_alarmActive || _settings.IsSoundMuted) return;
        _alarmPlayer.Position = TimeSpan.Zero;
        _alarmPlayer.Play();
    }

    // Sonido (no es una voz): respeta "Reproducir sonidos" y, si no hay archivos, no suena nada
    private void PlayRandomSound(string subCategoryPath)
    {
        if (_settings.IsSoundMuted) return;

        var files = AssetPaths.FindFiles(Path.Combine("audio", subCategoryPath), "*.*",
            f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                 f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        if (files.Length == 0) return;

        var targetFile = files.Length == 1 ? files[0] : files[_random.Next(files.Length)];
        LogService.Log($"[VoiceService] SONIDO -> {Path.GetFileName(targetFile)}");

        _soundPlayer.Open(new Uri(targetFile));
        _soundPlayer.Play();
    }

    private void PlayRandomVoice(string subCategoryPath)
    {
        if (_settings.IsMuted) return;

        // Intentar buscar en la carpeta específica del estado (primero la del usuario, luego la de serie)
        var files = AssetPaths.FindFiles(Path.Combine("audio", subCategoryPath), "*.*",
            f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                 f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

        // Lógica de Fallback si la carpeta no existe o está vacía
        if (files.Length == 0)
        {
            LogService.Log($"[VoiceService] Faltan audios en -> {subCategoryPath}. Buscando fallback.mp3...");

            files = AssetPaths.FindFiles(Path.Combine("audio", "system"), "*.*",
                f => f.EndsWith("fallback.mp3", StringComparison.OrdinalIgnoreCase) ||
                     f.EndsWith("fallback.wav", StringComparison.OrdinalIgnoreCase));

            // Si no se encuentra el archivo de fallback específico, abortar
            if (files.Length == 0)
            {
                LogService.Log("[VoiceService] OMITIDO: No se encontró 'fallback.mp3' en assets/audio/system/."); return;
            }
        }

        // Reproducir el archivo (ya sea el específico o el de fallback)
        var targetFile = files.Length == 1 ? files[0] : files[_random.Next(files.Length)];
        LogService.Log($"[VoiceService] REPRODUCIENDO -> {Path.GetFileName(targetFile)}");

        _mediaPlayer.Open(new Uri(targetFile));
        _mediaPlayer.Play();
    }

    public void Dispose()
    {
        _stateMachine.StateChanged -= OnStateChanged;
        _stateMachine.Tick -= OnStateMachineTick;
        _settings.PropertyChanged -= OnSettingsChanged;
        _blockedTimer.Tick -= OnBlockedTimerTick;
        _blockedTimer.Stop();
        _mediaPlayer.Close();
        _soundPlayer.Close();
        _alarmPlayer.MediaEnded -= OnAlarmEnded;
        _alarmPlayer.Close();
    }
}