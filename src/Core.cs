// ---------------------------------------------------------------------------
// BrickCode Studio - Windows edition (NQC only)
//
// An NQC editor for the LEGO RCX and Scout. Write a program, press F5, and it
// is compiled, downloaded and run on the brick. Everything on screen is an
// invocation of nqc.exe.
//
// (c) 2026 Jim Bolanos - MIT License
// Independent project, not affiliated with or endorsed by the LEGO Group.
//
// Build:  mcs -target:winexe -out:BrickCodeStudio.exe RCXTowerGUI.cs \
//             -r:System.Windows.Forms -r:System.Drawing -r:System
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BrickCode
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------
    static class App
    {
        public const string Name = "BrickCode Studio";
        public const string Version = "2.2.0";
        public const string Author = "Jim Bolanos";
        public const string Github = "https://github.com/JimBolanos";
        public const string GithubShort = "github.com/JimBolanos";
        public const string Rebrickable = "https://rebrickable.com/users/jimmy.bolanos/";
        public const string RebrickableShort = "rebrickable.com/users/jimmy.bolanos";

        public const int DefaultTimeoutMs = 90000;
        public const int FirmwareTimeoutMs = 420000;

        public static readonly string[] Targets =
            { "RCX2", "RCX", "CM", "Scout", "Spy", "Swan", "NXT" };

        public static readonly string[] TargetNotes =
        {
            "RCX 2.0 — firmware 0328 or newer (nqc default)",
            "RCX 1.0 — older firmware",
            "CyberMaster",
            "Scout (Droid Developer Kit)",
            "Spybotics",
            "Swan",
            "NXT — USB cable; nbc compiles, nxt_tool.py sends"
        };

        public const string SampleNxt =
            "// A first NXT program. Press F5, or the Compile & Run button.\r\n" +
            "\r\n" +
            "task main()\r\n" +
            "{\r\n" +
            "    ClearScreen();\r\n" +
            "    TextOut(10, LCD_LINE4, \"Hello NXT!\");\r\n" +
            "    PlayTone(880, 300);\r\n" +
            "    Wait(300);\r\n" +
            "\r\n" +
            "    OnFwd(OUT_BC, 50);      // forward\r\n" +
            "    Wait(1000);             // 1000 ms = 1 second\r\n" +
            "    Off(OUT_BC);\r\n" +
            "    Wait(1500);\r\n" +
            "}\r\n";

        public const string Sample =
            "// A first program. Press F5, or the Compile & Run button.\r\n" +
            "\r\n" +
            "task main()\r\n" +
            "{\r\n" +
            "    SetPower(OUT_A + OUT_C, 7);\r\n" +
            "\r\n" +
            "    OnFwd(OUT_A + OUT_C);   // forward\r\n" +
            "    Wait(200);              // 200 ticks = 2 seconds\r\n" +
            "\r\n" +
            "    OnRev(OUT_A + OUT_C);   // and back\r\n" +
            "    Wait(200);\r\n" +
            "\r\n" +
            "    Off(OUT_A + OUT_C);\r\n" +
            "    PlaySound(SOUND_UP);\r\n" +
            "}\r\n";
    }

    // -----------------------------------------------------------------------
    // Translations. Keys are the English strings, exactly as BrickCode Studio
    // does it, so the Spanish table can be lifted straight across.
    // -----------------------------------------------------------------------
    static class I18n
    {
        public static bool Spanish = false;

        static readonly Dictionary<string, string> Es =
            new Dictionary<string, string>(StringComparer.Ordinal);

        static void A(string en, string es) { Es[en] = es; }

        static I18n()
        {
            A("untitled", "sin título");
            A("New", "Nuevo");
            A("Open…", "Abrir…");
            A("Save", "Guardar");
            A("Save as…", "Guardar como…");
            A("Block editor…", "Editor de bloques…");
            A("New program", "Nuevo programa");
            A("Language", "Idioma");
            A("No brick connected", "Ningún ladrillo conectado");
            A("RCX tower connected", "Torre RCX conectada");
            A("Cancel", "Cancelar");
            A("Download", "Descargar");
            A("Run  (F5)", "Ejecutar  (F5)");
            A("Clear", "Limpiar");
            A("Console", "Consola");
            A("Settings", "Ajustes");
            A("Settings and brick utilities", "Ajustes y utilidades del ladrillo");
            A("Show / hide the console", "Mostrar / ocultar la consola");
            A("TARGET", "MODELO");
            A("PROGRAM SLOT", "RANURA DE PROGRAMA");
            A("EDITOR THEME", "TEMA DEL EDITOR");
            A("Dark", "Oscuro");
            A("Light", "Claro");
            A("Brick utilities (RCX / Scout)", "Utilidades del ladrillo (RCX / Scout)");
            A("Run", "Ejecutar");
            A("Clear brick", "Borrar ladrillo");
            A("Datalog", "Registro de datos");
            A("IR near", "IR cerca");
            A("IR far", "IR lejos");
            A("Send firmware", "Enviar firmware");
            A("firmware file (.lgo)", "archivo de firmware (.lgo)");
            A("Tower port", "Puerto de la torre");
            A("empty = let nqc decide  ·  usb for the USB tower, or a COM port",
              "vacío = que nqc decida  ·  usb para la torre USB, o un puerto COM");
            A("Test tools", "Probar herramientas");
            A("Remote", "Control remoto");
            A("Message", "Mensaje");
            A("Program", "Programa");
            A("Stop", "Detener");
            A("Sound", "Sonido");
            A("Remote command sent.", "Comando remoto enviado.");
            A("Sending remote command…", "Enviando comando remoto…");
            A("Test brick", "Probar ladrillo");
            A("The brick answered.", "El ladrillo respondió.");
            A("Asking the brick…", "Consultando al ladrillo…");
            A("Detect", "Detectar");
            A("Auto-shutoff", "Apagado automático");
            A("Apply", "Aplicar");
            A("min", "min");
            A("Ready.", "Listo.");
            A("NQC for the RCX and Scout  ·  compiled and sent by nqc",
              "NQC para RCX y Scout  ·  compilado y enviado por nqc");
            A("RCX 2.0 — firmware 0328 or newer (nqc default)",
              "RCX 2.0 — firmware 0328 o más nuevo (por defecto de nqc)");
            A("RCX 1.0 — older firmware", "RCX 1.0 — firmware antiguo");
            A("CyberMaster", "CyberMaster");
            A("Scout (Droid Developer Kit)", "Scout (Droid Developer Kit)");
            A("Spybotics", "Spybotics");
            A("Swan", "Swan");
            A("any nqc arguments, e.g. -firmware firm0328.lgo  ·  -sleep 5  ·  up/down for history",
              "cualquier argumento de nqc, ej. -firmware firm0328.lgo  ·  -sleep 5  ·  arriba/abajo historial");
            A("NQC programs", "Programas NQC");
            A("All files", "Todos los archivos");
            A("Open NQC file", "Abrir archivo NQC");
            A("Save program", "Guardar programa");
            A("Select firmware file", "Seleccionar archivo de firmware");
            A("RCX firmware (*.lgo, *.srec)", "Firmware del RCX (*.lgo, *.srec)");
            A("nqc default", "valor por defecto de nqc");
            A("Tower port: {0} ({1})", "Puerto de la torre: {0} ({1})");
            A("No tower port found automatically.", "No se encontró ningún puerto de torre automáticamente.");
            A("usb tower", "torre usb");
            A("from RCX_PORT in this environment", "desde RCX_PORT en este entorno");
            A("Finished.", "Terminado.");
            A("Finished with errors.", "Terminó con errores.");
            A("A command is already running. Wait for it or press Cancel.",
              "Ya hay un comando en ejecución. Espera o pulsa Cancelar.");
            A("'{0}' was not found. Put it next to this program or in your PATH - every action in this window runs it.",
              "No se encontró '{0}'. Pónlo junto a este programa o en tu PATH - cada acción de esta ventana lo usa.");
            A("Tool not found.", "Herramienta no encontrada.");
            A("Could not start {0}: {1}", "No se pudo iniciar {0}: {1}");
            A("Failed to start.", "No se pudo iniciar.");
            A("No answer in time. Stopping the command.", "Sin respuesta a tiempo. Deteniendo el comando.");
            A("Stopped.", "Detenido.");
            A("Save the program first - the compiler works on a file, not on the editor buffer.",
              "Guarda el programa primero - el compilador trabaja con un archivo, no con el editor.");
            A("Running on the brick.", "Ejecutándose en el ladrillo.");
            A("Downloaded to the brick.", "Descargado al ladrillo.");
            A("Compiling and running…", "Compilando y ejecutando…");
            A("Compiling and downloading…", "Compilando y descargando…");
            A("Command finished.", "Comando terminado.");
            A("Running command…", "Ejecutando comando…");
            A("Auto-shutoff set.", "Apagado automático configurado.");
            A("Select a firmware file (.lgo) first.", "Selecciona primero un archivo de firmware (.lgo).");
            A("Flashing firmware takes a while. Keep the RCX pointed at the tower and do not move it.",
              "Instalar el firmware tarda un rato. Mantén el RCX apuntando a la torre y no lo muevas.");
            A("Firmware sent.", "Firmware enviado.");
            A("Sending firmware…", "Enviando firmware…");
            A("About", "Acerca de");
            A("NXC for the NXT  ·  compiled by nbc, sent over USB", "NXC para el NXT  ·  compilado por nbc, enviado por USB");
            A("RCX / Scout  —  NQC blocks", "RCX / Scout  —  bloques NQC");
            A("NXT  —  NXC blocks", "NXT  —  bloques NXC");
            A("EV3  —  Pybricks (Python) blocks", "EV3  —  bloques Pybricks (Python)");
            A("HOME", "INICIO");
            A("CODE", "CÓDIGO");
            A("BLOCKS", "BLOQUES");
            A("ABOUT", "ACERCA DE");
            A("Home", "Inicio");
            A("Pick the brick you are programming", "Elige el ladrillo que vas a programar");
            A("Pick the brick you are building for", "Elige el ladrillo para el que construyes");
            A("Block editor", "Editor de bloques");
            A("USB tower  ·  3 sensors, 3 motors", "Torre USB  ·  3 sensores, 3 motores");
            A("Built-in light sensor  ·  stand-alone", "Sensor de luz integrado  ·  autónomo");
            A("USB or Bluetooth  ·  encoders", "USB o Bluetooth  ·  encoders");
            A("Blocks only  ·  paste the Python into Pybricks", "Solo bloques  ·  pega el Python en Pybricks");
            A("Writes NQC  ·  works offline", "Escribe NQC  ·  funciona sin internet");
            A("Writes NXC  ·  works offline", "Escribe NXC  ·  funciona sin internet");
            A("Writes Python  ·  paste it into Pybricks", "Escribe Python  ·  pégalo en Pybricks");
            A("VERSION", "VERSIÓN");
            A("NQC + NXC editor", "Editor NQC + NXC");
            A("Write NQC for the RCX and Scout, or NXC for the NXT, press F5, and the program compiles, downloads and runs on the brick.", "Escribe NQC para el RCX y el Scout, o NXC para el NXT, pulsa F5, y el programa se compila, se descarga y se ejecuta en el ladrillo.");
            A("Compilers", "Compiladores");
            A("NXC link", "Enlace NXC");
            A("USB (libusb)", "USB (libusb)");
            A("Platforms", "Plataformas");
            A("What it uses", "Qué usa");
            A("Documentation", "Documentación");
            A("Getting started", "Primeros pasos");
            A("Install", "Instalar");
            A("Firmware", "Firmware");
            A("Tools, one by one", "Herramientas, una por una");
            A("Troubleshooting", "Solución de problemas");
            A("Write a program, press F5, watch it run on the brick.", "Escribe un programa, pulsa F5 y míralo correr en el ladrillo.");
            A("Unzip, install the tower driver, run BrickCodeStudio.exe.", "Descomprime, instala el driver de la torre y ejecuta BrickCodeStudio.exe.");
            A("Sending firm0328.lgo to an RCX, and when you need to.", "Enviar firm0328.lgo a un RCX, y cuándo hace falta.");
            A("Every utility in the side bar, and what it does to the brick.", "Cada utilidad de la barra lateral y qué le hace al ladrillo.");
            A("The tower, the firmware, and what to try when nothing answers.", "La torre, el firmware y qué probar cuando nada responde.");
            A("Drag blocks instead of typing. Programs keep their blocks.", "Arrastra bloques en vez de escribir. Los programas guardan sus bloques.");
            A("Links", "Enlaces");
            A("Repository", "Repositorio");
            A("Issues & ideas", "Problemas e ideas");
            A("NQC reference", "Referencia de NQC");
            A("NXC guide", "Guía de NXC");
            A("Legal", "Legal");
            A("Builds and programs LEGO robots as a hobby — RCX, Scout, NXT and Powered Up hubs. Made BrickCode Studio so these classic bricks keep running on modern computers.", "Construye y programa robots LEGO como hobby: RCX, Scout, NXT y hubs Powered Up. Hizo BrickCode Studio para que estos ladrillos clásicos sigan funcionando en computadoras modernas.");
            A("Not affiliated with or endorsed by the LEGO Group. LEGO, MINDSTORMS, RCX, Scout and NXT are trademarks of the LEGO Group.", "Sin afiliación ni respaldo del Grupo LEGO. LEGO, MINDSTORMS, RCX, Scout y NXT son marcas del Grupo LEGO.");
            A("NXT utilities", "Utilidades NXT");
            A("USB cable: no tower, no port to set.", "Cable USB: sin torre ni puerto que configurar.");
            A("Stop program", "Parar programa");
            A("NXT Explorer", "Explorador NXT");
            A("NXT Screen", "Pantalla NXT");
            A("VLL", "VLL");
            A("VLL (Micro Scout)", "VLL (Micro Scout)");
            A("Not connected", "No conectado");
            A("Refresh", "Actualizar");
            A("Run", "Ejecutar");
            A("Send file…", "Enviar archivo…");
            A("Delete", "Borrar");
            A("Reading the NXT…", "Leyendo el NXT…");
            A("Could not start python. Press Test tools.", "No se pudo iniciar python. Pulsa Probar herramientas.");
            A("bytes", "bytes");
            A("file(s).", "archivo(s).");
            A("Delete {0} from the NXT?", "¿Borrar {0} del NXT?");
            A("NXT programs", "Programas NXT");
            A("Sending…", "Enviando…");
            A("Connect", "Conectar");
            A("Connected", "Conectado");
            A("Connecting…", "Conectando…");
            A("Disconnected. Press Connect.", "Desconectado. Pulsa Conectar.");
            A("Point the Scout's light at the Micro Scout.", "Apunta la luz del Scout al Micro Scout.");
            A("Beep", "Pitido");
            A("Sending VLL code…", "Enviando código VLL…");
            A("Python or the USB tool is missing. Press Test tools.", "Falta python o la herramienta USB. Pulsa Probar herramientas.");
            A("Version {0}", "Versión {0}");
            A("NQC editor  ·  Windows", "Editor NQC  ·  Windows");
            A("Write NQC for the RCX and Scout, press F5, and the program\ncompiles, downloads and runs on the brick.",
              "Escribe NQC para el RCX y Scout, pulsa F5, y el programa\nse compila, se descarga y se ejecuta en el ladrillo.");
            A("Compiler", "Compilador");
            A("NQC targets", "Modelos NQC");
            A("NQC link", "Enlace NQC");
            A("Highlighting", "Resaltado");
            A("Theme", "Tema");
            A("nqc (RCX / Scout)", "nqc (RCX / Scout)");
            A("IR tower (USB / serial)", "Torre IR (USB / serial)");
            A("Custom (regex)", "Propio (regex)");
            A("Light · Dark", "Claro · Oscuro");
            A("MIT License", "Licencia MIT");
            A("Independent project - not affiliated with or endorsed\nby the LEGO Group. LEGO and MINDSTORMS are trademarks\nof the LEGO Group.",
              "Proyecto independiente - sin afiliación ni respaldo\ndel Grupo LEGO. LEGO y MINDSTORMS son marcas\nregistradas del Grupo LEGO.");
            A("Close", "Cerrar");
            A("Bricks", "Ladrillos");
            A("Tools", "Herramientas");
            A("RCX tower", "Torre RCX");
            A("Firmware…", "Firmware…");
            A("Save changes to {0}?", "¿Guardar los cambios de {0}?");
            A("If you don't save, your changes are lost.", "Si no guardas, los cambios se pierden.");
            A("Don't save", "No guardar");
            A("{0}: NOT found in PATH.", "{0}: NO se encontró en el PATH.");
            A("{0}: found at {1}", "{0}: encontrado en {1}");
            A("Checking the tools…", "Revisando las herramientas…");
            A("Python was not found. Install Python 3 from the Microsoft Store, then start this program again - the USB tower needs it.",
              "No se encontró Python. Instálalo desde la Microsoft Store (Python 3) y vuelve a abrir este programa - la torre USB lo necesita.");
            A("rcx_tool.py is missing from this folder. It is what talks to the USB tower.",
              "Falta rcx_tool.py en esta carpeta. Es lo que habla con la torre USB.");
            A("Looking for the USB tower…", "Buscando la torre USB…");
            A("NXT", "NXT");
            A("NXT connected", "NXT conectado");
            A("NXT — USB cable; nbc compiles, nxt_tool.py sends", "NXT — cable USB; nbc compila, nxt_tool.py envía");
            A("Open program", "Abrir programa");
            A("NXC programs", "Programas NXC");
            A("Open NXC file", "Abrir archivo NXC");
            A("Block editor (NXT)…", "Editor de bloques (NXT)…");
            A("Pybricks block editor (EV3)…", "Editor de bloques Pybricks (EV3)…");
            A("EV3 (Pybricks)", "EV3 (Pybricks)");
            A("Compiling and sending…", "Compilando y enviando…");
            A("Compiling and running…", "Compilando y ejecutando…");
            A("Asking the NXT…", "Preguntando al NXT…");
            A("'{0}' was not found. Put it next to this program.", "No se encontró '{0}'. Ponlo junto a este programa.");
            A("nxt_tool.py is missing from this folder. It is what talks to the NXT over USB.",
              "Falta nxt_tool.py en esta carpeta. Es lo que habla con el NXT por USB.");
            A("Python was not found. Install Python 3 from the Microsoft Store, then start this program again - the NXT needs it.",
              "No se encontró Python. Instálalo desde la Microsoft Store (Python 3) y vuelve a abrir este programa - el NXT lo necesita.");
            A("This action is for the RCX. The NXT does not have it.", "Esta acción es del RCX. El NXT no la tiene.");
            A("Block editor not found. It lives beside the exe, in blocks\\{0}.",
              "No se encontró el editor de bloques. Está junto al exe, en blocks\\{0}.");
            A("Block editor opened in your browser. Build the program there, press Save, then open the file here.",
              "El editor de bloques se abrió en tu navegador. Arma el programa allí, pulsa Guardar y abre el archivo aquí.");
            A("The EV3 editor writes Python. Save it there, then copy the .py to the EV3 (Pybricks).",
              "El editor del EV3 escribe Python. Guárdalo allí y copia el .py al EV3 (Pybricks).");
            A("Serial tower on {0}: nqc talks to the brick by itself.",
              "Torre serial en {0}: nqc habla con el ladrillo por su cuenta.");
        }

        public static string T(string en)
        {
            if (!Spanish || en == null) return en;
            string v;
            if (Es.TryGetValue(en, out v)) return v;
            return en;
        }

        public static string T(string en, object a0)
        {
            return string.Format(CultureInfo.InvariantCulture, T(en), a0);
        }

        public static string T(string en, object a0, object a1)
        {
            return string.Format(CultureInfo.InvariantCulture, T(en), a0, a1);
        }
    }

    // -----------------------------------------------------------------------
    // Palette. The two sets come straight from BrickCode Studio.
    // -----------------------------------------------------------------------
    class Palette
    {
        public bool Dark;
        public Color Bg, Card, Darker, Fg, Muted, EditorFg, Border, Field;
        public Color Accent, Green, Yellow, Magenta, Red, Blue;
        public Color EditorBg, GutterBg, GutterFg;
        public Color SynKeyword, SynType, SynPreproc, SynNumber, SynString, SynComment;
        public Color LogOut, LogCmd, LogOk, LogErr, LogDim, LogInfo;
        public Color DotOk, DotBusy, DotError;

        static Color C(string hex)
        {
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(1, 2), 16),
                Convert.ToInt32(hex.Substring(3, 2), 16),
                Convert.ToInt32(hex.Substring(5, 2), 16));
        }

        public static Palette Make(bool dark)
        {
            Palette p = new Palette();
            p.Dark = dark;
            if (dark)
            {
                p.Accent = C("#4C8DFF");
                p.Bg = C("#0E1014");
                p.Card = C("#13161C");
                p.Darker = C("#0B0D11");
                p.Fg = C("#E7EAF0");
                p.Muted = C("#8B93A3");
                p.EditorFg = C("#D8DEE9");
                p.Border = C("#2B313D");
                p.Field = C("#1A1E26");
                p.Green = C("#22B573");
                p.Yellow = C("#F5A623");
                p.Magenta = C("#8C5CF0");
                p.Red = C("#F0609C");
                p.Blue = C("#3B82F6");
                p.EditorBg = C("#13161C");
                p.GutterBg = C("#0F1217");
                p.GutterFg = C("#5A6280");
                p.SynNumber = C("#b5cea8");
                p.SynString = C("#ce9178");
            }
            else
            {
                p.Accent = C("#0A73F0");
                p.Bg = C("#F5F5F7");
                p.Card = C("#FFFFFF");
                p.Darker = C("#F6F6F7");
                p.Fg = C("#1D1D1F");
                p.Muted = C("#6E6E73");
                p.EditorFg = C("#1D1D1F");
                p.Border = C("#D9D9DE");
                p.Field = C("#ECECF0");
                p.Green = C("#28B24B");
                p.Yellow = C("#F5A623");
                p.Magenta = C("#8B5CF6");
                p.Red = C("#F25C9B");
                p.Blue = C("#3B82F6");
                p.EditorBg = C("#FFFFFF");
                p.GutterBg = C("#EEF1F7");
                p.GutterFg = C("#9AA1B5");
                p.SynNumber = C("#2E7D32");
                p.SynString = C("#B25000");
            }
            p.SynKeyword = C("#569cd6");
            p.SynType = C("#4ec9b0");
            p.SynPreproc = C("#c586c0");
            p.SynComment = C("#6a9955");

            p.LogOut = p.EditorFg;
            p.LogCmd = C("#61DAFB");
            p.LogOk = C("#4ADE80");
            p.LogErr = C("#F87171");
            p.LogDim = C("#8A90A6");
            p.LogInfo = C("#FCD34D");

            p.DotOk = C("#22C55E");
            p.DotBusy = C("#F5A623");
            p.DotError = C("#EF4444");
            return p;
        }

        public Color Hover(Color c, double amount)
        {
            double t = Dark ? amount : -amount;
            return Mix(c, t > 0 ? Color.White : Color.Black, Math.Abs(t));
        }

        public static Color Mix(Color a, Color b, double t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Darken(Color c, double factor)
        {
            return Color.FromArgb((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));
        }
    }

    // -----------------------------------------------------------------------
    // Drawing helpers
    // -----------------------------------------------------------------------
    static class Dr
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0) { p.AddRectangle(r); return p; }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void Border(Graphics g, Rectangle r, int radius, Color c)
        {
            Rectangle rr = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (GraphicsPath p = Round(rr, radius))
            using (Pen pen = new Pen(c))
                g.DrawPath(pen, p);
        }

        public static void Card(Graphics g, Rectangle r, int radius, Color fill, Color border)
        {
            Fill(g, r, radius, fill);
            Border(g, r, radius, border);
        }

        public static void Dot(Graphics g, int cx, int cy, int d, Color c)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
        }

        /// Text drawn with GDI+ so it stays crisp on a coloured pill.
        public static void Text(Graphics g, string s, Font f, Color c, Rectangle r,
                                StringAlignment h, StringAlignment v)
        {
            if (string.IsNullOrEmpty(s)) return;
            using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
            using (SolidBrush b = new SolidBrush(c))
            {
                sf.Alignment = h;
                sf.LineAlignment = v;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(s, f, b, r, sf);
            }
        }

        public static void TextLeft(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            Text(g, s, f, c, r, StringAlignment.Near, StringAlignment.Center);
        }

        public static void TextCenter(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            Text(g, s, f, c, r, StringAlignment.Center, StringAlignment.Center);
        }

        public static void TextRight(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            Text(g, s, f, c, r, StringAlignment.Far, StringAlignment.Center);
        }

        public static int MeasureWidth(Graphics g, string s, Font f)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            SizeF sz = g.MeasureString(s, f, 10000,
                StringFormat.GenericTypographic);
            return (int)Math.Ceiling(sz.Width) + 2;
        }

        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }

    // -----------------------------------------------------------------------
    // The application icon, redrawn from the cairo original so the Windows
    // build shows exactly the same brick.
    // -----------------------------------------------------------------------
    static class BrickIcon
    {
        static void RR(Graphics g, double x, double y, double w, double h, double r,
                       int cr, int cg, int cb)
        {
            Rectangle rect = new Rectangle((int)Math.Round(x), (int)Math.Round(y),
                                           (int)Math.Round(w), (int)Math.Round(h));
            using (GraphicsPath p = Dr.Round(rect, (int)Math.Round(r)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(cr, cg, cb)))
                g.FillPath(b, p);
        }

        /// Draws the icon into a size x size box at (0,0) of the current transform.
        public static void Draw(Graphics g, float size)
        {
            GraphicsState st = g.Save();
            Dr.Quality(g);
            g.ScaleTransform(size / 512f, size / 512f);

            using (GraphicsPath outer = Dr.Round(new Rectangle(0, 0, 512, 512), 113))
            using (LinearGradientBrush lg = new LinearGradientBrush(
                       new Rectangle(0, 0, 512, 512),
                       Color.FromArgb(28, 42, 74), Color.FromArgb(18, 26, 48),
                       LinearGradientMode.Vertical))
                g.FillPath(lg, outer);

            RR(g, 66, 92, 380, 340, 44, 120, 126, 136);     // lower edge
            RR(g, 66, 84, 380, 336, 44, 222, 226, 232);     // body
            RR(g, 80, 92, 352, 20, 10, 244, 246, 250);      // top shine
            RR(g, 112, 128, 288, 138, 16, 60, 64, 72);      // screen frame
            RR(g, 122, 138, 268, 118, 10, 150, 176, 140);   // screen
            RR(g, 196, 170, 22, 22, 3, 34, 52, 34);         // eyes
            RR(g, 296, 170, 22, 22, 3, 34, 52, 34);
            RR(g, 196, 214, 108, 12, 5, 34, 52, 34);        // smile
            RR(g, 184, 200, 16, 14, 5, 34, 52, 34);
            RR(g, 300, 200, 16, 14, 5, 34, 52, 34);
            RR(g, 112, 292, 64, 48, 12, 70, 76, 88);        // arrow buttons
            RR(g, 336, 292, 64, 48, 12, 70, 76, 88);
            RR(g, 180, 362, 152, 30, 12, 70, 76, 88);       // back button

            using (SolidBrush o = new SolidBrush(Color.FromArgb(250, 140, 20)))
                g.FillEllipse(o, 256 - 46, 316 - 34, 92, 68);
            using (SolidBrush y = new SolidBrush(Color.FromArgb(255, 176, 70)))
                g.FillEllipse(y, 256 - 32, 310 - 18, 64, 36);

            for (int k = 0; k < 4; k++)
                RR(g, 112 + 64 * k, 404, 52, 20, 6, 40, 44, 52);

            g.Restore(st);
        }

        public static Bitmap Bitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Draw(g, size);
            }
            return bmp;
        }

        static Icon cached;

        public static Icon WindowIcon()
        {
            if (cached != null) return cached;
            try
            {
                using (Bitmap b = Bitmap(64))
                    cached = Icon.FromHandle(b.GetHicon());
            }
            catch (Exception) { cached = null; }
            return cached;
        }
    }
}
