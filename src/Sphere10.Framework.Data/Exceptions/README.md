# Exceptions

Data-layer exceptions for lookups that expect exactly one record.

`NoSingleRecordException` is a `SoftwareException` thrown when a query that must return one record returns none or several. Its message names the table or collection, the requested ID when one is given, and the number of records found. No Framework code throws it, so it is available to application code that checks the row count of a [DAC](../DAC/README.md) query.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`NoSingleRecordException`](NoSingleRecordException.cs) | class | Reports that no record or too many records matched a single-record lookup, with optional requested ID. |
