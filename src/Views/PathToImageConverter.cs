using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Breaksy.Services;

namespace Breaksy.Views;

/// <summary>
/// Convierte la ruta de una imagen en un <see cref="BitmapImage"/> cargado en memoria, para no dejar el
/// archivo bloqueado y que el editor de personaje pueda borrarlo o sustituirlo mientras se muestra.
/// </summary>
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || path.Length == 0 || !File.Exists(path)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            LogService.Log($"[PathToImageConverter] ERROR al cargar '{path}': {ex.Message}");
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
