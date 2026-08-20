Cypress.Commands.add("signInAsDemo", () => {
  cy.clearCookies();
  cy.intercept("POST", "/_blazor/negotiate*").as("blazorNegotiation");
  cy.visit("/signin");
  cy.get(".demo-login-panel").should("be.visible");
  cy.wait("@blazorNegotiation");
  cy.wait(1000);
  cy.contains("button", "Use demo account").click();
  cy.get("#user-name").should("have.value", "demo@activevitals.app");
  cy.get("#password").should("have.value", "Demo123!");
  cy.get('button[type="submit"]').click();
  cy.location("pathname", { timeout: 20000 }).should("eq", "/dashboard");
  cy.get(".dashboard-shell.demo-mode", { timeout: 20000 }).should("be.visible");
});
