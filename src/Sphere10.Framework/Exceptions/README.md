# Exceptions

Argument and invariant guards, the `SoftwareException` hierarchy and helpers that run actions while capturing or ignoring their exceptions.

`Guard` is the validation entry point. The `Argument*` methods throw `ArgumentNullException`, `ArgumentException` or `ArgumentOutOfRangeException` for bad parameters, `CheckIndex` and `CheckRange` validate collection positions, the file and directory checks throw the matching I/O exceptions, and `Ensure` and `Against` throw `InvalidOperationException` when an invariant fails. `SoftwareException` is the base of the Framework's own exceptions and records the process, thread, machine, user and culture where it was created. `Tools.Exceptions` runs actions while capturing, filtering or ignoring their exceptions, and `ValidateTasks` rethrows the faults of completed tasks as one `AggregateException`.

Other domains add their own `SoftwareException` subclasses, for example `ProcessSentryException` in [Environment](../Environment/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CancelledException`](CancelledException.cs) | class | `SoftwareException` that signals a cancelled operation. |
| [`FileAlreadyExistsException`](FileAlreadyExistsException.cs) | class | Thrown when a file that must not exist is present, with its `Path`. |
| [`Guard`](Guard.cs) | static class | Throws the appropriate exception for invalid arguments, indexes, ranges, missing or existing files and directories, failed casts and broken invariants. |
| [`IllegalValueException`](IllegalValueException.cs) | class | Thrown for a value that is not permitted, with an optional description. |
| [`InternalErrorException`](InternalErrorException.cs) | class | Thrown for a condition that indicates a defect in the code rather than bad input. |
| [`InvalidDataFormatException`](InvalidFormatException.cs) | class | Thrown when data does not have the expected format. |
| [`NotInitializedException`](NotInitializedException.cs) | class | Thrown when an object is used before it has been initialized. |
| [`SoftwareException`](SoftwareException.cs) | class | Base Framework exception with formatted messages, `Result` messages and the process, thread, user and culture context. |
| [`StreamOutOfBoundsException`](StreamOutOfBoundsException.cs) | class | Thrown when a stream operation addresses a position outside the stream. |
| [`Tools.Exceptions`](ExceptionTool.cs) | tools | Runs actions while ignoring, handling, filtering or capturing their exceptions, synchronously or asynchronously, and validates task faults. |
