#!/usr/bin/env python3
"""
NXT Tool - talk to a LEGO MINDSTORMS NXT over USB.
Uses PyUSB + libusb-1.0 through the libusbK driver on Windows 10/11
(the same driver setup as rcx_tool.py; bind it with Zadig).

It speaks the LEGO Communication Protocol (LCP), which is public:
  - direct commands (start/stop a program, motors off, battery, beep)
  - system commands (open/write/close/delete a file, list files, firmware)

Usage:
    python nxt_tool.py probe                - Is an NXT there? Name, battery, firmware
    python nxt_tool.py info                 - Same, plus free memory
    python nxt_tool.py upload FILE.rxe      - Copy a compiled program to the NXT
    python nxt_tool.py upload FILE.rxe run  - ... and start it
    python nxt_tool.py run NAME             - Start a program already on the NXT
    python nxt_tool.py stop                 - Stop the program, motors off
    python nxt_tool.py list                 - Files on the NXT
    python nxt_tool.py beep                 - A short beep (is the link alive?)

Exit code 0 = success, 1 = failure. Messages are printed in English.
"""

import sys
import os
import time

# --- LCP constants ----------------------------------------------------
VENDOR_ID  = 0x0694
PRODUCT_ID = 0x0002
EP_OUT     = 0x01
EP_IN      = 0x82
TIMEOUT_MS = 2000
NAME_LEN   = 20            # file names are packed in 20 bytes
CHUNK      = 60            # data bytes per SYS_WRITE (64-byte USB packet)

CMD_DIRECT_REPLY = 0x00
CMD_SYSTEM_REPLY = 0x01

DIRECT_START_PROGRAM    = 0x00
DIRECT_STOP_PROGRAM     = 0x01
DIRECT_PLAY_TONE        = 0x03
DIRECT_SET_OUTPUT_STATE = 0x04
DIRECT_GET_BATTERY      = 0x0B

SYS_OPEN_READ    = 0x80
SYS_OPEN_WRITE   = 0x81
SYS_WRITE        = 0x83
SYS_CLOSE        = 0x84
SYS_DELETE       = 0x85
SYS_FIND_FIRST   = 0x86
SYS_FIND_NEXT    = 0x87
SYS_GET_FIRMWARE = 0x88
SYS_GET_DEVINFO  = 0x9B

STATUS = {
    0x00: "success",
    0x20: "pending communication transaction in progress",
    0x40: "specified mailbox queue is empty",
    0x81: "no more handles",
    0x82: "no space",
    0x83: "no more files",
    0x84: "end of file expected",
    0x85: "end of file",
    0x86: "not a linear file",
    0x87: "file not found",
    0x88: "handle already closed",
    0x89: "no linear space",
    0x8A: "undefined error",
    0x8B: "file is busy",
    0x8C: "no write buffers",
    0x8D: "append not possible",
    0x8E: "file is full",
    0x8F: "file already exists",
    0x90: "module not found",
    0x91: "out of boundary",
    0x92: "illegal file name",
    0x93: "illegal handle",
    0xBD: "request failed (the program or file may not exist)",
    0xBE: "unknown command opcode",
    0xBF: "insane packet",
    0xC0: "data contains out-of-range values",
    0xDD: "communication bus error",
    0xDE: "no free memory in communication buffer",
    0xDF: "specified channel/connection is not valid",
    0xE0: "specified channel/connection not configured or busy",
    0xEC: "no active program",
    0xED: "illegal size specified",
    0xEE: "illegal mailbox queue ID specified",
    0xEF: "attempted to access invalid field of a structure",
    0xF0: "bad input or output specified",
    0xFB: "insufficient memory available",
    0xFF: "bad arguments",
}


def status_text(code):
    return "0x%02X %s" % (code, STATUS.get(code, "unknown status"))


