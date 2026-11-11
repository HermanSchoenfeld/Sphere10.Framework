// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises reversible native merging without duplicating command handlers or ownership in a host.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MergeableToolStripTests {
	[Test]
	public void RuntimeMergeRetainsOriginalItemsHandlersTagsImagesAndHostedControls() {
		using var image = new Bitmap(8, 8);
		using var target = new ToolStrip();
		using var source = new ToolStrip();
		using var fixer = new ToolStripVisualInheritanceFixer { ToolStrip = source };
		var tag = new object();
		var clicks = 0;
		var button = new ToolStripButton("Action") { Tag = tag, Image = image };
		button.Click += (_, _) => {
			clicks++;
		};
		var textBox = new TextBox { Text = "Retained input" };
		var hostedControl = new ToolStripControlHost(textBox);
		source.Items.AddRange(new ToolStripItem[] { button, hostedControl });

		Assert.That(fixer.MergeInto(target), Is.True);
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { button, hostedControl }));
		Assert.That(source.Items, Is.Empty);
		Assert.That(button.Tag, Is.SameAs(tag));
		Assert.That(button.Image, Is.SameAs(image));
		Assert.That(hostedControl.Control, Is.SameAs(textBox));
		button.PerformClick();
		Assert.That(clicks, Is.EqualTo(1));

		Assert.That(fixer.Unmerge(), Is.True);
		Assert.That(fixer.InheritedToolStrip, Is.Null);
		Assert.That(source.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { button, hostedControl }));
		Assert.That(target.Items, Is.Empty);
		Assert.That(textBox.Text, Is.EqualTo("Retained input"));
		Assert.That(button.IsDisposed || hostedControl.IsDisposed || textBox.IsDisposed, Is.False);
	}

	[Test]
	public void CaptionMatchingCombinesDefaultHeadingsAndRestoresTheirOriginalNativeSettings() {
		using var target = new MenuStrip();
		using var source = new MergeableMenuStrip();
		var file = new ToolStripMenuItem("&File");
		var exit = new ToolStripMenuItem("Exit");
		file.DropDownItems.Add(exit);
		target.Items.Add(file);
		var localFile = new ToolStripMenuItem(" File ");
		var localAction = new ToolStripMenuItem("Local action");
		localFile.DropDownItems.Add(localAction);
		var actions = new ToolStripMenuItem("Actions");
		source.Items.AddRange(new ToolStripItem[] { localFile, actions });

		Assert.That(source.MergeInto(target, matchMenuText: true), Is.True);
		source.Merge();
		source.Merge();
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { file, actions }));
		Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { exit, localAction }));
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.MatchOnly));
		Assert.That(localFile.MergeIndex, Is.Zero);

		Assert.That(source.Unmerge(), Is.True);
		Assert.That(source.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { localFile, actions }));
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { localAction }));
		Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { exit }));
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { file }));
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.Append));
		Assert.That(localFile.MergeIndex, Is.EqualTo(-1));
	}

	[Test]
	public void ExplicitMergeActionsAndIndicesKeepTheirNativeMeaning() {
		using var target = new MenuStrip();
		using var source = new MergeableMenuStrip();
		var file = new ToolStripMenuItem("File");
		var removed = new ToolStripMenuItem("Remove me");
		var retained = new ToolStripMenuItem("Keep me");
		file.DropDownItems.AddRange(new ToolStripItem[] { removed, retained });
		target.Items.Add(file);
		var localFile = new ToolStripMenuItem("File") { MergeAction = MergeAction.MatchOnly };
		var remove = new ToolStripMenuItem("Remove me") { MergeAction = MergeAction.Remove };
		var insert = new ToolStripMenuItem("Inserted first") { MergeAction = MergeAction.Insert, MergeIndex = 0 };
		localFile.DropDownItems.AddRange(new ToolStripItem[] { remove, insert });
		source.Items.Add(localFile);

		Assert.That(source.MergeInto(target, matchMenuText: true), Is.True);
		Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { insert, retained }));
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.MatchOnly));
		Assert.That(remove.MergeAction, Is.EqualTo(MergeAction.Remove));
		Assert.That(insert.MergeIndex, Is.Zero);

		Assert.That(source.Unmerge(), Is.True);
		Assert.That(file.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { removed, retained }));
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { remove, insert }));
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.MatchOnly));
		Assert.That(localFile.MergeIndex, Is.EqualTo(-1));
		Assert.That(removed.IsDisposed, Is.False);
	}

	/// <summary>Unmerge restores automatic state without overwriting configuration explicitly edited by the owner.</summary>
	[Test]
	public void CaptionMatchCleanupRetainsExplicitClientChangesToTheNativeMergeSettings() {
		using var target = new MenuStrip();
		using var source = new MergeableMenuStrip();
		var file = new ToolStripMenuItem("File");
		file.DropDownItems.Add("Application action");
		target.Items.Add(file);
		var localFile = new ToolStripMenuItem("File");
		localFile.DropDownItems.Add("Local action");
		source.Items.Add(localFile);
		source.MergeInto(target, matchMenuText: true);
		localFile.MergeAction = MergeAction.Insert;
		localFile.MergeIndex = 3;

		source.Unmerge();
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.Insert));
		Assert.That(localFile.MergeIndex, Is.EqualTo(3));
	}

	/// <summary>A disabled native merge never changes headings or forwards their preparation callbacks.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void DisabledCaptionMergeLeavesSourceSettingsAndPopupPreparationUntouched(bool disableSource) {
		using var target = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		var file = new PreparingMenuItem("File");
		file.DropDownItems.Add("Application action");
		target.Items.Add(file);
		var localFile = new ToolStripMenuItem("File");
		var localAction = new ToolStripMenuItem("Local action");
		localFile.DropDownItems.Add(localAction);
		source.Items.Add(localFile);
		var openings = 0;
		localFile.DropDownOpening += (_, _) => {
			openings++;
		};
		if (disableSource)
			source.AllowMerge = false;
		else
			target.AllowMerge = false;

		Assert.That(source.MergeInto(target, matchMenuText: true), Is.False);
		Assert.That(localFile.MergeAction, Is.EqualTo(MergeAction.Append));
		Assert.That(localFile.MergeIndex, Is.EqualTo(-1));
		file.PrepareDropDown();
		Assert.That(openings, Is.Zero);
		Assert.That(source.Unmerge(), Is.False);
		Assert.That(localAction.Owner, Is.SameAs(localFile.DropDown));
	}

	[Test]
	public void NewlyAddedItemsAndRetargetingReturnEveryNativeMergeToTheOriginalOwner() {
		using var firstTarget = new ToolStrip();
		using var secondTarget = new ToolStrip();
		using var source = new MergeableToolStrip();
		var first = new ToolStripButton("First");
		var second = new ToolStripButton("Second");
		source.Items.Add(first);
		Assert.That(source.MergeInto(firstTarget), Is.True);
		source.Items.Add(second);
		source.Merge();
		Assert.That(firstTarget.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { first, second }));

		Assert.That(source.MergeInto(secondTarget), Is.True);
		Assert.That(firstTarget.Items, Is.Empty);
		Assert.That(secondTarget.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { first, second }));
		Assert.That(source.Unmerge(), Is.True);
		Assert.That(source.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { first, second }));
		Assert.That(secondTarget.Items, Is.Empty);
		Assert.That(first.IsDisposed || second.IsDisposed, Is.False);
	}

	[Test]
	public void RemovingAnEarlierContributionDoesNotReenterOtherSourcesLayoutMerges() {
		using var target = new ToolStrip();
		using var firstSource = new MergeableToolStrip();
		using var secondSource = new MergeableToolStrip();
		var first = new ToolStripButton("First");
		var second = new ToolStripButton("Second");
		firstSource.Items.Add(first);
		secondSource.Items.Add(second);
		firstSource.MergeInto(target);
		secondSource.MergeInto(target);

		Assert.That(firstSource.Unmerge(), Is.True);
		Assert.That(first.Owner, Is.SameAs(firstSource));
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { second }));
		Assert.That(secondSource.Unmerge(), Is.True);
		Assert.That(second.Owner, Is.SameAs(secondSource));
		Assert.That(target.Items, Is.Empty);
	}

	[Test]
	public void UnmergePreservesItemsExplicitlyReparentedOrRemovedByTheClient() {
		using var target = new ToolStrip();
		using var foreignTarget = new ToolStrip();
		using var source = new MergeableToolStrip();
		using var removed = new ToolStripButton("Removed");
		var moved = new ToolStripButton("Moved");
		var retained = new ToolStripButton("Retained");
		source.Items.AddRange(new ToolStripItem[] { moved, removed, retained });
		source.MergeInto(target);
		foreignTarget.Items.Add(moved);
		target.Items.Remove(removed);

		source.Unmerge();
		Assert.That(moved.Owner, Is.SameAs(foreignTarget));
		Assert.That(removed.Owner, Is.Null);
		Assert.That(source.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { retained }));
		Assert.That(target.Items, Is.Empty);
		Assert.That(moved.IsDisposed || removed.IsDisposed || retained.IsDisposed, Is.False);
	}

	[Test]
	public void SourceDisposalEndsOutstandingMergesBeforeDestroyingItsOwnedItems() {
		using var target = new ToolStrip();
		using var source = new MergeableToolStrip();
		var button = new ToolStripButton("Owned action");
		var disposals = 0;
		button.Disposed += (_, _) => {
			disposals++;
		};
		source.Items.Add(button);
		source.MergeInto(target);

		source.Dispose();
		Assert.That(target.Items, Is.Empty);
		Assert.That(button.IsDisposed, Is.True);
		Assert.That(disposals, Is.EqualTo(1));
	}

	[Test]
	public void PlainSourceDisposalReleasesItsOutstandingItemsAndSubscriptions() {
		using var target = new ToolStrip();
		using var source = new ToolStrip();
		using var fixer = new ToolStripVisualInheritanceFixer { ToolStrip = source };
		var button = new ToolStripButton("Owned action");
		source.Items.Add(button);
		fixer.MergeInto(target);

		source.Dispose();
		Assert.That(fixer.ToolStrip, Is.Null);
		Assert.That(fixer.InheritedToolStrip, Is.Null);
		Assert.That(target.Items, Is.Empty);
		Assert.That(button.IsDisposed, Is.True);
	}

	[Test]
	public void StatusStripUsesTheSameReversibleMergeAndDisposalContract() {
		using var target = new StatusStrip();
		using var source = new MergeableStatusStrip();
		var label = new ToolStripStatusLabel("Status");
		source.Items.Add(label);

		Assert.That(source.MergeInto(target), Is.True);
		Assert.That(label.Owner, Is.SameAs(target));
		Assert.That(source.Unmerge(), Is.True);
		Assert.That(label.Owner, Is.SameAs(source));
		source.MergeInto(target);
		source.Dispose();
		Assert.That(target.Items, Is.Empty);
		Assert.That(label.IsDisposed, Is.True);
	}

	/// <summary>Each mergeable target returns its borrowed commands and hosted controls before native item disposal.</summary>
	[TestCase("toolbar", false)]
	[TestCase("toolbar", true)]
	[TestCase("menu", false)]
	[TestCase("menu", true)]
	[TestCase("status", false)]
	[TestCase("status", true)]
	public void DisposalOrderPreservesTheSourceOwnershipOfBorrowedItems(string stripKind, bool targetFirst) {
		using var image = new Bitmap(8, 8);
		using var target = CreateMergeableStrip(stripKind);
		using var source = CreatePlainStrip(stripKind);
		using var fixer = new ToolStripVisualInheritanceFixer { ToolStrip = source };
		var clicks = 0;
		var disposals = 0;
		var button = new ToolStripButton("Borrowed action") { Image = image };
		button.Click += (_, _) => {
			clicks++;
		};
		button.Disposed += (_, _) => {
			disposals++;
		};
		var textBox = new TextBox { Text = "Retained input" };
		var hostedControl = new ToolStripControlHost(textBox);
		var targetItem = new ToolStripButton("Target action");
		target.Items.Add(targetItem);
		source.Items.AddRange(new ToolStripItem[] { button, hostedControl });
		Assert.That(fixer.MergeInto(target), Is.True);
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { targetItem, button, hostedControl }));

		if (targetFirst) {
			// The target releases only its own commands; borrowed controls remain usable at their original source.
			target.Dispose();
			Assert.That(source.Items.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { button, hostedControl }));
			Assert.That(button.Owner, Is.SameAs(source));
			Assert.That(hostedControl.Owner, Is.SameAs(source));
			Assert.That(button.IsDisposed || hostedControl.IsDisposed || textBox.IsDisposed, Is.False);
			Assert.That(button.Image, Is.SameAs(image));
			Assert.That(textBox.Text, Is.EqualTo("Retained input"));
			Assert.That(fixer.InheritedToolStrip, Is.Null);
			Assert.That(targetItem.IsDisposed, Is.True);
			button.PerformClick();
			Assert.That(clicks, Is.EqualTo(1));
			source.Dispose();
		} else {
			// Closing the source still destroys its own commands without transferring their lifetime to the target.
			source.Dispose();
			Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { targetItem }));
			Assert.That(targetItem.IsDisposed, Is.False);
			target.Dispose();
		}

		Assert.That(button.IsDisposed && hostedControl.IsDisposed && textBox.IsDisposed, Is.True);
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(targetItem.IsDisposed, Is.True);
	}

	/// <summary>Caption-matched dropdown children return before disposing the target's own heading.</summary>
	[Test]
	public void TargetDisposalRestoresNestedMenuCommandsToTheirOriginalHeading() {
		using var source = new MergeableMenuStrip();
		using var target = new MergeableMenuStrip();
		var localFile = new ToolStripMenuItem("File");
		var localAction = new ToolStripMenuItem("Local action");
		localFile.DropDownItems.Add(localAction);
		source.Items.Add(localFile);
		var applicationFile = new ToolStripMenuItem("File");
		var applicationAction = new ToolStripMenuItem("Application action");
		applicationFile.DropDownItems.Add(applicationAction);
		target.Items.Add(applicationFile);
		source.MergeInto(target, matchMenuText: true);
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, localAction }));

		target.Dispose();
		Assert.That(localFile.Owner, Is.SameAs(source));
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { localAction }));
		Assert.That(localAction.Owner, Is.SameAs(localFile.DropDown));
		Assert.That(localFile.IsDisposed || localAction.IsDisposed, Is.False);
		Assert.That(applicationFile.IsDisposed && applicationAction.IsDisposed, Is.True);
		source.Dispose();
		Assert.That(localFile.IsDisposed && localAction.IsDisposed, Is.True);
	}

	/// <summary>A strip acting as both source and target unwinds its outgoing merge before returning incoming items.</summary>
	[Test]
	public void IntermediateTargetDisposalRestoresChainedSourceOwnership() {
		using var source = new MergeableToolStrip();
		using var intermediate = new MergeableToolStrip();
		using var target = new ToolStrip();
		var sourceAction = new ToolStripButton("Source action");
		var intermediateAction = new ToolStripButton("Intermediate action");
		source.Items.Add(sourceAction);
		intermediate.Items.Add(intermediateAction);
		source.MergeInto(intermediate);
		intermediate.MergeInto(target);
		Assert.That(sourceAction.Owner, Is.SameAs(target));

		intermediate.Dispose();
		Assert.That(sourceAction.Owner, Is.SameAs(source));
		Assert.That(sourceAction.IsDisposed, Is.False);
		Assert.That(source.InheritedToolStrip, Is.Null);
		Assert.That(intermediateAction.IsDisposed, Is.True);
		Assert.That(target.Items, Is.Empty);
		source.Dispose();
		Assert.That(sourceAction.IsDisposed, Is.True);
	}

	/// <summary>A matched heading prepares its original collection and replaces only its own visible contribution.</summary>
	[Test]
	public void CaptionMatchedPopupPreparationRebuildsChildrenAndPreservesBorrowedResources() {
		using var image = new Bitmap(8, 8);
		using var target = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		using var original = new ToolStripMenuItem("Original");
		using var firstPrepared = new ToolStripMenuItem("Prepared first");
		var secondPrepared = new ToolStripMenuItem("Prepared second");
		var applicationFile = new PreparingMenuItem("&File");
		var applicationAction = new ToolStripMenuItem("Application action");
		applicationFile.DropDownItems.Add(applicationAction);
		target.Items.Add(applicationFile);
		var localFile = new ToolStripMenuItem(" File ");
		var textBox = new TextBox { Text = "Borrowed input" };
		var hostedControl = new ToolStripControlHost(textBox) { Image = image };
		localFile.DropDownItems.AddRange(new ToolStripItem[] { original, hostedControl });
		source.Items.Add(localFile);
		var openings = 0;
		var clicks = 0;
		var applicationOpenings = 0;
		applicationFile.DropDownOpening += (_, _) => {
			applicationOpenings++;
		};
		secondPrepared.Click += (_, _) => {
			clicks++;
		};
		localFile.DropDownOpening += (_, _) => {
			openings++;
			Assert.That(hostedControl.Owner, Is.SameAs(localFile.DropDown));
			Assert.That(source.Visible, Is.False);
			localFile.DropDownItems.Clear();
			localFile.DropDownItems.AddRange(new ToolStripItem[] { hostedControl, openings == 1 ? firstPrepared : secondPrepared });
		};
		source.MergeInto(target, matchMenuText: true);
		source.Merge();
		source.Merge();

		applicationFile.PrepareDropDown();
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { applicationAction, hostedControl, firstPrepared }));
		applicationFile.PrepareDropDown();
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { applicationAction, hostedControl, secondPrepared }));
		Assert.That(openings, Is.EqualTo(2));
		Assert.That(applicationOpenings, Is.EqualTo(2));
		Assert.That(applicationFile.DropDown.Visible || localFile.DropDown.Visible, Is.False);
		Assert.That(original.Owner, Is.Null);
		Assert.That(firstPrepared.Owner, Is.Null);
		Assert.That(original.IsDisposed || firstPrepared.IsDisposed || hostedControl.IsDisposed || textBox.IsDisposed, Is.False);
		Assert.That(hostedControl.Image, Is.SameAs(image));
		Assert.That(textBox.Text, Is.EqualTo("Borrowed input"));
		secondPrepared.PerformClick();
		Assert.That(clicks, Is.EqualTo(1));

		source.Unmerge();
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction }));
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new ToolStripItem[] { hostedControl, secondPrepared }));
	}

	/// <summary>Preparation remains reachable when MatchOnly had no children to move during the initial merge.</summary>
	[Test]
	public void InitiallyEmptyMatchedPopupCanPopulateItsVisibleHeading() {
		using var target = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		var applicationFile = new PreparingMenuItem("File");
		var applicationAction = new ToolStripMenuItem("Application action");
		applicationFile.DropDownItems.Add(applicationAction);
		target.Items.Add(applicationFile);
		var localFile = new ToolStripMenuItem("File");
		var prepared = new ToolStripMenuItem("Prepared action");
		var openings = 0;
		localFile.DropDownOpening += (_, _) => {
			openings++;
			localFile.DropDownItems.Add(prepared);
		};
		source.Items.Add(localFile);
		source.MergeInto(target, matchMenuText: true);

		applicationFile.PrepareDropDown();
		Assert.That(openings, Is.EqualTo(1));
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, prepared }));
		source.Unmerge();
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { prepared }));
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction }));
	}

	/// <summary>Nested explicit MatchOnly headings forward only the popup being prepared.</summary>
	[Test]
	public void NestedMatchedPopupsPrepareTheirOwnersOnce() {
		using var target = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		var applicationFile = new PreparingMenuItem("File");
		var applicationTools = new PreparingMenuItem("Tools");
		var applicationAction = new ToolStripMenuItem("Application action");
		applicationTools.DropDownItems.Add(applicationAction);
		applicationFile.DropDownItems.Add(applicationTools);
		target.Items.Add(applicationFile);
		var localFile = new ToolStripMenuItem("File");
		var localTools = new ToolStripMenuItem("Tools") { MergeAction = MergeAction.MatchOnly };
		using var original = new ToolStripMenuItem("Original");
		var prepared = new ToolStripMenuItem("Prepared action");
		localTools.DropDownItems.Add(original);
		localFile.DropDownItems.Add(localTools);
		source.Items.Add(localFile);
		var rootOpenings = 0;
		var nestedOpenings = 0;
		localFile.DropDownOpening += (_, _) => {
			rootOpenings++;
		};
		localTools.DropDownOpening += (_, _) => {
			nestedOpenings++;
			localTools.DropDownItems.Clear();
			localTools.DropDownItems.Add(prepared);
		};
		source.MergeInto(target, matchMenuText: true);

		applicationFile.PrepareDropDown();
		Assert.That(rootOpenings, Is.EqualTo(1));
		Assert.That(nestedOpenings, Is.Zero);
		applicationTools.PrepareDropDown();
		Assert.That(rootOpenings, Is.EqualTo(1));
		Assert.That(nestedOpenings, Is.EqualTo(1));
		Assert.That(applicationTools.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, prepared }));

		source.Unmerge();
		Assert.That(localTools.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { prepared }));
		Assert.That(applicationTools.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction }));
	}

	/// <summary>Preparing several contributors keeps their shared heading and original contribution order intact.</summary>
	[Test]
	public void SharedMatchedPopupPreparesEveryContributorWithoutDuplicatingItsHeading() {
		using var target = new MergeableMenuStrip();
		using var firstSource = new MergeableMenuStrip();
		using var secondSource = new MergeableMenuStrip();
		var applicationFile = new PreparingMenuItem("File");
		var applicationAction = new ToolStripMenuItem("Application action");
		applicationFile.DropDownItems.Add(applicationAction);
		target.Items.Add(applicationFile);
		var firstFile = new ToolStripMenuItem("File");
		var firstAction = new ToolStripMenuItem("First action");
		firstFile.DropDownItems.Add(firstAction);
		firstSource.Items.Add(firstFile);
		var secondFile = new ToolStripMenuItem("File");
		var secondAction = new ToolStripMenuItem("Second action");
		secondFile.DropDownItems.Add(secondAction);
		secondSource.Items.Add(secondFile);
		var firstOpenings = 0;
		var secondOpenings = 0;
		firstFile.DropDownOpening += (_, _) => {
			firstOpenings++;
		};
		secondFile.DropDownOpening += (_, _) => {
			secondOpenings++;
		};
		firstSource.MergeInto(target, matchMenuText: true);
		secondSource.MergeInto(target, matchMenuText: true);

		applicationFile.PrepareDropDown();
		Assert.That(firstOpenings, Is.EqualTo(1));
		Assert.That(secondOpenings, Is.EqualTo(1));
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationFile }));
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, firstAction, secondAction }));
		applicationFile.PrepareDropDown();
		Assert.That(firstOpenings, Is.EqualTo(2));
		Assert.That(secondOpenings, Is.EqualTo(2));
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationFile }));
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, firstAction, secondAction }));

		firstSource.Unmerge();
		Assert.That(target.Items.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationFile }));
		Assert.That(applicationFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { applicationAction, secondAction }));
		Assert.That(firstAction.Owner, Is.SameAs(firstFile.DropDown));
	}

	/// <summary>Ending or retargeting a contribution removes its preparation hook from the previous visible heading.</summary>
	[TestCase("unmerge")]
	[TestCase("retarget")]
	[TestCase("source-disposal")]
	public void MatchedPopupPreparationSubscriptionsFollowTheMergeLifetime(string transition) {
		using var firstTarget = new MergeableMenuStrip();
		using var secondTarget = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		var firstFile = new PreparingMenuItem("File");
		firstFile.DropDownItems.Add("First target action");
		firstTarget.Items.Add(firstFile);
		var secondFile = new PreparingMenuItem("File");
		secondFile.DropDownItems.Add("Second target action");
		secondTarget.Items.Add(secondFile);
		var localFile = new ToolStripMenuItem("File");
		var localAction = new ToolStripMenuItem("Local action");
		localFile.DropDownItems.Add(localAction);
		source.Items.Add(localFile);
		var openings = 0;
		localFile.DropDownOpening += (_, _) => {
			openings++;
		};
		source.MergeInto(firstTarget, matchMenuText: true);
		firstFile.PrepareDropDown();
		Assert.That(openings, Is.EqualTo(1));

		switch (transition) {
			case "unmerge":
				source.Unmerge();
				break;
			case "retarget":
				source.MergeInto(secondTarget, matchMenuText: true);
				break;
			case "source-disposal":
				source.Dispose();
				break;
		}

		firstFile.PrepareDropDown();
		Assert.That(openings, Is.EqualTo(1));
		Assert.That(firstFile.DropDownItems.Cast<ToolStripItem>().Any(item => ReferenceEquals(item, localAction)), Is.False);
		if (transition == "retarget") {
			secondFile.PrepareDropDown();
			Assert.That(openings, Is.EqualTo(2));
			Assert.That(localAction.Owner, Is.SameAs(secondFile.DropDown));
		}
	}

	/// <summary>A prepared dynamic child returns to its source before the matched target heading is disposed.</summary>
	[Test]
	public void MatchedTargetDisposalRetainsPreparedSourceChildren() {
		using var target = new MergeableMenuStrip();
		using var source = new MergeableMenuStrip();
		var applicationFile = new PreparingMenuItem("File");
		applicationFile.DropDownItems.Add("Application action");
		target.Items.Add(applicationFile);
		var localFile = new ToolStripMenuItem("File");
		var prepared = new ToolStripMenuItem("Prepared action");
		localFile.DropDownOpening += (_, _) => {
			localFile.DropDownItems.Add(prepared);
		};
		source.Items.Add(localFile);
		source.MergeInto(target, matchMenuText: true);
		applicationFile.PrepareDropDown();
		Assert.That(prepared.Owner, Is.SameAs(applicationFile.DropDown));

		target.Dispose();
		Assert.That(source.InheritedToolStrip, Is.Null);
		Assert.That(localFile.DropDownItems.Cast<ToolStripItem>(), Is.EqualTo(new[] { prepared }));
		Assert.That(localFile.IsDisposed || prepared.IsDisposed, Is.False);
		Assert.That(applicationFile.IsDisposed, Is.True);
	}

	// Use ordinary sources to exercise the fixer's direct composition API for each native strip family.
	private static ToolStrip CreatePlainStrip(string stripKind) {
		return stripKind switch {
			"toolbar" => new ToolStrip(),
			"menu" => new MenuStrip(),
			"status" => new StatusStrip(),
			_ => throw new ArgumentOutOfRangeException(nameof(stripKind))
		};
	}

	// Mergeable targets supply their existing fixer's early disposal notification.
	private static ToolStrip CreateMergeableStrip(string stripKind) {
		return stripKind switch {
			"toolbar" => new MergeableToolStrip(),
			"menu" => new MergeableMenuStrip(),
			"status" => new MergeableStatusStrip(),
			_ => throw new ArgumentOutOfRangeException(nameof(stripKind))
		};
	}

	#region Inner Types

	// Exercise the native preparation event without displaying a dropdown window or using reflection in the tests.
	private sealed class PreparingMenuItem : ToolStripMenuItem {
		public PreparingMenuItem(string text)
			: base(text) {
		}

		public void PrepareDropDown() => OnDropDownShow(EventArgs.Empty);
	}

	#endregion
}
