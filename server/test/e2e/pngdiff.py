"""Prints how many pixels differ noticeably between two PNGs (used by effects.mjs). Needs Pillow or falls back to sips+raw."""
import sys
try:
    from PIL import Image, ImageChops
    a = Image.open(sys.argv[1]).convert("RGB"); b = Image.open(sys.argv[2]).convert("RGB")
    d = ImageChops.difference(a, b).convert("L").point(lambda v: 255 if v > 40 else 0)
    print(sum(1 for v in d.getdata() if v))
except ImportError:
    import zlib, struct
    def load(p):
        data = open(p, "rb").read()[8:]; pos = 0; idat = b""; w = h = ct = 0
        while pos < len(data):
            ln, = struct.unpack(">I", data[pos:pos + 4]); t = data[pos + 4:pos + 8]; body = data[pos + 8:pos + 8 + ln]; pos += 12 + ln
            if t == b"IHDR": w, h, bd, ct = struct.unpack(">IIBB", body[:10])
            elif t == b"IDAT": idat += body
        raw = zlib.decompress(idat); bpp = 4 if ct == 6 else 3; stride = w * bpp; out = bytearray(); prev = bytearray(stride); o = 0
        for _ in range(h):
            f = raw[o]; line = bytearray(raw[o + 1:o + 1 + stride]); o += 1 + stride
            for i in range(stride):
                a_ = line[i - bpp] if i >= bpp else 0; b_ = prev[i]; c_ = prev[i - bpp] if i >= bpp else 0
                if f == 1: line[i] = (line[i] + a_) & 255
                elif f == 2: line[i] = (line[i] + b_) & 255
                elif f == 3: line[i] = (line[i] + (a_ + b_) // 2) & 255
                elif f == 4:
                    p = a_ + b_ - c_; pa = abs(p - a_); pb = abs(p - b_); pc = abs(p - c_)
                    line[i] = (line[i] + (a_ if pa <= pb and pa <= pc else b_ if pb <= pc else c_)) & 255
            out += line; prev = line
        return w, h, bpp, out
    w, h, bpp, A = load(sys.argv[1]); _, _, _, B = load(sys.argv[2])
    n = 0
    for i in range(0, w * h * bpp, bpp):
        if abs(A[i] - B[i]) + abs(A[i + 1] - B[i + 1]) + abs(A[i + 2] - B[i + 2]) > 40: n += 1
    print(n)
