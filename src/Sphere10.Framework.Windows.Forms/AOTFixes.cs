// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

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
	// Settings persistence reflects both members and construction when restoring saved shortcut assignments.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(HotKeyAssignment))]
	// Nested grid and log-output preferences are saved with their owning settings and expanded by the Options property grid;
	// without their members they persist as empty objects and show no editable rows.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(VisualGridSettings))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(ApplicationGridColumnPreferences))]
	// The serializer also calls the log-output ShouldSerialize methods by name; without them a transient output choice is persisted.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods, typeof(LogOutputSelectorPreferences))]
	// The Options property grid activates the Columns editor named by its Editor attribute; without the constructor the row loses its editor button.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(VisualGridColumnsEditor))]
	// Keeping that editor makes its stock base resolvable by name, and TypeDescriptor then activates it for every other array property.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ArrayEditor))]
	// The stock collection form behind that editor applies its designer resources by property name and its table layout by control name;
	// without these members it opens with unplaced controls and blank ordering buttons.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Form))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TableLayoutPanel))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Button))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Label))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ListBox))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(PropertyGrid))]
	// ListControl resolves DisplayMember and ValueMember on these private item types.
	// String targets use documentation-style nested names and keep the item classes private.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Sphere10.Framework.Windows.Forms.EnumComboBox.ComboItem", "Sphere10.Framework.Windows.Forms")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Sphere10.Framework.Windows.Forms.FlagsCheckedListBox.CheckedBoxItem", "Sphere10.Framework.Windows.Forms")]
	// ControlStateEventProviderManager creates its built-in observers from their types; a trimmed constructor fails every form or screen that
	// holds such a control (18 November 2026: Open Files And File Blockers failed to open over its VisualGridDropDown).
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(FormEx.StateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(UserControlEx.StateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(PathSelectorControl.StateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ContainerControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SplitContainerControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TabControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(CommonControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SourceGridControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ToolStripControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ChartControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ExpandoControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(PadLockButtonControlStateEventProvider))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(VisualGridDropDownControlStateEventProvider))]
	internal static void Retain() {
	}
}
