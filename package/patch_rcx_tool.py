#!/usr/bin/env python3
"""Patch beckmx's rcx_tool.py: fix the program-slot opcode and add the
commands BrickCode Studio needs. Run with the file to patch as argv[1]."""

import io
import sys

path = sys.argv[1]
src = io.open(path, encoding="utf-8").read()


def must(old, new):
    global src
    if old not in src:
        raise SystemExit("pattern not found:\n" + old[:120])
    src = src.replace(old, new, 1)


# ---------------------------------------------------------------- opcodes
must(
    "OP_SELECT_PROGRAM          = 0x51",
    "OP_SELECT_PROGRAM          = 0x91   # 0x51 is PlaySound, not Select Program\n"
    "OP_PLAY_SOUND              = 0x51\n"
    "OP_STOP_ALL                = 0x50\n"
    "OP_START_TASK              = 0x71\n"
    "OP_IR_MODE                 = 0x31   # 0 = near, 1 = far\n"
    "OP_AUTO_OFF                = 0xB1   # minutes, 0 = never\n"
    "OP_UPLOAD_DATALOG          = 0xA4\n"
    "OP_REMOTE                  = 0xD2",
)

# ---------------------------------------------------------------- docstring
must(
    '    python rcx_tool.py download FILE     - Download compiled program (.rcx)\n"""',
    "    python rcx_tool.py download FILE [N] - Download compiled program (.rcx)\n"
    "    python rcx_tool.py run [N]           - Select slot N and start the program\n"
    "    python rcx_tool.py stop              - Stop all tasks\n"
    "    python rcx_tool.py clear             - Delete the tasks and subs in the slot\n"
    "    python rcx_tool.py near | far        - Set the IR range\n"
    "    python rcx_tool.py sleep MINUTES     - Auto-shutoff (0 = never)\n"
    "    python rcx_tool.py battery           - Battery level\n"
    "    python rcx_tool.py datalog [COUNT]   - Upload the datalog\n"
    "    python rcx_tool.py remote HEX        - Send an IR remote command (e.g. 0800)\n"
    "\n"
    "Opcodes follow the nqc source (rcxlib/RCX_Constants.h).\n"
    '"""',
)

# ---------------------------------------------------------------- new methods
NEW = '''
    # ---- commands added for BrickCode Studio ----

    def send_noreply(self, opcode, params=b""):
        """Fire and forget: the remote opcodes get no answer from the RCX."""
        pkt = self._build_packet(opcode, params)
        self.tower.wait_tx_ready()
        self.tower.write(pkt)
        self.toggle ^= 1
        return True

    def select_slot(self, slot):
        """slot is 1..5, as shown in the interface."""
        slot = max(1, min(5, int(slot)))
        if self.send(OP_SELECT_PROGRAM, bytes([slot - 1])) is None:
            print(f"  [FAIL] Could not select slot {slot}.")
            return False
        print(f"  Slot {slot} selected.")
        return True

    def run_program(self, slot=None):
        if slot is not None and not self.select_slot(slot):
            return False
        print("Starting the program ...")
        if self.send(OP_START_TASK, b"\\x00") is None:
            print("  [FAIL] No response."); return False
        print("  [OK] Running on the brick.")
        return True

    def stop_all(self):
        print("Stopping all tasks ...")
        ok = self.send(OP_STOP_ALL) is not None
        print("  [OK] Stopped." if ok else "  [FAIL] No response.")
        return ok

    def clear_slot(self, slot=None):
        if slot is not None and not self.select_slot(slot):
            return False
        print("Deleting tasks and subs ...")
        self.send(OP_DELETE_ALL_TASKS, timeout=3000)
        self.send(OP_DELETE_ALL_SUBS, timeout=3000)
        print("  [OK] Brick cleared.")
        return True

    def ir_mode(self, far):
        name = "far" if far else "near"
        print(f"Setting the IR range to {name} ...")
        ok = self.send(OP_IR_MODE, bytes([1 if far else 0])) is not None
        print(f"  [OK] IR {name}." if ok else "  [FAIL] No response.")
        return ok

    def auto_off(self, minutes):
        minutes = max(0, min(255, int(minutes)))
        print(f"Setting auto-shutoff to {minutes} min ...")
        ok = self.send(OP_AUTO_OFF, bytes([minutes])) is not None
        print("  [OK] Auto-shutoff set." if ok else "  [FAIL] No response.")
        return ok

    def remote(self, hex4):
        """hex4 is the 16-bit button mask, e.g. 0800 for motor A forward."""
        value = int(hex4, 16) & 0xFFFF
        self.send_noreply(OP_REMOTE, bytes([value & 0xFF, (value >> 8) & 0xFF]))
        print(f"  Remote command {value:04X} sent.")
        return True

    def datalog(self, count=None):
        """Entry 0 holds the number of entries, so read it first."""
        head = self.send(OP_UPLOAD_DATALOG, b"\\x00\\x00\\x01\\x00", timeout=4000)
        total = None
        if head and len(head) >= 4:
            total = head[2] | (head[3] << 8)
        if count is None:
            count = total if total else 100
        count = max(1, min(1000, int(count)))
        print(f"Uploading {count} datalog entries ...")
        r = self.send(OP_UPLOAD_DATALOG,
                      bytes([0, 0, count & 0xFF, (count >> 8) & 0xFF]),
                      timeout=8000)
        if not r:
            print("  [FAIL] No response. Is there a datalog on the brick?")
            return False
        body = r[1:]
        shown = 0
        for i in range(0, len(body) - 2, 3):
            source = body[i]
            value = body[i + 1] | (body[i + 2] << 8)
            if value >= 32768:
                value -= 65536
            print(f"  {shown:4d}  source {source:3d}  value {value}")
            shown += 1
        if shown == 0:
            print(f"  Raw: {r.hex()}")
        return True

'''

