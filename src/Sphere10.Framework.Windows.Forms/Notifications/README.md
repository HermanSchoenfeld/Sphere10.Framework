# Notifications

A passive on-screen popup that confirms a state change without taking focus.

`OnScreenNotificationForm` shows a short message centred near the bottom of a screen's working area, above ordinary windows, and hides itself after `DisplayDurationMilliseconds`. It never activates, so keyboard focus stays where it was. `ShowMessage(message)` uses the screen under the pointer, and `ShowMessage(message, screen)` uses a given screen. Reuse one instance for repeated status messages. The static `ShowTransient(message, screen, displayDurationMilliseconds)` creates a one-off popup that disposes itself once it hides, for example to show a number on every monitor. Call these members on the UI thread.

## Contents

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`OnScreenNotificationForm`](Forms/OnScreenNotificationForm.cs) | class | Topmost, non-activating status popup with a display duration, bottom margin and self-disposing transient variant |
