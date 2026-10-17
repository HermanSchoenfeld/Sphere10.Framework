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
using System.Threading.Tasks;
using Microsoft.Win32;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable, Category("Integration")]
public class RegistryUtilEditTests {
	private RegistryAddress _owned;
	private RegistryUtil _registry;

	[SetUp]
	public void CreateOwnedKey() {
		_owned = new RegistryAddress(RegistryHive.CurrentUser, RegistryView.Default, "Software\\Sphere10\\Tests\\RegistryUtil-" + Guid.NewGuid().ToString("N"));
		_registry = new RegistryUtil();
		using var key = _registry.OpenKey(_owned, writable: true, createIfMissing: true);
	}

	[TearDown]
	public void DeleteOnlyOwnedKey() {
		const string prefix = "Software\\Sphere10\\Tests\\RegistryUtil-";
		Guard.Ensure(_owned.Hive == RegistryHive.CurrentUser && _owned.SubKeyPath.StartsWith(prefix, StringComparison.Ordinal)
			&& _owned.SubKeyPath.Length == prefix.Length + 32, "Cleanup must stay in its exact owned registry subtree.");
		_registry.DeleteKey(_owned, ignoreMissing: true);
	}

	[Test]
	public void DescribingARegistryTreeDoesNotLoadItsDeferredData() {
		var reads = 0;
		var tree = new RegistryTree(Tools.Values.Future.Reloadable(() => { reads++; return Array.Empty<RegistryValue>(); }),
			Tools.Values.Future.Reloadable(() => { reads++; return Array.Empty<RegistrySubTree>(); }));
		Assert.That(tree.ToString(), Is.EqualTo(nameof(RegistryTree)));
		Assert.That(reads, Is.Zero);
	}

	[Test]
	public void RawValuesPreserveTheirTypeUnexpandedDataAndOwnedArrays() {
		var bytes = new byte[] { 1, 2, 255 };
		_registry.SetValue(_owned, "Binary", RegistryValueKind.Binary, bytes);
		bytes[0] = 3;
		_registry.SetValue(_owned, "Expansion", RegistryValueKind.ExpandString, "%TEMP%\\literal");
		var binary = _registry.ReadValue(_owned, "Binary");
		((byte[])binary.Data)[0] = 4;
		Assert.That(binary.Data, Is.EqualTo(new byte[] { 1, 2, 255 }));
		Assert.That(_registry.ReadValue(_owned, "Expansion").Data, Is.EqualTo("%TEMP%\\literal"));
		Assert.That(_registry.ReadValue(_owned, "Expansion").Kind, Is.EqualTo(RegistryValueKind.ExpandString));
		Assert.That(binary.Address, Is.EqualTo(_owned));
	}

	[Test]
	public void KeyAndValueRenamePreserveTheOwnedSubtreeAndStaleWritesDoNotOverwrite() {
		var before = _registry.CreateKey(_owned, "Before");
		var nested = _registry.CreateKey(before, "Nested");
		_registry.SetValue(nested, "Before", RegistryValueKind.QWord, -1L);
		var after = _registry.RenameKey(before, "After");
		var child = _registry.GetSubKeys(after).Single();
		var previous = _registry.ReadValue(child, "Before");
		_registry.RenameValue(child, "Before", "After", previous);
		Assert.That(_registry.ReadValue(child, "Before"), Is.Null);
		Assert.That(_registry.ReadValue(child, "After").Data, Is.EqualTo(-1L));
		var expected = _registry.ReadValue(child, "After");
		_registry.SetValue(child, "After", RegistryValueKind.QWord, 42L);
		Assert.Throws<InvalidOperationException>(() => _registry.DeleteValue(child, "After", expected));
		Assert.Throws<InvalidOperationException>(() => _registry.SetValue(child, "After", RegistryValueKind.QWord, 0L, expected: expected));
		Assert.That(_registry.ReadValue(child, "After").Data, Is.EqualTo(42L));
	}

	[Test]
	public void MissingValuesKeysAndCreateKeyAreExplicitNativeApiChoices() {
		var missing = _owned with { SubKeyPath = _owned.SubKeyPath + "\\Missing" };
		Assert.That(_registry.ReadValue(missing, "Missing"), Is.Null);
		Assert.That(_registry.ReadValues(missing, throwIfMissing: false), Is.Empty);
		_registry.DeleteValue(missing, "Missing", ignoreMissing: true);
		_registry.SetValue(missing, string.Empty, RegistryValueKind.DWord, -1, createKey: true);
		Assert.That(_registry.ReadValue(missing, string.Empty).Data, Is.EqualTo(-1));
	}

