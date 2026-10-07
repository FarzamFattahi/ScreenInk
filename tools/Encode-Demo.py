"""Encode generated ScreenInk frames. Requires Pillow; never captures a screen."""
from pathlib import Path
import argparse
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument("frames", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
frames = []
durations = []
previous = None
for path in sorted(args.frames.glob("frame-*.png")):
    with Image.open(path) as source:
        rgb = source.convert("RGB")
        pixels = rgb.tobytes()
        if pixels == previous:
            durations[-1] += 100
            continue
        previous = pixels
        frames.append(rgb.quantize(colors=128))
        durations.append(100)
if not frames:
    raise SystemExit("No frame-*.png files found")
args.output.parent.mkdir(parents=True, exist_ok=True)
frames[0].save(args.output, save_all=True, append_images=frames[1:],
               duration=durations, loop=0, optimize=True, disposal=2)
print(f"Saved {args.output}: {len(frames)} distinct frames, {sum(durations)/1000:.1f}s")
