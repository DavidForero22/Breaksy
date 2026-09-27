# Roadmap — Breaksy

Guía de desarrollo desde cero. El orden está pensado para validar primero los riesgos técnicos más grandes (el hook de teclado y la ventana de bloqueo), antes de invertir tiempo en pulido visual o contenido creativo que dependen de que esa base funcione.

Cada fase asume que la anterior está terminada y probada manualmente. No hace falta cerrar el checklist entero de una fase para empezar a tocar la siguiente si algo bloquea el avance, pero sí conviene no llegar a la fase 6 (bloqueo real) sin tener la máquina de estados sólida.

---

## Fase 0 — Prueba de concepto de riesgo técnico

Objetivo: confirmar cuanto antes que las dos piezas más inciertas del proyecto son viables, antes de construir nada más encima.

- [x] Proyecto .NET mínimo (WPF) que registre un hook de teclado de bajo nivel (`WH_KEYBOARD_LL` vía P/Invoke) y detecte pulsaciones (por consola).
- [x] El hook debe ser capaz de **suprimir** pulsaciones (no solo detectarlas).
- [x] Ventana WPF sin decoración, `Topmost = true`, que se mantenga por encima de otras ventanas incluso al hacer clic en otras apps.
- [x] Documentar en `docs/` cualquier limitación encontrada (antivirus que marque el hook como sospechoso, necesidad de ejecutar como administrador, comportamiento en múltiples monitores, etc.).

Si algo de esto falla o resulta muy inestable, es el momento de replantear el enfoque — mucho más barato ahora que con todo el resto del proyecto construido encima.

---

## Fase 1 — Esqueleto del proyecto .NET

- [x] Crear la solución y el proyecto WPF dentro de `src/Breaksy/`.
- [x] Configurar `.gitignore` para .NET (bin/, obj/, etc.).
- [x] Decidir versión de .NET (LTS recomendable) y dejarlo anotado en el README — **.NET 8**.
- [x] Ventana principal mínima que arranca y se cierra, sin lógica todavía — solo para confirmar que el proyecto compila y ejecuta.

---

## Fase 2 — Máquina de estados (lógica pura, sin UI)

Esta es la parte más importante de todo el proyecto: si esto está bien hecho, la UI luego es "solo" pintar lo que la máquina de estados dice.

- [x] Modelar los estados como enum en `Models/BreaksyState.cs`: `Disabled`, `Idle`, `Awaken`, `Paused`, `Warning`, `SeriousWarning`, `Blocked1`, `Blocked2`, `Sleeping`, `Waiting`.
- [x] Implementar las transiciones:
  - `Disabled` / `Idle` → `Awaken` (por clic del usuario).
  - `Awaken` (ciclo normal) → `Warning` (al agotar 20 min) / `Paused` / `Disabled`.
  - `Awaken` (con prórroga) → `SeriousWarning` (al agotar 5 min) / `Paused` / `Disabled`.
  - `Warning` → `Blocked1` (al agotar 1 min) / `Paused` / `Disabled`.
  - `SeriousWarning` → Blocked2 (al agotar 1 min) / `Paused` / `Disabled`.
  - `Blocked1` → `Awaken` con 5:00 (prórroga, extensionUsada = true) / `Sleeping`.
  - `Blocked2` → `Sleeping`.
  - `Sleeping` → `Waiting` automático tras 40s (reseteando extensionUsada).
  - `Waiting` → `Awaken` (por clic del usuario) / `Disabled`.
  - `Awaken` / `Warning` / `SeriousWarning` → `Paused` (al pulsar pausar).
- [x] Reglas de guarda: prohibir pausar/desactivar en `Blocked1`, `Blocked2` y `Sleeping`; prohibir prórroga en `Blocked2`.
- [x] Cronómetro desacoplado de la UI en `Services/` que emita eventos de tick y cambios de estado.
- [x] Pruebas unitarias o manuales simulando el tiempo para validar todas las transiciones antes de conectar la UI.

---

## Fase 3 — Overlay del personaje (esquina inferior derecha)

- [x] Ventana WPF pequeña, sin bordes, `Topmost`, posicionada en la esquina inferior derecha, que simplemente muestra una imagen estática.
- [x] Conectar la ventana a la máquina de estados de la Fase 2: que cambie de imagen según el estado (aunque sean placeholders/rectángulos de colores por ahora, no hace falta arte final todavía).
- [x] Contador visible del tiempo restante (aunque sea texto simple) para poder depurar visualmente que el temporizador avanza bien.
- [x] Menú contextual o controles al interactuar en `Awaken`/`Paused`/`Idle`/`Waiting`: Iniciar, Pausar, Reanudar, Desactivar y Salir.

