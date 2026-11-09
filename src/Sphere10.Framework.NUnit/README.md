<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.NUnit

Sphere10.Framework.NUnit provides reusable NUnit assertions and randomized integration tests for Sphere10 Framework lists, buffers, dictionaries, streams and Merkle trees. Reference it from a test project when you implement a Framework collection or stream contract and want to check the implementation against a reference implementation.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.NUnit` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`AssertEx`](AssertEx.cs) | static class | Randomized integration tests that compare an `IExtendedList<T>`, buffer, dictionary or `Stream` against a reference implementation, plus assertions for streams, Merkle trees, enum flags, regular expressions, 2D arrays, approximate values and paged-list page state. |
| [`TestItem<TInput, TExpected>`](TestItem.cs) | class | Test case that pairs one `Input` with its `Expected` result. |
| [`TestItem<TInput1, TInput2, TExpected>`](TestItem.cs) | class | Test case that pairs two inputs, `Input1` and `Input2`, with their `Expected` result. |
| [`Tools.NUnit`](NUnitTool.cs) | tools | Detects a GitHub Actions run, asserts that a sequence is empty or not empty, formats a 2D array as text and prints a sequence to the console. |
<!-- domains:end -->

## NuGet usage

Install `Sphere10.Framework.NUnit` in a .NET 10 test project. Its `Sphere10.Framework` and NUnit dependencies restore automatically, and the package is built against NUnit 5.0.0. Test discovery still requires the NUnit test adapter and Microsoft.NET.Test.Sdk in the consuming test project.

Test projects inside this repository reference `src/Sphere10.Framework.NUnit/Sphere10.Framework.NUnit.csproj` instead.

## Example

```csharp
using NUnit.Framework;
using Sphere10.Framework;
using Sphere10.Framework.NUnit;

[TestFixture]
public class ExtendedListContractTests {
	[Test]
	public void MatchesAReferenceList() {
		var list = new ExtendedList<int>(capacity: 0, capacityGrowthSize: 16, maxCapacity: 1000);
		AssertEx.ListIntegrationTest(list, (int)list.MaxCapacity, (rng, count) => rng.NextInts(count));
	}
}
```

`ListIntegrationTest` applies the same random adds, inserts, updates, reads, copies and removals to the list under test and to a `List<T>`, and it asserts after each step that both hold the same items. It seeds its random generator with a fixed value, so a failure repeats on the next run.

## What's Included

| Type | Namespace | Purpose |
|---|---|---|
| `AssertEx` | `Sphere10.Framework.NUnit` | Integration tests and assertions, listed below |
| `Tools.NUnit` | `Tools` | `IsEmpty` and `IsNotEmpty` assertions with optional messages, `Convert2DArrayToString` and `Print` for diagnostic output, and `IsGitHubAction`, which is `true` when the `GITHUB_ACTIONS` environment variable is `true` |
| `TestItem<TInput, TExpected>`, `TestItem<TInput1, TInput2, TExpected>` | `Sphere10.Framework.NUnit` | Input and expected-value holders for data-driven cases |

| `AssertEx` member | What it checks |
|---|---|
| `ListIntegrationTest` | An `IExtendedList<T>` against a `List<T>` or a supplied reference list. `mutateFromEndOnly` limits changes to the end of the list. |
| `BufferIntegrationTest` | An `IBuffer` against a list-backed buffer, through its span methods |
| `DictionaryIntegrationTest` | An `IDictionary<TKey, TValue>` against a `Dictionary<TKey, TValue>`, with random adds, updates, removals and clears. It compares the entries in any enumeration order, so sorted dictionaries pass. |
| `StreamIntegrationTests` | A `Stream` against a `MemoryStream`, with random seeks, writes, reads and length changes. The stream must be empty unless you supply the expected stream. |
| `AreEqual` | Two streams by length, position and content, or two `IDynamicMerkleTree` instances by size, leaves and root |
| `HasFlags`, `NotHasFlags`, `HasAllFlags` | Enum flag values |
| `RegexMatch`, `RegexNotMatch` | Whether a pattern matches a whole input, with optional named-group values |
| `AssertSame2DArrays` | The row counts, column counts and values of two 2D sequences, and prints both when they differ |
| `ApproxEqual` | Two `DateTime` values, or two numeric values, within a tolerance |
| `HasLoadedPages`, `HasDirtyPages` | That no page of a `PagedListBase<TItem>` outside the given page numbers is loaded or dirty |

The integration tests are helpers that your own `[Test]` methods call. `DictionaryIntegrationTest` first adds `maxCapacity / 2` generated pairs, and it skips a generated key that is already present.

## Dependencies

- **NUnit**: NUnit testing framework
- **Sphere10 Framework**: Core Sphere10 Framework

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.


