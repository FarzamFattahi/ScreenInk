// Optional development tool: npm install playwright, then node tools/Render-Banner.cjs
const { chromium } = require(process.env.SCREENINK_PLAYWRIGHT || 'playwright');
const path = require('path');
const { pathToFileURL } = require('url');
(async () => {
  const browser = await chromium.launch({ headless: true, channel: process.env.SCREENINK_BROWSER_CHANNEL || 'chrome' });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 480 }, deviceScaleFactor: 1 });
    const assets = path.resolve(__dirname, '../docs/assets');
    await page.goto(pathToFileURL(path.join(assets, 'banner.html')).href);
    await page.screenshot({ path: path.join(assets, 'banner.png') });
  } finally { await browser.close(); }
})().catch(error => { console.error(error.message); process.exit(1); });