---

## Fase 4 — Pantalla de bloqueo

- [x] Activación automática del hook supresor de teclado durante `Blocked1`, `Blocked2` y `Sleeping`.
- [x] Botones "¡5 minutos más!" y "Descansar" visibles en `Blocked1`; solo "Descansar" en `Blocked2`.
- [x] Habilitar pantalla de descanso durante `Sleeping` con cuenta atrás visual de 30 segundos.
- [x] Confirmar liberación segura del hook al entrar en `Waiting` o al conceder la prórroga hacia `Awaken`.

---

## Fase 5 — Sistema de audio / líneas de voz

- [x] Seleccionar mecanismo de audio (`System.Speech.Synthesis` o reproducción de archivos en `assets/voces/`).
- [x] Servicio de audio en `Services/` para reproducir pistas/frases según el evento de estado.
- [x] Disparadores de audio: avisos en `Warning`, avisos críticos en `SeriousWarning`, entrada a `Blocked1`, entrada a `Blocked2` y mensaje de despertar en `Waiting`.
- [x] Placeholders de texto o pitidos de depuración para validar la sincronización temporal sin depender de assets finales.

---

## Fase 6 — Integración completa y pruebas de uso real

- [X] Prueba del ciclo estándar: `Idle` → `Awaken` (20m) → `Warning` (1m) → `Blocked1` → `Sleeping` (40s) → `Waiting` → `Awaken`.
- [X] Prueba del ciclo con prórroga: `Blocked1` → `Awaken` (5m) → `SeriousWarning` (1m) → `Blocked2` → `Sleeping` (40s) → `Waiting` → `Awaken`.
- [X] Validación de pausas en fases permitidas y verificación de bloqueo estricto en estados de penalización.
- [X] Jornada de prueba en entorno real con IDE/editor habitual para detectar interferencias con atajos de teclado o foco de ventanas.

---

## Fase 7 — Assets finales

- [X] Ilustraciones por defecto del personaje en `assets/personaje/` para cada estado (`disabled`, `idle`, `awaken`, `paused`, `warning`, `serious_warning`, `blocked_1`, `blocked_2`, `sleeping`, `waiting`).
- [X] Líneas de audio por defecto procesadas en `assets/voces/` (`generico`, `avisos`, `avisos_serios`, `bloqueado_1`, `bloqueado_2`, `despertar`).
- [X] Reemplazo de placeholders gráficos y de sonido por los recursos definitivos.

---

## Fase 8 — Pulido y configuración

- [ ] Persistencia de alguna preferencia básica si hace falta (p. ej. recordar posición en pantalla si hay varios monitores).
- [ ] Icono de la app y bandeja del sistema (system tray) para poder desactivar/activar sin tener que hacer click sobre el personaje si está minimizado o difícil de alcanzar.
- [ ] Manejo de errores: qué pasa si el hook de teclado falla al registrarse (p. ej. permisos insuficientes) — la app no debería arrancar en un estado roto sin avisar.
- [ ] Arranque automático con Windows (opcional, a valorar si tiene sentido para tu caso de uso).

---

## Fase 9 — Empaquetado y publicación

- [ ] Generar un ejecutable/instalador (o al menos un build self-contained de .NET) para no depender de tener el SDK instalado.
- [ ] Rellenar en el README los apartados técnicos que quedaron pendientes ("se documentará más adelante").
- [ ] Decidir y añadir la licencia (según lo hablado: código en GPLv3, assets con licencia separada si se quiere más control sobre el arte).
- [ ] Si se decide abrir el repositorio: añadir guía de contribución básica (`CONTRIBUTING.md`) y dejar claro en el README que es un proyecto personal sin soporte garantizado.

---

## Notas generales

- No merece la pena avanzar a la Fase 3 en adelante si la Fase 0 no confirma que el bloqueo de teclado y la ventana always-on-top-no-cerrable funcionan de forma fiable en tu entorno real.
- La Fase 2 (máquina de estados) es la que más tiempo ahorra si se hace bien desde el principio: cualquier ambigüedad de las reglas que ya definimos en la conversación debería quedar resuelta en código antes de tocar la UI.
- Las Fases 3 a 6 pueden convivir con placeholders de arte y voz — no hace falta esperar a tener el contenido final para validar que la mecánica funciona.
