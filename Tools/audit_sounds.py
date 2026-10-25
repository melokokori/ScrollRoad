"""Ortam seslerinin nesnel denetimi (2026-10-25): kulakla dinlenmemiş sesler için ölçüm + spektrogram.

  python Tools/audit_sounds.py                 # Resources/EnvSounds'taki tüm .ogg
  python Tools/audit_sounds.py peel_loop ...   # yalnız bunlar
  python Tools/audit_sounds.py --png out.png   # spektrogram ızgarası

Ölçülenler: süre, tepe (dBFS), bütünleşik ses yüksekliği (LUFS), DC kayması, 8 kHz üstü enerji payı,
spektral ağırlık merkezi, tık sayısı (türevde ani sıçrama) ve döngülerde dikiş sıçraması (son örnekten
ilk örneğe geçişin, sesin olağan örnekten örneğe değişimine oranı). Uyarı eşikleri satır sonunda.
"""
import argparse
import json
import os
import subprocess
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV = os.path.join(ROOT, "Assets", "Resources", "EnvSounds")
SR = 44100

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_sound import ENV_SOUNDS  # noqa: E402


def decode(path):
    pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", "1", "-ar", str(SR), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(pcm, dtype=np.float32).copy()


def lufs(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", path, "-af", "loudnorm=print_format=json",
                          "-f", "null", "-"], capture_output=True).stderr.decode("utf-8", "replace")
    return float(json.loads(out[out.rindex("{"):out.rindex("}") + 1])["input_i"])


def measure(name):
    path = os.path.join(ENV, name + ".ogg")
    x = decode(path)
    loop = ENV_SOUNDS.get(name, (False,))[0]
    d = np.abs(np.diff(x))
    typical = np.percentile(d, 99) or 1e-9
    clicks = int(np.sum(d > 8 * typical))
    spec = np.abs(np.fft.rfft(x * np.hanning(len(x)))) ** 2
    f = np.fft.rfftfreq(len(x), 1 / SR)
    hf = float(spec[f > 8000].sum() / spec.sum())
    centroid = float((f * spec).sum() / spec.sum())
    seam = float(abs(x[0] - x[-1]) / (np.median(d) + 1e-9)) if loop else 0.0
    peak = 20 * np.log10(np.abs(x).max() + 1e-12)
    m = {
        "name": name, "loop": loop, "sec": len(x) / SR, "peak": peak, "lufs": lufs(path),
        "dc": float(x.mean()), "hf": hf, "centroid": centroid, "clicks": clicks, "seam": seam,
    }
    warn = []
    if peak > -0.5:
        warn.append("tepe kırpılmaya yakın")
    if abs(m["dc"]) > 0.01:
        warn.append("DC kayması")
    if hf > 0.25:
        warn.append("tiz ağır (cızırtı?)")
    if loop and seam > 20:
        warn.append("döngü dikişinde sıçrama")
    if clicks > 0 and loop:
        warn.append(f"{clicks} ani sıçrama")
    if m["lufs"] > (-24 if loop else -20):
        warn.append("yüksek")
    m["warn"] = warn
    return m, x


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("names", nargs="*")
    ap.add_argument("--png")
    args = ap.parse_args()
    names = args.names or sorted(f[:-4] for f in os.listdir(ENV) if f.endswith(".ogg"))
    rows = []
    for name in names:
        m, x = measure(name)
        rows.append((m, x))
        print(f"{name:20s} {'döngü' if m['loop'] else 'tek  '} {m['sec']:5.2f}s  tepe {m['peak']:6.1f}  "
              f"{m['lufs']:6.1f} LUFS  >8k {m['hf'] * 100:4.1f}%  merkez {m['centroid']:6.0f} Hz  "
              f"tık {m['clicks']:3d}  dikiş {m['seam']:5.1f}  {'; '.join(m['warn'])}")
    if args.png:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        cols = 3
        rws = (len(rows) + cols - 1) // cols
        fig, axes = plt.subplots(rws, cols, figsize=(cols * 4.2, rws * 1.9), squeeze=False)
        for ax in axes.flat:
            ax.axis("off")
        for ax, (m, x) in zip(axes.flat, rows):
            ax.axis("on")
            ax.specgram(x, NFFT=1024, Fs=SR, noverlap=512, cmap="magma", vmin=-130)
            ax.set_ylim(0, 12000)
            ax.set_title(f"{m['name']} ({m['lufs']:.0f} LUFS)", fontsize=8)
            ax.tick_params(labelsize=6)
        fig.tight_layout()
        fig.savefig(args.png, dpi=90)
        print("->", args.png)


if __name__ == "__main__":
    main()
