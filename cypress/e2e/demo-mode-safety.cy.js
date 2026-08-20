describe("Demo Mode safety", () => {
  beforeEach(() => cy.signInAsDemo());
  it("prevents profile changes", () => {
    cy.visit("/profile");
    cy.contains('button[type="submit"]', "Save Profile").should("be.disabled");
  });
  it("prevents adding selected renal foods", () => {
    cy.visit("/renal-foods");
    cy.contains("button", "Add selected to today").should("be.disabled");
  });
  it("prevents creating a new blood pressure reading", () => {
    cy.visit("/blood-pressure");
    cy.contains("button", /^New$/).should("be.disabled");
  });
});
