// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies typed deletion notifications and the heterogeneous native binding boundary.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BoundListEventTests {

	[Test]
	public void DeletionPublishesTypedAndBindingItemsAfterTrackingAndBeforeListChanged() {
		var item = new StringBuilder("Deleted");
		var items = new List<StringBuilder> { item };
		var bound = new BoundList<StringBuilder>(items);
		IBoundList binding = bound;
		var notifications = new List<string>();

		bound.ItemDeleted += (sender, args) => {
			Assert.That(sender, Is.SameAs(bound));
			Assert.That(args.Item, Is.SameAs(item));
			Assert.That(args.Item.Length, Is.EqualTo(7));
			Assert.That(items, Is.Empty);
			Assert.That(bound.RemovedItems, Is.EqualTo(new[] { item }));
			notifications.Add("typed");
		};
		binding.ItemDeleted += (sender, args) => {
			Assert.That(sender, Is.SameAs(bound));
			Assert.That(args.Item, Is.SameAs(item));
			notifications.Add("binding");
		};
		bound.ListChanged += (_, args) => {
			Assert.That(args.ListChangedType, Is.EqualTo(ListChangedType.ItemDeleted));
			Assert.That(args.NewIndex, Is.Zero);
			notifications.Add("list");
		};

		bound.RemoveAt(0);

		Assert.That(notifications, Is.EqualTo(new[] { "typed", "binding", "list" }));
	}

	[TestCase(0)]
	[TestCase(42)]
	public void ValueTypeDeletionPreservesTheTypedValueAndItsBindingProjection(int value) {
		var bound = new BoundList<int>(new List<int> { value });
		ItemDeletedEventArgs<int> typed = null;
		ItemDeletedEventArgs<object> projected = null;
		bound.ItemDeleted += (_, args) => typed = args;
		((IBoundList)bound).ItemDeleted += (_, args) => projected = args;

		bound.RemoveAt(0);

		Assert.That(typed, Is.Not.Null);
		Assert.That(typed.Item, Is.EqualTo(value));
		Assert.That(projected, Is.Not.Null);
		Assert.That(projected.Item, Is.TypeOf<int>().And.EqualTo(value));
		Assert.That(bound.RemovedItems, Is.EqualTo(new[] { value }));
	}

	[Test]
	public void TypedAndBindingSubscribersCanUnsubscribeIndependently() {
		var bound = new BoundList<string>(new List<string> { "First", "Second", "Third" });
		IBoundList binding = bound;
		var typedCalls = 0;
		var bindingCalls = 0;
		EventHandler<ItemDeletedEventArgs<string>> typed = (_, _) => typedCalls++;
		EventHandler<ItemDeletedEventArgs<object>> projected = (_, _) => bindingCalls++;
		bound.ItemDeleted += typed;
		binding.ItemDeleted += projected;
		bound.RemoveAt(0);

		bound.ItemDeleted -= typed;
		bound.RemoveAt(0);
		binding.ItemDeleted -= projected;
		bound.RemoveAt(0);

		Assert.That(typedCalls, Is.EqualTo(1));
		Assert.That(bindingCalls, Is.EqualTo(2));
	}

	[Test]
	public void ItemReplacementsFlowFromTypedHandlersThroughBindingHandlersAndBackToTheHook() {
		var original = new StringBuilder("Original");
		var typedReplacement = new StringBuilder("Typed replacement");
		var bindingReplacement = new StringBuilder("Binding replacement");
		var bound = new DeletionProbeList<StringBuilder>(new List<StringBuilder> { original });
		bound.ItemDeleted += (_, args) => args.Item = typedReplacement;
		((IBoundList)bound).ItemDeleted += (_, args) => {
			Assert.That(args.Item, Is.SameAs(typedReplacement));
			args.Item = bindingReplacement;
		};

		bound.RemoveAt(0);

		Assert.That(bound.ItemAfterDispatch, Is.SameAs(bindingReplacement));
		Assert.That(bound.RemovedItems, Is.EqualTo(new[] { original }));
	}

	[Test]
	public void DeletingAnAcceptedNewItemRemovesItsAdditionBeforeTheNotification() {
		var bound = new BoundList<StringBuilder>(new List<StringBuilder>());
		bound.BeginAddNew();
		bound.EndEdit(cancel: false);
		var notifications = 0;
		bound.ItemDeleted += (_, _) => {
			notifications++;
			Assert.That(bound.AddedItems, Is.Empty);
			Assert.That(bound.RemovedItems, Is.Empty);
		};

		bound.RemoveAt(0);

		Assert.That(notifications, Is.EqualTo(1));
	}

	// A genuine override observes the same mutable argument after base notification dispatch.
	private sealed class DeletionProbeList<T> : BoundList<T> {
		public DeletionProbeList(IList<T> items)
			: base(items) {
		}

		public T ItemAfterDispatch { get; private set; }

		protected override void OnItemDeleted(ItemDeletedEventArgs<T> args) {
			base.OnItemDeleted(args);
			ItemAfterDispatch = args.Item;
		}
	}
}
