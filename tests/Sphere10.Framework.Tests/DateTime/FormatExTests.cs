// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DateTimeTests {

	[Test]
	public void NextDayOfWeek_Tomorrow() {
		var startDayTime = new DateTime(2014, 09, 29); // Monday
		var nextDayOfWeek = startDayTime.ToNextDayOfWeek(DayOfWeek.Tuesday);
		Assert.That(nextDayOfWeek.DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
		Assert.That(nextDayOfWeek.Day, Is.EqualTo(30));
		Assert.That(nextDayOfWeek.Month, Is.EqualTo(9));
		Assert.That(nextDayOfWeek.Year, Is.EqualTo(2014));
	}

	[Test]
	public void NextDayOfWeek_NextMonday() {
		var startDayTime = new DateTime(2014, 09, 29); // Monday
		var nextDayOfWeek = startDayTime.ToNextDayOfWeek(DayOfWeek.Monday);
		Assert.That(nextDayOfWeek.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
		Assert.That(nextDayOfWeek.Day, Is.EqualTo(6));
		Assert.That(nextDayOfWeek.Month, Is.EqualTo(10));
		Assert.That(nextDayOfWeek.Year, Is.EqualTo(2014));
	}

	[Test]
	public void NextDayOfWeek_NextSunday() {
		var startDayTime = new DateTime(2014, 09, 29); // Monday
		var nextDayOfWeek = startDayTime.ToNextDayOfWeek(DayOfWeek.Sunday);
		Assert.That(nextDayOfWeek.DayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
		Assert.That(nextDayOfWeek.Day, Is.EqualTo(5));
		Assert.That(nextDayOfWeek.Month, Is.EqualTo(10));
		Assert.That(nextDayOfWeek.Year, Is.EqualTo(2014));
	}


	[Test]
	public void NextDayOfWeek_1HourAhead() {
		var startDayTime = new DateTime(2014, 09, 29, 11, 0, 0, 0); // Monday
		var nextDayOfWeek = startDayTime.ToNextDayOfWeek(DayOfWeek.Monday, TimeSpan.FromHours(12));
		Assert.That(startDayTime.DayOfWeek, Is.EqualTo(nextDayOfWeek.DayOfWeek));
		Assert.That(startDayTime.Day, Is.EqualTo(nextDayOfWeek.Day));
		Assert.That(startDayTime.Month, Is.EqualTo(nextDayOfWeek.Month));
		Assert.That(startDayTime.Year, Is.EqualTo(nextDayOfWeek.Year));
		Assert.That(nextDayOfWeek.Hour, Is.EqualTo(12));
	}

	[Test]
	public void NextDayOfWeek_168HoursAhead() {
		var startDayTime = new DateTime(2014, 09, 29, 12, 0, 0, 0); // Monday
		var nextDayOfWeek = startDayTime.ToNextDayOfWeek(DayOfWeek.Monday, TimeSpan.FromHours(12));
		Assert.That(startDayTime.DayOfWeek, Is.EqualTo(nextDayOfWeek.DayOfWeek));
		Assert.That(nextDayOfWeek.Day, Is.EqualTo(6));
		Assert.That(nextDayOfWeek.Month, Is.EqualTo(10));
		Assert.That(nextDayOfWeek.Year, Is.EqualTo(2014));
		Assert.That(nextDayOfWeek.Hour, Is.EqualTo(12));
	}


	[Test]
	public void NextDayOfMonth_NextMonth() {
		var start = new DateTime(2014, 09, 29); // Monday
		var nextDayOfMonth = start.ToNextDayOfMonth(29);
		Assert.That(nextDayOfMonth.DayOfWeek, Is.EqualTo(DayOfWeek.Wednesday));
		Assert.That(nextDayOfMonth.Day, Is.EqualTo(29));
		Assert.That(nextDayOfMonth.Month, Is.EqualTo(10));
		Assert.That(nextDayOfMonth.Year, Is.EqualTo(2014));
	}


	[Test]
	public void NextDayOfMonth_Tomorrow() {
		var start = new DateTime(2014, 09, 29); // Monday
		var nextDayOfMonth = start.ToNextDayOfMonth(30);
		Assert.That(nextDayOfMonth.DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
		Assert.That(nextDayOfMonth.Day, Is.EqualTo(30));
		Assert.That(nextDayOfMonth.Month, Is.EqualTo(9));
		Assert.That(nextDayOfMonth.Year, Is.EqualTo(2014));
	}

	[Test]
	public void NextDayOfMonth_1HourAhead() {
		var start = new DateTime(2014, 09, 29, 11, 0, 0, 0); // Monday
		var nextDayOfMonth = start.ToNextDayOfMonth(29, TimeSpan.FromHours(12));
		Assert.That(start.DayOfWeek, Is.EqualTo(nextDayOfMonth.DayOfWeek));
		Assert.That(start.Day, Is.EqualTo(nextDayOfMonth.Day));
		Assert.That(start.Month, Is.EqualTo(nextDayOfMonth.Month));
		Assert.That(start.Year, Is.EqualTo(nextDayOfMonth.Year));
		Assert.That(nextDayOfMonth.Hour, Is.EqualTo(12));
	}

	[Test]
	public void NextDayOfMonth_MonthsHoursAhead() {
		var start = new DateTime(2014, 09, 29, 12, 0, 0, 0); // Monday
		var nextDayOfMonth = start.ToNextDayOfMonth(29, TimeSpan.FromHours(12));
		Assert.That(nextDayOfMonth.Day, Is.EqualTo(29));
		Assert.That(nextDayOfMonth.Month, Is.EqualTo(10));
		Assert.That(nextDayOfMonth.Year, Is.EqualTo(2014));
		Assert.That(nextDayOfMonth.Hour, Is.EqualTo(12));
	}

}

