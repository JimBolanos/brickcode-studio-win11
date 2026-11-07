NXT on Windows - first test
===========================

Goal: check that this PC can talk to the NXT before we build the full editor
support. It takes about five minutes.

1. Install Python 3 from the Microsoft Store if you do not have it.
   (pyusb and libusb-1.0.dll are already in this folder.)

2. Turn the NXT on and plug it in with the USB cable.

3. Bind the NXT to the libusbK driver (once):
   - Right-click  driver\zadig-2.9.exe  ->  Run as administrator
   - Options -> List All Devices
   - Pick  "NXT"  in the top list (USB ID 0694 0002)
   - Choose  libusbK  in the box on the right, press  Install Driver
   Note: this replaces LEGO's driver for the NXT. The official LEGO software
   will not see the NXT until you reinstall LEGO's driver (Device Manager ->
   the NXT -> Update driver).

4. Double-click  test-nxt.bat
   It prints the NXT's name, battery and firmware, beeps, then sends and runs
   a tiny program that shows "Hello from Windows!".
   If you also drop  nbc.exe  (from nbc-1.2.1.r5.zip, SourceForge "NXT Tools"
   by John Hansen, together with its header files) into this folder, the test
   compiles the program first as well.

If anything fails, copy the text of the window and send it to Claude.


NXT en Windows - primera prueba
===============================
Objetivo: comprobar que este PC habla con el NXT antes de construir el soporte
completo. Pasos: 1) Python 3; 2) NXT encendido y por USB; 3) Zadig como
administrador -> Options -> List All Devices -> "NXT" (0694 0002) -> libusbK ->
Install Driver (reemplaza el driver de LEGO para el NXT); 4) doble clic en
test-nxt.bat. Si algo falla, copia el texto y mandaselo a Claude.
