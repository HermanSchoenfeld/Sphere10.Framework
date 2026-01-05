// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;
using CoreGraphics;
using CoreGraphics;
using Foundation;
using Sphere10.Framework;

namespace Sphere10.Framework.iOS
{
	public static class UIDatePickerExtensions {
		
		public static DateTime GetSafeDate(this UIDatePicker datePicker) {
			return DateTime.SpecifyKind( datePicker.Date.ToDateTime().ToLocalTime(), DateTimeKind.Unspecified);

		}

		public static void SetSafeDate(this UIDatePicker datePicker, DateTime dateTime, bool animated = false) {
			datePicker.SetDate(DateTime.SpecifyKind(dateTime, DateTimeKind.Local));
		}

		public static void SetDate(this UIDatePicker datePicker, DateTime dateTime, bool animated = false) {
			datePicker.SetDate(dateTime.ToNSDate(), animated);
		}

	}
}


