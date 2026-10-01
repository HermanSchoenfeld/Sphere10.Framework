// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: David Price
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid.Classes;

public class ObjectTypeInfo {
	public ObjectTypeInfo(PropertyInfo propertyInfo) {
		Guard.ArgumentNotNull(propertyInfo, nameof(propertyInfo));
		PropertyInfo = propertyInfo;
		Type = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
		TypeName = Type.Name;
		TypeFullName = Type.FullName;
		IsEnum = Type.IsEnum;
		if (IsEnum)
			EnumValues.AddRange(Type.GetEnumNames());
	}

	public ObjectTypeInfo(string name) {
		Guard.ArgumentNotNull(name, nameof(name));
		TypeName = name;
		TypeFullName = typeof(string).FullName;
		Type = typeof(string);
	}

	public PropertyInfo PropertyInfo { get; set; }

	public Type Type { get; set; }

	public string TypeName { get; set; }

	public string TypeFullName { get; set; }

	public bool IsEnum { get; set; }

	public List<string> EnumValues { get; set; } = new();
}

