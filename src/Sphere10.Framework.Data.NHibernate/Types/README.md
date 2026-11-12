# Provider column types

Provider-specific NHibernate readers preserve mapped database column definitions.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DecimalColumnType`](DecimalColumnType.cs) | class | Reads decimal columns through the provider's `GetDecimal` conversion while retaining NHibernate's decimal SQL mapping |

Register [DecimalColumnConvention](../Conventions/DecimalColumnConvention.cs) in
the application's FluentNHibernate conventions to apply this reader to decimal
and nullable decimal properties. This preserves SQLite decimal values when the
provider's general object read returns a binary floating-point value.
