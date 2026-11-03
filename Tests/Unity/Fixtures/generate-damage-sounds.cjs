// Original, deterministic camera-shake thumps; no external audio or dependencies.
const fs = require('node:fs');
const path = require('node:path');
const output = path.resolve(__dirname, '../../../Assets/Resources/DamageFeedback');
const rate = 44100;

function synthesize(name, duration, strong) {
    let seed = strong ? 1749 : 381;
    const random = () => {
        seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
        return seed / 4294967296;
    };
    const count = Math.round(duration * rate);
    const samples = new Float64Array(count);
    let lowNoise = 0;
    let phase = 0;
    let peak = 0;
    for (let i = 0; i < count; i++) {
        const time = i / rate;
        const progress = time / duration;
        lowNoise += 0.035 * (random() * 2 - 1 - lowNoise);
        const frequency = (strong ? 49 : 76) + (strong ? 48 : 59) * Math.exp(-time * 22);
        phase += Math.PI * 2 * frequency / rate;
        const attack = Math.min(1, time / 0.008);
        const tail = Math.sin(Math.PI * Math.min(1, (1 - progress) / 0.25) / 2) ** 2;
        const envelope = attack * Math.exp(-time * (strong ? 6.7 : 13.5)) * tail;
        const tone = Math.sin(phase) + 0.22 * Math.sin(phase * 0.5);
        samples[i] = (tone * (strong ? 0.62 : 0.5) + lowNoise * (strong ? 1.2 : 0.65)) * envelope;
        peak = Math.max(peak, Math.abs(samples[i]));
    }
    const gain = (strong ? 0.82 : 0.68) / peak;
    const wav = Buffer.alloc(44 + count * 2);
    wav.write('RIFF', 0);
    wav.writeUInt32LE(wav.length - 8, 4);
    wav.write('WAVEfmt ', 8);
    wav.writeUInt32LE(16, 16);
    wav.writeUInt16LE(1, 20);
    wav.writeUInt16LE(1, 22);
    wav.writeUInt32LE(rate, 24);
    wav.writeUInt32LE(rate * 2, 28);
    wav.writeUInt16LE(2, 32);
    wav.writeUInt16LE(16, 34);
    wav.write('data', 36);
    wav.writeUInt32LE(count * 2, 40);
    for (let i = 0; i < count; i++) wav.writeInt16LE(Math.round(samples[i] * gain * 32767), 44 + i * 2);
    fs.writeFileSync(path.join(output, name), wav);
}

fs.mkdirSync(output, { recursive: true });
synthesize('ShakeSmall.wav', 0.28, false);
synthesize('ShakeBig.wav', 0.62, true);
