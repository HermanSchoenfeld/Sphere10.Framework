# Rich Text

An RTF value type, its conversions and an extended rich text box with underline styles, subscript, superscript and typed values.

`RichText` wraps an RTF string so that rich text values can be told apart from plain strings, and it compares values by their plain text. `RichTextConversion` turns plain text into RTF, optionally with a font style, and RTF back into plain text. `RichTextTypeConverter` converts between `RichText` and strings for designers and data binding. `RichTextBoxEx` extends `RichTextBox` with `SelectionUnderlineStyle`, `SelectionUnderlineColor` and `SelectionEffect`, an `IValidator`-based typed `Value`, `BeginUpdate` and `EndUpdate`, styled appends, format removal and content measurement. The rich text cells of [SourceGrid](../SourceGrid/README.md) store `RichText` values and edit them with `RichTextBoxEx`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EffectType`](EffectType.cs) | enum | Normal, subscript or superscript character effect |
| [`RichText`](RichText.cs) | class | Wrapper around an RTF string that distinguishes rich text from plain text and compares by plain text |
| [`RichTextConversion`](RichTextConversion.cs) | static class | Converts plain text to `RichText`, optionally with a font style, and `RichText` to plain text, optionally without tabs and line breaks |
| [`RichTextTypeConverter`](RichTextTypeConverter.cs) | class | Type converter between `RichText` and `string` that also validates RTF strings |
| [`UnderlineColor`](UnderlineColor.cs) | enum | Rich edit underline colours |
| [`UnderlineStyle`](UnderlineStyle.cs) | enum | Rich edit underline styles such as word, double, dotted, dashed and wave |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`RichTextBoxEx`](Controls/RichTextBoxEx.cs) | class | Rich text box with underline style and colour, subscript and superscript, typed values, update batching and measurement |
