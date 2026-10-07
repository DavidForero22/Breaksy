# Guía de personalización

Breaksy permite cambiar las imágenes del personaje, sus voces y sonidos, y los textos de la interfaz sin tocar el código: basta con colocar archivos en la **carpeta de personalización**:

```
%AppData%\Breaksy\assets\
```

La forma más fácil de llegar a ella es **Configuración → Personalización → Abrir carpeta**, que además crea todas las subcarpetas vacías para que veas dónde va cada archivo. Los cambios se aplican la próxima vez que el personaje cambie de estado (las imágenes del estado actual se actualizan al momento).

> **Editor de personaje.** En **Configuración → Personalización** hay un editor integrado: eliges un estado, pulsas **Imagen** o **Sonido** y añades archivos (con clic o arrastrándolos) hasta un máximo de 30 por lista. El editor copia los archivos a las mismas carpetas que se describen aquí, así que ambos métodos son equivalentes y se pueden combinar.

> Esta carpeta **no se toca al actualizar Breaksy**. No modifiques la carpeta `assets\` que hay junto a `Breaksy.exe` (los archivos de serie): se sustituye por completo en cada actualización.

## Cómo funciona

- **Varias variantes por evento**: si una carpeta contiene varios archivos, se elige uno **al azar** cada vez. Así puedes añadir variedad (varias poses, varias frases) simplemente metiendo más archivos.
- **El nombre del archivo no importa** dentro de una carpeta de estado/evento; solo importa la extensión.
- **Prioridad**: para cada estado o evento, si tu carpeta tiene algún archivo se usan **solo los tuyos**; si está vacía, se usan los de serie. Los tuyos no se mezclan con los originales.
- **Fallback**: si ni tu carpeta ni la de serie tienen archivos, se usa un archivo por defecto (ver cada sección). También puedes personalizar ese archivo por defecto creando la carpeta correspondiente en tu carpeta de personalización.

## Imágenes del personaje

**Ruta:** `%AppData%\Breaksy\assets\character\<estado>\`
**Formatos:** `.png`, `.jpg`, `.jpeg`, `.gif` (se recomienda PNG con fondo transparente).
**Tamaño:** la imagen se escala manteniendo la proporción dentro de una ventana de 140×180 px. Una imagen vertical con esas proporciones (por ejemplo 280×360 px) se verá mejor.

| Carpeta | Estado | Cuándo se muestra |
|---|---|---|
| `disabled/` | Disabled | El usuario ha desactivado Breaksy manualmente. El personaje está inactivo. |
| `idle/` | Idle | Al arrancar la app, esperando el primer clic para empezar el ciclo. |
| `awaken/` | Awaken | Durante la cuenta atrás de trabajo (20:00 o la prórroga de 5:00). Es la imagen que más tiempo se ve. |
| `paused/` | Paused | El usuario ha pausado el temporizador. |
| `warning/` | Warning | Se ha agotado el tiempo de trabajo; minuto de aviso antes del primer bloqueo. |
| `serious_warning/` | SeriousWarning | Se ha agotado la prórroga; último minuto de aviso antes del bloqueo definitivo. |
| `blocked_1/` | Blocked1 | Primer bloqueo de pantalla, con las opciones "¡5 minutos más!" y "Descansar". |
| `blocked_2/` | Blocked2 | Segundo bloqueo, solo con la opción "Descansar". Tono más exigente. |
| `sleeping/` | Sleeping | Los 40 segundos de descanso tras pulsar "Descansar". |
| `waiting/` | Waiting | Fin del descanso; el personaje espera un clic para empezar un nuevo ciclo. |

**Fallback:** si la carpeta de un estado está vacía (tanto la tuya como la de serie), se usa `character\fallback\<estado>.<ext>` (por ejemplo `fallback/warning.png`). Si tampoco existe, se muestra un recuadro en blanco.

## Audio: voces y sonidos

**Ruta:** `%AppData%\Breaksy\assets\audio\<estado>\<evento>\`
**Formatos:** `.mp3`, `.wav`.

Todos los audios viven bajo `audio\`, pero Breaksy los distingue en dos tipos, que se activan o desactivan por separado en **Configuración → Aplicación**:

| Tipo | Qué es | Se controla con |
|---|---|---|
| **Voz** | Líneas que dice el personaje al avisar o al cambiar de estado. | **Reproducir voces** |
| **Sonido** | Efectos que no son voces: la alarma y los sonidos de pausar, reanudar, desactivar y volver de desactivado. | **Reproducir sonidos** |

El tipo lo decide Breaksy según la carpeta, no el contenido del archivo. En las tablas siguientes cada carpeta indica el tipo **de serie**.

### Cambiar el tipo o silenciar un audio

En **Configuración → Personalización → Editor de personaje**, al abrir la pestaña **Sonido** de un estado, cada evento (cada carpeta de las tablas siguientes) tiene sus propias opciones:

- **Tipo (Voz / Sonido):** decide qué interruptor de Aplicación controla ese audio. Por defecto es el tipo de serie indicado en las tablas.
- **Silenciar:** silencia solo ese audio. Solo se puede cambiar mientras el tipo del audio esté activado en **Configuración → Aplicación**; si "Reproducir voces" (o "Reproducir sonidos") está apagado, el interruptor aparece deshabilitado.

Estas opciones se guardan en `settings.json`, no en la carpeta de audio. En la parte inferior del editor, **Valores por defecto** permite, con confirmación y para todos los estados a la vez:

- **Restablecer imágenes:** borra tus imágenes y vuelve a usar las de serie.
- **Restablecer audios:** borra tus audios y vuelve a usar los de serie.
- **Restablecer tipos:** cada audio recupera su tipo de serie y se quitan los silencios individuales.

> El *fallback* de voz (`audio/system/fallback.mp3`) solo se usa en los eventos que son voces de serie. Si conviertes un sonido en voz y su carpeta está vacía, no suena nada.

### Al empezar o reanudar el trabajo (`audio\awaken\`)

Suenan al entrar en Awaken, según desde dónde se llegue:

| Carpeta | Tipo | Cuándo suena |
|---|---|---|
| `awaken/from_idle/` | Voz | Primer clic tras abrir la app. |
| `awaken/from_disabled/` | **Sonido** | Se reactiva Breaksy después de haberlo desactivado. |
| `awaken/from_blocked1/` | Voz | El usuario pulsa "¡5 minutos más!" en el primer bloqueo. |
| `awaken/from_waiting/` | Voz | El usuario hace clic tras el descanso para empezar un nuevo ciclo. |

> Reanudar desde una pausa no reproduce ninguna voz (tiene su propio sonido, ver más abajo).

### Pausar, reanudar y desactivar (sonidos)

| Carpeta | Tipo | Cuándo suena |
|---|---|---|
| `paused/pause/` | Sonido | El usuario pausa el temporizador. |
| `paused/resume/` | Sonido | El usuario quita la pausa y el conteo continúa. |
| `disabled/disable/` | Sonido | El usuario desactiva Breaksy. |

Estos sonidos **no traen archivos de serie**: si la carpeta está vacía no suena nada (no se usa ningún fallback). Basta con añadir tus propios `.mp3` o `.wav`.

### Avisos (`warning/` y `serious_warning/`)

Todos son voces. Ambas fases duran 1 minuto y tienen tres momentos:

| Carpeta | Cuándo suena |
|---|---|
| `warning/step_1/` | Al agotarse los 20 minutos de trabajo (inicio del aviso). |
| `warning/step_2/` | A los 20 s del aviso (quedan 40 s para el bloqueo). |
| `warning/step_3/` | A los 40 s del aviso (quedan 20 s para el bloqueo). |
| `serious_warning/step_1/` | Al agotarse la prórroga de 5 minutos. |
| `serious_warning/step_2/` | Quedan 40 s para el bloqueo definitivo. |
| `serious_warning/step_3/` | Quedan 20 s para el bloqueo definitivo. |

Conviene que las frases de `serious_warning` suenen más impacientes que las de `warning`.

### Bloqueos (`blocked_1/` y `blocked_2/`)

Todos son voces.

| Carpeta | Cuándo suena |
|---|---|
| `blocked_1/enter/` | Al bloquearse la pantalla por primera vez. |
| `blocked_1/1_min/` | Si el usuario lleva 1 minuto en el primer bloqueo sin elegir opción. |
| `blocked_1/5_min/` | Si lleva 5 minutos en el primer bloqueo. |
| `blocked_2/enter/` | Al entrar en el bloqueo definitivo (ya no hay prórroga). |
| `blocked_2/1_min/` | Lleva 1 minuto en el bloqueo definitivo sin descansar. |
| `blocked_2/5_min/` | Lleva 5 minutos en el bloqueo definitivo. |

### Archivos del sistema (`audio/system/`)

| Archivo | Tipo | Para qué sirve |
|---|---|---|
| `fallback.mp3` (o `.wav`) | Voz | Se reproduce cuando la carpeta de una **voz** está vacía (tanto la tuya como la de serie). Si tampoco existe, no suena nada. |
| `wakeup_alarm.wav` | Sonido | Alarma que se repite en bucle cuando termina el descanso (estado Waiting), hasta que el usuario empieza un nuevo ciclo. |

El fallback solo se aplica a las voces; los sonidos con la carpeta vacía simplemente no suenan.

## Idioma de la interfaz

El idioma se elige en **Configuración → General → Idioma** (English o Español). Por defecto es inglés y la elección se recuerda al cerrar la app.

Los textos de la interfaz están en archivos JSON, uno por idioma:

```
%AppData%\Breaksy\assets\lang\en.json
%AppData%\Breaksy\assets\lang\es.json
```

Para cambiar un texto, copia el archivo del idioma desde la carpeta `assets\lang\` que hay junto a `Breaksy.exe` a la carpeta anterior y edita los valores. Cada línea tiene la forma `"clave": "texto"`:

- **No cambies las claves**, solo los textos de la derecha.
- Los marcadores como `{0}` se sustituyen por valores (por ejemplo un número de versión): consérvalos en el texto.
- `\n` es un salto de línea.
- Si tu archivo no incluye una clave, se usa el texto en inglés; y si falta también ahí, se muestra la propia clave.
- Igual que con las imágenes y los audios, tu archivo se usa **en lugar** del de serie, y se conserva al actualizar Breaksy. Cambia de idioma o reinicia Breaksy para ver los cambios.

Los nombres de los estados del personaje (Trabajando, Pausado…) están en las claves `state.*`.

> De momento solo se pueden elegir English y Español: añadir un archivo con otro código de idioma no lo hace aparecer en la lista.

## Estructura completa de ejemplo

Es la estructura que crea **Abrir carpeta** (salvo `fallback`, `system` y `lang`, que puedes añadir tú si quieres cambiar los archivos por defecto o los textos):

```
%AppData%\Breaksy\assets\
├── character/
│   ├── fallback/          # Imágenes por defecto: <estado>.png
│   ├── disabled/
│   ├── idle/
│   ├── awaken/
│   ├── paused/
│   ├── warning/
│   ├── serious_warning/
│   ├── blocked_1/
│   ├── blocked_2/
│   ├── sleeping/
│   └── waiting/
├── lang/                  # Textos de la interfaz: en.json, es.json
└── audio/
    ├── system/            # fallback.mp3, wakeup_alarm.wav
    ├── awaken/
    │   ├── from_idle/
    │   ├── from_disabled/
    │   ├── from_blocked1/
    │   └── from_waiting/
    ├── warning/
    │   ├── step_1/
    │   ├── step_2/
    │   └── step_3/
    ├── serious_warning/
    │   ├── step_1/
    │   ├── step_2/
    │   └── step_3/
    ├── blocked_1/
    │   ├── enter/
    │   ├── 1_min/
    │   └── 5_min/
    ├── blocked_2/
    │   ├── enter/
    │   ├── 1_min/
    │   └── 5_min/
    ├── paused/
    │   ├── pause/
    │   └── resume/
    └── disabled/
        └── disable/
```

## Comprobar los cambios

Si algo no aparece como esperas, revisa el registro de la aplicación: `CharacterImageService` y `VoiceService` indican qué archivo han cargado, si era voz o sonido, o si han tenido que usar el fallback. El menú de pruebas de las opciones de desarrollador permite forzar estados sin esperar los 20 minutos.
