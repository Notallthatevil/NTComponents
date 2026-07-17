---
applyTo: "**/*.Tests.cs,**/*.IntegrationTests.cs"
---

# Testing Guidelines

## Frameworks & Libraries
- **Mocking:** `NSubstitute` — Use for creating test doubles and verifying interactions.
- **Test Data:** `AutoFixture` — Use to generate input data; customize fixtures only when specific values are required.
- **Runner:** `xUnit.v3` — Standard test framework.
- **Assertions:** `AwesomeAssertions` — Use for fluent, descriptive assertions.
- **Blazor Testing:** [`bUnit`](https://bunit.dev/docs/getting-started/index.html) — Use for unit testing Blazor components.

## Test Style & Quality
- **AAA Pattern:** Follow `Arrange`, `Act`, `Assert` with explicit comments.
- **Focus:** Each test must assert a single behavior or outcome.
- **Setup/Teardown:** Use constructor for setup; implement `IDisposable`/`IAsyncDisposable` for teardown.
- **Naming Conventions:**
  - **Test Class:** Name after the production method being tested.. (e.g., `CalculatePay`).
  - **Test Method:** `[State]_[ExpectedBehavior]` (e.g., `WithOvertime_ReturnsCorrectAmount`).
- **Data:** Prefer `AutoFixture` for generic data. Override specific values in `Arrange` only if relevant to the behavior.
- **Mocking:** Keep verifications explicit and minimal.

## File Organization
Follow a granular structure to keep tests navigable and focused.
1. **Class Folder:** Create a folder for the production class (e.g., `Employee`).
2. **Test File:** Create a separate `.cs` file for each method being tested, with a separate file for integration tests as needed.
   - **Naming:** `[MethodName].Tests.cs`, `[MethodName].IntegrationTests.cs`
   - **Example:** `AccountingPayroll.Core.Tests/Domain/Employee/CalculatePay.Tests.cs`
   - **Content:** This file should contain all test cases (states and expected behaviors) for that method.

## Coverage & Bug Reporting
- **Coverage:** Target high coverage for critical paths, edge cases, and error handling.
- **Intent:** Tests must verify business intent, not just implementation details.
- **Bugs:** If a bug is discovered while writing tests, document it clearly with a description and potential fix.

## Integration & E2E Testing

### Testcontainers
- Use `Testcontainers for .NET` for tests requiring real infrastructure (databases, queues, etc.).
- **Classification:** Using Testcontainers does not automatically make a test an integration test. Classify based on the scope of the test (unit vs. integration).
- Ensure containers are disposed cleanly to prevent resource leaks.

### Playwright
- Use **Playwright** for browser-based integration and end-to-end (E2E) tests.
- **Location:** Keep Playwright tests in the dedicated `Tests/[ProjectName].IntegrationTests` project so unit-test runs remain isolated from browser dependencies.
- **Naming:** Suffix Playwright test files with `_IntegrationTests.cs`.
- **Page Object Model (POM):** Use POM to abstract page interactions and selectors, keeping tests clean and maintainable.
- **Locators:** Prefer user-facing locators (`GetByRole`, `GetByText`, `GetByLabel`) over brittle CSS or XPath selectors.
- **Resilience:** Rely on Playwright's built-in auto-waiting. Avoid hardcoded `Thread.Sleep`.
- **Authentication:** Use storage state to cache authentication sessions, avoiding repeated login steps in every test.

## General Rules
- **Isolation:** Unit tests must be fast and isolated (no external dependencies).
- **Secrets:** **NEVER** commit secrets or environment-specific configuration. Use environment variables or secure storage.
- **Maintenance:** Update tests whenever business logic changes. Tests are an integral part of the feature.
