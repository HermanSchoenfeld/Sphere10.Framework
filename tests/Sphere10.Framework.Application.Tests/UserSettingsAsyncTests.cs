// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Checks awaited user-settings operations and independent ambient provider lifetimes.</summary>
[TestFixture]
[NonParallelizable]
public class UserSettingsAsyncTests {
	// Missing records follow the provider's creation policy and cannot finish before required autosave accepts them.
	[TestCase(false)]
	[TestCase(true)]
	public async Task GetAsyncHonorsAutoSaveAndAwaitsAcceptedCreationExactlyOnce(bool autoSave) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider(autoSave) { SaveRelease = release.Task };
		using var scope = UserSettings.UseProvider(provider);
		var creation = UserSettings.GetAsync<Preferences>("created");
		Preferences created;
		try {
			Assert.That(creation.IsCompleted, Is.EqualTo(!autoSave));
			Assert.That(provider.CreateCalls, Is.EqualTo(1));
			Assert.That(provider.SaveCalls, Is.EqualTo(autoSave ? 1 : 0));
			Assert.That(provider.RecordCount, Is.Zero);
		} finally {
			release.TrySetResult();
			created = await creation.WaitAsync(TimeSpan.FromSeconds(5));
		}

		// The facade returns the actual provider-bound value and never adds a second save.
		Assert.That(created.ID, Is.EqualTo("created"));
		Assert.That(created.Provider, Is.SameAs(provider));
		Assert.That(created.Name, Is.EqualTo("Default"));
		Assert.That(provider.RecordCount, Is.EqualTo(autoSave ? 1 : 0));
		Assert.That(provider.SaveCalls, Is.EqualTo(autoSave ? 1 : 0));
	}

	// Existing records load through async storage without passing through the creation/autosave path.
	[Test]
	public async Task GetAsyncLoadsAnExistingRecordWithItsOriginalProviderIdentity() {
		var provider = new ControlledProvider(autoSave: true);
		var stored = new Preferences { ID = "existing", Name = "Persisted" };
		provider.Seed(stored);
		using var scope = UserSettings.UseProvider(provider);

		var loaded = await UserSettings.GetAsync<Preferences>("existing");

		Assert.That(loaded, Is.SameAs(stored));
		Assert.That(loaded.Name, Is.EqualTo("Persisted"));
		Assert.That(loaded.ID, Is.EqualTo("existing"));
		Assert.That(loaded.Provider, Is.SameAs(provider));
		Assert.That(provider.LoadCalls, Is.EqualTo(1));
		Assert.That(provider.CreateCalls, Is.Zero);
		Assert.That(provider.SaveCalls, Is.Zero);
	}

	// Presence checks stay pending while the selected provider's asynchronous lookup is pending.
	[TestCase(false)]
	[TestCase(true)]
	public async Task HasAsyncAwaitsTheSelectedProvidersLookup(bool exists) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider { ContainsRelease = release.Task };
		if (exists)
			provider.Seed(new Preferences { ID = "lookup" });
		using var scope = UserSettings.UseProvider(provider);
		var lookup = UserSettings.HasAsync<Preferences>("lookup");
		bool found;
		try {
			Assert.That(lookup.IsCompleted, Is.False);
		} finally {
			release.TrySetResult();
			found = await lookup.WaitAsync(TimeSpan.FromSeconds(5));
		}

		Assert.That(found, Is.EqualTo(exists));
		Assert.That(provider.CreateCalls, Is.Zero);
		Assert.That(provider.SaveCalls, Is.Zero);
	}

	// Clearing the current user's records completes only after the asynchronous store acknowledges it.
	[Test]
	public async Task ClearAsyncAwaitsStorageBeforeRemovingItsRecords() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider { ClearRelease = release.Task };
		provider.Seed(new Preferences { ID = "first" });
		provider.Seed(new Preferences { ID = "second" });
		using var scope = UserSettings.UseProvider(provider);
		var clearing = UserSettings.ClearAsync();
		try {
			Assert.That(clearing.IsCompleted, Is.False);
			Assert.That(provider.RecordCount, Is.EqualTo(2));
		} finally {
			release.TrySetResult();
			await clearing.WaitAsync(TimeSpan.FromSeconds(5));
		}

		Assert.That(provider.RecordCount, Is.Zero);
		Assert.That(provider.ClearCalls, Is.EqualTo(1));
	}

	// A selected provider's failure must propagate rather than falling back to another user's provider.
	[TestCase("contains")]
	[TestCase("load")]
	[TestCase("save")]
	public async Task GetAsyncPropagatesStorageFailuresWithoutFallingBack(string stage) {
		var fallback = new ControlledProvider();
		var provider = new ControlledProvider(autoSave: true);
		var failure = new IOException("Selected storage failed.");
		if (stage == "contains")
			provider.ContainsFailure = failure;
		else if (stage == "load") {
			provider.Seed(new Preferences { ID = "rejected" });
			provider.LoadFailure = failure;
		} else
			provider.SaveFailure = failure;
		using var outer = UserSettings.UseProvider(fallback);
		using (UserSettings.UseProvider(provider))
			await Assert.ThatAsync(async () => await UserSettings.GetAsync<Preferences>("rejected"),
				Throws.TypeOf<IOException>().With.Message.EqualTo(failure.Message));

		Assert.That(UserSettings.Provider, Is.SameAs(fallback));
		Assert.That(fallback.ContainsCalls, Is.Zero);
		Assert.That(fallback.CreateCalls, Is.Zero);
		Assert.That(fallback.SaveCalls, Is.Zero);
		Assert.That(provider.SaveCalls, Is.EqualTo(stage == "save" ? 1 : 0));
		Assert.That(provider.RecordCount, Is.EqualTo(stage == "load" ? 1 : 0));
	}

	// Cancellation before or during lookup cannot create or publish a new user setting.
	[TestCase(false)]
	[TestCase(true)]
	public async Task CancelledGetAsyncDoesNotCreateOrPublishARecord(bool beforeCall) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider(autoSave: true) { ContainsRelease = release.Task };
		using var scope = UserSettings.UseProvider(provider);
		using var cancellation = new CancellationTokenSource();
		if (beforeCall)
			cancellation.Cancel();
		var creation = UserSettings.GetAsync<Preferences>("cancelled", cancellation.Token);
		try {
			cancellation.Cancel();
			await Assert.ThatAsync(async () => await creation.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		} finally {
			release.TrySetResult();
		}

		Assert.That(provider.CreateCalls, Is.Zero);
		Assert.That(provider.SaveCalls, Is.Zero);
		Assert.That(provider.RecordCount, Is.Zero);
	}

	// Required autosave also observes cancellation after creation has produced its value.
	[Test]
	public async Task CancellingPendingAutoSaveDoesNotReturnOrPersistAnUnacceptedSetting() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider(autoSave: true) { SaveRelease = release.Task };
		using var scope = UserSettings.UseProvider(provider);
		using var cancellation = new CancellationTokenSource();
		var creation = UserSettings.GetAsync<Preferences>("cancelled-save", cancellation.Token);
		try {
			Assert.That(creation.IsCompleted, Is.False);
			Assert.That(provider.CreateCalls, Is.EqualTo(1));
			Assert.That(provider.SaveCalls, Is.EqualTo(1));
			cancellation.Cancel();
			await Assert.ThatAsync(async () => await creation.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		} finally {
			release.TrySetResult();
		}

		Assert.That(provider.RecordCount, Is.Zero);
	}

	// Failed or cancelled clear operations retain the records that storage never accepted deleting.
	[TestCase(false)]
	[TestCase(true)]
	public async Task ClearAsyncPropagatesFailureOrCancellationWithoutRemovingRecords(bool cancelled) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider {
			ClearRelease = release.Task,
			ClearFailure = cancelled ? null : new IOException("Clear failed.")
		};
		provider.Seed(new Preferences { ID = "retained" });
		using var scope = UserSettings.UseProvider(provider);
		using var cancellation = new CancellationTokenSource();
		var clearing = UserSettings.ClearAsync(cancellation.Token);
		try {
			Assert.That(clearing.IsCompleted, Is.False);
			if (cancelled)
				cancellation.Cancel();
			else
				release.TrySetResult();
			if (cancelled)
				await Assert.ThatAsync(async () => await clearing.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
			else
				await Assert.ThatAsync(async () => await clearing.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<IOException>().With.Message.EqualTo("Clear failed."));
		} finally {
			release.TrySetResult();
		}

		Assert.That(provider.RecordCount, Is.EqualTo(1));
	}

	// Direct async dispatch starts on the caller's context instead of wrapping its browser-capable provider in Task.Run.
	[TestCase("get")]
	[TestCase("has")]
	[TestCase("clear")]
	public async Task AsyncFacadeStartsThePrimitiveOnTheCallingContext(string operationName) {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new ControlledProvider { ContainsRelease = release.Task, ClearRelease = release.Task };
		var context = new SynchronizationContext();
		var previous = SynchronizationContext.Current;
		using var scope = UserSettings.UseProvider(provider);
		Task operation;
		SynchronizationContext.SetSynchronizationContext(context);
		try {
			operation = operationName switch {
				"get" => UserSettings.GetAsync<Preferences>("context"),
				"has" => UserSettings.HasAsync<Preferences>("context"),
				_ => UserSettings.ClearAsync()
			};
		} finally {
			SynchronizationContext.SetSynchronizationContext(previous);
		}
		try {
			Assert.That(operationName == "clear" ? provider.ClearCalls : provider.ContainsCalls, Is.EqualTo(1));
			Assert.That(operationName == "clear" ? provider.ClearContext : provider.ContainsContext, Is.SameAs(context));
			Assert.That(operation.IsCompleted, Is.False);
		} finally {
			release.TrySetResult();
			await operation.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	// Nested synchronous scopes flow across awaits and restore their predecessor even after a failed operation.
	[Test]
	public async Task NestedProviderScopesRestoreTheirOwnersAfterAnAwaitedFailure() {
		var hadOriginal = UserSettings.TryGetProvider(out var original);
		var outer = new ControlledProvider();
		var inner = new ControlledProvider { ContainsFailure = new IOException("Nested failure.") };
		using (UserSettings.UseProvider(outer)) {
			await Task.Yield();
			Assert.That(UserSettings.Provider, Is.SameAs(outer));
			using (UserSettings.UseProvider(inner)) {
				await Task.Yield();
				Assert.That(UserSettings.Provider, Is.SameAs(inner));
				await Assert.ThatAsync(async () => await UserSettings.HasAsync<Preferences>(), Throws.TypeOf<IOException>());
			}
			Assert.That(UserSettings.Provider, Is.SameAs(outer));
		}

		Assert.That(UserSettings.TryGetProvider(out var restored), Is.EqualTo(hadOriginal));
		Assert.That(restored, Is.SameAs(original));
	}

	// Overlapping async flows may use the same record ID while retaining separate providers and the untouched process fallback.
	[Test]
	public async Task ConcurrentFlowsKeepIndependentProvidersAcrossAwaits() {
		var hadOriginal = UserSettings.TryGetProvider(out var original);
		var first = new ControlledProvider();
		var second = new ControlledProvider();
		first.Seed(new Preferences { ID = "shared", Name = "First user" });
		second.Seed(new Preferences { ID = "shared", Name = "Second user" });
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = 0;

		// Keep both ambient scopes open until the parent flow has inspected its own fallback.
		async Task<Preferences> ReadAsync(ControlledProvider provider) {
			using var scope = UserSettings.UseProvider(provider);
			if (Interlocked.Increment(ref entered) == 2)
				ready.TrySetResult();
			await release.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(UserSettings.Provider, Is.SameAs(provider));
			return await UserSettings.GetAsync<Preferences>("shared");
		}

		var firstRead = Task.Run(() => ReadAsync(first));
		var secondRead = Task.Run(() => ReadAsync(second));
		Preferences[] loaded;
		try {
			await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(UserSettings.TryGetProvider(out var current), Is.EqualTo(hadOriginal));
			Assert.That(current, Is.SameAs(original));
		} finally {
			release.TrySetResult();
			loaded = await Task.WhenAll(firstRead, secondRead).WaitAsync(TimeSpan.FromSeconds(5));
		}

		Assert.That(loaded[0].Name, Is.EqualTo("First user"));
		Assert.That(loaded[0].Provider, Is.SameAs(first));
		Assert.That(loaded[1].Name, Is.EqualTo("Second user"));
		Assert.That(loaded[1].Provider, Is.SameAs(second));
		Assert.That(UserSettings.TryGetProvider(out var restored), Is.EqualTo(hadOriginal));
		Assert.That(restored, Is.SameAs(original));
	}

	#region Inner Types

	/// <summary>An ordinary typed record shared by the facade scenarios.</summary>
	public class Preferences : SettingsObject {
		/// <summary>The stored value used to distinguish user records.</summary>
		[DefaultValue("Default")]
		public string Name { get; set; } = "Default";
	}

	// Controlled async storage uses real directional-base creation/identity behavior and exposes meaningful completion boundaries.
	private sealed class ControlledProvider : AsyncSettingsProviderBase {
		private readonly record struct SettingKey(Type SettingsType, object Id);

		private readonly Dictionary<SettingKey, SettingsObject> _records = new();
		private readonly bool _autoSave;

		// Configure only creation policy; all storage operations remain directly asynchronous.
		public ControlledProvider(bool autoSave = false) {
			_autoSave = autoSave;
		}

		public override bool AutoSaveNewSettings {
			get {
				return _autoSave;
			}
		}

		public Task ContainsRelease { get; init; } = Task.CompletedTask;

		public Task SaveRelease { get; init; } = Task.CompletedTask;

		public Task ClearRelease { get; init; } = Task.CompletedTask;

		public Exception ContainsFailure { get; set; }

		public Exception LoadFailure { get; set; }

		public Exception SaveFailure { get; set; }

		public Exception ClearFailure { get; init; }

		public int ContainsCalls { get; private set; }

		public int CreateCalls { get; private set; }

		public int LoadCalls { get; private set; }

		public int SaveCalls { get; private set; }

		public int ClearCalls { get; private set; }

		public SynchronizationContext ContainsContext { get; private set; }

		public SynchronizationContext ClearContext { get; private set; }

		public int RecordCount {
			get {
				return _records.Count;
			}
		}

		// A creation count detects facade code that bypasses the provider's creation path or duplicates autosave.
		public override Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			CreateCalls++;
			return base.NewSettingAsync(settingsObjectType, id, cancellationToken);
		}

		// Make lookup completion and its entry context observable without wrapping the provider in a worker.
		public override async Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			ContainsCalls++;
			ContainsContext = SynchronizationContext.Current;
			await ContainsRelease.WaitAsync(cancellationToken);
			if (ContainsFailure != null)
				throw ContainsFailure;
			return _records.ContainsKey(new SettingKey(settingsObjectType, id));
		}

		// Let the actual async base attach provider identity to the returned persisted value.
		protected override Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			LoadCalls++;
			if (LoadFailure != null)
				return Task.FromException<SettingsObject>(LoadFailure);
			return Task.FromResult(_records[new SettingKey(settingsObjectType, id)]);
		}

		// Publish a record only after storage acceptance; failure and cancellation leave it absent.
		protected override async Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken) {
			SaveCalls++;
			await SaveRelease.WaitAsync(cancellationToken);
			if (SaveFailure != null)
				throw SaveFailure;
			_records[new SettingKey(settings.GetType(), settings.ID)] = settings;
		}

		// Clear only after the controlled asynchronous storage boundary succeeds.
		public override async Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			ClearCalls++;
			ClearContext = SynchronizationContext.Current;
			await ClearRelease.WaitAsync(cancellationToken);
			if (ClearFailure != null)
				throw ClearFailure;
			_records.Clear();
		}

		// Satisfy the real provider delete contract without creating a facade-specific production hook.
		public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			_records.Remove(new SettingKey(settings.GetType(), settings.ID));
			return Task.CompletedTask;
		}

		// Seed actual stored records without invoking creation or save behavior under test.
		public void Seed(SettingsObject settings) {
			_records[new SettingKey(settings.GetType(), settings.ID)] = settings;
		}
	}

	#endregion
}
