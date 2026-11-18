const fs = require("node:fs");
const path = require("node:path");

const rate = 24000;
const tau = 2 * Math.PI;
const out = path.join(__dirname, "..", "Assets", "_Project", "Audio", "Campaign");
fs.mkdirSync(out, { recursive: true });

let seed = 38192;
function noise() {
  seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
  return seed / 2147483648 - 1;
}

const sine = (frequency, time) => Math.sin(tau * frequency * time);
const decay = (time, speed) => Math.exp(-time * speed);

function sample(name, seconds, voice, loop = false) {
  const count = Math.round(rate * seconds);
  const file = Buffer.alloc(44 + count * 2);
  file.write("RIFF", 0);
  file.writeUInt32LE(file.length - 8, 4);
  file.write("WAVEfmt ", 8);
  file.writeUInt32LE(16, 16);
  file.writeUInt16LE(1, 20);
  file.writeUInt16LE(1, 22);
  file.writeUInt32LE(rate, 24);
  file.writeUInt32LE(rate * 2, 28);
  file.writeUInt16LE(2, 32);
  file.writeUInt16LE(16, 34);
  file.write("data", 36);
  file.writeUInt32LE(count * 2, 40);

  let lowNoise = 0;
  for (let i = 0; i < count; i++) {
    const t = i / rate;
    const white = noise();
    lowNoise = lowNoise * 0.96 + white * 0.04;
    let value = voice(t, white, lowNoise);
    if (!loop) value *= Math.min(t * 500, 1, (seconds - t) * 60);
    file.writeInt16LE(Math.round(Math.max(-1, Math.min(1, value)) * 30000), 44 + i * 2);
  }
  fs.writeFileSync(path.join(out, name + ".wav"), file);
}

function impact(frequencies, speed) {
  return (t, w, n) => (frequencies.reduce((sum, f, i) =>
    sum + sine(f, t) / (i + 1), 0) * 0.35 + w * 0.35 + n * 0.1) * decay(t, speed);
}

function signal(notes, stride, duration) {
  return (t) => {
    const index = Math.floor(t / stride);
    const note = notes[index];
    const local = t % stride;
    if (!note || local > duration) return 0;
    const gate = Math.min(1, local * 160, (duration - local) * 65);
    return gate * (0.5 * sine(note, local) + 0.16 * sine(note * 1.5, local));
  };
}

function transmission(pitch, pattern) {
  return (t, w, n) => {
    const index = Math.floor(t / 0.16);
    const formant = pattern[index];
    if (!formant) return 0;
    const v = t % 0.16;
    const gate = Math.min(1, v * 72, (0.16 - v) * 40);
    const carrier = Math.sign(sine(pitch, t)) * 0.2 + sine(pitch * 2, t) * 0.12;
    return gate * (carrier * (sine(formant, t) + 0.48 * sine(formant * 2.3, t)) + n * 0.6 + w * 0.025);
  };
}

sample("current", 8, t => 0.24 * sine(60, t) + (0.11 + 0.04 * sine(0.375, t)) * sine(120, t), true);
sample("ventilation", 8, t => (0.2 + 0.07 * sine(0.25, t)) * (sine(31.375, t) + 0.38 * sine(55.125, t)), true);
sample("building", 16, t => 0.28 * sine(22.3125, t) + 0.19 * sine(22.625, t) + 0.08 * sine(46.75, t), true);
function campusScore(seconds, root, beatInterval, activity) {
  const texturePitch = root * 3 - 0.25;
  const harmonics = [root * 2.5, root * 3.75 + 0.125, root * 6.5];
  return (t, w, n) => {
    const slow = 0.48 + 0.45 * sine(1 / seconds, t);
    const swell = 0.44 + 0.38 * sine(2 / seconds, t + seconds / 8);
    const drop = 0.52 + 0.41 * sine(1 / seconds, t + seconds * 0.38);
    const bass = 0.23 * sine(root, t) + 0.055 * sine(root / 2, t);
    const hum = 0.11 * sine(texturePitch, t) * sine(8 / seconds, t);
    const pulseAge = t % beatInterval;
    const pulse = activity * (1 - decay(pulseAge, 60)) * decay(pulseAge, 5.6) *
      (0.20 * sine(root, t) + 0.065 * sine(root * 3, t));
    const ringAge = (t + 1.25) % (seconds / 3);
    const ring = harmonics.reduce((sum, pitch, index) => sum + sine(pitch, t) / (index + 1), 0)
      * .14 * decay(ringAge, 4) * (1 - decay(ringAge, 110));
    const bleed = (0.065 * n + .012 * w) * swell * slow;
    return (bass * (.44 + .56 * drop) + hum * swell + pulse + ring + bleed) *
      Math.min(1, t * 20, (seconds - t) * 20);
  };
}

