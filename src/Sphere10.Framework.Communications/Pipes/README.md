# Anonymous Pipes

Provides a `ProtocolChannel` over a pair of anonymous pipes between a parent process and a child process it starts.

`AnonymousPipe.ToChildProcess(path, arguments, argInjectorFunc)` creates an `AnonymousServerPipe` that starts the child and passes it the two pipe handles on its command line. Without an `argInjectorFunc` the read handle and then the write handle are appended to the arguments. The child calls `AnonymousPipe.FromChildProcess(new AnonymousPipeEndpoint(readHandle, writeHandle))`, which creates an `AnonymousClientPipe` that reverses the handle directions. Each message travels as one line of text. `TrySendBytes` writes Base64 and raises `ReceivedBytes` on the other side, while `TrySendString` writes a raw line reported by `ReceivedString`. Closing the server pipe waits up to five seconds for the child to exit and then kills it.

The channel lifecycle (`Open`, `Close`, `ReceivedBytes`) comes from the core `ProtocolChannel`, so these pipes can carry typed protocols, as the [project README](../README.md) shows. Sibling transports are [TCP](../TCP/README.md), [UDP](../UDP/README.md) and [WebSockets](../WebSockets/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AnonymousClientPipe`](AnonymousClientPipe.cs) | class | Child-process side that opens the pipes from an `AnonymousPipeEndpoint` received from the parent |
| [`AnonymousPipe`](AnonymousPipe.cs) | abstract class | Line-based `ProtocolChannel` over two anonymous pipe streams, with Base64 byte messages, raw string messages and factory methods for each side |
| [`AnonymousPipeEndpoint`](AnonymousPipeEndpoint.cs) | class | Reader and writer pipe handle strings that identify one end of the pipe pair |
| [`AnonymousServerPipe`](AnonymousServerPipe.cs) | class | Parent-process side that creates the pipes, starts the child process with their handles and stops the child on close |