# --- transport --------------------------------------------------------
class UsbTransport:
    """Bulk transfers to the NXT through pyusb + the bundled libusb dll."""

    def __init__(self):
        import usb.core
        import usb.backend.libusb1 as libusb1
        here = os.path.dirname(os.path.abspath(__file__))
        dll = os.path.join(here, "libusb-1.0.dll")
        if os.path.exists(dll):
            be = libusb1.get_backend(find_library=lambda x: dll)
        else:
            be = libusb1.get_backend()          # system libusb (Linux/macOS tests)
        if be is None:
            raise RuntimeError("Cannot load libusb-1.0 (libusb-1.0.dll next to this script).")
        self.usb = usb
        self.dev = usb.core.find(idVendor=VENDOR_ID, idProduct=PRODUCT_ID, backend=be)
        if self.dev is None:
            raise RuntimeError(
                "NXT not found over USB. Is it on and plugged in? "
                "On Windows it needs the libusbK driver (see driver\\README-driver.txt).")
        try:
            if self.dev.is_kernel_driver_active(0):
                self.dev.detach_kernel_driver(0)
        except (usb.core.USBError, NotImplementedError):
            pass
        try:
            self.dev.set_configuration()
        except usb.core.USBError:
            pass                                 # already configured
        try:
            usb.util.claim_interface(self.dev, 0)
        except usb.core.USBError as e:
            raise RuntimeError("Cannot open the NXT (%s). Another program may be using it, "
                               "or the libusbK driver is not installed for it." % e)

    def transact(self, req, reply_len=64):
        self.dev.write(EP_OUT, bytes(req), TIMEOUT_MS)
        return bytes(self.dev.read(EP_IN, reply_len, TIMEOUT_MS))

    def close(self):
        try:
            self.usb.util.release_interface(self.dev, 0)
            self.usb.util.dispose_resources(self.dev)
        except Exception:
            pass


# --- the NXT ----------------------------------------------------------
def pack_name(name):
    raw = name.encode("latin-1", "replace")[:NAME_LEN - 1]
    return raw + b"\x00" * (NAME_LEN - len(raw))


def nxt_file_name(path):
    """At most 15 characters plus .rxe (the NXT limit). Never cut the extension."""
    base = os.path.basename(path)
    dot = base.rfind(".")
    if dot > 0:
        base = base[:dot]
    return base[:15] + ".rxe"


class NXT:
    def __init__(self, transport, sleep=time.sleep):
        self.t = transport
        self.sleep = sleep

    def send(self, req, quiet_fail=False):
        """Send one packet, return the reply (bytes) or None on a USB error."""
        try:
            rep = self.t.transact(req)
        except Exception as e:
            if not quiet_fail:
                print("  [USB] %s" % e)
            return None
        return rep if len(rep) >= 3 else None

    def direct(self, op, payload=b"", quiet_fail=False):
        return self.send(bytes([CMD_DIRECT_REPLY, op]) + payload, quiet_fail)

    def system(self, op, payload=b"", quiet_fail=False):
        return self.send(bytes([CMD_SYSTEM_REPLY, op]) + payload, quiet_fail)

    # -- queries
    def battery_mv(self):
        r = self.direct(DIRECT_GET_BATTERY)
        if r and r[2] == 0 and len(r) >= 5:
            return r[3] | (r[4] << 8)
        return None

    def firmware(self):
        r = self.system(SYS_GET_FIRMWARE)
        if r and r[2] == 0 and len(r) >= 7:
            return "protocol %d.%d, firmware %d.%d" % (r[4], r[3], r[6], r[5])
        return None

    def device_info(self):
        r = self.system(SYS_GET_DEVINFO)
        if r and r[2] == 0 and len(r) >= 33:
            name = r[3:18].split(b"\x00")[0].decode("latin-1")
            free = r[29] | (r[30] << 8) | (r[31] << 16) | (r[32] << 24)
            return name, free
        return None, None

    # -- actions
    def beep(self):
        r = self.direct(DIRECT_PLAY_TONE, bytes([0xB8, 0x01, 0xF4, 0x01]))  # 440 Hz, 500 ms
        return bool(r and r[2] == 0)

    def stop(self):
        """Stop the running program (it is fine if none is running) and motors off."""
        self.direct(DIRECT_STOP_PROGRAM, quiet_fail=True)
        self.direct(DIRECT_SET_OUTPUT_STATE, bytes([0xFF]) + bytes(9), quiet_fail=True)
        return True

    def start(self, name):
        r = self.direct(DIRECT_START_PROGRAM, pack_name(name))
        if r is None:
            return False
        if r[2] != 0:
            print("  [FAIL] Could not start %s: %s" % (name, status_text(r[2])))
            return False
        return True

    def list_files(self):
        files = []
        r = self.system(SYS_FIND_FIRST, pack_name("*.*"))
        while r and r[2] == 0 and len(r) >= 28:
            handle = r[3]
            name = r[4:24].split(b"\x00")[0].decode("latin-1")
            size = r[24] | (r[25] << 8) | (r[26] << 16) | (r[27] << 24)
            files.append((name, size))
            r = self.system(SYS_FIND_NEXT, bytes([handle]))
        return files

    def upload(self, name, data, run=False):
        size = len(data)
        # A program from an earlier run may still hold the motors: stop it first.
        if run:
            self.stop()
            self.sleep(0.3)

        # Delete the old copy, open the new file. If the NXT says it is busy,
        # stop whatever is running and try again (up to 4 times).
        handle = None
        last = None
        for attempt in range(4):
            if attempt:
                self.direct(DIRECT_STOP_PROGRAM, quiet_fail=True)
                self.sleep(0.5)
            self.system(SYS_DELETE, pack_name(name), quiet_fail=True)  # failure is fine
            r = self.system(SYS_OPEN_WRITE, pack_name(name) + size.to_bytes(4, "little"))
            last = r
            if r is not None and r[2] == 0:
                handle = r[3]
                break
        if handle is None:
            if last is None:
                print("  [FAIL] The NXT did not answer.")
            else:
                print("  [FAIL] The NXT refused the file: %s" % status_text(last[2]))
            return False

        for i in range(0, size, CHUNK):
            chunk = data[i:i + CHUNK]
            r = self.system(SYS_WRITE, bytes([handle]) + chunk)
            if r is None or r[2] != 0:
                print("  [FAIL] Write failed at byte %d%s" %
                      (i, "" if r is None else ": " + status_text(r[2])))
                return False

        r = self.system(SYS_CLOSE, bytes([handle]))
        if r is None or r[2] != 0:
            print("  [FAIL] Could not close the file on the NXT.")
            return False
        print("  [OK] Sent %s (%d bytes)." % (name, size))

        if run:
            if not self.start(name):
                return False
            print("  [OK] Running on the NXT.")
        return True


