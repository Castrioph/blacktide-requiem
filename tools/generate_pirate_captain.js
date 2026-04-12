const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const OUT_DIR = path.join(process.cwd(), 'Assets', 'Sprites', 'PirateCaptain');
const SCALE = 4;
const LOW_W = 32;
const LOW_H = 32;
const FRAME_W = LOW_W * SCALE; // 128
const FRAME_H = LOW_H * SCALE; // 128
const SHEET_W = FRAME_W * 4; // 512
const SHEET_H = FRAME_H * 2; // 256
const FRAME_COUNT = 8;
const TARGET_CENTER_X = 15.5;
const TARGET_BASELINE_Y = 27;

const PALETTE_HEX = [
  '#0b0f14',
  '#1f2933',
  '#3a506b',
  '#5bc0be',
  '#f2f7f2',
  '#ffbf69',
  '#ee6c4d',
  '#6b705c',
  '#a5a58d',
  '#b7b7a4',
  '#d4a373',
  '#588157',
  '#3a5a40',
  '#e0fbfc',
  '#293241',
  '#98c1d9',
];

const C = {
  outline: '#0b0f14',
  coat: '#3a506b',
  coatShade: '#1f2933',
  trim: '#ffbf69',
  shirt: '#f2f7f2',
  skin: '#d4a373',
  beard: '#ee6c4d',
  hat: '#293241',
  hatShade: '#1f2933',
  hatMark: '#f2f7f2',
  boots: '#6b705c',
  bootsShade: '#3a5a40',
  belt: '#a5a58d',
  buckle: '#ffbf69',
  eye: '#0b0f14',
  accent: '#5bc0be',
  blade: '#e0fbfc',
  cloth: '#98c1d9',
};

function hexToRgba(hex) {
  const h = hex.replace('#', '');
  const v = parseInt(h, 16);
  return [(v >> 16) & 255, (v >> 8) & 255, v & 255, 255];
}

const PALETTE = new Map(PALETTE_HEX.map((hex) => [hex, hexToRgba(hex)]));

function createPixelGrid(w, h) {
  const data = new Int16Array(w * h);
  data.fill(-1);
  return { w, h, data };
}

function colorIndex(hex) {
  const idx = PALETTE_HEX.indexOf(hex);
  if (idx < 0) throw new Error(`Color not in palette: ${hex}`);
  return idx;
}

function pset(grid, x, y, hex) {
  if (x < 0 || y < 0 || x >= grid.w || y >= grid.h) return;
  grid.data[y * grid.w + x] = colorIndex(hex);
}

function rect(grid, x, y, w, h, hex) {
  for (let yy = y; yy < y + h; yy += 1) {
    for (let xx = x; xx < x + w; xx += 1) {
      pset(grid, xx, yy, hex);
    }
  }
}

function line(grid, x0, y0, x1, y1, hex) {
  let dx = Math.abs(x1 - x0);
  let sx = x0 < x1 ? 1 : -1;
  let dy = -Math.abs(y1 - y0);
  let sy = y0 < y1 ? 1 : -1;
  let err = dx + dy;

  while (true) {
    pset(grid, x0, y0, hex);
    if (x0 === x1 && y0 === y1) break;
    const e2 = err * 2;
    if (e2 >= dy) {
      err += dy;
      x0 += sx;
    }
    if (e2 <= dx) {
      err += dx;
      y0 += sy;
    }
  }
}

function measureOpaqueBounds(grid) {
  let minX = grid.w;
  let minY = grid.h;
  let maxX = -1;
  let maxY = -1;

  for (let y = 0; y < grid.h; y += 1) {
    for (let x = 0; x < grid.w; x += 1) {
      if (grid.data[y * grid.w + x] < 0) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }

  if (maxX < 0) {
    throw new Error('Frame contains no opaque pixels');
  }

  return { minX, minY, maxX, maxY };
}

function translateGrid(grid, dx, dy) {
  const out = createPixelGrid(grid.w, grid.h);
  for (let y = 0; y < grid.h; y += 1) {
    for (let x = 0; x < grid.w; x += 1) {
      const idx = grid.data[y * grid.w + x];
      if (idx < 0) continue;
      const nx = x + dx;
      const ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= grid.w || ny >= grid.h) {
        throw new Error(`Translated pixel out of bounds at (${nx}, ${ny})`);
      }
      out.data[ny * grid.w + nx] = idx;
    }
  }
  return out;
}

