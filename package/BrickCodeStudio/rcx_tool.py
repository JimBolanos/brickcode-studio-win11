#!/usr/bin/env python3
"""
RCX Tool - Upload firmware and programs to LEGO RCX via USB IR Tower.
Uses PyUSB + libusb-1.0 through libusbK driver on Windows 10.

Implements:
  - Full LEGO USB Tower vendor control-transfer initialization
  - Motorola S-Record (.lgo) parser
  - RCX IR protocol with toggle bit, complement framing, checksums
  - Firmware upload (delete -> start -> transfer -> unlock)
  - Program download

Usage:
    python rcx_tool.py tower             - Show USB tower info
    python rcx_tool.py ping              - Test if RCX is alive
    python rcx_tool.py info              - Battery + firmware version
    python rcx_tool.py firmware FILE     - Upload firmware (.lgo)
    python rcx_tool.py download FILE [N] - Download compiled program (.rcx)
    python rcx_tool.py run [N]           - Select slot N and start the program
    python rcx_tool.py stop              - Stop all tasks
    python rcx_tool.py clear             - Delete the tasks and subs in the slot
    python rcx_tool.py near | far        - Set the IR range
    python rcx_tool.py sleep MINUTES     - Auto-shutoff (0 = never)
    python rcx_tool.py battery           - Battery level
    python rcx_tool.py datalog [COUNT]   - Upload the datalog
    python rcx_tool.py remote HEX        - Send an IR remote command (e.g. 0800)
    python rcx_tool.py vll CODE [TOGGLE]  - Scout: send a VLL code to a Micro Scout

Opcodes follow the nqc source (rcxlib/RCX_Constants.h).
"""

import sys
import os
import time
import struct
import usb.core
import usb.backend.libusb1 as libusb1

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
LIBUSB_DLL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "libusb-1.0.dll")
VENDOR_ID  = 0x0694
PRODUCT_ID = 0x0001
EP_OUT     = 0x02
EP_IN      = 0x81

# LEGO USB Tower vendor requests (bmRequestType = 0xC0)
LTW_REQ_SET_PARM      = 0x02
LTW_REQ_FLUSH         = 0x03
LTW_REQ_RESET         = 0x04
LTW_REQ_SET_TX_SPEED  = 0xEF
LTW_REQ_SET_RX_SPEED  = 0xF1
LTW_REQ_GET_TX_STATE  = 0xF2
LTW_REQ_GET_VERSION   = 0xFD
LTW_REQ_GET_COPYRIGHT = 0xFE

LTW_PARM_MODE  = 0x01
LTW_PARM_RANGE = 0x02
LTW_MODE_IR    = 0x02
LTW_RANGE_LONG = 0x03
LTW_SPEED_2400 = 0x0008

# RCX opcodes
OP_ALIVE                   = 0x10
OP_GET_VERSIONS            = 0x15
OP_GET_BATTERY             = 0x30
OP_DELETE_ALL_TASKS        = 0x40
OP_TRANSFER_DATA           = 0x45
OP_SELECT_PROGRAM          = 0x91   # 0x51 is PlaySound, not Select Program
OP_PLAY_SOUND              = 0x51
OP_STOP_ALL                = 0x50
OP_START_TASK              = 0x71
OP_IR_MODE                 = 0x31   # 0 = near, 1 = far
OP_AUTO_OFF                = 0xB1   # minutes, 0 = never
OP_UPLOAD_DATALOG          = 0xA4
OP_REMOTE                  = 0xD2
OP_DELETE_FIRMWARE         = 0x65
OP_DELETE_ALL_SUBS         = 0x70
OP_START_FIRMWARE_DOWNLOAD = 0x75
OP_START_TASK_DOWNLOAD     = 0x25
OP_UNLOCK_FIRMWARE         = 0xA5


