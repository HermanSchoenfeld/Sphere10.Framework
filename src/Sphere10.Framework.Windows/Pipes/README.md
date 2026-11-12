# Named Pipes

Lists local named pipes and reports the process at the other end of a connected pipe stream.

`Tools.Windows.Pipes` lists the names under `\\.\pipe\` with `GetNames` or `GetNamesAsync` without opening any pipe instance, and `GetPath` validates a relative pipe name and builds its full path. `GetClientProcessId` and `GetServerProcessId` call `GetNamedPipeClientProcessId` and `GetNamedPipeServerProcessId` on an already connected stream that the caller keeps owning. `NamedPipePeer` exposes the same two peer queries as static methods.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`NamedPipePeer`](NamedPipePeer.cs) | static class | Returns the client PID of a connected `NamedPipeServerStream` or the server PID of a connected `NamedPipeClientStream` |
| [`Tools.Windows.Pipes`](NamedPipeUtil.cs) | static class | `Tools.Windows.Pipes` listing of local pipe names, validated pipe path building and peer PID queries on borrowed streams |