# --- command line -----------------------------------------------------
def cmd_probe(nxt, full=False):
    name, free = nxt.device_info()
    print("[OK] NXT found.")
    if name is not None:
        print("  Name     : %s" % name)
    fw = nxt.firmware()
    if fw:
        print("  Firmware : %s" % fw)
    mv = nxt.battery_mv()
    if mv is not None:
        print("  Battery  : %.2f V" % (mv / 1000.0))
    if full and free is not None:
        print("  Free     : %d bytes" % free)
    return True


def main(argv=None, transport=None):
    argv = sys.argv if argv is None else argv
    if len(argv) < 2:
        print(__doc__)
        return 1
    cmd = argv[1].lower()
    if cmd not in ("probe", "info", "upload", "run", "stop", "list", "beep"):
        print("Unknown: %s" % cmd)
        print(__doc__)
        return 1
    if cmd == "upload" and len(argv) < 3:
        print("Usage: nxt_tool.py upload <file.rxe> [run]")
        return 1
    if cmd == "run" and len(argv) < 3:
        print("Usage: nxt_tool.py run <name>")
        return 1

    own = transport is None
    try:
        t = transport or UsbTransport()
    except Exception as e:
        print("[ERROR] %s" % e)
        return 1
    nxt = NXT(t)
    ok = True
    try:
        if cmd in ("probe", "info"):
            ok = cmd_probe(nxt, full=(cmd == "info"))
        elif cmd == "beep":
            ok = nxt.beep()
            print("  [OK] Beep." if ok else "  [FAIL] No answer.")
        elif cmd == "stop":
            nxt.stop()
            print("  [OK] Stopped.")
        elif cmd == "list":
            files = nxt.list_files()
            for n, s in files:
                print("  %-24s %8d bytes" % (n, s))
            print("  %d file(s)." % len(files))
        elif cmd == "run":
            name = argv[2]
            if "." not in name:
                name += ".rxe"
            nxt.stop()
            nxt.sleep(0.3)
            ok = nxt.start(name)
            if ok:
                print("  [OK] Running on the NXT.")
        elif cmd == "upload":
            path = argv[2]
            if not os.path.isfile(path):
                print("[ERROR] Cannot read %s" % path)
                return 1
            with open(path, "rb") as f:
                data = f.read()
            name = nxt_file_name(path)
            print("Sending %s to the NXT ..." % name)
            ok = nxt.upload(name, data, run=(len(argv) > 3 and argv[3].lower() == "run"))
    finally:
        if own:
            t.close()
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
