# Guía de personalización

Breaksy permite cambiar las imágenes del personaje y las voces que reproduce sin tocar el código: basta con colocar archivos en la **carpeta de personalización**:

```
%AppData%\Breaksy\assets\
```

La forma más fácil de llegar a ella es **Configuración → Personalización → Abrir carpeta**, que además crea todas las subcarpetas vacías para que veas dónde va cada archivo. Los cambios se aplican la próxima vez que el personaje cambie de estado.

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

## Voces

**Ruta:** `%AppData%\Breaksy\assets\voices\<evento>\`
**Formatos:** `.mp3`, `.wav`.
Las voces no suenan si está activada la opción de silenciar sonidos en la configuración.

### Al empezar o reanudar el trabajo (`awaken/`)

Suenan al entrar en Awaken, según desde dónde se llegue:

| Carpeta | Cuándo suena |
|---|---|
| `awaken/from_idle/` | Primer clic tras abrir la app. |
| `awaken/from_disabled/` | Se reactiva Breaksy después de haberlo desactivado. |
| `awaken/from_blocked1/` | El usuario pulsa "¡5 minutos más!" en el primer bloqueo. |
| `awaken/from_waiting/` | El usuario hace clic tras el descanso para empezar un nuevo ciclo. |

> Reanudar desde una pausa no reproduce ninguna voz.

### Avisos (`warning/` y `serious_warning/`)

Ambas fases duran 1 minuto y tienen tres momentos:

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

| Carpeta | Cuándo suena |
|---|---|
| `blocked_1/enter/` | Al bloquearse la pantalla por primera vez. |
| `blocked_1/1_min/` | Si el usuario lleva 1 minuto en el primer bloqueo sin elegir opción. |
| `blocked_1/5_min/` | Si lleva 5 minutos en el primer bloqueo. |
| `blocked_2/enter/` | Al entrar en el bloqueo definitivo (ya no hay prórroga). |
| `blocked_2/1_min/` | Lleva 1 minuto en el bloqueo definitivo sin descansar. |
| `blocked_2/5_min/` | Lleva 5 minutos en el bloqueo definitivo. |

**Fallback:** si la carpeta de un evento está vacía (tanto la tuya como la de serie), se reproduce `voices\system\fallback.mp3` (o `fallback.wav`). Si tampoco existe, no suena nada.

## Estructura completa de ejemplo

Es la estructura que crea **Abrir carpeta** (salvo `fallback` y `system`, que puedes añadir tú si quieres cambiar los archivos por defecto):

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
└── voices/
    ├── system/
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
    └── blocked_2/
        ├── enter/
        ├── 1_min/
        └── 5_min/
```

## Comprobar los cambios

Si algo no aparece como esperas, revisa el registro de la aplicación: `CharacterImageService` y `VoiceService` indican qué archivo han cargado o si han tenido que usar el fallback. El menú de pruebas de las opciones de desarrollador permite forzar estados sin esperar los 20 minutos.
