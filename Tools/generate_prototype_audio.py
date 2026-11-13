"""AI-authored temporary synthesized SFX. No samples or third-party audio.
Replaces WAV bytes only; existing Unity .meta files must be retained.
"""
from pathlib import Path
import math
import struct
import wave

ROOT = Path(__file__).resolve().parents[1]
NOTES = {
    "match-short": [523], "match-middle": [659, 784],
    "match-long": [659, 784, 1047], "item": [880, 1175],
    "clear": [523, 659, 784, 1047], "lose": [330, 262, 196], "hit": [150, 95],
}

def generate():
    output = ROOT / "Assets/Resources/Audio/Prototype"
    output.mkdir(parents=True, exist_ok=True)
    for name, notes in NOTES.items():
        rate = 22050
        length = .09 if "match" in name else .13
        data = []
        for frequency in notes:
            for i in range(int(rate * length)):
                t = i / rate
                envelope = min(1, t / .006) * math.exp(-t * 28)
                value = math.sin(2 * math.pi * frequency * t) + .2 * math.sin(4 * math.pi * frequency * t)
                data.append(int(32767 * .25 * envelope * value))
        with wave.open(str(output / (name + ".wav")), "wb") as audio:
            audio.setnchannels(1)
            audio.setsampwidth(2)
            audio.setframerate(rate)
            audio.writeframes(struct.pack("<" + "h" * len(data), *data))

if __name__ == "__main__":
    generate()
