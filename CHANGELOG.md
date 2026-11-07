# Changelog

Versions before 2.1.1 were released as
[rcx-tower-gui](https://github.com/JimBolanos/rcx-tower-gui), under the name
RCX Tower GUI. They are kept here for the record.

## Unreleased — NXT and EV3

- **NXT mode.** Pick NXT as the brick: the editor switches to NXC, F5 compiles with the
  bundled `nbc.exe` and sends over USB with `nxt_tool.py` (LCP, pyusb + libusb-1.0).
  Test brick, Stop and Run work on the NXT; compiler errors are parsed and the line is
  highlighted. Hardware-tested on firmware 1.31 with WinUSB.
- **Block editors brought up to date:** RCX, NXT and Pybricks (EV3) pages, including the
  IR messages, colour / light / sound, Bluetooth and "set movement motors" blocks.
- **File → Pybricks block editor (EV3)…** opens the EV3 editor. The program is run from
  the Pybricks editor; this app sends nothing to the EV3.
- **NXT Explorer**: name, firmware, battery and free memory, the files on the brick, and
  Run, Send file… and Delete. **NXT Screen**: the NXT display live over USB, drawn as the
  brick, with clickable buttons and the keyboard (arrows, Enter, Esc). **VLL** window: the
  Scout sends VLL codes to a Micro Scout (`rcx_tool.py vll`). The side bar shows the tools
  of the selected brick only.
- In NXT mode the Settings panel drops the RCX / Scout utilities, the program slot and the
  nqc command line, and offers NXT utilities instead; the file chip says NXC; choosing NXT
  checks the brick at once.
- Files are saved **without a BOM**: nbc rejected it (`';' expected` on line 3).
- A USB error "operation not supported" now says to unplug and replug the NXT.
- **HOME screen** with one card per brick (RCX, Scout, NXT, EV3), the four bricks drawn
  from the Apple / Linux edition's drawings; the **bottom navigation** HOME / CODE / BLOCKS /
  ABOUT; and an **ABOUT screen** (what it uses, documentation, links, legal). The app now
  opens on HOME. **BLOCKS** opens a small menu to choose the block editor (RCX / Scout, NXT or
  EV3) instead of guessing from the current mode.
- `build.bat` puts a **BrickCode Studio shortcut on the desktop** (`make-shortcut.ps1`), pointing at
  `package\BrickCodeStudio\BrickCodeStudio.exe`; if it cannot be made the build still succeeds.
- `test-nxt.bat` tests the whole NXT chain without the GUI.
- `nbc.exe` bundled (MPL 1.1, unchanged); the NXT-only block page is no longer left out.

## 2.2.0 — blocks, for people who do not type yet

- **Visual block editor**, under **File → Block editor…**. Drag blocks together and
  it writes the NQC; save there, open the `.nqc` here, press F5 as always. It is a
  single HTML file in `blocks\` beside the exe, it needs no internet, and it opens in
  whatever browser the machine already has — rather than an embedded view, which
  would have meant a WebView2 runtime to install on every machine and this package
  is meant to be drop-and-run.
- The copy that ships here is **built pinned to NQC**: the switch to the NXT language
  is not in it. This edition carries `nqc` and not `nbc`, so NXC would only let
  someone build a program the app in front of them cannot compile. The pin is an
  attribute inside the file rather than a `#nqc-only` on the end of the address,
  because ShellExecute turns a `file:` URL back into a plain path before the browser
  sees it and loses the fragment on the way.
- Saved programs keep their blocks inside, in a comment at the end of the file, so
  the same `.nqc` can be opened in the editor again later and carried on. The
  comment does not change what `nqc` produces: the compiled `.rcx` is byte for byte
  the same with it and without it.
- `LEEME-PRIMERO.txt` and `README.es.md` describe the editor too — in 2.1.2 only the
  English ones did.
- `build.bat` now refreshes the exe inside `package\BrickCodeStudio` as well, and a
  new `package.bat` builds the release zip and refuses to build it from an exe older
  than anything under `src\`. 2.1.2 shipped with that packaged exe one build behind
  its own sources, which is not the sort of thing anyone notices until someone asks
  why a menu entry is not there.

## 2.1.2 — the package carries everything

- `package/BrickCodeStudio` is a ready-to-run folder: the editor, `nqc.exe`, the
  three RCX firmware files, pyusb, libusb and Zadig for the tower driver. The
  only thing left to install is Python 3.
- **The USB tower on Windows 10 and 11 does not work through nqc.** nqc opens
  `\\.\LEGOTOWER1`, LEGO's own driver, which was never signed for 64-bit
  Windows. nqc now only compiles, and `rcx_tool.py` carries the program to the
  brick over libusbK. A COM port in the Tower port box still uses nqc for
  everything, as before.
- Actions are chained: compile, then download, then run, stopping at the first
  failure, with each command shown in the console.
- Fixed a real bug in beckmx's `rcx_tool.py`: the program-slot opcode was `0x51`
  (PlaySound) instead of `0x91` (Select Program), so downloading to a slot other
  than 1 never landed where it should. Added the `run`, `stop`, `clear`, `near`,
  `far`, `sleep`, `battery`, `datalog` and `remote` commands the interface needs.
- **Test tools** now checks all five pieces: nqc.exe, python, rcx_tool.py,
  libusb-1.0.dll and pyusb.

## 2.1.1 — BrickCode Studio, Windows edition

Rebuilt as the Windows edition of
[BrickCode Studio](https://github.com/JimBolanos/brickcode-studio-apple-linux), NQC only.

- The whole interface redrawn to match BrickCode Studio: top bar with the file chip,
  mode, language, brick status and the Cancel / Download / Run buttons; left sidebar
  with the bricks and the tools; editor over the console; settings panel on the right;
  status bar. Same palette, same spacing, same icon.
- Light and Dark themes, with the exact colours of the original.
- English and Spanish, switched live from the top bar. The Spanish strings are the
  ones from BrickCode Studio.
- NQC only. Everything about NXC and the NXT is gone, and so is `rcx_tool.py`:
  every action is now a direct `nqc.exe` invocation.
- The brick utilities, the tower port, auto-shutoff, firmware and the free argument
  box with history all moved into the settings panel.
- The LEGO IR remote window, sending real `-raw1` opcodes.
- The app icon is drawn in code with GDI+, ported from the cairo original, so it is
  the same brick at 16 px and at 256 px, and it is embedded in the exe.
- Settings are remembered in `%APPDATA%\BrickCodeStudio\settings.txt`.
- The compiler error line is highlighted in the editor and scrolled into view.

### Earlier, as RCX Tower GUI

## 3.2

- Fixed a `MissingMethodException` on startup: one call resolved to a `String.Split`
  overload that does not exist on .NET Framework. Replaced with the `char[]` overload.
  Every external call in the assembly was audited against the .NET Framework API.
- Fixed the panel getting stuck: if a compile succeeded but `rcx_tool.py` could not be
  found afterwards, the buttons stayed disabled forever.

## 3.1

- Resizable window, compact layout below 800 px tall, and the size is remembered.

## 3.0

- Rewritten as a native Windows application. Earlier builds wrapped a hidden PowerShell
  process, which could die silently and leave nothing on screen.

## 2.0

- Scratch-style interface, `python` and `nqc` auto-detected in the application folder,
  compiler output shown in the console, and a 180 s watchdog for firmware uploads.
