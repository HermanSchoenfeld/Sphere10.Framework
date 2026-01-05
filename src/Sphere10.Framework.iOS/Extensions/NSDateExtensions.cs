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
	public static class DateExtensions
	{

		public static NSDate ToNSDate(this DateTime dateTime) {
			return NSDate.FromTimeIntervalSinceReferenceDate(
                (dateTime.ToUniversalTime()-(new DateTime(2001,1,1,0,0,0))).TotalSeconds
            );
		}
		
		public static DateTime ToDateTime(this NSDate nsDate) {
			return (new DateTime(2001,1,1,0,0,0)).AddSeconds(nsDate.SecondsSinceReferenceDate);
		}

        public static DateTime? ToNullableDateTime(this NSDate nsDate) {
            if (nsDate == null)
                return null;

            return (new DateTime(2001, 1, 1, 0, 0, 0)).AddSeconds(nsDate.SecondsSinceReferenceDate);
        }

        //public static DateTime NSDateToDateTime(this NSDate date) {
        //    DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime(
        //        new DateTime(2001, 1, 1, 0, 0, 0));
        //    return reference.AddSeconds(date.SecondsSinceReferenceDate);
        //}

        //public static NSDate DateTimeToNSDate(this DateTime date) {
        //    if (date.Kind == DateTimeKind.Unspecified)
        //        date = DateTime.SpecifyKind(date,  /* DateTimeKind.Local or DateTimeKind.Utc, this depends on each app */);
        //    return (NSDate) date;
        //}

	}
}