sample("menu", 48, campusScore(48, 52, 4, .48), true);
sample("exploration", 36, campusScore(36, 44, 6, .19), true);
sample("tension", 24, campusScore(24, 50, 2, .8), true);
sample("chase", 24, campusScore(24, 58, .75, 1), true);
sample("core", 36, campusScore(36, 42, 1.5, .78), true);
sample("discovery", 12, campusScore(12, 62, 3, .45), true);
sample("victory", 24, campusScore(24, 48, 8, .08), true);
sample("defeat", 16, campusScore(16, 36, 4, .22), true);
sample("focus", .14, signal([317], .14, .12));
sample("paper", .54, (t, w, n) => (w * .34 + n) * decay(t, 4) *
  (0.35 + .65 * Math.abs(sine(7, t))));
sample("terminal", .36, (t, w) => signal([229, 343, 288], .10, .08)(t) + w * .08 * decay(t, 14));
sample("inventory", .34, impact([76, 159, 297], 12));
sample("attack", .68, impact([49, 64, 992], 8));
sample("steps_floor", 0.22, impact([82, 158], 19));
sample("steps_tile", 0.2, impact([147, 367, 830], 24));
sample("button", 0.09, impact([350, 1100], 52));
sample("switch", 0.19, impact([166, 578, 1200], 31));
sample("pickup", 0.36, signal([466, 622, 785], 0.095, 0.085));
sample("access_ok", 0.45, signal([392, 587, 784], 0.13, 0.12));
sample("access_error", 0.53, signal([223, 174, 139], 0.16, 0.145));
sample("power", 0.76, (t, w, n) => (sine(42 + 250 * t, t) * 0.45 + n + w * 0.3) * decay(t, 4));
sample("flicker", 0.3, (t, w) => (sine(119, t) * 0.22 + w * 0.55) * decay(t, 17) * (sine(14, t) > 0 ? 1 : 0.22));
sample("blackout", 0.72, impact([44, 62, 108], 7));
sample("lock", 0.33, t => (sine(82, t) * 0.37 + sine(484, t) * 0.2) * decay(t % 0.14, 32));
sample("door", 0.75, (t, w, n) => (0.4 * n + w * 0.08 + sine(120 - t * 56, t) * 0.34) * decay(t, 4));
sample("servo", 0.66, (t, w) => (sine(167 + 155 * t, t) * 0.33 + sine(91, t) * 0.15 + w * 0.14) * decay(t, 3.9));
sample("robot_alert", 0.77, signal([380, 512, 438, 0], 0.18, 0.15));
sample("robot_chase", 0.8, (t, w) => (sine(98 + 22 * sine(5, t), t) * 0.4 + w * 0.22) * decay(t, 3.1));
sample("interference", 0.7, (t, w) => (w * 0.67 + sine(240 + 90 * sine(16, t), t) * 0.31) * decay(t, 6));
sample("distant_impact", 1.05, impact([34, 61, 143], 6));
sample("alarm", 2.4, (t, w) => (sine(510 + 15 * sine(1.25, t), t) * 0.35 + w * 0.08) * (0.45 + 0.4 * sine(1.25, t)) * decay(t, 0.8));
sample("can_throw", 0.2, (t, w, n) => (w * 0.28 + n * 0.7) * decay(t, 11));
sample("can_impact", 0.84, (t, w) => {
  const rings = (x) => x < 0 ? 0 : (sine(987, x) + 0.6 * sine(1533, x)) * decay(x, 43);
  return 0.35 * rings(t) + 0.19 * rings(t - 0.18) + 0.13 * rings(t - 0.29) + 0.2 * w * decay(t, 22);
});
sample("francis_broadcast", 3.04, transmission(116, [510, 420, 760, 380, 0, 610, 400, 750, 320, 0, 0, 680, 420, 340, 760, 510, 605, 310, 0]));
sample("francis_farewell", 3.2, transmission(97, [710, 530, 410, 0, 550, 490, 290, 0, 0, 690, 510, 630, 770, 0, 700, 400, 330, 230, 0, 0]));
