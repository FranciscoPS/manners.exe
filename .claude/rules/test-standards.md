---
paths:
  - "tests/**"
  - "Assets/Tests/**/*.cs"
  - "Source/**/Tests/**/*.{h,cpp}"
  - "Assets/Scripts/Editor/**/*Tests.cs"
---

# Test Standards

manners.exe keeps its EditMode tests in `Assets/Scripts/Editor/` (the predefined
Editor assembly, no asmdef). A test file anywhere else is not compiled: "0 tests, Passed".

- Naming follows the existing suites: class `[System]Tests` in a file of the same
  name (`OverrideCombinationTests`, `GraphicsBudgetTests`); each method is a
  PascalCase sentence that states the scenario and the expected result, with no
  underscores (`MultishotAlwaysEmitsHalfItsBulletsRoundedUp`)
- Every test has a clear arrange/act/assert structure, shown with blank lines (no comments in C#)
- Tests that need managers create them on purpose and point the singleton's static
  instance at them, then restore every static and destroy every object in `TearDown`
  (the pattern in `OverrideCombinationTests`)
- A test that needs Play Mode is a `[UnityTest]` or `[UnitySetUp]` that yields
  `new EnterPlayMode()`. Entering Play Mode reloads the domain: statics reset and
  locals captured before it come back null, so pass data through files or static methods
- PlayerPrefs are shared with the real game: save and restore every key a test touches
- Unit tests must not depend on external state (filesystem, network, database)
- Integration tests must clean up after themselves
- Performance tests must specify acceptable thresholds and fail if exceeded
- Test data must be defined in the test or in dedicated fixtures, never shared mutable state
- Mock external dependencies (the global leaderboard service, for one) — tests should be fast and deterministic
- Fix `Random.state` (save it, seed it, restore it) when a test touches randomness
- Map 2 (`MilitaryBase`) stays out of tests while it is paused
- Every bug fix must have a regression test that would have caught the original bug —
  and **you must watch it fail before you trust it.** Run the new test against the
  unfixed code, confirm it fails, then apply the fix and confirm it passes. A
  regression test that has only ever been seen passing is not known to test anything.

  > This is `.claude/rules/skill-authoring.md`'s "a gate you have not watched fail
  > is not a gate", applied to tests. A regression test for an iteration-order
  > defect can use a fixture where no cell takes part in two transfers per tick — it
  > then passes against the very bug it was written for. The fixture, not the
  > assertion, is what makes it useless, and only running it against the unfixed
  > code exposes that.

## Example

**Correct** (`Assets/Scripts/Editor/HealthTests.cs`):

```csharp
using NUnit.Framework;

public class HealthTests
{
    [Test]
    public void TakingDamageReducesCurrentHealthByTheDamageAmount()
    {
        var health = new Health(maxHealth: 100);

        health.TakeDamage(25);

        Assert.AreEqual(75, health.Current);
    }
}
```

**Incorrect**:

```csharp
[Test]
public void Test1()
{
    var h = new Health(100);
    h.TakeDamage(25);
    Assert.IsTrue(h.Current < 100);
}
```

Violations: no descriptive name, no arrange/act/assert separation, imprecise assertion.
