using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Breaksy.Services;

namespace Breaksy.Views;

/// <summary>Fuente de textos para XAML: avisa a los enlaces cuando cambia el idioma.</summary>
public class LocalizationSource : INotifyPropertyChanged
{
    public static LocalizationSource Instance { get; } = new();

    private LocalizationSource()
    {
        LocalizationService.LanguageChanged += () =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] => LocalizationService.Get(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Enlaza una propiedad al texto de una clave (para volver a enlazarla tras asignarle un texto fijo).</summary>
    public static void Bind(FrameworkElement element, DependencyProperty property, string key) =>
        element.SetBinding(property, new Binding($"[{key}]") { Source = Instance, Mode = BindingMode.OneWay });
}

/// <summary>Uso en XAML: <c>Text="{local:Loc settings.general}"</c>. Se actualiza al cambiar de idioma.</summary>
[MarkupExtensionReturnType(typeof(object))]
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }
    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = LocalizationSource.Instance, Mode = BindingMode.OneWay }
            .ProvideValue(serviceProvider);
}
