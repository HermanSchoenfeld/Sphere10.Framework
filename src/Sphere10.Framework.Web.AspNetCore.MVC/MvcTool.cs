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
using Sphere10.Framework;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tools.Web;

public static class Mvc {

	public static SelectList ToSelectList<TEnum>(object selectedItem = default, SortDirection? sort = null) where TEnum : Enum
		=> ToSelectList(typeof(TEnum), selectedItem, sort);

	public static SelectList ToSelectList(Type enumType, object selectedItem = default, SortDirection? sort = null) {
		var items = new List<SelectListItem>();
		foreach (var item in Enum.GetValues(enumType).Cast<Enum>()) {
			var title = Tools.Enums.GetDescription(item);
			var listItem = new SelectListItem {
				Value = item.ToString(),
				Text = title,
				Selected = selectedItem switch { null => false, _ => selectedItem.Equals(item) }
			};
			items.Add(listItem);
		}
		if (sort != null) {
			IComparer<SelectListItem> comparer = new ProjectionComparer<SelectListItem, string>(x => x.Text, StringComparer.InvariantCultureIgnoreCase);
			if (sort.Value == SortDirection.Descending)
				comparer = comparer.AsInverted();
			items.Sort(comparer);
		}

		return new SelectList(items, "Value", "Text", selectedItem?.ToString());
	}
}