function addOutline(grid) {
  const outlineIdx = colorIndex(C.outline);
  const out = createPixelGrid(grid.w, grid.h);
  out.data.set(grid.data);

  for (let y = 0; y < grid.h; y += 1) {
    for (let x = 0; x < grid.w; x += 1) {
      const idx = y * grid.w + x;
      if (grid.data[idx] >= 0) continue;

      const neighbors = [
        [x - 1, y],
        [x + 1, y],
        [x, y - 1],
        [x, y + 1],
        [x - 1, y - 1],
        [x + 1, y - 1],
        [x - 1, y + 1],
        [x + 1, y + 1],
      ];

      for (const [nx, ny] of neighbors) {
        if (nx < 0 || ny < 0 || nx >= grid.w || ny >= grid.h) continue;
        if (grid.data[ny * grid.w + nx] >= 0) {
          out.data[idx] = outlineIdx;
          break;
        }
      }
    }
  }

  return out;
}

function alignFrame(grid) {
  const bounds = measureOpaqueBounds(grid);
  const centerX = (bounds.minX + bounds.maxX) / 2;
  const dx = Math.round(TARGET_CENTER_X - centerX);
  const dy = TARGET_BASELINE_Y - bounds.maxY;
  return translateGrid(grid, dx, dy);
}

function drawCaptain(frame) {
  const g = createPixelGrid(LOW_W, LOW_H);

  const cycle = (frame / FRAME_COUNT) * Math.PI * 2;
  const legSwing = Math.round(Math.sin(cycle) * 2);
  const armSwing = -legSwing;

  const backFootX = 12 - legSwing;
  const frontFootX = 18 + legSwing;
  const backFootY = 26 - Math.max(0, legSwing);
  const frontFootY = 26 - Math.max(0, -legSwing);
  const backKneeX = 13 - Math.round(legSwing / 2);
  const frontKneeX = 17 + Math.round(legSwing / 2);
  const backKneeY = 23 - Math.max(0, legSwing > 1 ? 1 : 0);
  const frontKneeY = 23 - Math.max(0, legSwing < -1 ? 1 : 0);

  const handX = 19 + armSwing;
  const handY = 16 + (armSwing > 0 ? 1 : 0);
  const rearHandX = 12 - Math.max(0, -armSwing);
  const rearHandY = 17 + (armSwing < 0 ? 1 : 0);

  rect(g, 10, 6, 10, 2, C.hat);
  rect(g, 12, 3, 6, 3, C.hatShade);
  rect(g, 10, 2, 2, 3, C.accent);
  pset(g, 18, 6, C.hat);
  pset(g, 19, 6, C.hat);
  pset(g, 15, 4, C.hatMark);
  pset(g, 15, 5, C.hatMark);

  rect(g, 14, 8, 4, 4, C.skin);
  pset(g, 18, 9, C.skin);
  pset(g, 18, 10, C.skin);
  rect(g, 14, 10, 4, 3, C.beard);
  pset(g, 18, 11, C.beard);
  pset(g, 16, 9, C.eye);

  rect(g, 11, 12, 8, 8, C.coat);
  rect(g, 10, 16, 4, 6, C.coatShade);
  rect(g, 15, 13, 2, 4, C.shirt);
  rect(g, 17, 12, 2, 8, C.trim);
  rect(g, 10, 19, 4, 3, C.coat);
  pset(g, 10, 22, C.coat);
  rect(g, 11, 18, 8, 1, C.belt);
  pset(g, 15, 18, C.buckle);
  pset(g, 16, 18, C.buckle);

  line(g, 13, 14, rearHandX, rearHandY, C.coatShade);
  pset(g, rearHandX, rearHandY, C.skin);

  line(g, 17, 14, handX, handY, C.coat);
  pset(g, handX, handY, C.skin);
  pset(g, handX + 1, handY, C.trim);
  line(g, handX + 1, handY - 1, handX + 4, handY - 3, C.blade);
  pset(g, handX + 2, handY - 2, C.accent);

  line(g, 13, 20, backKneeX, backKneeY, C.bootsShade);
  line(g, backKneeX, backKneeY, backFootX, backFootY - 1, C.bootsShade);
  rect(g, backFootX - 1, backFootY, 3, 1, C.bootsShade);
  pset(g, backFootX + 1, backFootY, C.boots);

  line(g, 17, 20, frontKneeX, frontKneeY, C.boots);
  line(g, frontKneeX, frontKneeY, frontFootX, frontFootY - 1, C.boots);
  rect(g, frontFootX - 1, frontFootY, 3, 1, C.boots);
  pset(g, frontFootX, frontFootY, C.bootsShade);

  const outlined = addOutline(g);
  return alignFrame(outlined);
}

function upscaleToRgba(grid, scale) {
  const w = grid.w * scale;
  const h = grid.h * scale;
  const out = Buffer.alloc(w * h * 4, 0);

  for (let y = 0; y < grid.h; y += 1) {
    for (let x = 0; x < grid.w; x += 1) {
      const idx = grid.data[y * grid.w + x];
      if (idx < 0) continue;
      const rgba = PALETTE.get(PALETTE_HEX[idx]);
      for (let sy = 0; sy < scale; sy += 1) {
        for (let sx = 0; sx < scale; sx += 1) {
          const ox = x * scale + sx;
          const oy = y * scale + sy;
          const p = (oy * w + ox) * 4;
          out[p] = rgba[0];
          out[p + 1] = rgba[1];
          out[p + 2] = rgba[2];
          out[p + 3] = 255;
        }
      }
    }
  }

  return { w, h, data: out };
}

