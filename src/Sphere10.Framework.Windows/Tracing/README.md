# Event Tracing

Lists Event Tracing for Windows (ETW) sessions, starts and controls caller-owned real-time sessions, consumes their events as detached records and decodes kernel CPU-sampling events.

`Tools.Windows.TraceSessions` lists the running sessions as `EtwTraceSessionInfo` records. `StartRealTimeSession` creates a named manifest-provider session, and `StartSystemRealTimeSession` creates a separately owned system logger whose kernel stack events are chosen with `SetStackTracing`. `EnableProvider` and `DisableProvider` change one provider on one session, with `EtwTraceProviderOptions` for keywords, level, timeout and optional stack capture. `ConsumeRealTimeAsync` delivers each event as an `EtwTraceEventRecord` until cancellation or the session ends. Every control call takes the `EtwTraceSessionInfo` returned when the session started and acts only while the running session still matches its name, GUID, handle and output. `StopSessionIfCurrent` returns false when the session has already ended or been replaced. Ending a consumer does not stop its session, so callers stop the sessions they start with `StopSession` or `StopSessionIfCurrent`.

An `EtwTraceEventRecord` keeps the header fields, raw payload bytes, `EtwTraceProperty` values decoded with their manifest bytes, and `EtwTraceExtendedData` items such as stack addresses. Header process IDs describe the event writer, not the owner of the payload. `EtwCpuProfileDecoder` reads the documented `SampledProfile` and classic `StackWalk` events into `EtwCpuProfileSample` and `EtwCpuProfileStack` without starting a sampler or inferring process identity. Stack frames can be resolved with `Tools.Windows.Symbols` in the [Processes](../Processes/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EtwCpuProfileDecoder`](EtwCpuProfileDecoder.cs) | static class | Provider GUIDs, keyword and event types of kernel CPU sampling, and decoders for `SampledProfile` and `StackWalk` events |
| [`EtwCpuProfileSample`](SpecialTypes.cs) | record | One CPU sample's instruction pointer and sampled thread ID, with availability text |
| [`EtwCpuProfileStack`](SpecialTypes.cs) | record | One `StackWalk` payload with its original timestamp, process and thread IDs and stack addresses |
| [`EtwStackEvent`](SpecialTypes.cs) | record | A provider GUID and event type that selects kernel stack capture on a system logger |
| [`EtwTraceEventRecord`](EtwTraceEventRecord.cs) | record | One detached ETW event with header fields, payload bytes, decoded properties and extended data |
| [`EtwTraceExtendedData`](EtwTraceEventRecord.cs) | record | One extended-header item with type, raw data, match ID and stack addresses |
| [`EtwTraceProperty`](EtwTraceProperty.cs) | record | One manifest property with its original bytes and typed reads of unsigned integers and Unicode strings |
| [`EtwTraceProviderOptions`](EtwTraceProviderOptions.cs) | record | Keywords, level, enable timeout and stack-capture request for one provider on an owned session |
| [`EtwTraceSessionInfo`](EtwTraceSessionInfo.cs) | record | One trace session's name, GUID, session ID, log file, mode and lost-event count |
| [`Tools.Windows.TraceSessions`](EtwTraceUtil.cs) | static class | `Tools.Windows.TraceSessions` session listing, owned session start, provider control, stack tracing, real-time consumption and stop |
