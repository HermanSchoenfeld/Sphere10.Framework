# TextWriters

`TextWriter` sinks for the console, debug output, files, string builders and delegates, plus multicast, time-stamping and transforming decorators.

`TextWriterBase` funnels every `Write(string)`, `Write(char[], int, int)` and `WriteAsync` call into two abstract methods, `InternalWrite` and `InternalWriteAsync`. Derive from `SyncTextWriter` when the sink only has a synchronous write, because it runs `InternalWrite` on a thread-pool task for the asynchronous path. Derive from `AsyncTextWriter` when the sink is naturally asynchronous, because its synchronous writes start the asynchronous write on a worker and wait for it. `ActionTextWriter` turns a delegate into a writer without a subclass.

`TextWriterDecorator<TTextWriter>` rewrites each string with `DecorateText` before passing it to the inner writer. `TimeStampTextWriter` prefixes a time stamp and `TextWriterTransformer` applies any `Func<string, string>`. `MulticastTextWriter` sends every write to a list of writers under a lock, and writers can be added and removed while it is in use. `FileAppendTextWriter` opens, appends to and closes its file on every write, while `FileTextWriter` keeps a `FileStream` open until it is disposed. The loggers in the [Logging](../Logging/README.md) domain write to these sinks, and the [Text](../Text/README.md) domain holds the string formatting helpers.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionTextWriter`](ActionTextWriter.cs) | class | Synchronous writer that passes each written string to an `Action<string>`. |
| [`AsyncTextWriter`](AsyncTextWriter.cs) | abstract class | Writer base for asynchronous sinks whose synchronous writes wait for `InternalWriteAsync` on a worker. |
| [`ConsoleTextWriter`](ConsoleTextWriter.cs) | class | Writes to `Console`. |
| [`DebugTextWriter`](DebugTextWriter.cs) | class | Writes to the debugger's `Debug` output. |
| [`FileAppendTextWriter`](FileAppendTextWriter.cs) | class | Appends each write to a file path with `File.AppendAllText`, UTF-8 without a byte-order mark by default. |
| [`FileTextWriter`](FileTextWriter.cs) | class | Writes to a `FileStream` that it opens or receives and disposes with itself. |
| [`MulticastTextWriter`](MultiCastTextWriter.cs) | class | Routes every write to a lockable list of other `TextWriter` instances. |
| [`NoOpTextWriter`](NoOpTextWriter.cs) | class | Writer that discards everything. |
| [`StringBuilderTextWriter`](StringBuilderTextWriter.cs) | class | Appends writes to a `StringBuilder` exposed as `Builder`. |
| [`SyncTextWriter`](SyncTextWriter.cs) | abstract class | Writer base for synchronous sinks whose asynchronous writes run `InternalWrite` on a thread-pool task. |
| [`TextWriterBase`](TextWriterBase.cs) | abstract class | Funnels all string and character-array writes into `InternalWrite` and `InternalWriteAsync`. |
| [`TextWriterDecorator`](TextWriterDecorator.cs) | abstract class | `TextWriterDecorator<TextWriter>` for decorators over any writer. |
| [`TextWriterDecorator<TTextWriter>`](TextWriterDecorator.cs) | abstract class | Decorator that rewrites each string with `DecorateText` before writing it to the inner writer. |
| [`TextWriterTransformer`](TextWriterTransformer.cs) | class | Decorator that applies a `Func<string, string>` to each write. |
| [`TimeStampTextWriter`](TimeStampTextWriter.cs) | class | Decorator that prefixes each write with a formatted UTC or local time stamp. |
