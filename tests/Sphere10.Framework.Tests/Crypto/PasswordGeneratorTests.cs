// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class PasswordGeneratorTests {

	[Test]
	public void GeneratedPasswordsMeetTheDefaultPolicy() {
		for (var i = 0; i < 200; i++) {
			var password = PasswordGenerator.Generate();
			Assert.That(password, Has.Length.EqualTo(PasswordGenerator.DefaultLength));
			Assert.That(PasswordPolicy.Default.Validate(password).IsSuccess, Is.True, password);
		}
	}

	[Test]
	public void GeneratedPasswordsMeetAConfiguredPolicy() {
		var policy = new PasswordPolicy { MinimumLength = 4, RequiresSymbol = true };
		for (var i = 0; i < 200; i++) {
			var password = PasswordGenerator.Generate(policy, 4);
			Assert.That(password, Has.Length.EqualTo(4));
			Assert.That(policy.Validate(password).IsSuccess, Is.True, password);
		}
	}

	[Test]
	public void APasswordIsNeverShorterThanThePolicyMinimum()
		=> Assert.That(PasswordGenerator.Generate(new PasswordPolicy { MinimumLength = 40 }, 8), Has.Length.EqualTo(40));

	[Test]
	public void GeneratedPasswordsHaveNoWhitespaceAndDiffer() {
		var passwords = Enumerable.Range(0, 100).Select(_ => PasswordGenerator.Generate()).ToArray();
		Assert.That(passwords.Any(password => password.Any(char.IsWhiteSpace)), Is.False);
		Assert.That(passwords.Distinct().Count(), Is.EqualTo(passwords.Length));
	}

	[Test]
	public void RequiredCharactersAreNotAlwaysInFront() {
		// With a shuffle, the first character is a digit in roughly 10/62 of passwords; without one it never would be.
		var leadingDigits = Enumerable.Range(0, 400).Count(_ => char.IsDigit(PasswordGenerator.Generate()[0]));
		Assert.That(leadingDigits, Is.GreaterThan(0));
	}
}
