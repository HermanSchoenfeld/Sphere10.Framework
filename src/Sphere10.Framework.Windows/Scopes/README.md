# Windows Scopes

Adds Windows scope factories to the Framework `Tools.Scope` entry point and provides the processor-group affinity scope.

`WindowsScopeExtensions` uses C# extension members on the [core `Scope` tools](../../Sphere10.Framework/Scopes/README.md), so code that imports the `Tools` namespace calls `Tools.Scope.EnterUnmanagedMemoryScope`, `EnterUnmanagedUnicodeStringScope`, `EnterAlwaysOnScope`, `EnterThreadDpiAwarenessScope`, `EnterAttachToCommandPromptScope` and `EnterTokenPrivilegeScope`. Each returns a readonly struct scope from its owning domain: [Misc](../Misc/README.md) for unmanaged memory, [System](../System/README.md) for the always-on execution state, Windowing for thread DPI awareness, [Processes](../Processes/README.md) for console attachment and [Security](../Security/README.md) for token privileges. Dispose each scope with `using`. The affinity, token-privilege and always-on scopes are thread-bound and must end on the thread that entered them.

`ThreadGroupAffinityScope` pins the current native thread to one processor group and mask with `SetThreadGroupAffinity` and restores the exact previous affinity on disposal. Use it synchronously and nest scopes in reverse disposal order. Never hold it across an `await`. Its `DisposeAsync` returns a task faulted with `NotSupportedException`, and disposal on another thread fails a guard.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ThreadGroupAffinityScope`](ThreadGroupAffinityScope.cs) | struct | Synchronous scope that pins the current thread to a processor-group mask and restores its previous group affinity |
| [`WindowsScopeExtensions`](WindowsScopeExtensions.cs) | static class | Extension members on `Tools.Scope` that create the Windows memory, always-on, DPI, console-attachment and token-privilege scopes |
