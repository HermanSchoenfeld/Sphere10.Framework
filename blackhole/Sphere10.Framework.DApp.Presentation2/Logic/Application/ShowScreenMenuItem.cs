// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.DApp.Presentation2.Logic {

	public class ShowScreenMenuItem : ApplicationMenuItem {

		public Type ScreenType {
			get {
				return field;
			}
			set {
				Guard.ArgumentNotNull(value, nameof(value));
				Guard.Argument(IsValidScreenType(value), nameof(value), "Not an IApplicationScreen");
				field = value;
			}
		}

		public static ShowScreenMenuItem For<TScreen>(string icon, string title) where TScreen : IApplicationScreen
			=> new ShowScreenMenuItem { Icon = icon, Title = title, ScreenType = typeof(TScreen) };

		private static bool IsValidScreenType(Type screenType) => screenType.IsAssignableTo(typeof(IApplicationScreen));

	}

}


