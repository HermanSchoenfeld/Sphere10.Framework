// Copyright (c) Herman Schoenfeld 2018 - Present. Distributed under the MIT license.

using System;
using System.IO;
using Sphere10.Framework.Web.VisualRenderer;

var document = new DocumentBlock {
	Title = "NuGet renderer check",
	Children = [new ParagraphBlock { Text = [new TextInline { Text = "Rendered from the package" }] }]
};
var result = new HtmlRenderer().Render(document, new RenderOptions { Environment = RenderEnvironment.Offline });
if (!result.Html.Contains("Rendered from the package", StringComparison.Ordinal))
	throw new InvalidOperationException("The packaged renderer did not render its visual model.");
using var themes = EmbeddedThemes.OpenArchive();
if (themes.GetEntry("default/code.html") is null)
	throw new InvalidOperationException("The packaged embedded themes are incomplete.");
foreach (var name in new[] { "LICENSE.txt", "THIRD-PARTY-NOTICES.md" }) {
	if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "licenses", "Sphere10.Framework.Web.VisualRenderer", name)))
		throw new InvalidOperationException("The renderer build targets did not copy " + name);
}
Console.WriteLine("Packaged renderer, embedded themes and license deployment passed.");
