// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;
using Foundation;
using CoreGraphics;
using CoreGraphics;
using Sphere10.Framework;

namespace Sphere10.Framework.iOS
{
	public static class NSDataExtensions {

		public static byte[] ToByteArray(this NSData data) {
			var dataBytes = new byte[(uint)data.Length];
			System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32((uint)data.Length));
			return dataBytes;
		}

	}

}


