// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System.IO;
using System.Drawing;

namespace Sphere10.Framework;

public static class DrawingByteArrayExtensions {

	/// <summary>
	/// Converts the byte array to an Image object.
	/// </summary>
	/// <param name="bytes"></param>
	/// <returns></returns>
	public static Image ToImage(this byte[] bytes) {
		if (bytes == null || bytes.Length == 0)
			return null;

		using (var mem = new MemoryStream(bytes)) {
			return Image.FromStream(mem);
		}
	}

}

