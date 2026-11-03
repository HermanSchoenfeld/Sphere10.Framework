// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Data.Tests;

/// <summary>Verifies typed DataRow deletion payloads without changing the DataView binding boundary.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BoundDataViewEventTests {

	[Test]
	public void DeletingAViewItemNotifiesTheTypedRowAndTheBindingSubscriber() {
		using var table = CreateTable();
		var row = table.Rows[0];
		using var bound = new BoundDataView(table.DefaultView);
		IBoundList binding = bound;
		var notifications = new List<string>();
		bound.ItemDeleted += (sender, args) => {
			Assert.That(sender, Is.SameAs(bound));
			Assert.That(args.Item, Is.SameAs(row));
			Assert.That(args.Item.RowState, Is.EqualTo(DataRowState.Deleted));
			notifications.Add("typed");
		};
		binding.ItemDeleted += (sender, args) => {
			Assert.That(sender, Is.SameAs(bound));
			Assert.That(args.Item, Is.SameAs(row));
			notifications.Add("binding");
		};
		Assert.That(bound[0], Is.TypeOf<DataRowView>());

		bound.RemoveAt(0);

		Assert.That(bound.Count, Is.Zero);
		Assert.That(notifications, Is.EqualTo(new[] { "typed", "binding" }));
	}

	[Test]
	public void BindingSubscribersCanUnsubscribeWithoutRemovingTypedSubscribers() {
		using var table = CreateTable();
		table.Rows.Add("Second");
		table.AcceptChanges();
		using var bound = new BoundDataView(table.DefaultView);
		IBoundList binding = bound;
		var typedCalls = 0;
		var bindingCalls = 0;
		bound.ItemDeleted += (_, _) => typedCalls++;
		EventHandler<ItemDeletedEventArgs<object>> handler = (_, _) => bindingCalls++;
		binding.ItemDeleted += handler;
		bound.RemoveAt(0);

		binding.ItemDeleted -= handler;
		bound.RemoveAt(0);

		Assert.That(typedCalls, Is.EqualTo(2));
		Assert.That(bindingCalls, Is.EqualTo(1));
	}

	[Test]
	public void ValidRowReplacementsPassThroughBothChannelsAndTheOverride() {
		using var table = CreateTable();
		var original = table.Rows[0];
		var typedReplacement = table.NewRow();
		var bindingReplacement = table.NewRow();
		using var bound = new DeletionProbeDataView(table.DefaultView);
		bound.ItemDeleted += (_, args) => args.Item = typedReplacement;
		((IBoundList)bound).ItemDeleted += (_, args) => {
			Assert.That(args.Item, Is.SameAs(typedReplacement));
			args.Item = bindingReplacement;
		};

		bound.RemoveAt(0);

		Assert.That(bound.ItemAfterDispatch, Is.SameAs(bindingReplacement));
		Assert.That(original.RowState, Is.EqualTo(DataRowState.Deleted));
	}

	[Test]
	public void DisposalDetachesTypedAndBindingDeletionNotifications() {
		using var table = CreateTable();
		using var bound = new BoundDataView(table.DefaultView);
		var typedCalls = 0;
		var bindingCalls = 0;
		bound.ItemDeleted += (_, _) => typedCalls++;
		((IBoundList)bound).ItemDeleted += (_, _) => bindingCalls++;
		bound.Dispose();

		table.Rows[0].Delete();

		Assert.That(typedCalls, Is.Zero);
		Assert.That(bindingCalls, Is.Zero);
	}

	// Accepted rows retain their identity when DataView deletion changes their state.
	private static DataTable CreateTable() {
		var table = new DataTable();
		table.Columns.Add("Name", typeof(string));
		table.Rows.Add("First");
		table.AcceptChanges();
		return table;
	}

	// Verify a normal subclass can observe mutations after the existing virtual hook dispatch.
	private sealed class DeletionProbeDataView : BoundDataView {
		public DeletionProbeDataView(DataView view)
			: base(view) {
		}

		public DataRow ItemAfterDispatch { get; private set; }

		protected override void OnItemDeleted(ItemDeletedEventArgs<DataRow> args) {
			base.OnItemDeleted(args);
			ItemAfterDispatch = args.Item;
		}
	}
}
