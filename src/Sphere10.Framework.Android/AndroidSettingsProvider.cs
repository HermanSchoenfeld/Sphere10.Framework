// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Android.App;
using System.IO;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Android {

	public class AndroidSettingsProvider : DirectorySettingsProvider {
		private const string AppSettingsFolderName = "ApplicationSettings";
		public AndroidSettingsProvider()
			: base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettingsFolderName)) {
		}
	}
}



