describe("Dashboard nutrition trends", () => {
  beforeEach(() => cy.signInAsDemo());

  it("shows protein and phosphorus as percentage-of-goal panels", () => {
    cy.get('.nutrition-trend-grid[aria-label="Nutrition percentage-of-goal trends"]')
      .should("be.visible")
      .within(() => {
        cy.contains("h2", "Protein vs target").should("be.visible");
        cy.contains("h2", "Net phosphorus vs maximum").should("be.visible");
        cy.contains("100% goal line").should("exist");
        cy.contains(/Protein close to goal \d of 7 days\./).should("be.visible");
        cy.contains(/Phosphorus under maximum \d of 7 days\./).should("be.visible");
        cy.contains("Close means 80%–120% of the protein target.").should("be.visible");
        cy.contains("Partial").should("exist");
      });
  });
});
