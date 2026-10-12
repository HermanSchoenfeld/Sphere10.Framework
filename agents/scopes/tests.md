# Test Instructions

Follow [the repository instructions](../AGENTS.md) and load the
[unit-testing skill](../.github/skills/unit-testing/SKILL.md) before changing
tests. Preserve behavioral coverage when public contracts change.

Use [Patterns and Practices](../docs/guidelines/patterns-and-practices.md) for
fixture hierarchies. A fixture can supply a concrete test implementation of an
abstract framework base; test construction alone does not justify keeping an
inheritance-only production class concrete.

Follow [Code Styling](../docs/guidelines/code-styling.md#generic-constraints):
one `where` constraint per aligned line, with the opening brace at the end of
the final constraint line. Apply the same format to nested fixture types.