# ===================================================================
# S-Record parser for .lgo files
# ===================================================================
def parse_srec(filepath):
    """Parse Motorola S-Record .lgo file.
    Returns (data_bytes, start_address)."""
    image = {}
    start_addr = 0x8000  # default

    with open(filepath, "r") as f:
        for line in f:
            line = line.strip()
            if len(line) < 4:
                continue
            rtype = line[0:2]

            if rtype == "S0":
                continue  # header record, skip

            if rtype == "S1":
                # S1 record: 2-byte address
                count = int(line[2:4], 16)   # byte count (addr + data + cksum)
                addr  = int(line[4:8], 16)   # 16-bit address
                # data bytes: from offset 8 to end-2 (last byte is checksum)
                data_hex = line[8 : 8 + (count - 3) * 2]
                for i in range(0, len(data_hex), 2):
                    b = int(data_hex[i:i+2], 16)
                    image[addr] = b
                    addr += 1

            elif rtype == "S9":
                # S9 record: entry point (16-bit)
                start_addr = int(line[4:8], 16)

    if not image:
        raise ValueError("No S1 data records found in file")

    # Convert sparse dict to contiguous byte array
    min_addr = min(image.keys())
    max_addr = max(image.keys())
    size = max_addr - min_addr + 1
    data = bytearray(size)
    for addr, b in image.items():
        data[addr - min_addr] = b

    print(f"  S-Record: {len(image)} data bytes, "
          f"addr range 0x{min_addr:04X}-0x{max_addr:04X}, "
          f"entry=0x{start_addr:04X}")
    return bytes(data), start_addr


# ===================================================================
# .hex file loader (WebPBrick copy-paste format)
# ===================================================================
def load_hex_file(filepath):
    """Load a .hex file containing space-separated 0xNN values.
    Returns raw bytes (RCXI image data)."""
    text = open(filepath, "r").read()
    # Split on whitespace / commas / semicolons, filter hex tokens
    tokens = text.replace(",", " ").replace(";", " ").split()
    result = bytearray()
    for tok in tokens:
        tok = tok.strip()
        if not tok:
            continue
        try:
            if tok.lower().startswith("0x"):
                result.append(int(tok, 16))
            elif all(c in "0123456789abcdefABCDEF" for c in tok) and len(tok) == 2:
                result.append(int(tok, 16))
            else:
                continue  # skip non-hex tokens
        except ValueError:
            continue
    if len(result) < 12:
        raise ValueError(f"Too few bytes ({len(result)}) in hex file")
    print(f"  Loaded {len(result)} bytes from hex file")
    return bytes(result)


# ===================================================================
# .rcx image parser
# ===================================================================
def parse_rcxi(filepath):
    """Parse NQC compiled .rcx (RCXI) image file, or .hex (WebPBrick format).
    Returns list of (frag_type, frag_index, bytecode) tuples.
    frag_type: 0=task, 1=subroutine, 2=sound"""
    if filepath.lower().endswith(".hex"):
        data = load_hex_file(filepath)
    else:
        data = open(filepath, "rb").read()
    if len(data) < 12 or data[0:4] != b"RCXI":
        raise ValueError(f"Not a valid RCXI file (header: {data[0:4]})")

    version       = data[4] | (data[5] << 8)
    frag_count    = data[6] | (data[7] << 8)
    symbol_count  = data[8] | (data[9] << 8)
    target_type   = data[10]
    target_names  = {0: "RCX", 1: "CyberMaster", 2: "Scout", 3: "RCX2", 4: "Spybotics", 5: "Swan"}

    print(f"  RCXI version  : 0x{version:04X}")
    print(f"  Target        : {target_names.get(target_type, f'unknown({target_type})')}")
    print(f"  Fragments     : {frag_count}")
    print(f"  Symbols       : {symbol_count}")

    fragments = []
    offset = 12  # after header

    for i in range(frag_count):
        if offset + 4 > len(data):
            raise ValueError(f"Truncated fragment header at offset {offset}")
        ftype  = data[offset]
        findex = data[offset + 1]
        flen   = data[offset + 2] | (data[offset + 3] << 8)
        offset += 4

        if offset + flen > len(data):
            raise ValueError(f"Truncated fragment data at offset {offset}")
        bytecode = data[offset:offset + flen]
        offset += flen

        # Skip padding to 4-byte boundary
        pad = (4 - flen) & 3
        offset += pad

        ftype_name = {0: "task", 1: "sub", 2: "sound"}.get(ftype, f"type{ftype}")
        print(f"  Fragment {i}    : {ftype_name} #{findex}, {flen} bytes")
        fragments.append((ftype, findex, bytecode))

    return fragments


