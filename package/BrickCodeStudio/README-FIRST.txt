BrickCode Studio - Windows edition (NQC only)
=============================================

Everything you need is in this folder. There is no installer: unzip and run.


BEFORE YOU START
----------------

  1) The LEGO IR TOWER.

     The RCX and the Scout have no data cable. There is no USB socket on
     the brick. Everything - the program, the firmware, remote commands -
     travels as infrared light, the way a TV remote works:

         your PC  --USB-->  IR tower  ))) infrared (((  -->  RCX / Scout

     Without a tower this editor still writes and compiles NQC perfectly
     well, but nothing ever reaches the brick.

     USB tower     part x431c01, came in sets 3804, 9786 and 9794.
                   Powered by USB. ID 0694:0001. This is the one this
                   package is set up for, and the one that needs the
                   driver in step 2.

     Serial tower  part 9713, DE-9 connector, came in 9719, 9785, 9790
                   and 9793. Needs a 9 V battery. If that is yours, you
                   need no driver and no Python: type its port (COM1,
                   say) into "Tower port" under Settings and nqc does
                   everything.

  2) Python 3, from the Microsoft Store (search for "Python 3.13" or
     newer). It is the one piece that cannot be bundled. Everything else
     is here: nqc, the firmware files, the libraries and the driver.


STEPS
-----

  1. Plug the USB IR tower into the computer.

  2. Install the tower driver:
     open  driver\  and follow  driver\README-driver.txt
     (it is Zadig, it takes a minute).

  3. Double-click  BrickCodeStudio.exe

  4. In the program, press "Test tools".
     It should find all five: nqc.exe, python, rcx_tool.py,
     libusb-1.0.dll and pyusb.

  5. Switch the RCX on and point its IR window at the tower's, facing
     each other, 10 to 25 cm apart. Press "Test brick".
     If it answers, you are set.

     If it does not: infrared is directional and bounces off walls. Keep
     them facing each other, out of direct sunlight and from under a
     fluorescent lamp, with fresh batteries in the brick.

  6. If the RCX shows only a number and no running figure, it has no
     firmware. In Settings -> Brick utilities, choose
     firmware\firm0328.lgo and press "Send firmware".
     It takes 3 to 5 minutes. Do not move the RCX.

  7. Write your program and press F5.


WHAT IS IN HERE
---------------

  BrickCodeStudio.exe   the editor
  nqc.exe               the NQC compiler
  rcx_tool.py           what talks to the USB tower
  usb\                  pyusb, already bundled (no pip needed)
  libusb-1.0.dll        the USB library rcx_tool uses
  firmware\             firm0309 (RCX 1.0), firm0328 (RCX 2.0), firm0332
  driver\               Zadig and the driver instructions
  examples\             NQC example programs
  licenses\             the licence of every piece


WHY rcx_tool.py IS NEEDED AND nqc ALONE IS NOT
----------------------------------------------

  On Windows, nqc opens the tower as \\.\LEGOTOWER1, which is LEGO's own
  driver. That driver was never signed for 64-bit Windows 10 or 11, so
  nqc by itself cannot download to the brick.

  So here nqc only compiles, and rcx_tool.py carries the program to the
  brick through the libusbK driver that Zadig installs.

  If your tower is SERIAL (a COM port) none of this applies: type the
  port (COM1, say) into "Tower port" under Settings and the program will
  use nqc for everything.


  (c) 2026 Jim Bolanos - MIT License
  Independent project, not affiliated with the LEGO Group.
