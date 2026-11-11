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
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// This component allows you to modify a base form's ToolStrip by merging a hidden tool strip on the sub-form.
/// It works around the visual designer's inherited ToolStrip limitation while retaining the actual runtime items.
/// </summary>
public class ToolStripVisualInheritanceFixer : Component {
	private readonly record struct MergedItem(ToolStripItem Item, ToolStrip Owner);

	private readonly record struct MovedItem(ToolStripItem Item, ToolStrip Owner, int Index);

	private readonly record struct MenuAdjustment(ToolStripMenuItem Item, MergeAction Action, int Index, int MatchedIndex);

	private readonly record struct MatchedMenu(ToolStripDropDownItem Source, ToolStripDropDownItem Target);

	private const string ClonedTag = "__$$Cloned";

	[ThreadStatic]
	private static bool _reverting;

	[ThreadStatic]
	private static bool _preparingOwner;
	private static readonly PropertyDescriptor _visibleProperty = TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Visible)];
	private static readonly Action<ToolStripDropDownItem, EventArgs> _onDropDownShow = typeof(ToolStripDropDownItem)
		.GetMethod("OnDropDownShow", BindingFlags.Instance | BindingFlags.NonPublic)
		.CreateDelegate<Action<ToolStripDropDownItem, EventArgs>>();
	private readonly List<MergedItem> _mergedItems = new();
	private readonly List<MatchedMenu> _matchedMenus = new();
	private readonly List<MenuAdjustment> _menuAdjustments = new();
	private ToolStrip _toolStrip;
	private ToolStrip _inheritedToolStrip;
	private bool _toolStripVisible;
	private bool _merging;
	private bool _matchMenuText;

	#region Construction

	public ToolStripVisualInheritanceFixer()
		: this(null) {
	}

	/// <summary>Adds the merger to the same component container as its owning window or control.</summary>
	public ToolStripVisualInheritanceFixer(IContainer container) => container?.Add(this);

	#endregion

	#region Properties

	/// <summary>The base form's tool strip. Expose its designer modifier as protected when selecting it on a derived form.</summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Description("The base form's tool strip. If you cannot find it, make its designer modifier protected or greater.")]
	[Category("Behavior")]
	public ToolStrip InheritedToolStrip {
		get => _inheritedToolStrip;
		set {
			if (ReferenceEquals(_inheritedToolStrip, value))
				return;
			Unmerge();
			_inheritedToolStrip = value;
			if (value != null) {
				value.Disposed += InheritedToolStripDisposed;
				if (GetMergeFixer(value) is { } targetFixer)
					targetFixer.Disposed += InheritedToolStripDisposed;
			}
		}
	}

	/// <summary>The source tool strip whose items retain their existing callbacks, state and resource ownership.</summary>
	[Description("The tool strip on this form to be merged with the inherited tool strip.")]
	[Category("Behavior")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public ToolStrip ToolStrip {
		get => _toolStrip;
		set {
			if (ReferenceEquals(_toolStrip, value))
				return;
			Unmerge();
			if (_toolStrip != null) {
				_toolStrip.VisibleChanged -= ToolStripVisibleChanged;
				_toolStrip.Layout -= ToolStripLayout;
				_toolStrip.Disposed -= ToolStripDisposed;
				if (!_toolStrip.IsDisposed && !_toolStrip.Disposing)
					_toolStrip.Visible = _toolStripVisible;
			}
			_toolStrip = value;
			if (value != null) {
				_toolStripVisible = !_visibleProperty.ShouldSerializeValue(value);
				value.VisibleChanged += ToolStripVisibleChanged;
				value.Layout += ToolStripLayout;
				value.Disposed += ToolStripDisposed;
				value.Visible = false;
			}
		}
	}

	#endregion

	#region Merging

	/// <summary>Merges the original source items into a target, optionally combining default menu headings by displayed caption.</summary>
	/// <remarks>
	/// Explicit MergeAction and MergeIndex settings retain their native meaning. Mergeable targets return borrowed items before disposal;
	/// call Unmerge before disposing a plain ToolStrip target, which provides no notification before disposing its items.
	/// Automatic caption matches retain MatchOnly and the matched index while merged, then restore the original settings on Unmerge.
	/// </remarks>
	public bool MergeInto(ToolStrip target, bool matchMenuText = false) {
		Guard.ArgumentNotNull(target, nameof(target));
		Guard.ArgumentNotNull(ToolStrip, nameof(ToolStrip));
		Guard.Argument(!ReferenceEquals(ToolStrip, target), nameof(target), "A tool strip cannot merge into itself.");
		InheritedToolStrip = target;
		_matchMenuText = matchMenuText;
		return Merge();
	}

	/// <summary>Merges pending source items into the configured inherited strip.</summary>
	public bool Merge() {
		if (_merging || _reverting || ToolStrip is not { IsDisposed: false } || InheritedToolStrip is not { IsDisposed: false })
			return false;
		_merging = true;
		try {
			if (Tools.Runtime.IsDesignMode)
				return MergeDesignerClone();

			// Native merge history restores normal ownership; this snapshot only protects explicit client edits.
			var sourceItems = EnumerateItems(ToolStrip).Select(item => new MergedItem(item, item.Owner)).ToArray();
			var canMerge = ToolStrip.AllowMerge && InheritedToolStrip.AllowMerge && (ToolStrip.GetType().IsAssignableFrom(InheritedToolStrip.GetType()) || InheritedToolStrip.GetType().IsAssignableFrom(ToolStrip.GetType()));
			if (canMerge)
				_menuAdjustments.AddRange(MatchMenuHeadings(ToolStrip, InheritedToolStrip));
			var matchedMenus = canMerge ? EnumerateMatchedMenus(ToolStrip.Items, InheritedToolStrip.Items).ToArray() : Array.Empty<MatchedMenu>();

			// Reverting an earlier contribution makes native code reapply later sources directly.
			// Retain automatic matches until Unmerge so those reapplications still combine the same headings.
			var merged = ToolStripManager.Merge(ToolStrip, InheritedToolStrip);
			foreach (var sourceItem in sourceItems) {
				if (!ReferenceEquals(sourceItem.Owner, sourceItem.Item.Owner) && !_mergedItems.Any(item => ReferenceEquals(item.Item, sourceItem.Item)))
					_mergedItems.Add(new MergedItem(sourceItem.Item, sourceItem.Item.Owner));
			}

			// MatchOnly retains the hidden heading, so its preparation must follow the heading the user opens.
			foreach (var matchedMenu in matchedMenus) {
				if (_matchedMenus.Contains(matchedMenu))
					continue;
				if (!_matchedMenus.Any(menu => ReferenceEquals(menu.Target, matchedMenu.Target)))
					matchedMenu.Target.DropDownOpening += MatchedMenuDropDownOpening;
				_matchedMenus.Add(matchedMenu);
			}
			ToolStrip.Visible = false;
			return merged;
		} finally {
			_merging = false;
		}
	}

	/// <summary>Returns merged items to their source without reclaiming items the client explicitly moved or removed.</summary>
	public bool Unmerge() => Unmerge(false);

	// Prepare only this owner's commands; a projection must not prepare other sources merged into its native header.
	internal static void NotifyDropDownOpening(ToolStripDropDownItem item) {
		var previousPreparingOwner = _preparingOwner;
		_preparingOwner = true;
		try {
			_onDropDownShow(item, EventArgs.Empty);
		} finally {
			_preparingOwner = previousPreparingOwner;
		}
	}

	/// <summary>Unmerges borrowed items and removes the source and target subscriptions.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing)
			ToolStrip = null;
		base.Dispose(disposing);
	}

	// Control raises Disposed before IsDisposed becomes true, so its notification supplies the disposal intent explicitly.
	private bool Unmerge(bool sourceDisposing, bool restoreSourceVisibility = true) {
		var target = _inheritedToolStrip;
		_inheritedToolStrip = null;
		foreach (var menu in _matchedMenus.Select(menu => menu.Target).Distinct())
			menu.DropDownOpening -= MatchedMenuDropDownOpening;
		_matchedMenus.Clear();
		if (target == null) {
			RestoreMenuHeadings();
			return false;
		}
		target.Disposed -= InheritedToolStripDisposed;
		if (GetMergeFixer(target) is { } targetFixer)
			targetFixer.Disposed -= InheritedToolStripDisposed;

		// Clear the target first so native ResumeLayout cannot merge the returned items again.
		var previousMerging = _merging;
		var previousReverting = _reverting;
		_merging = true;
		_reverting = true;
		try {
			var movedItems = _mergedItems.Where(item => !item.Item.IsDisposed && !ReferenceEquals(item.Item.Owner, item.Owner))
				.Select(item => new MovedItem(item.Item, item.Item.Owner, item.Item.Owner?.Items.IndexOf(item.Item) ?? -1)).ToArray();
			var reverted = false;
			if (!target.IsDisposed && ToolStrip != null) {
				// Source Layout can merge additional items; undo every native history entry for this source.
				while (ToolStripManager.RevertMerge(target, ToolStrip))
					reverted = true;
			}

			// Native history restores source membership even for removed items; restore the client's explicit destination afterward.
			foreach (var movedItem in movedItems.OrderBy(item => item.Index)) {
				movedItem.Item.Owner?.Items.Remove(movedItem.Item);
				if (movedItem.Owner is { IsDisposed: false })
					movedItem.Owner.Items.Insert(Math.Min(movedItem.Index, movedItem.Owner.Items.Count), movedItem.Item);
			}
			RestoreMenuHeadings();
			if (sourceDisposing || ToolStrip is { IsDisposed: true }) {
				foreach (var item in _mergedItems.Where(item => !movedItems.Any(moved => ReferenceEquals(moved.Item, item.Item))))
					item.Item.Dispose();
			} else if (restoreSourceVisibility && ToolStrip != null)
				ToolStrip.Visible = _toolStripVisible;
			_mergedItems.Clear();
			return reverted;
		} finally {
			RestoreMenuHeadings();
			_merging = previousMerging;
			_reverting = previousReverting;
		}
	}

	// Only default Append headings opt into caption matching; explicit native merge instructions are untouched.
	private MenuAdjustment[] MatchMenuHeadings(ToolStrip source, ToolStrip target) {
		if (!_matchMenuText || source is not MenuStrip || target is not MenuStrip)
			return Array.Empty<MenuAdjustment>();
		var adjustments = new List<MenuAdjustment>();
		foreach (var item in source.Items.OfType<ToolStripMenuItem>().Where(item => item.MergeAction == MergeAction.Append)) {
			if (_menuAdjustments.Any(adjustment => ReferenceEquals(adjustment.Item, item)))
				continue;
			var match = target.Items.OfType<ToolStripMenuItem>().FirstOrDefault(candidate =>
				string.Equals(GetMenuCaption(candidate.Text), GetMenuCaption(item.Text), StringComparison.OrdinalIgnoreCase));
			if (match == null)
				continue;
			var matchedIndex = target.Items.IndexOf(match);
			adjustments.Add(new MenuAdjustment(item, item.MergeAction, item.MergeIndex, matchedIndex));
			item.MergeAction = MergeAction.MatchOnly;
			item.MergeIndex = matchedIndex;
		}
		return adjustments.ToArray();
	}

	// Restore automatic settings after native history has unwound, retaining subsequent explicit client edits.
	private void RestoreMenuHeadings() {
		foreach (var adjustment in _menuAdjustments) {
			if (adjustment.Item.MergeAction != MergeAction.MatchOnly || adjustment.Item.MergeIndex != adjustment.MatchedIndex)
				continue;
			adjustment.Item.MergeAction = adjustment.Action;
			adjustment.Item.MergeIndex = adjustment.Index;
		}
		_menuAdjustments.Clear();
	}

	// Follow native MatchOnly headings recursively; a visible heading can prepare an initially empty source popup too.
	private static IEnumerable<MatchedMenu> EnumerateMatchedMenus(ToolStripItemCollection source, ToolStripItemCollection target) {
		foreach (var sourceMenu in source.OfType<ToolStripDropDownItem>().Where(menu => menu.MergeAction == MergeAction.MatchOnly)) {
			var match = target.Cast<ToolStripItem>().FirstOrDefault(item => item.Text.Length == sourceMenu.Text.Length && string.Equals(item.Text, sourceMenu.Text, StringComparison.InvariantCultureIgnoreCase));
			if (match == null && sourceMenu.MergeIndex >= 0 && sourceMenu.MergeIndex < target.Count)
				match = target[sourceMenu.MergeIndex];
			if (match is not ToolStripDropDownItem targetMenu)
				continue;
			yield return new MatchedMenu(sourceMenu, targetMenu);
			foreach (var nestedMenu in EnumerateMatchedMenus(sourceMenu.DropDownItems, targetMenu.DropDownItems))
				yield return nestedMenu;
		}
	}

	// Keep item identity across nested headings; native history still owns original indices and restoration.
	private static IEnumerable<ToolStripItem> EnumerateItems(ToolStrip source) {
		foreach (ToolStripItem item in source.Items) {
			yield return item;
			if (item is ToolStripDropDownItem dropdown)
				foreach (var child in EnumerateItems(dropdown.DropDown))
					yield return child;
		}
	}

	// Mnemonic markers do not alter the displayed menu heading.
	private static string GetMenuCaption(string text) => (text ?? string.Empty).Replace("&", string.Empty).Trim();

	// A mergeable target disposes its existing fixer before the native control releases its items.
	private static ToolStripVisualInheritanceFixer GetMergeFixer(ToolStrip toolStrip) {
		return toolStrip switch {
			MergeableMenuStrip menu => menu.MergeFixer,
			MergeableStatusStrip status => status.MergeFixer,
			MergeableToolStrip toolbar => toolbar.MergeFixer,
			_ => null
		};
	}

	#endregion

	#region Designer support

	// At design time only a clone is merged to prevent inherited-control serialization problems.
	private bool MergeDesignerClone() {
		var previousClones = InheritedToolStrip.Items.Cast<ToolStripItem>().Where(item => item.Tag is string tag && tag == ClonedTag).ToArray();
		foreach (var item in previousClones)
			InheritedToolStrip.Items.Remove(item);
		return ToolStripManager.Merge(BasicClone(ToolStrip), InheritedToolStrip);
	}

	/// <summary>Clones a tool strip for this component's designer preview; runtime merging always uses the original items.</summary>
	private static ToolStrip BasicClone(ToolStrip original) {
		var clone = new ToolStrip();
		foreach (ToolStripItem item in original.Items) {
			var clonedItem = Clone(item);
			if (clonedItem != null)
				clone.Items.Add(clonedItem);
		}
		return clone;
	}

	/// <summary>Clones a ToolStripItem using reflection, or returns null when no default constructor is available.</summary>
	private static ToolStripItem Clone(ToolStripItem original) {
		ToolStripItem clone = null;
		if (original is ICloneable cloneable) {
			clone = (ToolStripItem)cloneable.Clone();
		} else if (original.GetType().GetConstructor(Type.EmptyTypes) != null) {
			clone = (ToolStripItem)Activator.CreateInstance(original.GetType());
			var fields = original.GetType().GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var field in fields) {
				var fieldValue = field.GetValue(original);
				if (field.FieldType.Namespace != original.GetType().Namespace)
					field.SetValue(clone, fieldValue);
			}
			var properties = original.GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var property in properties) {
				if (property.Name == "Owner" || property.Name == "OwnerItem") {
					// We ignore these properties as they cause problems.
					continue;
				}
				if (property.CanWrite)
					property.SetValue(clone, property.GetValue(original, null), null);
			}
			clone.Tag = ClonedTag;
			clone.Visible = true;
		}
		return clone;
	}

	#endregion

	#region Handlers

	// Source layout includes newly added runtime items without reconstructing the owner's native commands.
	private void ToolStripLayout(object sender, LayoutEventArgs args) {
		if (!Tools.Runtime.IsDesignMode)
			Merge();
	}

	// The designer merges a clone when its hidden source is laid out.
	private void ToolStripVisibleChanged(object sender, EventArgs args) {
		if (Tools.Runtime.IsDesignMode && ToolStrip is { Visible: false })
			Merge();
	}

	// Return this contribution before the owner edits its popup, then merge its prepared children into the visible menu.
	private void MatchedMenuDropDownOpening(object sender, EventArgs args) {
		if (_merging || _reverting || _preparingOwner || _inheritedToolStrip is not { IsDisposed: false } target)
			return;
		var source = ToolStrip;
		var menus = _matchedMenus.Where(menu => ReferenceEquals(menu.Target, sender)).Select(menu => menu.Source).Distinct().ToArray();
		if (menus.Length == 0)
			return;
		var matchMenuText = _matchMenuText;
		Unmerge(false, restoreSourceVisibility: false);
		try {
			foreach (var menu in menus.Where(menu => !menu.IsDisposed))
				_onDropDownShow(menu, EventArgs.Empty);
		} finally {
			// A callback can dispose or retarget its owner; only restore the contribution that is still ours.
			if (ReferenceEquals(ToolStrip, source) && source is { IsDisposed: false, Disposing: false } && _inheritedToolStrip == null && !target.IsDisposed && !target.Disposing)
				MergeInto(target, matchMenuText);
		}
	}

	// Source disposal ends its outstanding native merges before releasing the component subscriptions.
	private void ToolStripDisposed(object sender, EventArgs args) {
		Unmerge(true);
		Dispose();
	}

	// Mergeable targets notify through their fixer before native item disposal; plain targets only provide the final control notification.
	private void InheritedToolStripDisposed(object sender, EventArgs args) => Unmerge();

	#endregion
}
