# Startup

Reads where Windows starts programs at logon or boot (Run keys, startup folders, StartupApproved data, logon registry entries, scheduled tasks and WMI subscriptions) without changing them.

`WindowsStartupUtil` (`Tools.Windows.Startup`) lists the Run and RunOnce registry locations as [`RegistryAddress`](../Registry/README.md) values, reads their values, maps a location to its StartupApproved category and reads the raw approval bytes without interpreting the enabled state. It returns the per-user or all-users startup folder and its files. `ReadRegistrations` reads the extra launch-source families chosen in `WindowsStartupSources` as `WindowsStartupRegistration` records, bounded by a row limit and a WMI query timeout. A registration's presence is not evidence that it has run.

`ReadTarget` interprets one registered command line as a `WindowsStartupTarget` without searching `PATH` or starting a process. `ReadShortcutTargetAsync` reads a startup shortcut's stored fields through the [Shell](../Shell/README.md) domain without resolving it. `WindowsStartupRegistrationDataSource` captures the selected sources once and then pages, searches and sorts the captured rows without querying WMI or the registry again.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowsStartupUtil`](WindowsStartupUtil.cs) | class | `Tools.Windows.Startup` Run-key, StartupApproved, startup-folder, launch-source and target reads |