	[Test]
	public void RecursiveRegistrationReadsHonorCallerLimitsAndRetainEveryRawValue() {
		_registry.SetValue(_owned, "Root", RegistryValueKind.String, "Raw");
		var child = _registry.CreateKey(_owned, "Child");
		_registry.SetValue(child, "Nested", RegistryValueKind.Binary, new byte[] { 1, 2 });
		var tree = _registry.ReadTree(_owned, 2, 10, TimeSpan.FromSeconds(3));
		Assert.That(tree.Values.Single().Name, Is.EqualTo("Root"));
		Assert.That(tree.SubKeys.Single().Contents.Values.Single().Data, Is.EqualTo(new byte[] { 1, 2 }));
		Assert.Throws<InvalidOperationException>(() => { _ = _registry.ReadTree(_owned, 0, 10, TimeSpan.FromSeconds(3)).SubKeys; });
		Assert.Throws<InvalidOperationException>(() => { _ = _registry.ReadTree(_owned, 2, 1, TimeSpan.FromSeconds(3)).Values; });
		Assert.That(_registry.GetSubKeys(_owned).Single(), Is.EqualTo(child));
	}

	[Test]
	public void TreeFuturesDeferNativeValuesAndChildDataThenCacheTheirCallerOwnedArrays() {
		_registry.SetValue(_owned, "Root", RegistryValueKind.String, "Before");
		var child = _registry.CreateKey(_owned, "Child");
		_registry.SetValue(child, "Value", RegistryValueKind.String, "Before");
		var tree = _registry.ReadTree(_owned, 2, 10, TimeSpan.FromSeconds(3));
		Assert.That(((Reloadable<RegistryValue[]>)tree.ValuesFuture).Loaded, Is.False);
		Assert.That(((Reloadable<RegistrySubTree[]>)tree.SubKeysFuture).Loaded, Is.False);
		_registry.SetValue(_owned, "Root", RegistryValueKind.String, "After creation");
		var childTree = tree.SubKeys.Single().Contents;
		Assert.That(((Reloadable<RegistryValue[]>)childTree.ValuesFuture).Loaded, Is.False, "Reading child names must not open child value handles.");
		_registry.SetValue(child, "Value", RegistryValueKind.String, "Before first access");
		var values = childTree.Values;
		Assert.That(values.Single().Data, Is.EqualTo("Before first access"));
		_registry.SetValue(child, "Value", RegistryValueKind.String, "After first access");
		Assert.That(childTree.Values, Is.SameAs(values), "The resolved future supplies the same owned array, without a getter clone.");
		Assert.That(childTree.Values.Single().Data, Is.EqualTo("Before first access"));
		Assert.That(tree.Values.Single().Data, Is.EqualTo("After creation"));
	}

	[Test]
	public void ExplicitTreeMembershipBelongsToItsCallerRatherThanRepeatedDefensiveCopies() {
		var values = new[] { new RegistryValue(_owned, "Original", RegistryValueKind.String, "Before") };
		var children = Array.Empty<RegistrySubTree>();
		var tree = new RegistryTree(values, children);
		Assert.That(tree.Values, Is.SameAs(values));
		Assert.That(tree.SubKeys, Is.SameAs(children));
		values[0] = new RegistryValue(_owned, "Replacement", RegistryValueKind.String, "After");
		Assert.That(tree.Values.Single().Name, Is.EqualTo("Replacement"));
	}

	[Test]
	public async Task LazyReadTimeBudgetExcludesIdleTimeAndAsyncReadsFullyResolveTheirFutures() {
		_registry.SetValue(_owned, "Root", RegistryValueKind.String, "Raw");
		var child = _registry.CreateKey(_owned, "Child");
		_registry.SetValue(child, "Nested", RegistryValueKind.String, "Raw child");
		var lazy = _registry.ReadTree(_owned, 2, 10, TimeSpan.FromSeconds(1));
		await Task.Delay(1100);
		Assert.That(lazy.SubKeys.Single().Contents.Values.Single().Data, Is.EqualTo("Raw child"));
		var complete = await _registry.ReadTreeAsync(_owned, 2, 10, TimeSpan.FromSeconds(3));
		Assert.That(((Reloadable<RegistryValue[]>)complete.ValuesFuture).Loaded, Is.True);
		Assert.That(((Reloadable<RegistrySubTree[]>)complete.SubKeysFuture).Loaded, Is.True);
		var nested = complete.SubKeys.Single().Contents;
		Assert.That(((Reloadable<RegistryValue[]>)nested.ValuesFuture).Loaded, Is.True);
		Assert.That(((Reloadable<RegistrySubTree[]>)nested.SubKeysFuture).Loaded, Is.True);
		Assert.That(nested.Values.Single().Data, Is.EqualTo("Raw child"));
	}

	[Test]
	public async Task CanceledAsyncRegistryReadsCannotOpenOrMaterializeTheOwnedKey() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await _registry.GetSubKeysAsync(_owned, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await _registry.ReadValuesAsync(_owned, cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await _registry.ReadTreeAsync(_owned, 2, 10, TimeSpan.FromSeconds(3), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}
