# Fase 0 — Prueba de concepto de riesgo técnico

Registro de las pruebas realizadas para validar la viabilidad de los dos mecanismos más críticos del proyecto: la ventana *always-on-top* no cerrable, y la supresión del teclado mediante hook de bajo nivel. Este documento se irá ampliando a medida que se hagan más pruebas (otros equipos, antivirus adicionales, distintos monitores, etc.).

Estado: ✅ resultados iniciales positivos, sin bloqueantes encontrados hasta ahora.

---

## 1. Ventana *always-on-top*

- Implementado con `Topmost="True"` en `MainWindow.xaml`.
- Confirmado que la ventana permanece por encima de otras aplicaciones incluso al cambiar el foco a otra ventana.

Pendiente de probar en fases futuras: comportamiento con varios monitores, y con otras aplicaciones que también usen `Topmost` (donde el orden entre "topmost" puede no ser determinista).

---

## 2. Cierre de la ventana

**Objetivo**: comprobar que se puede impedir el cierre por vías normales (Alt+F4, barra de tareas) y permitirlo únicamente desde un control propio de la interfaz.

**Implementación**: flag temporal `OnBtnCloseClick` en `MainWindow.xaml.cs`, distinguiendo el cierre disparado por el botón propio (`_allowClosingButton`) del disparado por el sistema operativo, cancelando este último en el evento `Closing`.

**Resultado**: ✅ funciona como se esperaba.
- Alt+F4 → bloqueado.
- Cierre desde clic derecho en la barra de tareas → bloqueado.
- Botón "Cerrar (prueba)" de la interfaz → cierra la aplicación con normalidad.
- Probado en un usuario **sin permisos de administrador**: sin problemas.

**Limitación conocida y aceptada**: "Finalizar tarea" desde el Administrador de Tareas de Windows mata el proceso directamente a nivel de sistema operativo, sin pasar por el evento `Closing`. No hay forma de evitar esto desde ningún proceso de Windows sin privilegios especiales — se acepta como límite razonable del diseño, no como un fallo a corregir.

---

## 3. Supresión de teclado

**Objetivo**: comprobar que el hook `WH_KEYBOARD_LL` no solo puede detectar pulsaciones, sino también suprimirlas, evitando que lleguen a cualquier aplicación (no solo a Breaksy).

**Implementación**: flag temporal `HideKeys` en `KeyboardHookService.cs`. Cuando está activo, `HookCallback` devuelve un valor distinto de cero sin llamar a `CallNextHookEx`, evitando así que la pulsación continúe su camino.

**Resultado**: ✅ funciona como se esperaba. Con el flag activo, el teclado queda completamente inhabilitado a nivel de sistema (no solo dentro de la ventana de Breaksy), y el ratón sigue funcionando con normalidad.

**Probado en un usuario sin permisos de administrador**: sin problemas — no fue necesario elevar privilegios para instalar el hook ni para suprimir las pulsaciones.

---

## 4. Análisis antivirus (VirusTotal)

**Objetivo**: comprobar si el uso de `SetWindowsHookEx` + supresión de teclas (un patrón que se solapa con el comportamiento típico de un keylogger) genera falsos positivos relevantes en motores antivirus.

**Archivo analizado**: ejecutable compilado (`Breaksy.exe`), no el código fuente.

**Resultado**: de 71 motores de análisis:
- 67 no detectaron código malicioso.
- 4 no pudieron procesar el archivo.
- 0 lo marcaron como malicioso.

Este resultado es mejor de lo esperado — se anticipaban algunos falsos positivos puntuales dado el patrón de hook + supresión, pero no se ha dado ningún caso en esta build.

**A tener en cuenta de cara al futuro**:
- El ejecutable no está firmado digitalmente. Aunque VirusTotal no lo marque, es probable que Windows SmartScreen muestre avisos al ejecutarlo por primera vez en otro equipo — es un tema independiente del análisis antivirus, pendiente para la fase de empaquetado.
- Conviene repetir este análisis en builds futuras (sobre todo tras añadir la ventana de bloqueo definitiva y el resto de lógica), ya que el comportamiento puede cambiar según se añada más código.
- VirusTotal es un servicio público: el archivo subido queda accesible para terceros. No representa un problema con esta build de prueba, pero es algo a recordar antes de subir builds con contenido más sensible.