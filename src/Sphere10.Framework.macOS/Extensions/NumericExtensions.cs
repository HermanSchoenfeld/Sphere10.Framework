// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;

namespace Sphere10.Framework
{
	public static class NumericExtensions
	{
		public static NSNumber ToNSNumber(this decimal value) {
			return NSNumber.FromDouble((double)value);
		}

		public static NSNumber ToNSNumber(this double value) {
			return NSNumber.FromDouble(value);
		}

		public static NSNumber ToNSNumber(this float value) {
			return NSNumber.FromFloat(value);
		}

		public static NSNumber ToNSNumber(this int value) {
			return NSNumber.FromUInt32((uint)value);
		}

	}
}

