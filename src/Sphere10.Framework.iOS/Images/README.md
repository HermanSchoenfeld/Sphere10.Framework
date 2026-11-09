# Images

PNG images that the legacy iOS project embeds as resources for its calendar view, combo box and placeholder images.

The project file declares every image here as an `EmbeddedResource`. `CalendarMonthView` in [UI](../UI/README.md) requests the `Calendar` images by file name through the internal `Tools.iOSTool.EmbeddedImage` helper, and `UIComboBox` requests `closebox.png` the same way. The `MissingImage`, `MissingFile`, `ErrorImage` and `BlankImage` properties of `Tools.iOSTool` request the matching `Misc` images by name without the extension. The project is reference source that no solution or CI job builds, as the [project README](../README.md) explains.

## Contents

This folder declares no types. It contains these image resources.

### Calendar

| File | Used by |
|---|---|
| [`datecell.png`](Calendar/datecell.png) | `CalendarMonthView` day cell |
| [`datecellmarked.png`](Calendar/datecellmarked.png) | `CalendarMonthView` marked day cell |
| [`datecellselected.png`](Calendar/datecellselected.png) | `CalendarMonthView` selected day cell |
| [`leftarrow.png`](Calendar/leftarrow.png) | `CalendarMonthView` previous-month button |
| [`rightarrow.png`](Calendar/rightarrow.png) | `CalendarMonthView` next-month button |
| [`shadow.png`](Calendar/shadow.png) | `CalendarMonthView` shadow |
| [`today.png`](Calendar/today.png) | `CalendarMonthView` today cell |
| [`todayselected.png`](Calendar/todayselected.png) | `CalendarMonthView` selected today cell |
| [`topbar.png`](Calendar/topbar.png) | `CalendarMonthView` header bar |

### Misc

| File | Used by |
|---|---|
| [`blank.png`](Misc/blank.png) | `Tools.iOSTool.BlankImage` |
| [`closebox.png`](Misc/closebox.png) | `UIComboBox` close button |
| [`closebox_pressed.png`](Misc/closebox_pressed.png) | No code reference |
| [`error-image.png`](Misc/error-image.png) | `Tools.iOSTool.ErrorImage` |
| [`missing-file.png`](Misc/missing-file.png) | `Tools.iOSTool.MissingFile` |
| [`missing-image.png`](Misc/missing-image.png) | `Tools.iOSTool.MissingImage` |
| [`No.png`](Misc/No.png) | No code reference |
| [`Yes.png`](Misc/Yes.png) | No code reference |
