// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;

namespace Sphere10.Framework.iOS {
	public sealed class GenericCellDescriptor {
		public GenericCellDescriptor() {
			ImageContentMode = UIViewContentMode.ScaleAspectFill;
			CanSelect = true;
			Accessory = UITableViewCellAccessory.None;
		}

		public UIViewContentMode ImageContentMode;
		public Func<UIImage> ImageGetter;
		public string Title;
		public string Description;
		public bool CanSelect;
		public object Tag;
		public Action Action;
		public UITableViewCellAccessory Accessory;
		public string BadgeText;
		public UIColor BadgeBackgroundColor;
		public UIColor BadgeTextColor;
		public Action<UITableViewCell, GenericCellDescriptor> CellConfig;
	}
}


