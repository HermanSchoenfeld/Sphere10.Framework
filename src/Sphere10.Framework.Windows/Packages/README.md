# App Packages

Lists and removes the current user's MSIX and AppX packages through the in-box Windows PowerShell Appx module.

`AppxPackageSentry` is a `ProcessSentry` that runs Windows PowerShell without profiles or an interactive shell. `GetPackagesAsync` returns `WindowsAppPackageInfo` records with identity, publisher, install location, signature kind, status and framework, resource, bundle and optional flags. `RemovePackageAsync` removes one full package name from the current user and then checks that the registration is gone. Both operations target the current user even when the caller is elevated. Other users' registrations and provisioned images are not changed. Cancellation terminates the helper process but cannot roll back a removal that Windows has already started.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AppxPackageSentry`](AppxPackageSentry.cs) | class | `ProcessSentry` that lists the current user's Appx packages and removes one package by full name, verifying the removal |
| [`WindowsAppPackageInfo`](WindowsAppPackageInfo.cs) | record | Detached identity and metadata of one registered MSIX or AppX package |
