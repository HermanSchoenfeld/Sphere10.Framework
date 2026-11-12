# Sessions

Lists interactive and Remote Desktop sessions on a local or remote server and logs off, disconnects, messages or shadows a verified session.

`Tools.Windows.Sessions` enumerates sessions through the WTS API as `WindowsSessionInfo` records with station, user, domain, connection state, logon time, client name and protocol. It reports the current session ID and whether that session is remote. `Logoff`, `Disconnect`, `SendMessage` and `StartRemoteControl` take the `WindowsSessionInfo` the caller read and act only when the session still matches it, which `MatchesIdentity` checks by server, session ID, user, domain and logon time. `SendMessage` does not wait for the user's answer. Native permissions and remote-control consent stay under Windows policy. Every operation has an async variant that runs on a worker.

`WtsServerScope` opens a remote WTS server handle and closes it on disposal, and it borrows the local-server pseudo handle without closing it. `WtsMemoryScope` frees memory returned by WTS functions with `WTSFreeMemory`. Process session IDs are read from `WindowsProcessCounters` in the [Processes](../Processes/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.Windows.Sessions`](SessionUtil.cs) | static class | `Tools.Windows.Sessions` session listing, current-session queries and identity-checked logoff, disconnect, message and remote control |
| [`WindowsSessionInfo`](WindowsSessionInfo.cs) | record | One WTS session with server, ID, station, user, domain, state, logon time, client and read error code, and an identity match check |
| [`WtsMemoryScope`](WtsMemoryScope.cs) | class | Scope that frees WTS-allocated memory with `WTSFreeMemory` |
| [`WtsServerScope`](WtsServerScope.cs) | class | Scope that opens a named WTS server and closes its handle, borrowing the local-server handle when no name is given |
