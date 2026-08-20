describe("Demo authentication", () => {
  it("signs in with the Demo Mode account", () => {
    cy.signInAsDemo();
    cy.contains(".demo-mode-banner", "Demo Mode").should("be.visible");
    cy.contains("h1", "Vitals Dashboard").should("be.visible");
  });

  it("redirects an anonymous visitor away from a protected page", () => {
    cy.clearCookies();
    cy.visit("/dashboard");
    cy.location("pathname", { timeout: 10000 }).should("eq", "/signin");
  });
});
