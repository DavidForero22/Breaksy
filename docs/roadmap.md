# Roadmap — Breaksy

Guía de desarrollo desde cero. El orden está pensado para validar primero los riesgos técnicos más grandes (el hook de teclado y la ventana de bloqueo), antes de invertir tiempo en pulido visual o contenido creativo que dependen de que esa base funcione.

Cada fase asume que la anterior está terminada y probada manualmente. No hace falta cerrar el checklist entero de una fase para empezar a tocar la siguiente si algo bloquea el avance, pero sí conviene no llegar a la fase 6 (bloqueo real) sin tener la máquina de estados sólida.

---

## Fase 0 — Prueba de concepto de riesgo técnico

Objetivo: confirmar cuanto antes que las dos piezas más inciertas del proyecto son viables, antes de construir nada más encima.

- [x] Proyecto .NET mínimo (WPF) que registre un hook de teclado de bajo nivel (`WH_KEYBOARD_LL` vía P/Invoke) y detecte pulsaciones (por consola).
- [ ] El hook debe ser capaz de **suprimir** pulsaciones (no solo detectarlas).
- [x] Ventana WPF sin decoración, `Topmost = true`, que se mantenga por encima de otras ventanas incluso al hacer clic en otras apps.
- [ ] Probar que esa ventana **no se puede cerrar** con Alt+F4 ni desde la barra de tareas mientras el hook está activo.
- [ ] Documentar en `docs/` cualquier limitación encontrada (antivirus que marque el hook como sospechoso, necesidad de ejecutar como administrador, comportamiento en múltiples monitores, etc.).

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

- [ ] Modelar los estados como enum o similar en `Models/`: `Dormido`, `Corriendo`, `Pausado`, `Avisando`, `Bloqueado1`, `Bloqueado2`, `Durmiendo`, `Desactivado`.
- [ ] Implementar las transiciones exactas que definimos:
  - Dormido → Corriendo (click)
  - Corriendo → Pausado / Avisando (al llegar a 0) / Desactivado
  - Avisando → Pausado / Bloqueado1 (al agotar el minuto)
  - Bloqueado1 → Corriendo con 5:00 (prórroga, marcando `extensionUsada = true`) / Durmiendo
  - Bloqueado2 (solo alcanzable tras agotar la prórroga) → Durmiendo
  - Durmiendo → Corriendo automático tras 40s (reseteando `extensionUsada`)
  - Cualquier estado → Desactivado (excepto durante Bloqueado1/Bloqueado2, donde no se permite)
- [ ] Reglas de guarda: no permitir pausar durante Bloqueado1/Bloqueado2; no mostrar prórroga en Bloqueado2.
- [ ] Cronómetro/temporizador desacoplado de la UI (un servicio en `Services/` que emita eventos o exponga el tiempo restante).
- [ ] Tests manuales o unitarios de la máquina de estados **sin ventanas ni gráficos** — por ejemplo, simulando el paso del tiempo y comprobando que las transiciones ocurren donde deben. Esto es mucho más fácil de depurar ahora que una vez esté todo mezclado con la UI.

---

## Fase 3 — Overlay del personaje (esquina inferior derecha)

- [ ] Ventana WPF pequeña, sin bordes, `Topmost`, posicionada en la esquina inferior derecha, que simplemente muestra una imagen estática.
- [ ] Conectar la ventana a la máquina de estados de la Fase 2: que cambie de imagen según el estado (aunque sean placeholders/rectángulos de colores por ahora, no hace falta arte final todavía).
- [ ] Contador visible del tiempo restante (aunque sea texto simple) para poder depurar visualmente que el temporizador avanza bien.
- [ ] Menú de interacción al hacer click durante `Corriendo`/`Pausado`: Pausar / Reiniciar / Desactivar.

---

## Fase 4 — Pantalla de bloqueo

- [ ] Ventana de bloqueo a pantalla completa, `Topmost`, sin controles de cierre.
- [ ] Activar el hook de teclado (de la Fase 0) solo mientras esta ventana está activa.
- [ ] Botones "¡5 minutos más!" y "Descansar" — visibles solo en Bloqueado1; solo "Descansar" en Bloqueado2.
- [ ] Verificar que el ratón sigue funcionando con normalidad para poder pulsar los botones.
- [ ] Confirmar que al cerrar el bloqueo (por cualquiera de las dos vías) el hook de teclado se libera correctamente — un fallo aquí dejaría al usuario sin poder escribir nada, así que merece pruebas exhaustivas.

---

## Fase 5 — Sistema de audio / líneas de voz

- [ ] Decidir entre `System.Speech.Synthesis` (TTS en tiempo real) o reproducir clips de audio pregrabados — impacta directamente en qué se necesita en `assets/voces/`.
- [ ] Servicio de audio en `Services/` que reciba "reproduce la línea de tipo X" y sepa qué archivo/frase usar según el estado.
- [ ] Enganchar los disparadores: los 2 avisos durante `Avisando`, entrada a `Bloqueado1`, entrada a `Bloqueado2`, y el mensaje al despertar tras `Durmiendo`.
- [ ] Placeholder de líneas de texto (aunque no esté grabado el audio final) para poder probar el timing sin depender del arte/voz definitivos.

---

## Fase 6 — Integración completa y pruebas de uso real

- [ ] Ciclo completo de principio a fin: Dormido → Corriendo → Avisando → Bloqueado1 → prórroga → Bloqueado2 → Descansar → Durmiendo → vuelta a Corriendo.
- [ ] Probar también el camino corto: Bloqueado1 → Descansar directamente (sin prórroga).
- [ ] Probar pausar en distintos puntos (Corriendo, Avisando) y confirmar que no se puede en los bloqueos.
- [ ] Usarlo tú mismo un día completo de trabajo real — es la prueba de fuego para detectar timings molestos, bugs de estado, o el hook de teclado fallando en combinación con tu editor/IDE habitual.

---

## Fase 7 — Assets finales

- [ ] Ilustraciones definitivas del personaje para cada carpeta ya creada en `assets/personaje/` (dormido, corriendo, pausado, avisando, bloqueado_1, bloqueado_2, durmiendo).
- [ ] Grabación/generación final de las líneas de voz en `assets/voces/`, organizadas por las carpetas ya preparadas (generico, avisos, bloqueado_1, bloqueado_2, despertar).
- [ ] Sustituir los placeholders de la Fase 3/5 por el contenido real.

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
