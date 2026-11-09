# Events

Sender-free event delegates, call-argument payloads and before and after event arguments for intercepted operations.

`EventHandlerEx` and its generic forms with up to five arguments are the delegate types that Framework events use instead of `EventHandler<TEventArgs>`, so handlers receive the payload without a sender or an `EventArgs` subclass. `EventHandlerExExtensions.InvokeAsync` raises a handler on a thread-pool task. `WaitNext` adds its completion handler to the delegate value it receives rather than to the event field, so raising the original event does not complete the returned task.

The observable collections in [Collections](../Collections/README.md) describe each intercepted call with a `CallArgs` subclass, raise it in a `PreEventArgs<TArgs>` before the operation and in a `PostEventArgs<TArgs>` or `PostEventArgs<TArgs, TResult>` afterwards, and choose which operations notify through `EventTraits`. `ISuppressableEvents` lets a caller switch notifications off during bulk work.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CallArgs`](CallArgs.cs) | class | Indexed argument list of an intercepted call. |
| [`EventHandlerEx`](EventHandlerEx.cs) | delegate | Parameterless event handler without a sender. |
| [`EventHandlerEx<T>`](EventHandlerEx.cs) | delegate | Event handler with one argument and no sender. |
| [`EventHandlerEx<T1, T2>`](EventHandlerEx.cs) | delegate | Event handler with two arguments and no sender. |
| [`EventHandlerEx<T1, T2, T3>`](EventHandlerEx.cs) | delegate | Event handler with three arguments and no sender. |
| [`EventHandlerEx<T1, T2, T3, T4>`](EventHandlerEx.cs) | delegate | Event handler with four arguments and no sender. |
| [`EventHandlerEx<T1, T2, T3, T4, T5>`](EventHandlerEx.cs) | delegate | Event handler with five arguments and no sender. |
| [`EventHandlerExExtensions`](EventHandlerExExtensions.cs) | static class | Adds `InvokeAsync` (raise on a thread-pool task) and `WaitNext` to the `EventHandlerEx` delegates. |
| [`EventTraits`](EventTraits.cs) | enum | Flags for the operation kinds that raise events, such as read, write, count, search, fetch, add, update, insert and remove. |
| [`IndexCountCallArgs`](IndexCountCallArgs.cs) | class | Call arguments with an index and a count. |
| [`IndexItemsCallArgs<T>`](IndexItemsCallArgs.cs) | class | Call arguments with an index and the items at it. |
| [`ISuppressableEvents`](ISuppressableEvents.cs) | interface | Exposes `SuppressNotifications` to switch an object's events off and on. |
| [`ItemsCallArgs<T>`](ItemsCallArgs.cs) | class | Call arguments with a sequence of items. |
| [`PostEventArgs`](PostEventArgs.cs) | class | Base arguments of an event raised after an operation. |
| [`PostEventArgs<TArgs>`](PostEventArgs.cs) | class | After-operation arguments that carry the call's `CallArgs`. |
| [`PostEventArgs<TArgs, TResult>`](PostEventArgs.cs) | class | After-operation arguments that carry the call's arguments and its result. |
| [`PreEventArgs`](PreEventArgs.cs) | class | Base arguments of an event raised before an operation. |
| [`PreEventArgs<TArgs>`](PreEventArgs.cs) | class | Before-operation arguments whose `CallArgs` are passed on to the matching after event. |
| [`Trigger`](Trigger.cs) | class | Object whose `Fire()` raises its `Fired` event. |
| [`VoidCallArgs`](VoidCallArgs.cs) | class | Empty call arguments for operations without parameters. |
