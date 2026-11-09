# Services

A status indicator and start/stop button for one Windows service.

`ServiceStatusControl` shows a coloured status light, a status label and a detail line for the service named by `ServiceName`. Call `Start()` after the control is on a form. It schedules a synchronous job that polls the service every `RefreshRateSec` (2) seconds and removes the job when the parent form is disposed. Setting `Status` updates the display directly. With `UseNssm` set, the button starts or stops the service through NSSM. Without it, the handler only calls the service controller when the service lookup fails, so the button has no effect on an installed service.

## Contents

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ServiceStatusControl`](Controls/ServiceStatusControl.cs) | class | Polls a Windows service, shows its state as a traffic light and starts or stops it through NSSM |
