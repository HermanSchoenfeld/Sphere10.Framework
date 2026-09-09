// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DACDecoratorEventTests {
	[Test]
	public void EventsForwardSenderSqlAndUnsubscriptionAcrossDecorators() {
		var inner = new NotifyingDAC();
		var decorator = new ObservingDAC(new ObservingDAC(inner));
		var notifications = new List<(IDAC sender, string sql)>();
		var removedNotifications = 0;
		EventHandlerEx<IDAC, string> removed = (_, _) => removedNotifications++;
		decorator.Executing += (sender, sql) => notifications.Add((sender, sql));
		decorator.Executed += (sender, sql) => notifications.Add((sender, sql));
		decorator.Executing += removed;
		decorator.Executed += removed;
		decorator.Executing -= removed;
		decorator.Executed -= removed;

		inner.Notify("SELECT 1");

		Assert.That(notifications, Has.Count.EqualTo(2));
		Assert.That(notifications, Is.All.EqualTo((inner, "SELECT 1")));
		Assert.That(decorator.ExecutingCalls, Is.EqualTo(1));
		Assert.That(decorator.ExecutedCalls, Is.EqualTo(1));
		Assert.That(removedNotifications, Is.Zero);
	}

	private sealed class NotifyingDAC : MSSQLDAC {
		public NotifyingDAC()
			: base("Data Source=localhost;Initial Catalog=unused") {
		}

		public void Notify(string sql) {
			NotifyExecuting(sql);
			NotifyExecuted(sql);
		}
	}

	private sealed class ObservingDAC : DACDecorator {
		public ObservingDAC(IDAC inner)
			: base(inner) {
		}

		public int ExecutingCalls { get; private set; }

		public int ExecutedCalls { get; private set; }

		protected override void OnExecuting(string sql) => ExecutingCalls++;

		protected override void OnExecuted(string sql) => ExecutedCalls++;
	}
}