const crcTable = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n += 1) {
    let c = n;
    for (let k = 0; k < 8; k += 1) {
      c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
    }
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i += 1) {
    c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const typeBuf = Buffer.from(type, 'ascii');
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const crcIn = Buffer.concat([typeBuf, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcIn), 0);
  return Buffer.concat([len, typeBuf, data, crc]);
}

function encodePng(w, h, rgba) {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0);
  ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  ihdr[10] = 0;
  ihdr[11] = 0;
  ihdr[12] = 0;

  const stride = w * 4;
  const raw = Buffer.alloc((stride + 1) * h);
  for (let y = 0; y < h; y += 1) {
    const rowStart = y * (stride + 1);
    raw[rowStart] = 0;
    rgba.copy(raw, rowStart + 1, y * stride, y * stride + stride);
  }

  const compressed = zlib.deflateSync(raw, { level: 9 });

  return Buffer.concat([
    signature,
    chunk('IHDR', ihdr),
    chunk('IDAT', compressed),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

function blit(dst, dstW, src, srcW, srcH, dx, dy) {
  for (let y = 0; y < srcH; y += 1) {
    for (let x = 0; x < srcW; x += 1) {
      const sp = (y * srcW + x) * 4;
      const dp = ((dy + y) * dstW + (dx + x)) * 4;
      dst[dp] = src[sp];
      dst[dp + 1] = src[sp + 1];
      dst[dp + 2] = src[sp + 2];
      dst[dp + 3] = src[sp + 3];
    }
  }
}

function validatePixels(rgba) {
  for (let i = 0; i < rgba.length; i += 4) {
    const a = rgba[i + 3];
    if (a !== 0 && a !== 255) {
      throw new Error('Found semi-transparent pixel');
    }
    if (a === 0) continue;

    const hex = `#${[rgba[i], rgba[i + 1], rgba[i + 2]]
      .map((n) => n.toString(16).padStart(2, '0'))
      .join('')}`;

    if (!PALETTE.has(hex)) {
      throw new Error(`Pixel color outside palette: ${hex}`);
    }
  }
}

function validateFrames(grids) {
  const baselines = new Set();

  grids.forEach((grid, index) => {
    const bounds = measureOpaqueBounds(grid);
    const centerX = (bounds.minX + bounds.maxX) / 2;
    baselines.add(bounds.maxY);

    if (Math.abs(centerX - TARGET_CENTER_X) > 0.5) {
      throw new Error(`Frame ${index} is not centered: ${centerX}`);
    }
  });

  if (baselines.size !== 1 || !baselines.has(TARGET_BASELINE_Y)) {
    throw new Error(`Unexpected baseline values: ${Array.from(baselines).join(', ')}`);
  }
}

function main() {
  fs.mkdirSync(OUT_DIR, { recursive: true });

  const sheet = Buffer.alloc(SHEET_W * SHEET_H * 4, 0);
  const metadata = {
    image: 'captain_walk_sheet.png',
    frameWidth: FRAME_W,
    frameHeight: FRAME_H,
    columns: 4,
    rows: 2,
    frameCount: FRAME_COUNT,
    palette: PALETTE_HEX,
    frames: [],
    animations: {
      walk: [],
    },
  };

  const lowFrames = [];

  for (let i = 0; i < FRAME_COUNT; i += 1) {
    const lowFrame = drawCaptain(i);
    lowFrames.push(lowFrame);

    const frame = upscaleToRgba(lowFrame, SCALE);
    const name = `captain_walk_${String(i).padStart(2, '0')}`;
    validatePixels(frame.data);
    fs.writeFileSync(path.join(OUT_DIR, `${name}.png`), encodePng(frame.w, frame.h, frame.data));

    const col = i % 4;
    const row = Math.floor(i / 4);
    const x = col * FRAME_W;
    const y = row * FRAME_H;
    blit(sheet, SHEET_W, frame.data, FRAME_W, FRAME_H, x, y);

    metadata.frames.push({
      name,
      index: i,
      x,
      y,
      w: FRAME_W,
      h: FRAME_H,
      durationMs: 100,
    });
    metadata.animations.walk.push(name);
  }

  validateFrames(lowFrames);
  validatePixels(sheet);

  fs.writeFileSync(path.join(OUT_DIR, 'captain_walk_sheet.png'), encodePng(SHEET_W, SHEET_H, sheet));
  fs.writeFileSync(path.join(OUT_DIR, 'captain_walk_sheet.json'), JSON.stringify(metadata, null, 2));

  console.log(`Generated in: ${OUT_DIR}`);
}

main();
