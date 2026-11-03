// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.FastReflection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

public class ReflectionPropertyResolver : IPropertyResolver {
	public static ReflectionPropertyResolver SharedInstance = new ReflectionPropertyResolver();

	/// <summary>Reads a public property through the shared reflection accessor, retaining the missing-property sentinel.</summary>
	public object ReadValue(object item, string propertyPath) {
		var propertyInfo = item.GetType().GetProperty(propertyPath);
		if (propertyInfo == null)
			return string.Empty;
		return propertyInfo.FastGetValue(item);
	}
}
