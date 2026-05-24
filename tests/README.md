# Test Infrastructure

**Engine**: Unity 2022.3.51
**Test Framework**: Unity Test Framework (NUnit)
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-05-24

## Directory Layout

```
tests/
  unit/           # Isolated unit tests (formulas, state machines, logic)
  integration/    # Cross-system and save/load tests
  smoke/          # Critical path test list for /smoke-check gate
  evidence/       # Screenshot logs and manual test sign-off records
```

## Unity Test Location

Actual Unity tests live in `Assets/Scripts/Tests/` (assembled via `ClassBrawl.Tests.asmdef`).
This `tests/` root directory holds documentation, smoke checklists, and test evidence.

Current test files:
- `Assets/Scripts/Tests/Core/DamageFormulasTests.cs` — Damage calculation formulas
- `Assets/Scripts/Tests/Core/KnockbackFormulasTests.cs` — Knockback physics formulas
- `Assets/Scripts/Tests/Core/FocusFormulasTests.cs` — Focus accumulation formulas
- `Assets/Scripts/Tests/Feature/MatchFormulasTests.cs` — Match scoring formulas
- `Assets/Scripts/Tests/Feature/DrawFormulasTests.cs` — Skill draw probability formulas
- `Assets/Scripts/Tests/TestUtilities/TestDataFactory.cs` — Test data factories

## Running Tests

**In Editor**: Window → General → Test Runner → Edit Mode → Run All
**Via CLI (CI)**: `game-ci/unity-test-runner@v4` (see `.github/workflows/tests.yml`)

## Test Naming

- **Files**: `[System]_[Feature]Tests.cs` (e.g., `Core_DamageFormulasTests.cs`)
- **Functions**: `[Scenario]_[Expected]` (e.g., `BaseAttack_ReturnsExpectedDamage`)
- **NUnit conventions**: `[Test]` attribute, `[SetUp]`/`[TearDown]` for isolation

## Story Type → Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | Automated unit test — must pass | `Assets/Scripts/Tests/[System]/` |
| Integration | Integration test OR playtest doc | `tests/integration/[system]/` |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` |

## CI

Tests run automatically on every push to `main` and on every pull request.
A failed test suite blocks merging.

**Required secret**: `UNITY_LICENSE` — add to GitHub repository secrets before first CI run.
