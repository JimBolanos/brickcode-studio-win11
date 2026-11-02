# Notes for Claude

BrickCode Studio, Windows edition: a C# WinForms editor for the LEGO RCX and
Scout. **NQC only** — this package carries `nqc.exe` and no `nbc`, so there is
no NXT and no NXC anywhere in it.

The other edition, GTK4 and C, for Linux and macOS, lives in
[brickcode-studio-apple-linux](https://github.com/JimBolanos/brickcode-studio-apple-linux)
and does support the NXT.

## Build

```
build.bat        compiles src\*.cs, and refreshes the exe inside package\
package.bat      builds BrickCodeStudio-Windows-completo.zip for a release
```

`build.bat` uses the C# compiler that ships with the .NET Framework, so there
is nothing to install. **It only runs on Windows** — there is no way to produce
the real `.exe` from a Mac or a Linux container. `mcs` from mono will catch
syntax and type errors but does not produce a shippable binary.

2.1.2 shipped with `package\BrickCodeStudio\BrickCodeStudio.exe` one build
behind its own sources, because `build.bat` wrote only `dist\` and the packaged
copy was refreshed by hand. Both scripts now guard against that: `build.bat`
refreshes it, and `package.bat` refuses to zip an exe older than any file under
`src\`. Do not work around that refusal — rebuild.

## The block editor

`package\BrickCodeStudio\blocks\brickcode-blocks.html` is **generated**. Do not
edit it here. It is built in the other repo:

```
cd blocks
python3 build.py
python3 pin.py nqc-only /path/to/this/repo/package/BrickCodeStudio/blocks/brickcode-blocks.html
```

`pin.py` sets `data-pin="nqc-only"` on `<html>`, which pins the editor to NQC
and takes the language switch away. The pin has to live inside the file:
`OpenBlockEditor()` opens the page with `ShellExecute`, which resolves a
`file:` URL back to a plain path before the browser sees it and loses
everything after the `#`. A URL fragment cannot be relied on here, and cannot
override a pin.

That matters because this edition has no `nbc`. Offering NXC would only let
someone build a program the app in front of them cannot compile.

## The USB tower

`nqc` opens the tower as `\\.\LEGOTOWER1`, LEGO's own driver, which was never
signed for 64-bit Windows 10 or 11. So **nqc compiles only**, and
`rcx_tool.py` carries the program to the brick over libusbK, bound with Zadig.
That approach is [beckmx's](https://github.com/beckmx/lego-tower-rcx).

A serial tower on a COM port is different: there nqc does everything, and the
Python path is not used at all.

`rcx_tool.py` here is patched — upstream used opcode `0x51` (PlaySound) instead
of `0x91` (Select Program) for the program slot, so downloading to any slot but
the first never landed where it should.

## Untested

Opening the editor from **File → Block editor…** on real Windows. The pin is
inside the file so it should hold, but nobody has watched it happen yet. If the
NQC/NXC switch appears, the pin did not take.
