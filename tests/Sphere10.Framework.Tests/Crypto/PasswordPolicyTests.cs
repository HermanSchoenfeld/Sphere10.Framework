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
public class PasswordPolicyTests {

	[Test]
	public void TheDefaultPolicyAcceptsAStrongPassword()
		=> Assert.That(PasswordPolicy.Default.Validate("Example-Password-42").IsSuccess, Is.True);

	[Test]
	public void TheDefaultPolicyNamesEveryBrokenRule() {
		var result = new PasswordPolicy().Validate("  ");
		Assert.That(result.IsFailure, Is.True);
		Assert.That(result.ErrorMessages.ToArray(), Is.EqualTo(new[] {
			"Password needs at least 1 digit",
			"Password needs at least 1 lower case letter",
			"Password needs at least 1 upper case letter",
			"Password cannot have spaces, tabs or other whitespace",
			"Passwords need to contain 12 or more characters"
		}));
	}

	[TestCase("example-password-42", "Password needs at least 1 upper case letter")]
	[TestCase("EXAMPLE-PASSWORD-42", "Password needs at least 1 lower case letter")]
	[TestCase("Example-Password-xx", "Password needs at least 1 digit")]
	[TestCase("Example Password 42", "Password cannot have spaces, tabs or other whitespace")]
	[TestCase("Example\tPassword42", "Password cannot have spaces, tabs or other whitespace")]
	[TestCase("Short-Pw-42", "Passwords need to contain 12 or more characters")]
	public void TheDefaultPolicyRejectsOneBrokenRule(string password, string message)
		=> Assert.That(PasswordPolicy.Default.Validate(password).ErrorMessages.ToArray(), Is.EqualTo(new[] { message }));

	[Test]
	public void ANullPasswordIsCheckedAsEmpty()
		=> Assert.That(PasswordPolicy.Default.Validate(null).ErrorMessages.Count(), Is.EqualTo(4));

	[Test]
	public void AConfiguredPolicyAppliesItsOwnRules() {
		var policy = new PasswordPolicy { MinimumLength = 6, RequiresDigit = false, RequiresUpperCase = false, RequiresSymbol = true, AllowsWhitespace = true };
		Assert.That(policy.Validate("ab cd!").IsSuccess, Is.True);
		Assert.That(policy.Validate("ab cde").ErrorMessages.ToArray(), Is.EqualTo(new[] { "Password needs at least 1 symbol" }));
		Assert.That(policy.Validate("ab!").ErrorMessages.ToArray(), Is.EqualTo(new[] { "Passwords need to contain 6 or more characters" }));
	}

	[Test]
	public void ASubclassAddsRules() {
		var result = new NoUsernamePolicy().Validate("Alice-Password-42");
		Assert.That(result.ErrorMessages.ToArray(), Is.EqualTo(new[] { "Password cannot contain the username" }));
	}

	private sealed class NoUsernamePolicy : PasswordPolicy {
		public override Result Validate(string password) {
			var result = base.Validate(password);
			if (password?.Contains("Alice") == true)
				result.AddError("Password cannot contain the username");
			return result;
		}
	}
}
