"""Parse an RCXI file and print its structure."""
import sys

def read2(d, off):
    return d[off] | (d[off+1] << 8)

def read4(d, off):
    return d[off] | (d[off+1]<<8) | (d[off+2]<<16) | (d[off+3]<<24)

fname = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\server\lego-tools\nqc\test.rcx"
d = open(fname, 'rb').read()
print(f"File: {fname}")
print(f"File size: {len(d)} bytes")
print(f"Raw hex: {' '.join(f'{b:02x}' for b in d)}")
print()

# Header
print("=" * 60)
print("HEADER (12 bytes, offsets 0-11)")
print("=" * 60)
sig = d[0:4]
ver = read2(d, 4)
frag_count = read2(d, 6)
sym_count = read2(d, 8)
target_type = d[10]
reserved = d[11]

target_names = {0:"RCX", 1:"CyberMaster", 2:"Scout", 3:"RCX2", 4:"Spybotics", 5:"Swan"}
print(f"  [0-3]   Signature:      {sig} (0x{read4(d,0):08x})")
print(f"  [4-5]   Version:        0x{ver:04x} (v{ver>>8}.{ver&0xff:02d})")
print(f"  [6-7]   FragmentCount:  {frag_count}")
print(f"  [8-9]   SymbolCount:    {sym_count}")
print(f"  [10]    TargetType:     {target_type} ({target_names.get(target_type, '?')})")
print(f"  [11]    Reserved:       {reserved}")
print()

# Fragments
off = 12
chunk_names = {0: "Task", 1: "Sub", 2: "Sound", 3: "Animation"}
for i in range(frag_count):
    print("=" * 60)
    print(f"FRAGMENT {i} (header at offset {off})")
    print("=" * 60)
    ftype = d[off]
    fnumber = d[off+1]
    flen = read2(d, off+2)
    print(f"  [{off}]     Type:       {ftype} ({chunk_names.get(ftype, '?')})")
    print(f"  [{off+1}]     Number:     {fnumber}")
    print(f"  [{off+2}-{off+3}]   DataLen:    {flen} (0x{flen:04x})")
    
    data_start = off + 4
    data_end = data_start + flen
    print(f"  Data at offsets {data_start}-{data_end-1}:")
    
    # Print data in rows of 16
    for row in range(0, flen, 16):
        chunk = d[data_start+row:data_start+min(row+16, flen)]
        hex_str = ' '.join(f'{b:02x}' for b in chunk)
        print(f"    {row:04x}: {hex_str}")
    
    pad = (4 - (flen % 4)) % 4
    print(f"  Padding: {pad} bytes")
    off = data_end + pad
    print()

# Symbols
print("=" * 60)
print(f"SYMBOL TABLE ({sym_count} symbols, starting at offset {off})")
print("=" * 60)
sym_type_names = {0: "Task", 1: "Sub", 2: "Variable"}
for i in range(sym_count):
    stype = d[off]
    sindex = d[off+1]
    slen = d[off+2]
    sreserved = d[off+3]
    name_bytes = d[off+4:off+4+slen]
    name = name_bytes.decode('ascii', errors='replace').rstrip('\x00')
    print(f"  Symbol {i}: type={stype} ({sym_type_names.get(stype,'?')}), "
          f"index={sindex}, nameLen={slen}, name=\"{name}\"")
    print(f"    Header at [{off}-{off+3}]: {' '.join(f'{b:02x}' for b in d[off:off+4])}")
    print(f"    Name at [{off+4}-{off+4+slen-1}]: {' '.join(f'{b:02x}' for b in name_bytes)}")
    off = off + 4 + slen

print()
print(f"End of file at offset {off} (file size = {len(d)})")