must('        print("Press Run on the RCX to execute.")\n        return True\n',
     '        print("Press Run on the RCX to execute.")\n        return True\n' + NEW)

# ---------------------------------------------------------------- main()
OLD_MAIN_START = 'def main():\n    if len(sys.argv) < 2:'
idx = src.index(OLD_MAIN_START)
end = src.index('if __name__ == "__main__":')

NEW_MAIN = '''def main():
    if len(sys.argv) < 2:
        print(__doc__); sys.exit(1)

    cmd = sys.argv[1].lower()

    def arg(n, default=None):
        return sys.argv[n] if len(sys.argv) > n else default

    try:
        tower = LEGOTower()
        tower.initialize()
    except Exception as e:
        print(f"[ERROR] {e}"); sys.exit(1)

    rcx = RCXComm(tower)
    ok = True

    if cmd == "tower":
        print("Tower OK.")
    elif cmd == "ping":
        ok = rcx.ping()
    elif cmd == "info":
        rcx.ping(); print()
        rcx.get_battery(); print()
        rcx.get_versions()
    elif cmd == "battery":
        ok = rcx.get_battery() is not None
    elif cmd == "firmware":
        if arg(2) is None:
            print("Usage: rcx_tool.py firmware <file.lgo>"); sys.exit(1)
        ok = rcx.upload_firmware(arg(2))
    elif cmd == "download":
        if arg(2) is None:
            print("Usage: rcx_tool.py download <file.rcx> [slot]"); sys.exit(1)
        ok = rcx.download_program(arg(2), int(arg(3, "1")))
    elif cmd == "run":
        ok = rcx.run_program(arg(2))
    elif cmd == "stop":
        ok = rcx.stop_all()
    elif cmd == "clear":
        ok = rcx.clear_slot(arg(2))
    elif cmd in ("near", "far"):
        ok = rcx.ir_mode(cmd == "far")
    elif cmd == "sleep":
        if arg(2) is None:
            print("Usage: rcx_tool.py sleep <minutes>"); sys.exit(1)
        ok = rcx.auto_off(arg(2))
    elif cmd == "datalog":
        ok = rcx.datalog(arg(2))
    elif cmd == "remote":
        if arg(2) is None:
            print("Usage: rcx_tool.py remote <hex>   e.g. 0800"); sys.exit(1)
        ok = rcx.remote(arg(2))
    else:
        print(f"Unknown: {cmd}"); print(__doc__); sys.exit(1)

    sys.exit(0 if ok else 1)

'''

src = src[:idx] + NEW_MAIN + src[end:]

# slot is 1-based everywhere now
must(
    "        # Select program slot\n        if slot > 0:\n"
    "            print(f\"Selecting program slot {slot} ...\")\n"
    "            self.send(OP_SELECT_PROGRAM, bytes([slot - 1]))",
    "        # Select program slot (1..5)\n        if slot and slot > 0:\n"
    "            self.select_slot(slot)",
)

must("def download_program(self, filepath, slot=0):",
     "def download_program(self, filepath, slot=1):")

io.open(path, "w", encoding="utf-8").write(src)
print("patched", path)
