# Installed Applications

Reads, removes and launches entries from the Windows uninstall registry without deciding which applications to show or run.

`WindowsUninstallRegistration` works over `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`. `GetRoots` returns the current-user and local-machine roots as [`RegistryAddress`](../Registry/README.md) values, plus the 32-bit machine view on 64-bit Windows. `GetRegistrations` lists the registration keys under one root, and `ReadRegistration` reads one key as a bounded `RegistryTree`. Each read has an async variant that runs on a worker.

`RemoveRegistration` deletes only the key it is given. The caller verifies and approves the registration first. `Launch` starts a caller-selected executable through the Windows shell and returns a `Process` that the caller owns. Registry access goes through `RegistryUtil` in the [Registry](../Registry/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowsUninstallRegistration`](WindowsUninstallRegistration.cs) | class | Lists and reads uninstall-registration keys in the user and machine hives, deletes one supplied key and shell-launches a chosen executable |
