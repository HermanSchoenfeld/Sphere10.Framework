// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.Foundation;

namespace Sphere10.Framework {
	public static class StringExtensions
	{
		public static NSString ToNSString(this string str)
		{
			return new NSString(str);
		}
		
		//public static string ToCLRString(this NSString nsString)
	//	{
	//		return nsString.ToString();
	//	}
	}
}


