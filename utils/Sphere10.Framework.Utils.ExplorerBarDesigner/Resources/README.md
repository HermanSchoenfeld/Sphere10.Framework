# Resources

The Luna explorer-bar theme that the designer loads as its starting point.

`LunaExplorerBarTheme.xml` is an `ExplorerBarInfoSurrogate` document that sets the task pane, task item, expando and header colors, padding, borders and images. `Properties/Resources.resx` embeds it as the `LunaExplorerBarTheme` string resource. `DesignerForm` reads it with `Tools.Xml.Read<ExplorerBarInfoSurrogate>` and converts it to the `ExplorerBarSettings` object shown in its property grid. Edit this file to change the theme a new designer session starts from.

## Contents

This folder declares no types. It holds one data file.

| File | Purpose |
|---|---|
| [`LunaExplorerBarTheme.xml`](LunaExplorerBarTheme.xml) | Serialized `ExplorerBarInfoSurrogate` holding the default Luna theme for the designer. |
