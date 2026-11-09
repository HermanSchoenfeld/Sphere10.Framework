# Explorer Bar Designer

A Windows Forms tool for designing a custom theme for the Framework's explorer-bar `TaskPane` and exporting it as XML.

The designer opens with a sample task pane of two expandos with two task items each. Its property grid shows an `ExplorerBarSettings` object grouped into the **Task Pane**, **Task Item**, **Task Expando** and **Header Info** categories. Every property change converts the settings to an `ExplorerBarInfoSurrogate` and applies it to the sample with `TaskPane.UseCustomTheme`, so the preview updates at once. Each session starts from the Luna theme in [Resources](Resources/README.md).

**Export** serializes the current settings with `Tools.Xml.WriteToString` and shows the XML in a text window for copying. An application applies that theme by reading the XML with `Tools.Xml.Read<ExplorerBarInfoSurrogate>` and passing the result to `TaskPane.UseCustomTheme`. The explorer-bar controls and the surrogate types belong to [Sphere10.Framework.Windows.Forms](../../src/Sphere10.Framework.Windows.Forms/ExplorerBar/README.md).

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Utils.ExplorerBarDesigner` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Resources](Resources/README.md) | The Luna explorer-bar theme that the designer loads as its starting point. | [source](Resources/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`DesignerForm`](DesignerForm.cs) | class | Main form that edits an explorer-bar theme in a property grid, previews it on a task pane and exports it as XML. It starts from the embedded Luna theme. |
| [`ExplorerBarSettings`](DesignerForm.cs) | class | Flat property-grid model of every task pane, task item, expando and header setting, converted to and from `ExplorerBarInfoSurrogate` by `DesignerForm`. |
| [`ExportForm`](ExportForm.cs) | class | Dialog whose static `ShowDialogAsync(parent, text)` shows the exported theme XML in a text box. |
<!-- domains:end -->

## Build and run

The project targets `net10.0-windows` and references `Sphere10.Framework.Windows.Forms`. `src/Sphere10.Framework (Win).sln` builds it in its Utils folder in every configuration. `src/Sphere10.Framework (CrossPlatform).sln` does not include it.

From the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.ExplorerBarDesigner -c Release -p:BuildRevision=0
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DesignerForm`](DesignerForm.cs) | partial class | Main window with the sample task pane, the settings property grid and the **Export** button, and the `ToSettings` and `FromSettings` conversions between `ExplorerBarInfoSurrogate` and `ExplorerBarSettings`. |
| [`ExplorerBarSettings`](DesignerForm.cs) | class | Property-grid model of every task pane, task item, expando and header color, image, border, padding, font and alignment setting. |
| [`ExportForm`](ExportForm.cs) | partial class | Text window whose `ShowDialogAsync` shows the exported theme XML. |
| [`Resources`](Properties/Resources.Designer.cs) | class | Generated resource accessor whose `LunaExplorerBarTheme` string holds the starting theme. |
