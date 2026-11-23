// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MimeTypeTests {

	[TestCase("png")]
	[TestCase(".png")]
	[TestCase("PNG")]
	[TestCase("photo.png")]
	[TestCase(@"C:\Pictures\Holiday\photo.png")]
	[TestCase("/home/user/pictures/photo.png")]
	[TestCase("archive.tar.png")]
	public void ExtensionsFileNamesAndPathsResolveByTheirExtension(string filePathOrExt) {
		Assert.That(Tools.FileSystem.TryGetMimeType(filePathOrExt, out var mimeType), Is.True);
		Assert.That(mimeType, Is.EqualTo("image/png"));
		Assert.That(Tools.FileSystem.GetMimeType(filePathOrExt), Is.EqualTo("image/png"));
	}

	[TestCase("webp", "image/webp")]
	[TestCase("avif", "image/avif")]
	[TestCase("svg", "image/svg+xml")]
	[TestCase("woff2", "font/woff2")]
	[TestCase("mp4", "video/mp4")]
	[TestCase("webm", "video/webm")]
	[TestCase("mp3", "audio/mpeg")]
	[TestCase("wav", "audio/wav")]
	[TestCase("js", "text/javascript")]
	[TestCase("json", "application/json")]
	[TestCase("wasm", "application/wasm")]
	[TestCase("md", "text/markdown")]
	[TestCase("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
	[TestCase("jfif", "image/jpeg")]
	public void CurrentFormatsMapToTheirRegisteredTypes(string extension, string mimeType)
		=> Assert.That(Tools.FileSystem.GetMimeType(extension), Is.EqualTo(mimeType));

	[Test]
	public void AFileInfoResolvesByItsExtension()
		=> Assert.That(new FileInfo(Path.Combine(Path.GetTempPath(), "report.pdf")).GetMimeType(), Is.EqualTo("application/pdf"));

	[TestCase("unknownextension")]
	[TestCase(@"C:\Data\file.unknownextension")]
	public void UnknownExtensionsMapToTheGenericType(string filePathOrExt)
		=> Assert.That(Tools.FileSystem.GetMimeType(filePathOrExt), Is.EqualTo("application/octet-stream"));

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void AnEmptyArgumentHasNoMimeType(string filePathOrExt) {
		Assert.That(Tools.FileSystem.TryGetMimeType(filePathOrExt, out var mimeType), Is.False);
		Assert.That(mimeType, Is.Null);
		Assert.That(() => Tools.FileSystem.GetMimeType(filePathOrExt), Throws.InstanceOf<InvalidOperationException>());
	}
}
