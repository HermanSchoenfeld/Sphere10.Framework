// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using NUnit.Framework;

namespace Sphere10.Framework.Tests;

// The core library embeds its data files through Properties/Resources.resx; these read each one through the generated Resources class.
[TestFixture]
public class EmbeddedResourceTests {

	[Test]
	public void TheMimeTypeTableIsEmbedded() => Assert.That(Resources.MimeTypes, Is.Not.Empty);

	[Test]
	public void TheStopWordListLoadsFromTheResources() {
		var stopWords = RakeAlgorithm.StopListHelper.ReadDefaultStopListWords();
		Assert.That(stopWords, Has.Count.GreaterThan(100));
		Assert.That(stopWords, Does.Contain("the"));
	}
}
