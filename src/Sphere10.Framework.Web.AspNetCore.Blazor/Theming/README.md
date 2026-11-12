# Theming

The scoped theme service and the catalogue of built-in theme presets with their stylesheet paths and dark or light classification.

`AddSphere10Blazor` registers `IThemeService` as a scoped `ThemeService`, which starts with `ThemeMode.Light`. Register your own `ThemeService(ThemeMode)` before `AddSphere10Blazor` to choose another default. `SetTheme` selects a preset and raises `Changed` when the theme actually changes. `Toggle` selects `Light` from any dark preset and `Dark` from any light preset. The selection lasts for the service scope and is not persisted across browser reloads. `ThemeCatalog.All` lists one `ThemeDefinition` for each `ThemeMode`, and the theme provider, selector and toggle components in the `Theming` folder of [UI](../UI/README.md) render from it.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IThemeService`](IThemeService.cs) | interface | Current `ThemeMode` of one service scope, with `SetTheme`, `Toggle` and a `Changed` event. |
| [`ThemeCatalog`](ThemeCatalog.cs) | static class | Immutable definitions of every built-in theme, with `Get` and `TryGet` lookups by mode. |
| [`ThemeDefinition`](ThemeDefinition.cs) | class | Mode, stable ID, display name, stylesheet path and dark flag of one built-in theme. |
| [`ThemeMode`](ThemeMode.cs) | enum | The 20 built-in presets, such as `Light`, `Dark`, `Blue`, `ClassicBlue`, `Hud` and `AppStack`. |
| [`ThemeService`](ThemeService.cs) | class | Default `IThemeService` that holds the current theme, defaulting to `Light`. |
| [`ThemeServiceBase`](ThemeServiceBase.cs) | abstract class | Base theme service whose `Toggle` calls `SetTheme` with `Light` or `Dark` and whose `OnChanged` raises `Changed`. |