# ===================================================================
# LEGO USB Tower
# ===================================================================
class LEGOTower:
    def __init__(self):
        be = libusb1.get_backend(find_library=lambda x: LIBUSB_DLL)
        if be is None:
            raise RuntimeError(f"Cannot load libusb-1.0 from {LIBUSB_DLL}")

        self.dev = usb.core.find(idVendor=VENDOR_ID, idProduct=PRODUCT_ID,
                                 backend=be)
        if self.dev is None:
            raise RuntimeError("LEGO USB Tower not found!")

        try:
            if self.dev.is_kernel_driver_active(0):
                self.dev.detach_kernel_driver(0)
        except (usb.core.USBError, NotImplementedError):
            pass

        try:
            self.dev.set_configuration(1)
        except usb.core.USBError:
            pass

        cfg = self.dev.get_active_configuration()
        intf = cfg[(0, 0)]
        self.ep_out = self.ep_in = None
        for ep in intf:
            if ep.bEndpointAddress == EP_OUT:
                self.ep_out = ep
            elif ep.bEndpointAddress == EP_IN:
                self.ep_in = ep
        if not self.ep_out or not self.ep_in:
            raise RuntimeError("Cannot find USB endpoints")
        print(f"[OK] LEGO USB Tower connected (config {cfg.bConfigurationValue})")

    def _ctrl_in(self, bRequest, wValue=0, wIndex=0, length=8):
        return self.dev.ctrl_transfer(0xC0, bRequest, wValue, wIndex,
                                      length, timeout=2000)

    def initialize(self):
        print("Initializing tower ...")
        # Version
        d = self._ctrl_in(LTW_REQ_GET_VERSION, length=8)
        if len(d) >= 7:
            print(f"  Firmware : v{d[4]}.{d[5]} build {d[6] | (d[7]<<8) if len(d)>=8 else 0}")
        # Reset
        d = self._ctrl_in(LTW_REQ_RESET, length=4); time.sleep(0.3)
        print(f"  Reset    : {'OK' if len(d)>=3 and d[2]==0 else 'warn'}")
        # Flush
        d = self._ctrl_in(LTW_REQ_FLUSH, wValue=0x0003, length=4)
        print(f"  Flush    : {'OK' if len(d)>=3 and d[2]==0 else 'warn'}")
        # Mode, Range, Speed
        self._ctrl_in(LTW_REQ_SET_PARM, wValue=LTW_PARM_MODE,
                       wIndex=LTW_MODE_IR, length=6)
        self._ctrl_in(LTW_REQ_SET_PARM, wValue=LTW_PARM_RANGE,
                       wIndex=LTW_RANGE_LONG, length=6)
        self._ctrl_in(LTW_REQ_SET_TX_SPEED, wValue=LTW_SPEED_2400, length=6)
        self._ctrl_in(LTW_REQ_SET_RX_SPEED, wValue=LTW_SPEED_2400, length=6)
        print(f"  IR mode  : long range, 2400 baud")
        # Copyright
        try:
            d = self._ctrl_in(LTW_REQ_GET_COPYRIGHT, length=48)
            if len(d) > 4:
                print(f"  (C)      : {bytes(d[4:]).decode('ascii','replace').rstrip(chr(0))}")
        except:
            pass
        self._drain()
        print("Tower ready.\n")

    def _drain(self):
        try:
            while True:
                self.ep_in.read(64, timeout=100)
        except:
            pass

    def wait_tx_ready(self, timeout_ms=2000):
        t0 = time.time()
        while (time.time() - t0) * 1000 < timeout_ms:
            try:
                d = self._ctrl_in(LTW_REQ_GET_TX_STATE, length=6)
                if len(d) >= 5 and d[4] == 0x01:
                    return True
            except:
                pass
            time.sleep(0.01)
        return False

    def write(self, data: bytes):
        pkt = self.ep_out.wMaxPacketSize or 8
        off = 0
        while off < len(data):
            self.ep_out.write(data[off:off + pkt], timeout=2000)
            off += pkt
            if off < len(data):
                time.sleep(0.004)

    def read(self, timeout=1500):
        buf = bytearray()
        deadline = time.time() + timeout / 1000.0
        last_rx = time.time()
        while time.time() < deadline:
            try:
                d = self.ep_in.read(64, timeout=50)
                if d:
                    buf.extend(d)
                    last_rx = time.time()
            except usb.core.USBTimeoutError:
                if buf and (time.time() - last_rx) > 0.05:
                    break
            except usb.core.USBError:
                break
        return bytes(buf)


