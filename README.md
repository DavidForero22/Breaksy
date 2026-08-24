# Breaksy

Aplicación de escritorio para Windows que fuerza descansos visuales periódicos mientras se programa, siguiendo la regla 20-20-20 (cada 20 minutos, mirar algo a 20 pies/6 metros de distancia durante 20 segundos).

## El problema

Es fácil perder la noción del tiempo mientras se está programando y encadenar varias horas seguidas frente al editor sin descansar la vista. Los temporizadores manuales (móvil, alarmas sueltas) fallan porque dependen de la disciplina del usuario para volver a activarlos tras cada descanso, y ese hábito se abandona con facilidad.

## La idea

Un personaje (mascota) que vive anclado en la esquina inferior derecha de la pantalla, siempre visible por encima de cualquier otra ventana. Mientras el temporizador corre, el usuario puede ver su estado de un vistazo. Cuando se cumplen los 20 minutos, el personaje empieza a mostrar señales de impaciencia y, si se ignora, termina bloqueando la pantalla por completo —incluyendo el teclado— hasta que el usuario decide conscientemente descansar o pedir una prórroga.

Al elegir descansar, el personaje duerme y bloquea el teclado durante 40 segundos. Al finalizar, pasa a un estado de espera activa aguardando un clic del usuario para reanudar el ciclo de trabajo.

## Objetivo del proyecto

Proyecto personal para uso propio, centrado en resolver un problema real de hábitos de trabajo. Sirve también como excusa para aprender C#/.NET y las APIs de Windows relacionadas con hooks de teclado, ventanas superpuestas y síntesis de voz.

## Diseño funcional

### Estados del personaje

| Estado | Descripción |
|---|---|
| **Disabled** | Personaje completamente inactivo. Estado de reposo tras desactivar manualmente. |
| **Idle** | Personaje en reposo inicial, esperando el primer clic del usuario para arrancar el ciclo de trabajo. |
| **Awaken** | Temporizador activo, cuenta atrás en progreso (20:00 en ciclo normal o 5:00 en prórroga). |
| **Paused** | Cuenta congelada, contexto conservado. Solo se puede pausar durante *Awaken*, *Warning* o *SeriousWarning*. |
| **Warning** | Fase de 1 minuto (20:00–21:00) tras agotarse el tiempo inicial. Muestra avisos y líneas de queja leves. Se puede pausar. |
| **SeriousWarning** | Fase de 1 minuto tras agotarse la prórroga de 5 minutos. Avisos e impaciencia más severos antes del bloqueo definitivo. Se puede pausar. |
| **Blocked1** | Primer bloqueo: pantalla y teclado inhabilitados. Dos opciones disponibles: **"¡5 minutos más!"** (prórroga única) o **"Descansar"**. No se puede pausar ni cerrar. |
| **Blocked2** | Segundo bloqueo: alcanzable tras agotar la prórroga y el minuto de *SeriousWarning*. Solo queda la opción **"Descansar"**. No se puede pausar ni cerrar. |
| **Sleeping** | Fase de descanso activo durante exactamente 40 segundos tras pulsar "Descansar". El teclado continúa bloqueado. |
| **Waiting** | Fin del descanso. El personaje despierta y espera un clic del usuario para iniciar un nuevo ciclo (vuelta a *Awaken* con 20:00 y prórroga reseteada).|

### Reglas clave

- La prórroga de "5 minutos más" solo puede usarse **una vez** por ciclo completo.
- Al agotarse la prórroga de 5 minutos, se pasa a **SeriousWarning** (1 min) y posteriormente a **Blocked2** sin opción a más tiempo extra.
- El descanso en **Sleeping** es de **40 segundos** fijos con teclado suprimido.
- Al terminar los 40 segundos, la app pasa a **Waiting** y no reanuda el cronómetro automáticamente; requiere un clic del usuario para no consumir tiempo si el puesto está vacío.
- Durante **Blocked1**, **Blocked2** y **Sleeping** no se puede pausar, reiniciar ni desactivar la aplicación; la interacción queda restringida a los botones de la interfaz mediante ratón.

## Estructura del proyecto

```
Breaksy/
├── README.md
├── docs/                      # Documentación adicional
├── assets/
│   ├── personaje/              # Ilustraciones/sprites del personaje, organizadas por estado
│   │   ├── dormido/
│   │   ├── corriendo/
│   │   ├── pausado/
│   │   ├── avisando/
│   │   ├── bloqueado_1/
│   │   ├── bloqueado_2/
│   │   └── durmiendo/
│   └── voces/                  # Líneas de voz (audio), organizadas por contexto
│       ├── generico/            # Líneas neutras / de arranque
│       ├── avisos/               # Líneas durante la fase de Avisando
│       ├── bloqueado_1/          # Líneas del primer bloqueo
│       ├── bloqueado_2/          # Líneas del segundo bloqueo (más exigentes)
│       └── despertar/            # Línea al terminar el descanso de 40s
└── src/
    └── Breaksy/                 # Código fuente de la aplicación (C# / .NET / WPF)
        ├── Models/               # Modelos de datos y estado
        ├── ViewModels/           # Lógica de presentación
        ├── Views/                # Ventanas y XAML
        ├── Services/             # Temporizador, audio/voz, gestión de estado
        └── Native/               # Interop con la API de Windows (hook de teclado, always-on-top)
```

## Stack técnico (resumen)

- **Lenguaje/Framework**: C# sobre .NET 8 (LTS), interfaz con WPF.
- **Motivo de la elección**: el requisito de bloquear el teclado a nivel de sistema requiere un hook de bajo nivel de Windows (`WH_KEYBOARD_LL`), al que C# accede de forma nativa vía P/Invoke. WPF permite ventanas *always-on-top* sin decoración de forma sencilla, y `System.Speech.Synthesis` cubre la síntesis de voz sin dependencias externas.
- **Alcance de plataforma**: Windows únicamente (de momento).

## Estado del proyecto

Fase 6 en progreso: realizar pruebas y verificar funcionamiento en un ciclo normal.
