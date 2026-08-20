const pages = [
  ["/dashboard", ".dashboard-shell.demo-mode", "Vitals Dashboard"],
  ["/blood-pressure", ".entry-grid", "Blood Pressure"],
  ["/blood-glucose", ".entry-grid", "Blood Glucose"],
  ["/weight", ".entry-grid", "Weight"],
  ["/exercise", ".entry-grid", "Exercise"],
  ["/reports", ".report-card-grid", "Reports"],
  ["/nutrition", ".dashboard-shell.demo-mode", "Nutrition"],
  ["/fluid-intake", ".entry-grid", "Fluid Intake"],
  ["/labs", ".kidney-lab-entry-grid", "Kidney Labs"],
  ["/renal-foods", ".renal-food-panel", "Renal Friendly Foods"],
  ["/profile", ".profile-grid", "Profile"]
];

describe("DailyVitals portfolio pages", () => {
  beforeEach(() => cy.signInAsDemo());
  for (const [route, readySelector, heading] of pages) {
    it(`loads ${route} with synthetic Demo Mode data`, () => {
      cy.visit(route);
      cy.location("pathname").should("eq", route);
      cy.get(readySelector, { timeout: 20000 }).should("be.visible");
      cy.contains("h1", heading).should("be.visible");
      cy.contains(".demo-mode-banner", "Demo Mode").should("be.visible");
    });
  }
});
