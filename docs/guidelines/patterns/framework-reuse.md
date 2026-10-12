# Framework Reuse and Adoption

This applies to all development in this repository. Sphere10 provides reusable
code, patterns and architecture, and its own development must use those foundations.

Use existing Sphere10 abstractions and utilities before introducing another
implementation. Trace the real consumers and registration path, then extend
the layer that owns the behavior. Apply the [coding guide](../code-styling.md)
and the matching [repository skills](../../../agents/skills/README.md).

- Before implementing anything, find and understand the existing code,
  abstractions, utilities, patterns and architecture that address the problem.
  Reuse them rather than creating parallel implementations or local substitutes.
- Extend or compose the existing framework where it falls short. Add a new
  abstraction only when the existing architecture cannot reasonably support the
  requirement; explain that gap and keep the addition consistent with the framework.
- When adding or improving a capability, adopt it in the relevant repository
  consumers in the same change. Supporting code alone is not a completed
  implementation.
- Trace the real startup, registration and user-facing execution paths. Update
  existing applications, demos and examples to consume the new APIs.
- Replace the superseded manual configuration or duplicated logic in those
  consumers. Do not leave the old path active alongside an unused replacement.
- Keep compatibility code only where it has an identified consumer or purpose,
  and clearly distinguish it from the recommended, actively demonstrated path.
- Verify the integration through the actual consumer's configuration and
  behavior, in addition to testing the supporting API in isolation.
- Update documentation to match the code that runs. Before claiming completion,
  audit the whole change for new capabilities that were added but never adopted.

For concrete utility choices, read [Framework Utilities](framework-utilities.md).
For design responsibilities, select the applicable topics from the
[Patterns and Practices index](../patterns-and-practices.md).
