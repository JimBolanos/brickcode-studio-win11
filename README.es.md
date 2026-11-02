# BrickCode Studio Win11

Un editor de NQC para el LEGO RCX y el Scout, para **Windows 10 y 11**. Escribe un
programa, pulsa **F5**, y se compila, se descarga y se ejecuta en el ladrillo. Cada
botón de la ventana es una llamada a `nqc.exe`; no hay nada más en medio.

Esta es la versión para Windows de [BrickCode Studio](https://github.com/JimBolanos/brickcode-studio-osx-linux),
con la misma interfaz y el mismo editor visual de bloques, y solo NQC — sin NXC ni
NXT. Trae todo lo que necesita: el compilador, los firmwares del RCX, las librerías
USB y el driver de la torre.

> Continúa a [rcx-tower-gui](https://github.com/JimBolanos/rcx-tower-gui), la versión
> anterior de la misma idea.

![BrickCode Studio en Windows](docs/screenshot-nqc-en.png)

¿Prefieres arrastrar bloques a escribir? **Archivo → Editor de bloques…** lo abre en
tu navegador. Escribe el NQC por ti, y en esta edición viene fijado a NQC: el paquete
trae `nqc` y no `nbc`, así que ofrecer NXC aquí solo serviría para armar programas que
esta app no puede compilar.

![El editor visual de bloques, escribiendo NQC](docs/screenshot-blocks-en.png)

---

## Hace falta la torre infrarroja de LEGO

El RCX y el Scout **no tienen cable de datos**. El ladrillo no trae puerto USB ni
nunca lo tuvo. Todo — tu programa, el firmware, los comandos del control remoto —
viaja como **luz infrarroja**, igual que el control de un televisor.

Así que la torre no es un accesorio, es la única entrada:

```
   tu PC  ──USB──▶  torre IR de LEGO  ))) infrarrojo (((  ▶  RCX / Scout
```

Sin ella, este editor escribe y compila NQC perfectamente, pero nada llega nunca al
ladrillo.

### Las dos torres

| | Torre USB | Torre serial |
|---|---|---|
| Pieza LEGO | `x431c01` | `9713` |
| Venía en los sets | 3804, 9786, 9794 | 9719, 9785, 9790, 9793 |
| Conector | USB | serial DE-9 |
| Alimentación | del propio USB | **necesita una pila de 9 V** |
| ID USB | `0694:0001` | — |
| En Windows 10/11 | necesita el driver libusbK (Zadig, va incluido) | funciona como siempre |
| En esta app | `nqc` compila, `rcx_tool.py` envía | `nqc` hace todo |

La USB es la común en el mercado de segunda mano y es para la que está preparado este
paquete. Si tienes la serial, sirve un adaptador USB-a-serial; escribe su puerto COM en
**Puerto de la torre** dentro de Ajustes y la app usará `nqc` para todo.

### Para que el ladrillo responda

- Enciende el RCX — la pantalla tiene que mostrar algo.
- Apunta la **ventanita infrarroja** del ladrillo a la de la torre, a unos 10–25 cm.
- Que queden de frente. El infrarrojo es direccional y rebota en las paredes.
- Evita el sol directo o un fluorescente justo encima; los dos tapan la señal.
- Pilas nuevas en el ladrillo. Un RCX cansado contesta el ping y luego falla a la
  mitad de subir el firmware.

Pulsa **Probar ladrillo** en la app para comprobar todo esto antes de culpar al
software.

---

## Cómo usarlo

Llévate la carpeta [`package/BrickCodeStudio`](package/BrickCodeStudio) completa — ahí
está todo: el editor, `nqc.exe`, los firmwares del RCX, pyusb, libusb y el instalador
del driver. Lo único que pones tú es **Python 3 desde la Microsoft Store**.

1. Conecta la torre USB infrarroja.
2. Instala el driver de la torre: `driver\zadig-2.9.exe`, siguiendo
   [`driver/LEEME-driver.txt`](package/BrickCodeStudio/driver/LEEME-driver.txt).
3. Abre `BrickCodeStudio.exe`.
4. Pulsa **Probar herramientas** — debe encontrar nqc.exe, python, rcx_tool.py,
   libusb-1.0.dll y pyusb.
5. Pulsa **Probar ladrillo** con el RCX encendido y apuntando a la torre.

No instala nada, y no escribe fuera de
`%APPDATA%\BrickCodeStudio\settings.txt`, donde recuerda el tamaño de la ventana,
el idioma, el tema, el modelo y el puerto de la torre.

Si solo quieres el editor, `dist/BrickCodeStudio.exe` es el .exe pelado; pon
`nqc.exe` a su lado.

## Sobre la torre USB en Windows 10 y 11

`nqc` abre la torre como `\\.\LEGOTOWER1` — el driver propio de LEGO, que nunca se
firmó para Windows 10 ni 11 de 64 bits. Así que **nqc por sí solo no puede descargar
al ladrillo en un Windows moderno**, hagas lo que hagas.

La salida, la que encontró [beckmx](https://github.com/beckmx/lego-tower-rcx), es
asignarle a la torre el driver **libusbK** con Zadig y hablarle desde Python. Por eso
en esta versión:

- **Editor visual de bloques** (**Archivo → Editor de bloques…**): arrastra bloques
  en lugar de escribir. Es un solo archivo HTML, sin internet, que vive junto al exe,
  se abre en tu navegador y viene fijado a NQC, porque esta edición trae `nqc` y no
  `nbc`. Guardas ahí, abres el `.nqc` aquí. Los programas llevan sus bloques dentro,
  así que puedes volver a abrirlos y seguir armando.

- **nqc.exe compila** — esa parte está intacta y funciona perfecto.
- **`rcx_tool.py` lleva el programa al ladrillo** a través de libusbK.

Los dos los maneja la app; tú nunca escribes ninguno de los dos comandos. pyusb va
incluido, así que no hay que hacer `pip install`.

**Con torre serial nada de esto aplica.** Escribe el puerto (`COM1`, por ejemplo) en
**Puerto de la torre** dentro de Ajustes y la app usa `nqc` para todo, directamente.

## La ventana

**Barra superior** — la ficha del archivo (haz clic para Nuevo / Abrir / Guardar /
Guardar como / Acerca de), `+` para un programa nuevo, el modo `NQC · RCX / Scout`,
el idioma, el estado del ladrillo, y **Cancelar**, **Descargar** y **Ejecutar (F5)**.

**Barra lateral** — la torre RCX con su punto de estado, y las herramientas: Control
remoto, Registro de datos, Firmware, Probar ladrillo, Probar herramientas. **Ajustes**,
abajo, abre el panel de la derecha.

**Editor** — resaltado de NQC y números de línea. Cuando `nqc` reporta un error, la
línea queda marcada y el editor se desplaza hasta ella.

**Consola** — todo lo que imprime `nqc`, debajo del comando que lo produjo.

**Panel de ajustes** — modelo (`-T`), ranura de programa (`-pgm`), tema del editor,
las utilidades del ladrillo, el puerto de la torre, el apagado automático, y una caja
de argumentos libres para `nqc` con historial con las flechas arriba/abajo.

| Claro | Oscuro |
|---|---|
| ![Ajustes, claro](docs/screenshot-settings-en.png) | ![Ajustes, oscuro](docs/screenshot-settings-dark-en.png) |

## Qué ejecuta cada botón

| Botón | Torre USB (libusbK) | Torre serial (puerto COM) |
|---|---|---|
| Ejecutar (F5) | `nqc -T<m> -O prog.rcx prog.nqc` → `rcx_tool download prog.rcx <ranura>` → `rcx_tool run <ranura>` | `nqc -T<m> -S<puerto> -pgm <ranura> -d prog.nqc -run` |
| Descargar | lo mismo, sin el último paso | `nqc -T<m> -S<puerto> -pgm <ranura> -d prog.nqc` |
| Ejecutar (utilidades) | `rcx_tool run <ranura>` | `nqc -T<m> -pgm <ranura> -run` |
| Borrar ladrillo | `rcx_tool clear <ranura>` | `nqc -T<m> -pgm <ranura> -clear` |
| Registro de datos | `rcx_tool datalog` | `nqc -T<m> -datalog` |
| IR cerca / lejos | `rcx_tool near` / `far` | `nqc -T<m> -near` / `-far` |
| Enviar firmware | `rcx_tool firmware <archivo.lgo>` | `nqc -T<m> -firmware <archivo.lgo>` |
| Apagado automático | `rcx_tool sleep <minutos>` | `nqc -T<m> -sleep <minutos>` |
| Control remoto | `rcx_tool remote <máscara>` | `nqc -T<m> -raw1 D2<máscara>` |
| Probar ladrillo | `rcx_tool ping` | `nqc -T<m> -near` |
| Detectar | `rcx_tool tower` | lee `RCX_PORT` |

La caja libre de `nqc`, abajo en el panel de ajustes, siempre va directo a `nqc`, con
historial con las flechas arriba/abajo.

### El rcx_tool.py de este paquete está parcheado

Es el de [beckmx](https://github.com/beckmx/lego-tower-rcx), con dos cambios:

- **El opcode para elegir la ranura estaba mal.** Enviaba `0x51`, que es *PlaySound*;
  el opcode de Select Program es `0x91`. Por eso descargar a una ranura distinta de la
  1 nunca llegaba a donde debía. Corregido.
- Se añadieron los comandos que necesita la interfaz: `run`, `stop`, `clear`, `near`,
  `far`, `sleep`, `battery`, `datalog` y `remote`. Todos los opcodes salen del código
  de nqc (`rcxlib/RCX_Constants.h`).

[`package/patch_rcx_tool.py`](package/patch_rcx_tool.py) aplica esos cambios sobre una
copia limpia, así se ve exactamente qué se tocó.

## Idiomas

Inglés y español, desde la barra superior — toda la interfaz, los mensajes de la
consola y los diálogos.

![Español, oscuro](docs/screenshot-settings-dark-es.png)

## Compilar desde el código

```
build.bat
```

Usa el compilador de C# que viene con el .NET Framework, así que no hay nada que
instalar. El código son cinco archivos en `src/`:

| Archivo | Qué contiene |
|---|---|
| `Core.cs` | constantes, traducciones, paleta, ayudas de dibujo, el icono |
| `Controls.cs` | botones, campos, desplegables, tarjetas, el editor, la consola |
| `Chrome.cs` | barra superior, ficha del archivo, barra lateral, barra de estado |
| `Panels.cs` | panel de ajustes, ventana del control remoto, Acerca de |
| `Main.cs` | distribución, acciones y el lanzador de nqc |

El icono se dibuja por código con GDI+, portado del original en cairo, así que es el
mismo ladrillo en todos los tamaños.

`build.bat` también refresca `package\BrickCodeStudio\BrickCodeStudio.exe`, la copia
que va dentro de la carpeta lista para usar. La 2.1.2 salió con esa copia una
compilación por detrás de su propio código; ahora ya no se puede desfasar.

Para armar el zip que va en un release:

```
package.bat
```

Comprime `package\BrickCodeStudio` completa, y se niega a hacerlo si algún archivo de
`src\` es más nuevo que el exe empaquetado.

## Acerca de

![Acerca de](docs/screenshot-about-en.png)

---

Licencia MIT · © 2026 Jim Bolaños · [github.com/JimBolanos](https://github.com/JimBolanos)

Proyecto independiente — sin afiliación ni respaldo del Grupo LEGO.
LEGO y MINDSTORMS son marcas registradas del Grupo LEGO.
