// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.VisualRenderer.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PlainTextRenderingTests {
	[TestCase("Standalone <renderer>", TestName = "TextIncludesDocumentTitle")]
	[TestCase("https://example.test/original", TestName = "TextPreservesSourceVisibleLinkOverride")]
	[TestCase("Nested value", TestName = "TextTraversesNestedDatabaseValues")]
	[TestCase("code text fallback", TestName = "TrustedMarkupUsesExplicitTextRepresentation")]
	public void TextIncludesPreparedContent(string expectedText) {
		var text = new TextRenderer().Render(RendererTestData.CreateDocument());
		Assert.That(text, Does.Contain(expectedText));
	}

	[Test]
	public void EmptyTextOverrideSuppressesHtmlOnlyContent() {
		var text = new TextRenderer().Render(RendererTestData.CreateDocument());
		Assert.That(text, Does.Not.Contain("Ignored HTML-only content"));
	}
}
