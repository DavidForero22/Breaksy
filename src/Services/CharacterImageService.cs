using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Gestiona la resolución de imágenes del personaje basadas en el estado actual.
/// </summary>
public class CharacterImageService
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".gif"];
    private readonly string _baseImagePath;
    private readonly Random _random;

    public CharacterImageService()
    {
        _random = new Random();
        _baseImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "character");
    }

    /// <summary>
    /// Devuelve la ruta de una imagen al azar de la carpeta del estado.
    /// Si no hay, busca la plantilla específica en fallback/{estado}.*
    /// </summary>
    public string GetImagePathForState(BreaksyState state)
    {
        string stateKey = state switch
        {
            BreaksyState.Disabled => "disabled",
            BreaksyState.Idle => "idle",
            BreaksyState.Awaken => "awaken",
            BreaksyState.Paused => "paused",
            BreaksyState.Warning => "warning",
            BreaksyState.SeriousWarning => "serious_warning",
            BreaksyState.Blocked1 => "blocked_1",
            BreaksyState.Blocked2 => "blocked_2",
            BreaksyState.Sleeping => "sleeping",
            BreaksyState.Waiting => "waiting",
            _ => "fallback"
        };

        var targetDir = Path.Combine(_baseImagePath, stateKey);
        string[] files = Array.Empty<string>();

        // Buscar imágenes en la carpeta específica del estado
        if (Directory.Exists(targetDir))
        {
            files = Directory.GetFiles(targetDir, "*.*")
                             .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                             .ToArray();
        }

        string chosenImagePath = string.Empty;

        if (files.Length > 0)
        {
            chosenImagePath = files[_random.Next(files.Length)];
        }
        else
        {
            // Buscar la plantilla con el nombre exacto del estado en fallback/
            LogService.Log($"[CharacterImageService] No hay imágenes en '{stateKey}'. Buscando plantilla en fallback/{stateKey}...");

            var fallbackDir = Path.Combine(_baseImagePath, "fallback");
            if (Directory.Exists(fallbackDir))
            {
                // Buscar coincidencia exacta por nombre de estado (independientemente de la extensión)
                var fallbackStateFile = Directory.GetFiles(fallbackDir, $"{stateKey}.*")
                                                 .FirstOrDefault(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

                if (fallbackStateFile != null)
                {
                    chosenImagePath = fallbackStateFile;
                }
            }
        }

        // Si no se encontró absolutamente ninguna imagen
        if (string.IsNullOrEmpty(chosenImagePath))
        {
            LogService.Log($"[CharacterImageService] AVISO: No hay ninguna imagen ni plantilla para el estado '{stateKey}'. Se mostrará recuadro blanco.");
            return string.Empty;
        }

        try
        {
            var fileInfo = new FileInfo(chosenImagePath);
            double sizeKb = fileInfo.Length / 1024.0;

            using var stream = File.OpenRead(chosenImagePath);
            var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

            LogService.Log($"[CharacterImageService] ASSET ACTIVO: {fileInfo.Name} ({sizeKb:F1} KB, {frame.PixelWidth}x{frame.PixelHeight} px)");
        }
        catch (Exception ex)
        {
            LogService.Log($"[CharacterImageService] ASSET ACTIVO: {Path.GetFileName(chosenImagePath)} (No se pudieron leer propiedades: {ex.Message})");
        }

        return chosenImagePath;
    }
}