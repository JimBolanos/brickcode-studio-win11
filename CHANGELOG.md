# Changelog

Versions before 2.1.1 were released as
[rcx-tower-gui](https://github.com/JimBolanos/rcx-tower-gui), under the name
RCX Tower GUI. They are kept here for the record.

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
[BrickCode Studio](https://github.com/JimBolanos/brickcode-studio), NQC only.

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
