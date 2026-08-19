import puppeteer from "puppeteer";
import path from "node:path";

const baseUrl = process.env.DAILYVITALS_URL ?? "http://localhost:5085";
const targetPath = process.env.DAILYVITALS_CAPTURE_PATH ?? "/dashboard";
const outputFile =
  process.env.DAILYVITALS_SCREENSHOT_FILE ?? "dashboard-overview-desktop.png";
const readySelector = process.env.DAILYVITALS_READY_SELECTOR ?? ".dashboard-shell.demo-mode";
const outputPath = path.resolve(
  "docs",
  "screenshots",
  outputFile,
);

const browser = await puppeteer.launch({ headless: true });

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

  if (targetPath !== "/dashboard") {
    await page.goto(`${baseUrl}${targetPath}`, {
      waitUntil: "networkidle0",
      timeout: 30_000,
    });
  }

  await page.waitForSelector(readySelector, {
    visible: true,
    timeout: 30_000,
  });
  await page.evaluate(() => document.fonts.ready);
  await new Promise((resolve) => setTimeout(resolve, 750));

  await page.screenshot({
    path: outputPath,
    type: "png",
    fullPage: false,
  });

  console.log(`Saved ${outputPath}`);
} finally {
  await browser.close();
}
