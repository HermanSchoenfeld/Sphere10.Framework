# CSV

Fast CSV parsing with a forward-only reader, a cached random-access reader, and configurable error handling.

The domain is an MIT-licensed CSV reader originally written by Sébastien Lorion, kept with its original structure. `CsvReader` reads records forward-only from a `TextReader` and implements `IDataReader`, so it can feed ADO.NET consumers such as `DataTable.Load`. Fields are read by index or by header name when the reader was created with `hasHeaders: true`. Delimiter, quote, escape and comment characters, value trimming and buffer size are set through its constructors.

`CachedCsvReader` keeps every record it reads, which allows `MoveTo`, `MoveToStart` and data binding through its internal binding list. Malformed input is handled according to `DefaultParseErrorAction`. The default, `ParseErrorAction.RaiseEvent`, raises the `ParseError` event, whose handler can set `AdvanceToNextLine` to skip the record. Otherwise the `MalformedCsvException` is thrown. `MissingFieldAction` decides whether a short record is an error or is padded with empty or null values. For simple whole-file reads, `Tools.Data.ReadCsv` in the project root wraps this reader and returns a `DataTable`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CachedCsvReader`](CachedCsvReader.cs) | class | CSV reader that caches records for random access, record moves and data binding. |
| [`CsvReader`](CsvReader.cs) | class | Fast, forward-only CSV reader that implements `IDataReader` and enumerates records as string arrays. |
| [`MalformedCsvException`](MalformedCsvException.cs) | class | Thrown for malformed CSV, carrying the raw data, position, record index and field index. |
| [`MissingFieldAction`](MissingFieldAction.cs) | enum | Chooses whether a missing field raises a parse error or is replaced by an empty string or null. |
| [`MissingFieldCsvException`](MissingFieldCsvException.cs) | class | `MalformedCsvException` raised when a record has fewer fields than expected. |
| [`ParseErrorAction`](ParseErrorAction.cs) | enum | Chooses whether a parse error raises the `ParseError` event, skips to the next line or throws. |
| [`ParseErrorEventArgs`](ParseErrorEventArgs.cs) | class | Data for `CsvReader.ParseError` with the error and a settable follow-up action. |
| [`ValueTrimmingOptions`](ValueTrimmingOptions.cs) | enum | Flags selecting whether unquoted values, quoted values, both or neither are trimmed. |

`CsvReader.RecordEnumerator`, the struct returned by `CsvReader.GetEnumerator`, is declared in [CsvReader.RecordEnumerator.cs](CsvReader.RecordEnumerator.cs).
