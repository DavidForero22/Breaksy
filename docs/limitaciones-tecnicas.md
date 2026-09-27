# Limitaciones técnicas

Limitaciones conocidas de Breaksy, en su mayoría impuestas por Windows. No son errores de la aplicación, pero conviene tenerlas presentes.

## Bloqueo de teclado

El bloqueo se implementa con un hook global de teclado de bajo nivel (`WH_KEYBOARD_LL`, ver `src/Services/KeyboardHookService.cs`).

### No bloquea en ventanas ejecutadas como administrador

Windows aplica el aislamiento de privilegios de la interfaz (UIPI): un proceso con un nivel de integridad normal no puede interceptar las pulsaciones dirigidas a un proceso elevado. Si la ventana activa se ejecuta **como administrador** (por ejemplo, Visual Studio o una terminal elevados), las teclas llegan a esa ventana aunque Breaksy esté en un bloqueo.

- Windows **no devuelve ningún error** en este caso, así que Breaksy no puede detectarlo ni avisar.
- **Solución:** ejecutar Breaksy también como administrador si se trabaja habitualmente con aplicaciones elevadas.

### Combinaciones reservadas por el sistema

Algunas combinaciones las gestiona Windows antes de que lleguen a cualquier hook y no se pueden bloquear:

- `Ctrl + Alt + Supr` (secuencia de atención segura).
- `Win + L` (bloquear sesión).

### Windows puede retirar el hook sin avisar

Si el callback del hook tarda más de lo permitido en responder (valor `LowLevelHooksTimeout` del registro, unos 300 ms por defecto en Windows 10/11), Windows puede desinstalarlo en silencio. Para evitarlo, el callback debe mantenerse lo más ligero posible y el hilo de la interfaz no debe quedar bloqueado con trabajo pesado.

### Si el hook no se puede registrar

Si Windows rechaza el registro del hook, Breaksy avisa al arrancar (o con una notificación en la bandeja si ocurre durante un bloqueo) y sigue funcionando **sin bloquear el teclado**: los niveles Intermedio y Estricto se comportan como el nivel Ligero en lo que respecta al teclado. La configuración muestra un aviso mientras dure esta situación.

## El ratón no se bloquea

Por diseño, solo se bloquea el teclado: el ratón tiene que seguir funcionando para pulsar **"Descansar"** o **"+5 minutos"**. Esto significa que, durante un bloqueo, se puede seguir usando el ratón en otras aplicaciones (en el nivel Estricto la ventana de advertencia lo hace más evidente, pero no lo impide).

## Plataforma

- Solo funciona en **Windows** (depende de WPF, WinForms para la bandeja y las APIs nativas de `user32.dll`).
- Requiere **.NET 8** (o una publicación *self-contained*).

## Configuración

- Las opciones **no se guardan en disco**: al reiniciar la aplicación vuelven a sus valores por defecto.

## Interfaz

- El menú del icono de la bandeja del sistema usa el estilo nativo de Windows (WinForms `ContextMenuStrip`), no el tema oscuro del resto de la aplicación.
- El icono de la aplicación (`assets/icon/icon.svg`) contiene una imagen rasterizada incrustada, no vectores reales: no escala por encima de su resolución original (681×626 px).
