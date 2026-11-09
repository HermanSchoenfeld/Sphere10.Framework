# Progress Bar

A progress bar that draws a percentage or custom text over the bar.

`ProgressBarEx` paints the bar itself with the visual style renderer and centres a caption on it. With `DisplayStyle` set to `Percentage`, the caption is `Value` followed by a percent sign, so it equals the real percentage only when `Maximum` is 100. With `CustomText`, the caption is the `CustomText` property.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ProgressBarDisplayText`](ProgressBarDisplayText.cs) | enum | Whether the bar shows its value as a percentage or the custom text |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ProgressBarEx`](Controls/ProgressBarEx.cs) | class | Double-buffered progress bar that draws a centred percentage or custom caption |
