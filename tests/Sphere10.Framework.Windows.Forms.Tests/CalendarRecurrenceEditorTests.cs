// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.VisualCalendar;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>The recurrence editor shows any rule, gives the same rule back, offers only what the frequency uses and says what is wrong.</summary>
[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class CalendarRecurrenceEditorTests {
	private static readonly DateTime Start = new(2030, 1, 7, 6, 30, 0);

	private static readonly CalendarRecurrence[] Rules = {
		CalendarRecurrence.Once(Start),
		CalendarRecurrence.Daily(Start, 3) with { End = Start.AddYears(1) },
		CalendarRecurrence.Weekly(Start, CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Saturday, 2),
		CalendarRecurrence.Monthly(Start, new[] { 1, 15, 31 }, CalendarMonths.March | CalendarMonths.December, lastDayOfMonth: true),
		CalendarRecurrence.MonthlyDayOfWeek(Start, CalendarWeeksOfMonth.Second | CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Friday, CalendarMonths.January | CalendarMonths.July),
		CalendarRecurrence.Daily(Start) with { RepeatEvery = TimeSpan.FromMinutes(15), RepeatFor = TimeSpan.FromHours(2) }
	};

	[TestCaseSource(nameof(Rules))]
	public void ARuleShownIsTheRuleReadBack(CalendarRecurrence rule) {
		using var editor = new CalendarRecurrenceEditor { Recurrence = rule };
		Assert.That(editor.Recurrence.Describe(), Is.EqualTo(rule.Describe()));
		Assert.That(editor.ValidateRecurrence().IsSuccess, Is.True);
		Assert.That(Find<Label>(editor, "_summary").Text, Is.EqualTo(rule.Describe()), "The rule is put into words.");
	}

	[Test]
	public void OnlyTheChoicesTheFrequencyUsesAreEnabled() {
		using var editor = new CalendarRecurrenceEditor();
		var expected = new (CalendarRecurrence Rule, string[] Enabled)[] {
			(Rules[0], Array.Empty<string>()),
			(Rules[1], new[] { "_interval" }),
			(Rules[2], new[] { "_interval", "_daysOfWeek" }),
			(Rules[3], new[] { "_daysOfMonth", "_lastDayOfMonth", "_months" }),
			(Rules[4], new[] { "_daysOfWeek", "_weeksOfMonth", "_months" })
		};
		foreach (var (rule, enabled) in expected) {
			editor.Recurrence = rule;
			var actual = new[] { "_interval", "_daysOfWeek", "_daysOfMonth", "_lastDayOfMonth", "_weeksOfMonth", "_months" }.Where(name => Find<Control>(editor, name).Enabled);
			Assert.That(actual, Is.EqualTo(enabled), rule.Frequency.ToString());
		}
		Assert.That(Find<Label>(editor, "_intervalUnit").Text, Is.EqualTo("Days"));
		editor.Recurrence = Rules[2];
		Assert.That(Find<Label>(editor, "_intervalUnit").Text, Is.EqualTo("Weeks"));
	}

	[Test]
	public void ChoicesOfAnotherFrequencyAreKeptWhileEditingAndLeftOutOfTheRule() {
		using var editor = new CalendarRecurrenceEditor { Recurrence = Rules[2] };
		Find<EnumComboBox>(editor, "_frequency").SelectedEnum = CalendarFrequency.Daily;
		Assert.That((editor.Recurrence.Frequency, editor.Recurrence.DaysOfWeek), Is.EqualTo((CalendarFrequency.Daily, CalendarDaysOfWeek.None)));
		Find<EnumComboBox>(editor, "_frequency").SelectedEnum = CalendarFrequency.Weekly;
		Assert.That(editor.Recurrence.Describe(), Is.EqualTo(Rules[2].Describe()), "Switching away and back loses nothing.");
	}

	[Test]
	public void AnEditRaisesTheChangeAndAnUnusableRuleIsExplained() {
		using var editor = new CalendarRecurrenceEditor { Recurrence = Rules[3] };
		var changes = 0;
		editor.RecurrenceChanged += () => changes++;
		var days = Find<TextBox>(editor, "_daysOfMonth");
		days.Text = "5, 20";
		Assert.That((changes, editor.Recurrence.DaysOfMonth), Is.EqualTo((1, new[] { 5, 20 })));
		days.Text = "5, soon";
		Assert.That(editor.ValidateRecurrence().ErrorMessages, Has.Some.Contains("numbers from 1 to 31"));
		Assert.That(Find<Label>(editor, "_summary").Text, Does.Contain("numbers from 1 to 31"));
		days.Text = string.Empty;
		Find<CheckBox>(editor, "_lastDayOfMonth").Checked = false;
		Assert.That(editor.ValidateRecurrence().ErrorMessages, Has.Some.Contains("day of the month"));
	}

	private static T Find<T>(Control parent, string name) where T : Control => parent.Controls.Find(name, true).OfType<T>().Single();
}
