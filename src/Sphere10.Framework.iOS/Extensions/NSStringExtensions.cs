// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Foundation;


namespace Sphere10.Framework.iOS {
	public static class NSStringExtensions {
		public static NSString ToNSString(this string clrString) {
			return new NSString(clrString);
		}
	}
}


