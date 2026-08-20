import { defineConfig } from "cypress";

export default defineConfig({
  allowCypressEnv: false,
  e2e: {
    baseUrl: process.env.CYPRESS_BASE_URL ?? "http://127.0.0.1:5090",
    supportFile: "cypress/support/e2e.js",
    specPattern: "cypress/e2e/**/*.cy.js"
  },
  viewportWidth: 1440,
  viewportHeight: 900,
  defaultCommandTimeout: 10000,
  screenshotsFolder: "cypress/screenshots",
  videosFolder: "cypress/videos",
  video: false,
  screenshotOnRunFailure: true,
  retries: { runMode: 1, openMode: 0 }
});
