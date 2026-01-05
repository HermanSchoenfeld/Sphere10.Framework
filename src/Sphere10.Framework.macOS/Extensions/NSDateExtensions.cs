// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
#if MONOTOUCH
using MonoTouch.Foundation;
#else
using MonoMac.Foundation;
#endif

namespace Sphere10.Framework {
	public static class DateExtensions
	{

		public static NSDate ToNSDate(this DateTime dateTime) {
			return NSDate.FromTimeIntervalSinceReferenceDate((dateTime.ToUniversalTime()-(new DateTime(2001,1,1,0,0,0))).TotalSeconds);
		}
		
		public static DateTime ToDateTime(this NSDate nsDate) {
			return (new DateTime(2001,1,1,0,0,0)).AddSeconds(nsDate.SecondsSinceReferenceDate);
		}

	}
}


