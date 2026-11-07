# Notes for Claude

BrickCode Studio, Windows edition: a C# WinForms editor for the LEGO RCX, the
Scout and the NXT, plus an entry point to the EV3 (Pybricks) block editor.
RCX and Scout use NQC (`nqc.exe`) through the USB tower. The NXT uses NXC:
`nbc.exe` compiles, `nxt_tool.py` talks LCP over USB (pyusb + libusb-1.0,
WinUSB or libusbK driver). The EV3 is Python only: the app opens
`blocks\pybricks-blocks.html` and sends nothing to the brick.

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

## The block editors

The three pages in `package\BrickCodeStudio\blocks\` are **generated**. Do not
edit them here. They are built in the other repo and copied over:

```
blocks/brickcode-blocks-rcx.html  ->  blocks\brickcode-blocks.html      (data-pin="nqc-only")
blocks/brickcode-blocks-nxt.html  ->  blocks\brickcode-blocks-nxt.html  (data-pin="nxc-only")
pybricks/pybricks-blocks.html     ->  blocks\pybricks-blocks.html
```

The pin is an attribute on `<html>` inside each file, which pins the editor to
one language and removes the language switch. It has to live inside the file:
`OpenPage()` opens the page with `ShellExecute`, which resolves a `file:` URL
back to a plain path and loses everything after the `#`. That is why there are
separate files instead of a URL fragment.

## The NXT

`nxt_tool.py` has `info`, `beep`, `upload [run]`, `run`, `stop`, `list`.
`Compile(run)` in `Main.cs` chains `NbcStep` (nbc.exe, `-O=out.rxe`) and
`NxtStep` (upload, optional run). nbc prints `# Status` noise, which `Post()`
filters, and errors as `File "x" ; line N` / `# Error: ...`, which it parses
to highlight the line. Hardware-tested on firmware 1.31 with WinUSB. Do not
swap an NXT-G user's driver for libusbK.

nbc.exe is MPL 1.1 (John Hansen, BricxCC), bundled unchanged; see
`licenses\nbc-LICENSE-MPL-1.1.txt`.

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
