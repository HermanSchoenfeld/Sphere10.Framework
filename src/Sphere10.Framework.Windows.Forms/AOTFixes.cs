// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Preserves the reflected metadata required by this project's native controls.</summary>
internal static class AOTFixes {
	// PropertyGrid activates its default tab through reflection; losing this constructor silently removes every row.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors, "System.Windows.Forms.PropertyGridInternal.PropertiesTab", "System.Windows.Forms")]
	// Designer resources apply the navigation width through TypeDescriptor.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SplitContainer))]
	// The default explorer theme includes block-selector settings read by XmlSerializer.
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlockSelectorInfoSurrogate))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlockSelectorInfo))]
	// ListControl resolves DisplayMember and ValueMember on these private item types.
	// String targets use documentation-style nested names and keep the item classes private.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Sphere10.Framework.Windows.Forms.EnumComboBox.ComboItem", "Sphere10.Framework.Windows.Forms")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Sphere10.Framework.Windows.Forms.FlagsCheckedListBox.CheckedBoxItem", "Sphere10.Framework.Windows.Forms")]
	internal static void Retain() {
	}
}
