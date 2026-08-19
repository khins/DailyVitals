import puppeteer from "puppeteer";
import fs from "node:fs/promises";
import path from "node:path";

const baseUrl = process.env.DAILYVITALS_URL ?? "http://localhost:5085";
const manifestPath = path.resolve(
  process.env.DAILYVITALS_SCREENSHOT_MANIFEST ??
    "tools/screenshot-manifest.json",
);
const outputDirectory = path.resolve("docs", "screenshots");

const singleCapture = process.env.DAILYVITALS_CAPTURE_PATH
  ? [
      {
        route: process.env.DAILYVITALS_CAPTURE_PATH,
        file:
          process.env.DAILYVITALS_SCREENSHOT_FILE ??
          "dashboard-overview-desktop.png",
        selector:
          process.env.DAILYVITALS_READY_SELECTOR ??
          ".dashboard-shell.demo-mode",
      },
    ]
  : null;

const captures =
  singleCapture ?? JSON.parse(await fs.readFile(manifestPath, "utf8"));

if (!Array.isArray(captures) || captures.length === 0) {
  throw new Error(`No screenshot definitions found in ${manifestPath}.`);
}

await fs.mkdir(outputDirectory, { recursive: true });

const browser = await puppeteer.launch({ headless: true });
const failures = [];

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1 });

  await page.goto(`${baseUrl}/signin`, {
    waitUntil: "networkidle0",
    timeout: 30_000,
  });

  if (!page.url().includes("/dashboard")) {
    const demoButton = await page.waitForSelector(".demo-login-panel button", {
      visible: true,
      timeout: 10_000,
    });
    await demoButton.click();
    await page.waitForFunction(
      () => {
        const userName = document.querySelector("#user-name");
        const password = document.querySelector("#password");
        return userName?.value && password?.value;
      },
      { timeout: 10_000 },
    );

    await Promise.all([
      page.waitForNavigation({ waitUntil: "networkidle0", timeout: 30_000 }),
      page.click('button[type="submit"]'),
    ]);
  }

  for (const capture of captures) {
    try {
      await page.goto(`${baseUrl}${capture.route}`, {
        waitUntil: "networkidle0",
        timeout: 30_000,
      });
      await page.waitForSelector(".dashboard-shell.demo-mode", {
        visible: true,
        timeout: 30_000,
      });
      await page.waitForSelector(capture.selector, {
        visible: true,
        timeout: 30_000,
      });
      await page.evaluate(() => document.fonts.ready);
      await new Promise((resolve) => setTimeout(resolve, 750));

      const outputPath = path.join(outputDirectory, capture.file);
      await page.screenshot({
        path: outputPath,
        type: "png",
        fullPage: false,
      });

      const png = await fs.readFile(outputPath);
      const width = png.readUInt32BE(16);
      const height = png.readUInt32BE(20);
      if (width !== 1440 || height !== 900) {
        throw new Error(`Expected 1440x900, received ${width}x${height}.`);
      }

      console.log(`PASS ${capture.route} -> ${capture.file}`);
    } catch (error) {
      failures.push({ capture, error });
      console.error(`FAIL ${capture.route}: ${error.message}`);
    }
  }
} finally {
  await browser.close();
}

if (failures.length > 0) {
  console.error(`\n${failures.length} of ${captures.length} captures failed.`);
  process.exitCode = 1;
} else {
  console.log(`\nCaptured and validated ${captures.length} screens.`);
}
