// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using System;
using System.Net;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class ParserTests {

	[Test]
	public void Parse() {
		Assert.That(Tools.Parser.Parse<byte>("1"), Is.EqualTo(1));
		Assert.That(Tools.Parser.Parse<int>("1"), Is.EqualTo(1));
		Assert.That(Tools.Parser.Parse<long>("1"), Is.EqualTo(1));
		Assert.That(Tools.Parser.Parse<float>("1"), Is.EqualTo(1.0));
		var guid = Guid.NewGuid();
		Assert.That(Tools.Parser.Parse<Guid>(guid.ToString()), Is.EqualTo(guid));
		Assert.That(Tools.Parser.Parse<Guid>(guid.ToStrictAlphaString()), Is.EqualTo(guid));
	}

	[Test]
	public void Throws() {
		Assert.Throws<FormatException>(() => Tools.Parser.Parse<int?>("s"));
		Assert.Throws<FormatException>(() => Tools.Parser.Parse<int?>("null"));
	}

	[Test]
	public void Nullable() {
		Assert.That(Tools.Parser.Parse<int?>(""), Is.Null);
		Assert.That(Tools.Parser.Parse<int?>(null), Is.Null);
	}

	[Test]
	public void SafeParse() {
		//ClassicAssert.IsNull(Tools.Parser.SafeParseInt32("bad"));
		Assert.That(Tools.Parser.SafeParse<int?>("bad"), Is.Null);
		Assert.That(Tools.Parser.SafeParse<int>("bad"), Is.EqualTo(default(int)));
	}


	[Test]
	public void IPAddr() {
		Assert.That(Tools.Parser.Parse<IPAddress>("127.0.0.1"), Is.EqualTo(IPAddress.Parse("127.0.0.1")));
		Assert.Throws<FormatException>(() => Tools.Parser.Parse<IPAddress>("asdfhaskdfj"));
	}
}

