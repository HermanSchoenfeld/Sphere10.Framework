# Logging

Windows Forms log views, text box loggers and an optional log output selector with a configuration wizard and remembered preferences.

`TextBoxLogger` writes timestamped messages to a `TextBox` through `TextBoxWriter`, which appends on the box's UI thread. `LogScreen` is a host-owned application screen whose `Logger` posts bounded worker writes to its text view, and `TextLog` is a plain multiline text surface. These views display messages and do not choose an output.

`LogOutputSelectorControl` lets a user turn optional logging on and choose a file, the application log, a screen or a standard stream. The first check opens a wizard built from the `LogOutput*WizardScreen` steps, and `LogOutputConfigurationDialog` shows the current details with a Change button. `LogOutputConfiguration` holds the chosen destination, file, rolling, encoding, level and timestamp options. `LogOutputSelectorPreferences` carries the remembered values when a screen owns persistence, and `LogOutputSelectorSettings` is the selector's own settings record. Configure, start and stop the selector on its UI thread. `EnqueueInfo` is safe from worker threads, and `DisposeAsync` returns a drain that the owner should await or retain. `LogOutputLifetime` keeps accepted writes alive through screen destruction and application exit when an application registers it as a service. `Tools.Windows.Forms.Logging` (`WindowsFormsLoggingUtil`) creates selectors and registers outputs with that service. The project README describes the selector workflow in detail. Chart log outputs live in [Charts](../Charts/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`LogOutputConfiguration`](LogOutputConfiguration.cs) | class | Plain output options for the destination, file path, rolling limits, encoding, levels, timestamps and Remember After Close |
| [`LogOutputDestination`](LogOutputDestination.cs) | enum | Output target, which is a file, the application log, standard output, standard error or a screen |
| [`LogOutputLifetime`](LogOutputLifetime.cs) | class | Application service that closes registered outputs on their UI thread and joins their pending drains at exit |
| [`LogOutputSelectorPreferences`](LogOutputSelectorPreferences.cs) | class | Settings object with the remembered enabled choice and configuration, persisted only when remembering was requested |
| [`LogOutputSelectorSettings`](LogOutputSelectorSettings.cs) | class | Settings record of a selector's configuration and logging state, keyed by its owner identity |
| [`TextBoxLogger`](TextBoxLogger.cs) | class | Logger decorator that writes timestamped messages asynchronously to a `TextBox` |
| [`TextBoxWriter`](TextBoxWriter.cs) | class | Synchronized text writer that appends to a `TextBox` on its UI thread and ignores a disposed box |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`LogOutputSelectorControl`](Controls/LogOutputSelectorControl.cs) | class | Check box and options button that configure, start, stop, queue to and persist an optional log output |
| [`TextLog`](Controls/TextLog.cs) | class | Multiline text view with native selection and `AppendText`, in the `Sphere10.Framework.Windows.Forms.TextBoxes` namespace |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`LogOutputConfigurationDialog`](Forms/LogOutputConfigurationDialog.cs) | class | Shows a selector's current output details with start, stop and change actions |
| [`LogOutputDestinationWizardScreen`](Forms/LogOutputDestinationWizardScreen.cs) | class | Wizard step that chooses the output destination |
| [`LogOutputFileWizardScreen`](Forms/LogOutputFileWizardScreen.cs) | class | Wizard step that chooses and validates the output file |
| [`LogOutputFormattingWizardScreen`](Forms/LogOutputFormattingWizardScreen.cs) | class | Wizard step that chooses message levels, timestamps, file encoding and Remember After Close |
| [`LogOutputReviewWizardScreen`](Forms/LogOutputReviewWizardScreen.cs) | class | Wizard step that summarises the chosen configuration before Finish |
| [`LogOutputRollingWizardScreen`](Forms/LogOutputRollingWizardScreen.cs) | class | Wizard step that turns rolling files on and sets their count and size limits |
| [`LogOutputWizardScreen`](Forms/LogOutputWizardScreen.cs) | class | Non-generic designer base of the logging wizard steps over a `LogOutputConfiguration` |
| [`LogScreen`](Forms/LogScreen.cs) | class | Application screen whose logger posts bounded writes to its text view and drops messages outside its lifetime |
