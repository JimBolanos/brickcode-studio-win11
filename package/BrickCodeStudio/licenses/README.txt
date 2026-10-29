What is bundled here, and under what licence
============================================

BrickCodeStudio.exe
  BrickCode Studio, Windows edition. MIT License.
  (c) 2026 Jim Bolanos - https://github.com/JimBolanos
  -> BrickCodeStudio-LICENSE-MIT.txt

nqc.exe
  NQC 3.1 r6, the Not Quite C compiler, by Dave Baum and contributors.
  Mozilla Public License 1.0. Source: https://github.com/jverne/nqc
  The Windows binary is the one distributed with
  https://github.com/beckmx/lego-tower-rcx
  -> nqc-LICENSE-MPL-1.0.md

rcx_tool.py, parse_rcx.py
  By beckmx, from https://github.com/beckmx/lego-tower-rcx
  Mozilla Public License 1.0.
  Modified here: the program-slot opcode was corrected from 0x51
  (PlaySound) to 0x91 (Select Program), and the commands run, stop,
  clear, near, far, sleep, battery, datalog and remote were added, so
  that every button in BrickCode Studio has something to call. The
  opcodes follow the nqc source (rcxlib/RCX_Constants.h).
  -> rcx_tool-LICENSE-MPL-1.0.txt

usb\  (pyusb 1.3.1)
  By Wander Lairson Costa and contributors. BSD 3-Clause.
  https://github.com/pyusb/pyusb
  Bundled unchanged so that no pip install is needed.
  -> pyusb-LICENSE-BSD-3.txt

libusb-1.0.dll, libusb0.dll, libusb-0-1-4.dll
  libusb. LGPL 2.1 or later. https://github.com/libusb/libusb
  The binaries are the ones distributed with beckmx/lego-tower-rcx.
  -> libusb-LICENSE-LGPL-2.1.txt

driver\zadig-2.9.exe
  Zadig 2.9, by Pete Batard (Akeo). GPL v3.
  https://zadig.akeo.ie/  -  source: https://github.com/pbatard/libwdi
  Bundled unchanged. The GPL v3 entitles you to the source, which is in
  the project repository linked above.
  -> zadig-LICENSE-GPL-3.0.txt

firmware\firm0309.lgo, firm0328.lgo, firm0332.lgo
  The RCX firmware is LEGO's. These are the copies that ship with the
  nqc project and with beckmx/lego-tower-rcx, included here so the
  install works offline. They are redistributed the same way nqc does.

examples\*.nqc
  Example programs from BrickCode Studio. MIT License.

LEGO and MINDSTORMS are trademarks of the LEGO Group. This is an
independent project, not affiliated with or endorsed by the LEGO Group.
