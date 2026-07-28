import { mkdir, readFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const svg = await readFile(join(root, 'public', 'plannyt-mark.svg'), 'utf8');
const output = join(root, 'public', 'icons');
const sizes = [72, 96, 128, 144, 152, 192, 384, 512];

await mkdir(output, { recursive: true });
const browser = await chromium.launch();

try {
  for (const size of sizes) {
    const page = await browser.newPage({
      viewport: { width: size, height: size },
      deviceScaleFactor: 1,
    });
    await page.setContent(
      `<style>html,body,svg{width:100%;height:100%;margin:0;display:block}</style>${svg}`,
    );
    await page.screenshot({
      path: join(output, `icon-${size}x${size}.png`),
      omitBackground: false,
    });
    await page.close();
  }
} finally {
  await browser.close();
}
