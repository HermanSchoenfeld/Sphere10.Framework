# Process Sentries

Read this topic when wrapping an external command-line executable or choosing
between a Sentry and a native Windows utility.

## Executable wrappers

[ProcessSentry](../../../src/Sphere10.Framework/Environment/ProcessSentry.cs)
is the existing command-process wrapper. It owns each launched process, serializes
command admission with AsyncSynchronizedGate and completes only after process exit
and stdout/stderr collection. `RunAsync` returns its exit code;
`RunCapturedAsync(string[])` uses structured arguments and returns a detached
[ProcessExecutionResult](../../../src/Sphere10.Framework/Environment/ProcessExecutionResult.cs).
`RunWithErrorCodeCheckAsync` throws
[ProcessSentryException](../../../src/Sphere10.Framework/Environment/ProcessSentryException.cs)
with the actual completed transcript when the code differs from the expected code.
The output writer is borrowed, serialized and never disposed by the sentry.

| Sentry | Executable boundary |
|---|---|
| [NssmSentry](../../../src/Sphere10.Framework.Windows/Services/NssmSentry.cs) | `nssm.exe`: typed Install, Remove, Start and Stop commands. NSSM must be available to the consuming process. |
| [ServiceControlManagerSentry](../../../src/Sphere10.Framework.Windows/Services/ServiceControlManagerSentry.cs) | `sc.exe`: command-line service creation/deletion. |
| [PktMonSentry](../../../src/Sphere10.Framework.Windows/Networking/PktMonSentry.cs) | In-box `pktmon.exe`: structured read-only queries, explicit start/stop and ETL conversions; bounded owned command execution. |

A Sentry is not a container for native methods merely because its executable
operates on the same domain. Windows service queries and native controls belong
to `ServiceUtil`, accessed through `Tools.Windows.Services`. Keep native resource
utilities separate from executable wrappers. See
[Windows resource layers](framework-reuse.md#windows-abstraction-layers) and
[Windows utility discovery](framework-utilities.md#windows-utility-facade-and-source-folders).

Inspect executable selection, grammar, accepted exit codes, output routing,
ownership and real consumers before extending a Sentry. Typed command construction
belongs in the wrapper; process mechanics belong in ProcessSentry. Prefer the
structured argument API. Raw argument strings require the executable's precise
quoting rules. A zero exit code is only the command's result, not proof of a
Windows resource's identity or the completion of its application workflow.

## Cancellation and ownership

Cancellation terminates the launched command's owned process tree and drains its
exit/output before returning cancellation. Cleanup has a separate five-second
budget; failed cleanup is reported together with the original error. Queued
cancellation does not kill the currently admitted command. Output writer failures
also terminate/drain the owned child rather than abandon it behind a failed pump.
`BreakRunningProcess` requests termination; callers still await the run task for
completion. `CanRunAsync` preserves cancellation and returns false for nonzero
exit codes or launch failures. It is not a semantic health check.

The generic runner has no default execution deadline: the caller supplies one.
PktMonSentry applies a 30-second command deadline. Killing a helper cannot roll back
a command that already changed Windows; the application must reconcile the resource
through its native middle-layer identity and state. ProcessSentry does not elevate
via `runas`; use the consuming application's explicit supported elevation path.

For PktMon, stopping the short CLI helper is different from stopping a system-wide
capture. SystemExpert owns the capture policy, checks native ETW identity and a
unique output path before the documented global stop, and rechecks afterward.
External controllers can race that checked global operation. A failed/unknown
cleanup remains visible; no global filter reset or unverified foreign stop is
issued. Applications must state these built-in controller limits honestly.

[ProcessSentryTests](../../../tests/Sphere10.Framework.Tests/Environment/ProcessSentryTests.cs)
exercise harmless owned children, completed transcripts, cancellation, queued
admission and borrowed-writer ownership. Native wrappers remain distinct from
executable sentries; follow [sync/async contracts](sync-async.md) and
[resource scopes](resource-scopes.md).
