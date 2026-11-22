// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
public class LoadingIndicatorTests {

	[Test]
	public void ShowingTurnsTheIndicatorOnUntilItsScopeEnds() {
		var indicator = new LoadingIndicator();
		Assert.That(indicator.IsActive, Is.False);
		Assert.That(indicator.Message, Is.Null);
		using (indicator.Show("Opening Screen…")) {
			Assert.That(indicator.IsActive, Is.True);
			Assert.That(indicator.Message, Is.EqualTo("Opening Screen…"));
		}
		Assert.That(indicator.IsActive, Is.False);
		Assert.That(indicator.Message, Is.Null);
	}

	[Test]
	public void OverlappingWorkKeepsTheIndicatorOnAndShowsTheLatestMessageStillShown() {
		var indicator = new LoadingIndicator();
		var first = indicator.Show("Reading Services…");
		var second = indicator.Show("Opening Registry…");
		Assert.That(indicator.Message, Is.EqualTo("Opening Registry…"));
		second.Dispose();
		Assert.That(indicator.Message, Is.EqualTo("Reading Services…"), "The earlier work's message returns when the later work ends.");
		var third = indicator.Show("Saving…");
		first.Dispose();
		Assert.That(indicator.Message, Is.EqualTo("Saving…"));
		third.Dispose();
		Assert.That(indicator.IsActive, Is.False);
	}

	[Test]
	public void TheSameMessageShownTwiceEndsIndependently() {
		var indicator = new LoadingIndicator();
		var first = indicator.Show("Reading…");
		var second = indicator.Show("Reading…");
		first.Dispose();
		Assert.That(indicator.IsActive, Is.True);
		second.Dispose();
		Assert.That(indicator.IsActive, Is.False);
	}

	[Test]
	public void EndingAScopeAgainChangesNothing() {
		var indicator = new LoadingIndicator();
		var changes = 0;
		indicator.Changed += () => changes++;
		var scope = indicator.Show("Working…");
		scope.Dispose();
		scope.Dispose();
		Assert.That(changes, Is.EqualTo(2), "One change turns it on and one turns it off.");
		Assert.That(indicator.IsActive, Is.False);
	}

	[Test]
	public void ChangesAreRaisedWithTheStateAlreadyApplied() {
		var indicator = new LoadingIndicator();
		var observed = new List<(bool Active, string Message)>();
		indicator.Changed += () => observed.Add((indicator.IsActive, indicator.Message));
		using (indicator.Show("Closing Screen…")) {
		}
		Assert.That(observed, Is.EqualTo(new[] { (true, "Closing Screen…"), (false, (string)null) }));
	}
}
