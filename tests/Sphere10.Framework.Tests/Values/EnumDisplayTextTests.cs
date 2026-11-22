// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies the text a user sees for enum values (owner, 22 November 2026: choice values read as Title Case words) and the identifier word splitting beneath it.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EnumDisplayTextTests {

	[TestCase(Choice.ApplyTemplate, "Apply Template")]
	[TestCase(Choice.Last30Days, "Last 30 Days")]
	[TestCase(Choice.SubscriptionMonthly, "Subscription Monthly")]
	[TestCase(Choice.PaymentFailed, "Payment Failed")]
	[TestCase(Choice.ECDSA_SECP256k1, "ECDSA SECP256k1")]
	[TestCase(Choice.IPAddress, "IP Address")]
	[TestCase(Choice.SKU, "SKU")]
	[TestCase(Choice.Tier1, "Tier1")]
	[TestCase(Choice.IPv4, "IPv4")]
	[TestCase(Choice.lastMonth, "Last Month")]
	public void NameReadsAsTitleCaseWords(Choice value, string expected) => Assert.That(value.GetDisplayText(), Is.EqualTo(expected));

	[Test]
	public void DescriptionWins() => Assert.That(Choice.EditTerms.GetDisplayText(), Is.EqualTo("Change the Terms"));

	[Test]
	public void ToolAndExtensionAgree() => Assert.That(Tools.Enums.GetDisplayText(Choice.PaymentFailed), Is.EqualTo(Choice.PaymentFailed.GetDisplayText()));

	[Test]
	public void UndefinedValueShowsItsNumber() => Assert.That(((Choice)999).GetDisplayText(), Is.EqualTo("999"));

	[Test]
	public void NullIsRejected() => Assert.That(() => Tools.Enums.GetDisplayText(null), Throws.ArgumentNullException);

	[TestCase(Access.None, "None")]
	[TestCase(Access.Read, "Read")]
	[TestCase(Access.WriteAccess, "Write Access")]
	[TestCase(Access.Read | Access.WriteAccess, "Read, Write Access")]
	[TestCase(Access.Read | Access.ExecuteScripts, "Read, Run Scripts")]
	[TestCase(Access.Everything, "Everything")]
	public void FlagsListEachMembersText(Access value, string expected) => Assert.That(value.GetDisplayText(), Is.EqualTo(expected));

	[Test]
	public void FlagsWithUndefinedBitsShowTheNumber() => Assert.That((Access.Read | (Access)64).GetDisplayText(), Is.EqualTo("65"));

	[TestCase("ApplyTemplate", "Apply Template")]
	[TestCase("Last_30_Days", "Last 30 Days")]
	[TestCase("major-versions applicable", "Major Versions Applicable")]
	[TestCase("URLs", "URLs")]
	[TestCase("SHA256Hash", "SHA256 Hash")]
	[TestCase("MyXYZValue", "My XYZ Value")]
	[TestCase("A", "A")]
	[TestCase("", "")]
	public void IdentifierWords(string identifier, string expected) => Assert.That(Tools.Text.ToTitleCaseWords(identifier), Is.EqualTo(expected));

	#region Inner Types

	/// <summary>Choice values named as the products name them.</summary>
	public enum Choice {
		ApplyTemplate,

		[Description("Change the Terms")]
		EditTerms,

		Last30Days,

		SubscriptionMonthly,

		PaymentFailed,

		ECDSA_SECP256k1,

		IPAddress,

		SKU,

		Tier1,

		IPv4,

		lastMonth
	}

	/// <summary>A flags enum with a described member and a named combination.</summary>
	[Flags]
	public enum Access {
		None = 0,

		Read = 1 << 0,

		WriteAccess = 1 << 1,

		[Description("Run Scripts")]
		ExecuteScripts = 1 << 2,

		Everything = Read | WriteAccess | ExecuteScripts
	}

	#endregion
}
