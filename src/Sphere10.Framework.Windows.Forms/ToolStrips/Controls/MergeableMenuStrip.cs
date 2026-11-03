// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// This component allows you to modify a base forms ToolStrip.
/// Microsoft has intentionally added this limitation due to the complexities it could not overcome. This component simply works around
/// those limitations by allowing you to merge a (hidden) tool strip on the sub form with the readonly inherited base form.
/// </summary>
public class MergeableMenuStrip : MenuStrip {
	public MergeableMenuStrip() {
		MergeFixer = new ToolStripVisualInheritanceFixer();
		MergeFixer.ToolStrip = this;
	}

	/// <summary>
	/// The base form's menu strip.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Description("Set this to the base forms MenuStrip. If you can't find it, make sure it's modifer is set to to 'protected' or greater.")]
	[Category("Behavior")]
	public MenuStrip InheritedToolStrip {
		get => MergeFixer.InheritedToolStrip as MenuStrip;
		set => MergeFixer.InheritedToolStrip = value;
	}

	// Incoming mergers use the fixer's disposal notification before the target disposes borrowed items.
	internal ToolStripVisualInheritanceFixer MergeFixer { get; }

	/// <summary>Merges the original menu items into the target while retaining their handlers and resources.</summary>
	/// <remarks>Unmerge before disposing a plain MenuStrip target; mergeable targets return borrowed items automatically.</remarks>
	public bool MergeInto(MenuStrip target, bool matchMenuText = false) {
		return MergeFixer.MergeInto(target, matchMenuText);
	}

	/// <summary>Merges newly added items into the configured inherited menu.</summary>
	public bool Merge() {
		return MergeFixer.Merge();
	}

	/// <summary>Returns this menu's merged items before it is hidden, detached or disposed.</summary>
	public bool Unmerge() {
		return MergeFixer.Unmerge();
	}

	// Visual inheritance source strips stay hidden on the derived form's designer surface.
	protected override void InitLayout() {
		base.InitLayout();
		Visible = false;
	}

	/// <summary>Returns merged items before the source menu disposes its own items.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing)
			MergeFixer?.Dispose();
		base.Dispose(disposing);
	}

}

