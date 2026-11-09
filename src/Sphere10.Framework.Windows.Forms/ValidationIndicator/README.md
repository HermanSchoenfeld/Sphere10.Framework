# Validation Indicator

A small status icon control that runs asynchronous validation handlers and shows valid, error, validating or disabled state with a message tooltip.

`ValidationIndicator` is a `UserControlEx`, described in [State Events](../StateEvent/README.md). Call `RunValidation` to start a cycle. The control switches to `ValidationState.Validating`, runs `OnPerformValidation` and each `PerformValidation` handler on the thread pool, and then shows `Valid` only when every handler set `ValidationResult` to `true`. The tooltip combines the messages of the handlers that agree with the outcome. Requests made during a cycle are folded into one further cycle, and only the last cycle's result is shown. `ValidationStateChanged` reports each state change. When `RunValidatorWhenEnabled` is set, enabling the control starts a cycle, and disabling it hides the control.

Handlers run on worker threads, so they must not touch controls directly.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ValidationIndicatorEvent`](ValidationIndicatorEvent.cs) | class | Event data that a validation handler fills with its result and message. |
| [`ValidationState`](ValidationState.cs) | enum | Indicator states: valid, error, validating and disabled. |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ValidationIndicator`](Controls/ValidationIndicator.cs) | class | Icon control that runs validation handlers off the UI thread and shows the combined state and message. |
