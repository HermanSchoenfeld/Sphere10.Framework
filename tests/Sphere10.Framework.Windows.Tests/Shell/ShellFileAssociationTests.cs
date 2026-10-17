// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class ShellFileAssociationTests {
	private const int BufferError = unchecked((int)0x80004003);
	private const int MissingAssociation = unchecked((int)0x80070483);

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	[TestCase(".txt\0.exe")]
	public void InvalidAssociationIdentifiersAreRejectedBeforeNativeLookup(string association) {
		Assert.That(() => Tools.Windows.Shell.QueryFileAssociation(association), Throws.ArgumentException);
	}

	[Test]
	public void OversizedAssociationIdentifiersAreRejectedBeforeNativeLookup() {
		Assert.That(() => Tools.Windows.Shell.QueryFileAssociation(new string('x', 32768)), Throws.ArgumentException);
	}

	[TestCase("")]
	[TestCase(" ")]
	[TestCase("open\0other")]
	public void InvalidVerbsAreRejectedBeforeNativeLookup(string verb) {
		Assert.That(() => Tools.Windows.Shell.QueryFileAssociation(".txt", verb), Throws.ArgumentException);
	}

	[Test]
	public void OversizedVerbsAreRejectedBeforeNativeLookup() {
		Assert.That(() => Tools.Windows.Shell.QueryFileAssociation(".txt", new string('x', 32768)), Throws.ArgumentException);
	}

	[Test, Category("Integration")]
	public void UnregisteredExtensionRetainsTheShellsFallbackStringsAndPerFieldStatus() {
		var extension = ".sphere10-unregistered-" + Guid.NewGuid().ToString("N");
		var result = Tools.Windows.Shell.QueryFileAssociation(extension);
		Assert.That(result.Association, Is.EqualTo(extension));
		Assert.That(result.IsProgId, Is.False);
		var command = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => WinAPI.SHLWAPI.AssocQueryStringW(
			WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOTRUNCATE | WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOFIXUPS,
			WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_COMMAND, extension, null, output, ref length));
		var executable = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => WinAPI.SHLWAPI.AssocQueryStringW(
			WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOTRUNCATE | WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOFIXUPS,
			WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_EXECUTABLE, extension, null, output, ref length));
		// Some Windows versions resolve an unregistered extension to the native Open With handler.
		Assert.That(result.Command, Is.EqualTo(command.Value));
		Assert.That(result.ExecutablePath, Is.EqualTo(executable.Value));
		Assert.That(result.HResults[nameof(result.Command)], Is.EqualTo(command.HResult));
		Assert.That(result.HResults[nameof(result.ExecutablePath)], Is.EqualTo(executable.HResult));
		Assert.That(result.HResults.Keys, Is.EquivalentTo(new[] {
			nameof(result.ProgId), nameof(result.FriendlyDocumentName), nameof(result.FriendlyApplicationName), nameof(result.ExecutablePath),
			nameof(result.Command), nameof(result.DefaultIcon), nameof(result.DelegateExecute), nameof(result.ApplicationId)
		}));
	}

	[Test, Category("Integration")]
	public void ExtensionLookupPreservesTheNativeCurrentUserResolutionAndRawCommand() {
		const string extension = ".txt";
		var result = Tools.Windows.Shell.QueryFileAssociation(extension);
		var native = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => WinAPI.SHLWAPI.AssocQueryStringW(
			WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOTRUNCATE | WinAPI.SHLWAPI.ASSOCF.ASSOCF_NOFIXUPS,
			WinAPI.SHLWAPI.ASSOCSTR.ASSOCSTR_COMMAND, extension, null, output, ref length));
		Assert.That(result.Command, Is.EqualTo(native.Value));
		Assert.That(result.HResults[nameof(result.Command)], Is.EqualTo(native.HResult));
	}

	[Test, Category("Integration")]
	public void AnExplicitProgIdUsesItsFixedRegistrationRatherThanTheUsersExtensionChoice() {
		var result = Tools.Windows.Shell.QueryFileAssociation("exefile", "open", isProgId: true);
		Assert.That(result.IsProgId, Is.True);
		Assert.That(result.Verb, Is.EqualTo("open"));
		Assert.That(result.Command, Is.Not.Empty);
		Assert.That(result.HResults[nameof(result.Command)], Is.Zero);
	}

	[Test]
	public void AStringThatGrowsDuringLookupIsReadAgainWithoutReturningItsTruncatedPrefix() {
		const string complete = "Native association \u03a9 \ud83d\udcc4";
		var reads = 0;
		var result = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => {
			reads++;
			if (output == null) {
				length = 4;
				return 1;
			}
			if (length < complete.Length + 1) {
				output.Append("cut");
				length = (uint)complete.Length + 1;
				return BufferError;
			}
			output.Append(complete);
			length = (uint)complete.Length + 1;
			return 0;
		});
		Assert.That(result.Value, Is.EqualTo(complete));
		Assert.That(result.HResult, Is.Zero);
		Assert.That(reads, Is.EqualTo(3));
	}

	[Test]
	public void MissingStringsPreserveTheirNativeFailureWithoutAllocatingAnOutputBuffer() {
		var reads = 0;
		var result = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => {
			reads++;
			Assert.That(output, Is.Null);
			return MissingAssociation;
		});
		Assert.That(result.Value, Is.Empty);
		Assert.That(result.HResult, Is.EqualTo(MissingAssociation));
		Assert.That(reads, Is.EqualTo(1));
	}

	[TestCase(0u)]
	[TestCase(1048577u)]
	[TestCase(uint.MaxValue)]
	public void InvalidNativeBufferRequirementsAreBoundedBeforeAllocating(uint requestedLength) {
		var reads = 0;
		var result = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => {
			reads++;
			Assert.That(output, Is.Null);
			length = requestedLength;
			return 1;
		});
		Assert.That(result.Value, Is.Empty);
		Assert.That(result.HResult, Is.EqualTo(BufferError));
		Assert.That(reads, Is.EqualTo(1));
	}

	[Test]
	public void AContinuallyGrowingRegistrationStopsAfterBoundedRetriesWithoutExposingPartialData() {
		var reads = 0;
		var result = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => {
			reads++;
			if (output == null) {
				length = 2;
				return 1;
			}
			output.Append("x");
			length *= 2;
			return BufferError;
		});
		Assert.That(result.Value, Is.Empty);
		Assert.That(result.HResult, Is.EqualTo(BufferError));
		Assert.That(reads, Is.EqualTo(5));
	}

	[Test]
	public void AFailedReadNeverReturnsItsPartiallyWrittenOutput() {
		var result = ShellUtility.ReadAssociationString((StringBuilder output, ref uint length) => {
			if (output == null) {
				length = 8;
				return 1;
			}
			output.Append("partial");
			return MissingAssociation;
		});
		Assert.That(result.Value, Is.Empty);
		Assert.That(result.HResult, Is.EqualTo(MissingAssociation));
	}
}