# ===================================================================
# RCX IR protocol
# ===================================================================
class RCXComm:
    def __init__(self, tower: LEGOTower):
        self.tower = tower
        self.toggle = 0  # toggle bit (bit 3 of opcode)

    def _build_packet(self, opcode: int, params: bytes = b"") -> bytes:
        """Build wire packet: preamble + [byte, ~byte]... + checksum pair."""
        # Apply toggle bit
        op = opcode | (self.toggle << 3)
        body = bytes([op]) + params

        pkt = bytearray([0x55, 0xFF, 0x00])
        for b in body:
            pkt.append(b)
            pkt.append(~b & 0xFF)
        cksum = sum(body) & 0xFF
        pkt.append(cksum)
        pkt.append(~cksum & 0xFF)
        return bytes(pkt)

    def _parse_reply(self, raw: bytes):
        """Find preamble, extract data bytes (strip complements + checksum)."""
        idx = 0
        while idx <= len(raw) - 3:
            if raw[idx] == 0x55 and raw[idx+1] == 0xFF and raw[idx+2] == 0x00:
                break
            idx += 1
        else:
            return None

        # Collect byte pairs after preamble
        pairs = []
        i = idx + 3
        while i + 1 < len(raw):
            pairs.append(raw[i])
            i += 2

        if len(pairs) < 2:
            return bytes(pairs) if pairs else None

        # Last pair is checksum — strip it
        return bytes(pairs[:-1])

    def send(self, opcode, params=b"", timeout=2000, retries=4):
        pkt = self._build_packet(opcode, params)
        for attempt in range(retries):
            self.tower._drain()
            self.tower.wait_tx_ready()
            self.tower.write(pkt)
            raw = self.tower.read(timeout=timeout)
            if raw:
                self.toggle ^= 1  # flip toggle on success
                parsed = self._parse_reply(raw)
                return parsed if parsed is not None else raw
            time.sleep(0.25)
        return None

    # ---- commands ----
    def ping(self):
        print("Pinging RCX ...")
        r = self.send(OP_ALIVE)
        if r:
            print(f"  [OK] RCX is alive!  (reply: {r.hex()})")
            return True
        print("  [FAIL] No response.")
        print("    - RCX ON?  IR facing tower?  Close (~10 cm)?")
        return False

    def get_battery(self):
        r = self.send(OP_GET_BATTERY, timeout=3000)
        if r and len(r) >= 3:
            mv = r[1] | (r[2] << 8)
            print(f"  Battery : {mv} mV ({mv/1000:.2f} V)")
            return mv
        elif r:
            print(f"  Battery raw: {r.hex()}")
        return None

    def get_versions(self):
        r = self.send(OP_GET_VERSIONS, b"\x01\x03\x05\x07", timeout=3000)
        if r and len(r) >= 9:
            rom = f"{r[1]}.{r[2]}"
            fw  = f"{r[5]}.{r[6]}"
            print(f"  ROM version : {rom}")
            print(f"  FW  version : {fw}")
        elif r:
            print(f"  Versions raw: {r.hex()}")
        return r

    # ---- firmware upload ----
    def upload_firmware(self, filepath):
        if not os.path.exists(filepath):
            print(f"File not found: {filepath}"); return False

        print(f"Parsing {filepath} ...")
        fw_data, start_addr = parse_srec(filepath)
        fw_len = len(fw_data)

        # Firmware checksum = sum of first min(len, 19456) bytes, mod 65536
        cksum_len = min(fw_len, 19456)
        fw_cksum = sum(fw_data[:cksum_len]) & 0xFFFF
        print(f"  Length   : {fw_len} bytes")
        print(f"  Checksum : 0x{fw_cksum:04X} (over first {cksum_len} bytes)")
        print()

        # 1. Ping
        if not self.ping():
            return False

        # 2. Delete firmware
        print("\n[1/4] Deleting old firmware ...")
        r = self.send(OP_DELETE_FIRMWARE, b"\x01\x03\x05\x07\x0B", timeout=6000)
        print(f"  {'OK' if r else 'WARN - no confirmation'}")
        time.sleep(2)

        # 3. Begin firmware download (all little-endian)
        print(f"\n[2/4] Starting download (addr=0x{start_addr:04X}, cksum=0x{fw_cksum:04X}) ...")
        params = struct.pack("<HH", start_addr, fw_cksum) + b"\x00"
        r = self.send(OP_START_FIRMWARE_DOWNLOAD, params, timeout=5000)
        if r:
            print(f"  OK (reply: {r.hex()})")
        else:
            print("  FAIL - RCX rejected"); return False

        # 4. Transfer data in chunks
        CHUNK_SIZE = 200
        total_chunks = (fw_len + CHUNK_SIZE - 1) // CHUNK_SIZE
        print(f"\n[3/4] Transferring {total_chunks} chunks ({CHUNK_SIZE} bytes each) ...")

        offset = 0
        seq = 1
        while offset < fw_len:
            chunk = fw_data[offset:offset + CHUNK_SIZE]
            remaining = fw_len - offset - len(chunk)

            # Seq: 1-based; last chunk uses seq=0
            this_seq = seq if remaining > 0 else 0

            # Data checksum: sum of data bytes only, mod 256
            data_cksum = sum(chunk) & 0xFF

            # Params: seq(LE16) + length(LE16) + data + data_checksum
            params = struct.pack("<HH", this_seq, len(chunk)) + chunk + bytes([data_cksum])

            r = self.send(OP_TRANSFER_DATA, params, timeout=5000, retries=5)
            pct = min(100, int((offset + len(chunk)) / fw_len * 100))
            if r:
                print(f"\r  [{pct:3d}%] chunk {seq}/{total_chunks} "
                      f"(seq={this_seq}, {offset+len(chunk)}/{fw_len})", end="", flush=True)
            else:
                print(f"\n  FAIL at chunk {seq} (offset {offset})"); return False

            seq += 1
            offset += len(chunk)
            time.sleep(0.02)

        print()

        # 5. Unlock firmware
        print("\n[4/4] Unlocking firmware ...")
        unlock_key = b"\x4C\x45\x47\x4F\xAE"  # "LEGO" + ®
        r = self.send(OP_UNLOCK_FIRMWARE, unlock_key, timeout=15000, retries=1)
        if r:
            print(f"  OK (reply: {r.hex()})")
        else:
            print("  No confirmation (RCX may be processing - this can be normal)")

        print("\n=== Firmware upload complete! ===")
        print("The RCX should beep and show the running-man icon.")
        return True

    # ---- program download ----
    def _download_fragment(self, ftype, findex, bytecode):
        """Download a single task or subroutine to the RCX."""
        ftype_name = {0: "task", 1: "sub"}.get(ftype, f"type{ftype}")
        opcode = OP_START_TASK_DOWNLOAD if ftype == 0 else OP_START_SUB_DOWNLOAD

        # BeginTask/Sub: 0x00, index, 0x00, len_lo, len_hi (5 param bytes)
        params = bytes([0x00, findex, 0x00]) + struct.pack("<H", len(bytecode))
        print(f"  Starting {ftype_name} #{findex} download ({len(bytecode)} bytes) ...")
        r = self.send(opcode, params, timeout=3000)
        if not r:
            print(f"  FAIL - RCX rejected {ftype_name} #{findex} begin")
            return False
        status = r[0] if r else 0xFF
        if status != 0xDA and status != 0xCA and len(r) > 1:
            # Check if second byte indicates error
            pass
        print(f"    Begin OK (reply: {r.hex()})")

        # Transfer bytecode in 20-byte chunks
        CHUNK = 20
        total = (len(bytecode) + CHUNK - 1) // CHUNK
        seq = 1
        off = 0
        while off < len(bytecode):
            chunk = bytecode[off:off + CHUNK]
            remaining = len(bytecode) - off - len(chunk)
            this_seq = seq if remaining > 0 else 0
            data_cksum = sum(chunk) & 0xFF
            params = struct.pack("<HH", this_seq, len(chunk)) + chunk + bytes([data_cksum])
            r = self.send(OP_TRANSFER_DATA, params, timeout=5000, retries=5)
            pct = min(100, int((off + len(chunk)) / len(bytecode) * 100))
            if r:
                print(f"\r    [{pct:3d}%] chunk {seq}/{total}", end="", flush=True)
            else:
                print(f"\n    FAIL at chunk {seq}"); return False
            seq += 1
            off += len(chunk)
            time.sleep(0.02)
        print()
        return True

    def download_program(self, filepath, slot=1):
        """Download a compiled .rcx program to the RCX."""
        if not os.path.exists(filepath):
            print(f"File not found: {filepath}"); return False

        print(f"Parsing {filepath} ...")
        fragments = parse_rcxi(filepath)
        if not fragments:
            print("  No fragments found!"); return False
        print()

        if not self.ping():
            return False

        # Stop any running programs
        print("\nStopping RCX ...")
        self.send(0x50)  # StopAll

        # Select program slot (1..5)
        if slot and slot > 0:
            self.select_slot(slot)

        # Delete existing tasks and subs
        print("Deleting old tasks ...")
        self.send(OP_DELETE_ALL_TASKS, timeout=3000)
        print("Deleting old subs ...")
        self.send(OP_DELETE_ALL_SUBS, timeout=3000)

        # Download each fragment
        for ftype, findex, bytecode in fragments:
            if ftype > 1:
                print(f"  Skipping fragment type {ftype} (not task/sub)")
                continue
            if not self._download_fragment(ftype, findex, bytecode):
                return False

        # Play completion sound
        print("\nPlaying completion sound ...")
        self.send(0x51, bytes([0x05]))  # PlaySound(5)

        print(f"\n=== Program downloaded successfully! ===")
        print("Press Run on the RCX to execute.")
        return True

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
        if self.send(OP_START_TASK, b"\x00") is None:
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

    def vll(self, code, toggle=0):
        """Scout -> Micro Scout. Opcode 0xE2 (0xEA on every second send), source 2
        (a constant), then the VLL code. The codes are BricxCC's MSVLL_* values."""
        op = 0xE2 | (0x08 if int(toggle) else 0)
        # send_noreply ORs in its own toggle bit, which starts at 0 in a new process
        self.send_noreply(op, bytes([0x02, int(code) & 0xFF]))
        print(f"  VLL code {int(code)} sent.")
        return True

    def datalog(self, count=None):
        """Entry 0 holds the number of entries, so read it first."""
        head = self.send(OP_UPLOAD_DATALOG, b"\x00\x00\x01\x00", timeout=4000)
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



# ===================================================================
def main():
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
    elif cmd == "vll":
        if arg(2) is None:
            print("Usage: rcx_tool.py vll <code> [toggle]   e.g. 0 (motor forward)"); sys.exit(1)
        ok = rcx.vll(arg(2), arg(3, 0))
    elif cmd == "datalog":
        ok = rcx.datalog(arg(2))
    elif cmd == "remote":
        if arg(2) is None:
            print("Usage: rcx_tool.py remote <hex>   e.g. 0800"); sys.exit(1)
        ok = rcx.remote(arg(2))
    else:
        print(f"Unknown: {cmd}"); print(__doc__); sys.exit(1)

    sys.exit(0 if ok else 1)

if __name__ == "__main__":
    main()
