# Test Project Guidelines

These instructions apply to work under `Tests/*`.

## Frameworks & Libraries

- **Mocking:** Use `NSubstitute` for test doubles and interaction verification.
- **Test Data:** Use `AutoFixture` to generate input data. Customize fixtures only when specific values are required for the behavior under test.
- **Runner:** Use `xUnit.v3`.
- **Assertions:** Use `AwesomeAssertions` for fluent, descriptive assertions.
- **Blazor Testing:** Use `bUnit` for Blazor component unit tests.

## Test Style & Quality

- Follow the Arrange, Act, Assert pattern with explicit comments.
- Each test should assert a single behavior or outcome.
- Use the test class constructor for setup. Implement `IDisposable` or `IAsyncDisposable` for teardown when needed.
- Name test classes after the production method or behavior being tested.
- Name test methods with the pattern `[State]_[ExpectedBehavior]`, for example `WithOvertime_ReturnsCorrectAmount`.
- Prefer `AutoFixture` for generic data. Override specific values in Arrange only when they matter to the behavior.
- Keep mock verifications explicit and minimal.
- Verify business intent instead of implementation details.

## File Organization

Use a granular structure so tests stay navigable and focused.

1. Create a folder for the production class or component under test.
2. Create a separate test file for each method, behavior, or component surface being tested.
3. Use `[MethodName].Tests.cs` for unit tests and `[MethodName].IntegrationTests.cs` for integration tests when practical.
4. Keep all states and expected behaviors for that method or component surface in the corresponding file.

For Blazor components, place component tests in the appropriate Blazor test project and use bUnit render assertions, event dispatch, and semantic queries where possible.

## Blazor Component Test Expectations

- Every new or meaningfully changed Blazor component should have corresponding bUnit coverage.
- Use `xUnit`, `NSubstitute`, `AutoFixture`, and `AwesomeAssertions` with bUnit tests.
- Prefer assertions against rendered behavior, accessible text, roles, labels, and callback effects over brittle markup details.
- Mock injected services explicitly and keep component setup readable.
- When component structure or TnTComponents usage is unclear, inspect existing tests and match the local pattern before adding new helpers.

## Coverage & Bug Reporting

- Target high coverage for critical paths, edge cases, and error handling.
- Add or update tests whenever business logic, DTOs, repository contracts, pages, moderation flows, billing flows, or component behavior changes.
- If a bug is discovered while writing tests, document it clearly with a description and potential fix.

## Integration & E2E Testing

### Testcontainers

- Use `Testcontainers for .NET` for tests requiring real infrastructure such as databases or queues.
- Using Testcontainers does not automatically make a test an integration test. Classify based on the scope of the test.
- Dispose containers cleanly to prevent resource leaks.

### Playwright

- Use Playwright for browser-based integration and end-to-end tests.
- Playwright tests must reside in a dedicated integration test project named `[ProjectName].IntegrationTests`.
- Suffix Playwright test files with `_IntegrationTests.cs`.
- Use the Page Object Model to abstract page interactions and selectors.
- Prefer user-facing locators such as `GetByRole`, `GetByText`, and `GetByLabel` over brittle CSS or XPath selectors.
- Rely on Playwright auto-waiting. Avoid hardcoded `Thread.Sleep`.
- Use storage state to cache authentication sessions and avoid repeated login steps in every test.

## Validation

- Unit tests must be fast and isolated with no external dependencies.
- Run focused test filters while iterating.
- Before finishing, run the affected test project with `dotnet test` when practical.
- For Blazor component work, ensure the relevant bUnit tests pass locally before finalizing.
- If a validation command cannot be run, state that clearly in the final response.

## General Rules

- Never commit secrets or environment-specific configuration. Use environment variables or secure storage.
- Update tests whenever business logic changes. Tests are part of the feature, not a follow-up task.