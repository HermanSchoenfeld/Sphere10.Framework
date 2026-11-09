# Event Logs

Reads and watches Windows Event Log records as detached entries, and writes Framework log output to an event log.

`WindowsEventLogUtil` (`Tools.Windows.EventLogs`) reads records under caller-supplied limits for entry count, per-read timeout and total time, and disposes each native record after copying it. `ReadEvents` and `ReadEventsAsync` take a log name, and the newer `ReadEventsAsync` overload takes a `WindowsEventLogQuery` with `WindowsEventLogReadOptions`. The query turns provider, event IDs, maximum level, time range, writer process, activity ID and one event-data name/value test into the restricted XPath Windows accepts. `WatchEventsAsync` holds a subscription to new events only until cancellation and delivers each `WindowsEventLogEntry` on a worker, so the callback must do bounded work.

Each entry keeps the read failures of its level, message, XML, properties and system metadata as separate exceptions, so a missing message is not mistaken for an empty one. `WindowsEventLogProperty` values are the native variants, not values parsed from rendered text. `EventLogLogger` is a Framework `LoggerBase` that writes to an event log source and creates the source when it does not exist, which needs administrative rights.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EventLogLogger`](EventLogLogger.cs) | class | `LoggerBase` that writes debug, info, warning and error messages to a named event source, creating the source on first use |
| [`WindowsEventLogEntry`](WindowsEventLogEntry.cs) | class | One detached event record: identity, time, provider, level, message, XML, writer IDs, activity IDs, user SID, properties and per-field read errors |
| [`WindowsEventLogUtil`](WindowsEventLogUtil.cs) | class | Bounded historical reads and cancellable live subscriptions that return detached `WindowsEventLogEntry` rows |
