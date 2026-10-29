LEGO USB IR Tower driver
========================

  This is ONLY for the USB tower (part x431c01, ID 0694:0001).
  If yours is the serial tower (part 9713, DE-9 connector) you need none
  of this: type its COM port into Settings and you are done.

Windows 10 and 11 ship no driver for the LEGO USB tower, and LEGO's own
driver was never signed for 64-bit Windows. The way round it is to bind
the tower to libusbK, which is signed, using Zadig.

You only do this once.


STEPS
-----

  1. Plug the USB tower in.

  2. Right-click  zadig-2.9.exe  ->  "Run as administrator".

  3. In the menu:  Options  ->  List All Devices
     (tick it, or the tower will not be listed).

  4. Pick  "LEGO USB Tower"  from the top list.
     Check that it reads  USB ID:  0694  0001
     If it is not there, unplug the tower and plug it in again.

  5. In the box on the right, use the little arrows to choose  libusbK.

  6. Press  "Install Driver"  (or "Replace Driver" if one is already there).

  7. Wait for  "The driver was installed successfully".
     It can take half a minute. That is all.


CHECK IT WORKED
---------------

  Open BrickCode Studio, press "Test tools", then "Detect" in Settings.
  If the tower answers, the driver is in place.

  You can also check from PowerShell:

      pnputil /enum-devices /connected | Select-String -Context 2 "LEGO"


IMPORTANT
---------

  Once libusbK is bound, the tower no longer uses the default driver.
  That means BricxCC and  nqc -Susb  will NOT be able to use it.

  That is not a problem here: BrickCode Studio sends programs with
  rcx_tool.py, which talks through libusbK. nqc only compiles.

  To undo it, open Device Manager, uninstall the device with "Delete the
  driver software for this device" ticked, and plug the tower in again.


ABOUT ZADIG
-----------

  Zadig 2.9, by Pete Batard (Akeo). GPL v3.
  Bundled here for convenience; it is not our work.

  Site     https://zadig.akeo.ie/
  Source   https://github.com/pbatard/libwdi
  Licence  ..\licenses\zadig-LICENSE-GPL-3.0.txt

  The GPL v3 entitles you to the source: it is at the link above, in the
  project's own repository.
