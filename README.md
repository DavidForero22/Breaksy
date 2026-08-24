# Breaksy

Aplicación de escritorio para Windows que fuerza descansos visuales periódicos mientras se programa, siguiendo la regla 20-20-20 (cada 20 minutos, mirar algo a 20 pies/6 metros de distancia durante 20 segundos).

## El problema

Es fácil perder la noción del tiempo mientras se está programando y encadenar varias horas seguidas frente al editor sin descansar la vista. Los temporizadores manuales (móvil, alarmas sueltas) fallan porque dependen de la disciplina del usuario para volver a activarlos tras cada descanso, y ese hábito se abandona con facilidad.

## La idea

Un personaje (mascota) que vive anclado en la esquina inferior derecha de la pantalla, siempre visible por encima de cualquier otra ventana. Mientras el temporizador corre, el usuario puede ver su estado de un vistazo. Cuando se cumplen los 20 minutos, el personaje empieza a mostrar señales de impaciencia y, si se ignora, termina bloqueando la pantalla por completo —incluyendo el teclado— hasta que el usuario decide conscientemente descansar o pedir una prórroga.

La clave del diseño es que **la única vía de salida del bloqueo es una decisión activa del usuario**, y que el propio personaje se encarga de reactivarse solo tras el descanso, sin depender de que el usuario se acuerde de rearmarlo.

## Objetivo del proyecto

Proyecto personal para uso propio, centrado en resolver un problema real de hábitos de trabajo. Sirve también como excusa para aprender C#/.NET y las APIs de Windows relacionadas con hooks de teclado, ventanas superpuestas y síntesis de voz.

## Diseño funcional

### Estados del personaje

| Estado | Descripción |
|---|---|
| **Dormido (inicial)** | Personaje inactivo. Estado de reposo antes de arrancar o tras desactivar manualmente. |
| **Corriendo** | Temporizador activo, cuenta atrás desde 20:00. |
| **Pausado** | Cuenta congelada, contexto conservado. Se muestra un icono de pausa en la cara del personaje. Solo se puede pausar durante *Corriendo* o *Avisando*, nunca durante un bloqueo. |
| **Avisando** | Fase de 1 minuto (20:00–21:00) tras agotarse el tiempo. El personaje muestra 2 avisos con líneas de voz, con enfado creciente. Se puede pausar (para emergencias). |
| **Bloqueado (1ª vez)** | Pantalla bloqueada y teclado inhabilitado. Dos opciones: **"¡5 minutos más!"** (concede una única prórroga) o **"Descansar"**. No se puede pausar ni cerrar. |
| **Bloqueado (2ª vez)** | Se llega aquí solo si ya se usó la prórroga y el tiempo extendido (5 min) también se agota. Líneas de voz más exigentes. Solo queda la opción **"Descansar"**, sin prórroga disponible. |
| **Durmiendo (post-descanso)** | Tras elegir "Descansar", el personaje duerme exactamente 40 segundos. Al despertar, suelta una línea de voz y reinicia el ciclo completo automáticamente (vuelta a *Corriendo* con 20:00 y la prórroga reseteada). |
| **Desactivado** | El usuario apaga el personaje manualmente (p. ej. cuando deja de programar). Vuelve a *Dormido*, temporizador a 0, sin progreso guardado. |

### Reglas clave

- La prórroga de "5 minutos más" solo puede usarse **una vez** por ciclo completo.
- Al agotarse los 5 minutos extra, se pasa directamente a **Bloqueado (2ª vez)** sin repetir la fase de *Avisando*.
- El descanso tras "Descansar" es de **40 segundos** fijos y se reactiva solo, sin intervención del usuario.
- Durante un bloqueo (1ª o 2ª vez) no se puede pausar, reiniciar ni cerrar la aplicación de forma normal; solo se puede interactuar mediante los botones del propio diálogo de bloqueo, usando el ratón.

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

Fase 0 en progreso: prueba de concepto del hook de teclado (detección, sin supresión todavía) y ventana WPF sin decoración.
