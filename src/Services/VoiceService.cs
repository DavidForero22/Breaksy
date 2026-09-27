using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Gestiona la reproducción de audios en la carpeta assets/voices.
/// Reacciona a los eventos de la máquina de estados y a los ticks de tiempo.
/// </summary>
public class VoiceService : IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly SettingsService _settings;
    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherTimer _blockedTimer;
    private readonly Random _random;

    private int _blockedSeconds = 0;

    public VoiceService(BreaksyStateMachine stateMachine, SettingsService settings)
    {
        _stateMachine = stateMachine;
        _settings = settings;
        _mediaPlayer = new MediaPlayer();
        _random = new Random();

        // Temporizador secundario exclusivo para contar el tiempo DENTRO de los bloqueos
        _blockedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _blockedTimer.Tick += OnBlockedTimerTick;

        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.Tick += OnStateMachineTick;
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
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
            case BreaksyState.Disabled: PlayRandomVoice(@"awaken\from_disabled"); break;
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

    private void PlayRandomVoice(string subCategoryPath)
    {
        if (_settings.IsMuted) return;

        // Intentar buscar en la carpeta específica del estado (primero la del usuario, luego la de serie)
        var files = AssetPaths.FindFiles(Path.Combine("voices", subCategoryPath), "*.*",
            f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                 f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

        // Lógica de Fallback si la carpeta no existe o está vacía
        if (files.Length == 0)
        {
            LogService.Log($"[VoiceService] Faltan audios en -> {subCategoryPath}. Buscando fallback.mp3...");

            files = AssetPaths.FindFiles(Path.Combine("voices", "random"), "*.*",
                f => f.EndsWith("fallback.mp3", StringComparison.OrdinalIgnoreCase) ||
                     f.EndsWith("fallback.wav", StringComparison.OrdinalIgnoreCase));

            // Si no se encuentra el archivo de fallback específico, abortar
            if (files.Length == 0)
            {
                LogService.Log("[VoiceService] OMITIDO: No se encontró 'fallback.mp3' en assets/voices/random/."); return;
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
        _blockedTimer.Tick -= OnBlockedTimerTick;
        _blockedTimer.Stop();
        _mediaPlayer.Close();
    }
}