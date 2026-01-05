// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Drawing;
using MonoMac.CoreGraphics;
using System.IO;

namespace Sphere10.Framework {
	public static class ByteArrayExtensions {

		public static NSImage ToNSImage(this byte[] data) {
			return NSImage.FromStream(new MemoryStream(data, false));
		}

	}
}


