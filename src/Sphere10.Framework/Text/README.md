# Text

String formatting with named tokens, type-converter parsing, casing and inflection, tokenizing, keyword extraction, Unicode block ranges and a fluent regular-expression builder.

`StringFormatter.FormatEx` extends `string.Format` with `{Name}` tokens. A token is resolved from a caller-supplied function or dictionary, then from the registered `ITokenResolver` instances, which by default know `CurrentDate`, `CurrentYear` and `StartPath`, and numeric tokens such as `{0}` still take positional arguments. `Tools.Text` exposes the same formatting next to casing conversion (`ToCasing` with a `TextCasing` and a `FirstCharacterPolicy`), sentence enumeration, random strings, Base62 and Base32 conversion and optionally encrypted text compression. `GenericParser`, and `Tools.Parser` which forwards to it, parse a string into any type that has a `TypeConverter`, or into an enum.

`Inflector` pluralizes, singularizes and converts between Pascal, camel, underscored and dashed word forms. `RakeAlgorithm` scores candidate keywords in text with the RAKE method and an embedded English stop-word list. `StringTokenizer` splits a string by delimiter characters and walks the tokens. `FastStringBuilder` and `ParagraphBuilder` build strings from parts and sentences. `UnicodeRange` names the Unicode blocks as `ValueRange<char>` values. The `ReadableRex` subfolder builds regular expressions from readable fluent calls, starting from `RegexPattern.With`. The [TextWriters](../TextWriters/README.md) domain holds the `TextWriter` sinks and decorators.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DictionaryTokenResolver`](DictionaryTokenResolver.cs) | class | `ITokenResolver` that looks tokens up in a dictionary. |
| [`FastStringBuilder`](FastStringBuilder.cs) | class | Collects appended, prepended and inserted strings in a list and joins them once in `ToString()`. |
| [`FirstCharacterPolicy`](FirstCharacterPolicy.cs) | enum | Flags for which characters may start an identifier produced by `Tools.Text.ToCasing`, with `HtmlDomObj` and `CSharpVar` presets. |
| [`GenericParser`](GenericParser.cs) | static class | `Parse`, `TryParse` and `SafeParse` from a string to any type with a `TypeConverter`, or to an enum. |
| [`Inflector`](Inflector.cs) | static class | English pluralization and singularization plus title, human, Pascal, camel, underscore, dash and ordinal word forms. |
| [`ITokenResolver`](ITokenResolver.cs) | interface | Resolves a named format token to a value for `StringFormatter`. |
| [`ParagraphBuilder`](ParagraphBuilder.cs) | class | Builds text from trimmed sentences, converted to sentence case by default, with paragraph breaks between paragraphs. |
| [`RakeAlgorithm`](RakeAlgorithm.cs) | class | RAKE keyword extraction that scores candidate phrases in text samples against a stop-word list. |
| [`StringFormatter`](StringFormatter.cs) | static class | `FormatEx` and `FormatWithDictionary` that resolve `{Name}` tokens through resolvers, optionally recursively, alongside positional arguments. |
| [`StringTokenizer`](StringTokenizer.cs) | class | Splits a string by delimiter characters and walks the tokens with `NextToken`, `PeekToken` and `SkipToken`. |
| [`StringWriterEx`](ExtentedStringWriter.cs) | class | `StringWriter` that reports a chosen `Encoding` instead of UTF-16. |
| [`TextBank`](TextBank.cs) | class | List of the string forms of objects that can be searched for substrings. |
| [`TextCasing`](TextCasing.cs) | enum | Target casings `PascalCase`, `CamelCase`, `SnakeCase` and `KebabCase`. |
| [`Tools.Parser`](ParseTool.cs) | tools | `Parse`, `TryParse` and `SafeParse` that forward to `GenericParser`. |
| [`Tools.Text`](TextTool.cs) | tools | Casing conversion, sentence search, token formatting, random strings, Base62 and Base32, byte conversion and text compression helpers. |
| [`UnicodeRange`](UnicodeRange.cs) | static class | Named `ValueRange<char>` values for the Unicode blocks, from `BasicLatin` onwards. |

### ReadableRex

| Type | Kind | Purpose |
|---|---|---|
| [`RegexAlternation`](ReadableRex/RegexAlternation.cs) | class | Alternation and conditional constructs reached through `RegexPattern.Choice`. |
| [`RegexLazyQuantifier`](ReadableRex/RegexLazyQuantifier.cs) | class | Lazy forms of the quantifiers, reached through `RegexQuantifier.Lazy`. |
| [`RegexPattern`](ReadableRex/RegexPattern.cs) | class | Fluent regular-expression builder with literals, character classes, groups, sets and anchors that converts to a pattern string. |
| [`RegexQuantifier`](ReadableRex/RegexQuantifier.cs) | class | Repetition counts for the preceding element, reached through `RegexPattern.Repeat`. |
| [`RegexRange`](ReadableRex/RegexRange.cs) | class | Character ranges such as `Of('a', 'z')` and `AnyLetter` for use inside sets. |
