# Presentation

Host-independent web models for navigation menus and messages shown to a user.

`Menu` is a navigation tree of text, URLs, icons, glyphs and child menus. Its parameterless constructor sets the URL to `#` and `SubMenus` to an empty array. The class serializes to XML as nested `<Menu text="..." url="..." icon="..." glyph="...">` elements, so a navigation tree can be kept in an XML file. `UserMessage` pairs message text with a `UserMessageType` of information, warning or error, and defaults to an empty information message. The application renders both models. The ASP.NET Core layer collects messages for the current request through `HttpContext.GetUserMessages()`. The [Sitemap](../Sitemap/README.md) domain holds the other XML-serializable model of this library.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Menu`](Menu.cs) | class | XML-serializable navigation node with `Text`, `Url`, `Icon`, `Glyph` and child `SubMenus`. |
| [`UserMessage`](UserMessage.cs) | class | Sealed message for a user, with its text and `UserMessageType`. |
| [`UserMessageType`](UserMessageType.cs) | enum | Severity of a `UserMessage`, either `Information`, `Warning` or `Error`. |
