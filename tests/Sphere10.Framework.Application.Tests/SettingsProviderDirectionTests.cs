// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Checks completion, context, failures and provider state across both settings-provider directions.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SettingsProviderDirectionTests {
	// Keep concurrent edits on the actual settings root while either storage direction encrypts a separate value.
	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task EncryptedSavePreservesLiveEditsAndOwnershipAcrossAcceptedAndRejectedWrites(bool asynchronous, bool rejected) {
		using var synchronous = new SyncStorageProvider();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var asynchronousProvider = new AsyncStorageProvider { SaveRelease = release.Task };
		ISettingsProvider provider = asynchronous ? asynchronousProvider : synchronous;
		var cache = new CachedSettingsProvider(provider);
		var settings = (Preferences)await cache.NewSettingAsync(typeof(Preferences), "profile");
		settings.Provider = cache;
		settings.Secret = "original";
		var changes = 0;
		settings.OnChanged += () => changes++;
		synchronous.ReleaseSave.Reset();
		if (rejected) {
			synchronous.SaveError = new InvalidOperationException("Write failed.");
			asynchronousProvider.SaveError = new InvalidOperationException("Write failed.");
		}

		// Suspend the write after encryption, then edit the instance retained by its owner and cache.
		var saving = settings.SaveAsync();
		try {
			await (asynchronous ? asynchronousProvider.SaveStarted.Task : synchronous.SaveStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(settings.Secret, Is.EqualTo("original"));
			Assert.That(changes, Is.Zero);
			Assert.That(asynchronous ? asynchronousProvider.StoredSecret : synchronous.StoredSecret, Is.EqualTo("cipher:original"));
			settings.Secret = "edited";
		} finally {
			synchronous.ReleaseSave.Set();
			release.TrySetResult();
			if (rejected)
				await Assert.ThatAsync(async () => await saving.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InvalidOperationException>());
			else
				await saving.WaitAsync(TimeSpan.FromSeconds(5));
		}

		// Completion must retain the newer plaintext and the original runtime identity.
		Assert.That(settings.Secret, Is.EqualTo("edited"));
		Assert.That(settings.ID, Is.EqualTo("profile"));
		Assert.That(settings.Provider, Is.SameAs(cache));
		Assert.That(await cache.LoadSettingAsync(typeof(Preferences), "profile"), Is.SameAs(settings));
		Assert.That(changes, Is.EqualTo(rejected ? 0 : 1));
		Assert.That(await provider.ContainsSettingAsync(typeof(Preferences), "profile"), Is.EqualTo(!rejected));
		if (!rejected)
			Assert.That(((Preferences)await provider.LoadSettingAsync(typeof(Preferences), "profile")).Secret, Is.EqualTo("original"));

		// A later explicit save persists the edit rather than restoring or decrypting the previous value over it.
		synchronous.SaveError = null;
		asynchronousProvider.SaveError = null;
		await settings.SaveAsync();
		Assert.That(((Preferences)await provider.LoadSettingAsync(typeof(Preferences), "profile")).Secret, Is.EqualTo("edited"));
		Assert.That(settings.Secret, Is.EqualTo("edited"));
		Assert.That(changes, Is.EqualTo(rejected ? 1 : 2));
	}

	// Verify the sync source adapter remains pending and avoids caller-context capture.
	[Test]
	public async Task SyncSourceAsyncAdapterReturnsBeforeStorageCompletesAndRunsOutsideTheCallerContext() {
		// Hold the synchronous storage primitive while the async adapter returns its pending operation.
		using var provider = new SyncStorageProvider();
		provider.ReleaseSave.Reset();
		var settings = new Preferences { ID = "profile", Secret = "plain" };
		var context = new RecordingSynchronizationContext();
		var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() => {
			var previous = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(context);
			try {
				returned.SetResult(provider.SaveSettingAsync(settings));
			} finally {
				SynchronizationContext.SetSynchronizationContext(previous);
			}
		});
		Task writing = null;
		try {
			await provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			writing = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(writing.IsCompleted, Is.False);
			Assert.That(provider.PrimitiveContext, Is.Null);
			Assert.That(provider.StoredSecret, Is.EqualTo("cipher:plain"));
		} finally {
			provider.ReleaseSave.Set();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			if (writing != null)
				await writing.WaitAsync(TimeSpan.FromSeconds(5));
		}

		// Verify plaintext and publication only after storage completion.
		Assert.That(settings.Secret, Is.EqualTo("plain"));
		Assert.That(provider.ContainsSetting(typeof(Preferences), settings.ID), Is.True);
		Assert.That(context.Posts, Is.Zero);
	}

	// Verify rejected or cancelled sync-source writes do not publish records.
	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncSourceAsyncAdapterPropagatesFailureOrQueuedCancellationWithoutPublishing(bool cancelled) {
		// Choose a failed write or cancellation before the adapter reaches storage.
		using var provider = new SyncStorageProvider { SaveError = cancelled ? null : new InvalidOperationException("Write failed.") };
		using var cancellation = new CancellationTokenSource();
		if (cancelled)
			cancellation.Cancel();
		var settings = new Preferences { ID = "rejected", Secret = "plain" };

		// Observe the original failure or cancellation from the returned operation.
		if (cancelled)
			await Assert.ThatAsync(async () => await provider.SaveSettingAsync(settings, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		else
			await Assert.ThatAsync(async () => await provider.SaveSettingAsync(settings), Throws.TypeOf<InvalidOperationException>());

		// Verify plaintext and publication only after storage completion.
		Assert.That(settings.Secret, Is.EqualTo("plain"));
		Assert.That(provider.ContainsSetting(typeof(Preferences), settings.ID), Is.False);
		Assert.That(provider.SaveCalls, Is.EqualTo(cancelled ? 0 : 1));
	}

	// Verify the async source bridge waits without capturing the caller context.
	[Test]
	public async Task AsyncSourceSyncAdapterStartsOutsideTheCallerContextAndWaitsForStorage() {
		// Hold async storage while invoking the synchronous bridge under a caller context.
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new AsyncStorageProvider { SaveRelease = release.Task };
		var settings = new Preferences { ID = "profile", Secret = "plain" };
		var context = new RecordingSynchronizationContext();
		var saving = Task.Run(() => {
			var previous = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(context);
			try {
				provider.SaveSetting(settings);
			} finally {
				SynchronizationContext.SetSynchronizationContext(previous);
			}
		});
		try {
			await provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(saving.IsCompleted, Is.False);
			Assert.That(provider.PrimitiveContexts, Is.All.Null);
			Assert.That(provider.StoredSecret, Is.EqualTo("cipher:plain"));
		} finally {
			release.TrySetResult();
			await saving.WaitAsync(TimeSpan.FromSeconds(5));
		}

		// Verify plaintext and publication only after storage completion.
		Assert.That(settings.Secret, Is.EqualTo("plain"));
		Assert.That(context.Posts, Is.Zero, "The asynchronous factory must start inside the worker, before it can capture a context.");
	}

	// Verify the complete synchronous facade preserves async-provider identity and values.
	[Test]
	public void AsyncSourceSupportsSynchronousCreationReadDeleteAndClearWithProviderIdentity() {
		// Run the full synchronous facade under a context that exposes captured continuations.
		var provider = new AsyncStorageProvider();
		var context = new RecordingSynchronizationContext();
		var previous = SynchronizationContext.Current;
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);

		// Create a provider-bound instance and persist its initial values.
		var settings = (Preferences)provider.NewSetting(typeof(Preferences), "first");
		Assert.That(settings.ID, Is.EqualTo("first"));
		Assert.That(settings.Provider, Is.SameAs(provider));
		provider.SaveSetting(settings);
		Assert.That(provider.ContainsSetting(typeof(Preferences), "first"), Is.True);

		// Load with caller defaults while retaining persisted values and provider identity.
		var defaults = new Preferences { ID = "first", Secret = "caller default" };
		var loaded = (Preferences)provider.LoadSetting(defaults);
		Assert.That(loaded.Secret, Is.EqualTo("initial"));
		Assert.That(loaded.ID, Is.EqualTo("first"));
		Assert.That(loaded.Provider, Is.SameAs(provider));
		Assert.That(defaults.Secret, Is.EqualTo("caller default"));
		Assert.That(((Preferences)provider.LoadSetting(typeof(Preferences), "first")).Secret, Is.EqualTo("initial"));

		// Delete and clear through the same async-first provider facade.
		provider.DeleteSetting(loaded);
		Assert.That(provider.ContainsSetting(typeof(Preferences), "first"), Is.False);
		provider.SaveSetting(settings);
		provider.ClearSettings();
		Assert.That(provider.ContainsSetting(typeof(Preferences), "first"), Is.False);
		Assert.That(provider.PrimitiveContexts, Is.All.Null);
		Assert.That(context.Posts, Is.Zero);
	}

	// Verify synchronous bridging preserves the storage failure and restores plaintext.
	[Test]
	public void AsyncSourceSyncAdapterUnwrapsStorageFailuresAndRestoresPlaintext() {
		// Reject the storage write after the provider has encrypted its input.
		var provider = new AsyncStorageProvider { SaveError = new InvalidOperationException("Write failed.") };
		var settings = new Preferences { ID = "failed", Secret = "plain" };

		// Verify the bridge preserves the original storage exception.
		Assert.That(() => provider.SaveSetting(settings), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Write failed."));

		// Verify plaintext and publication only after storage completion.
		Assert.That(settings.Secret, Is.EqualTo("plain"));
		Assert.That(provider.ContainsSetting(typeof(Preferences), settings.ID), Is.False);
	}

	// Verify cancellation releases the pending write and restores plaintext.
	[Test]
	public async Task AsyncSourceCancellationCompletesTheOperationAndRestoresPlaintext() {
		// Suspend the storage primitive so cancellation happens during an active write.
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new AsyncStorageProvider { SaveRelease = release.Task };
		using var cancellation = new CancellationTokenSource();
		var cache = new CachedSettingsProvider(provider);
		var settings = (Preferences)await cache.NewSettingAsync(typeof(Preferences), "cancelled");
		settings.Provider = cache;
		settings.Secret = "plain";
		var changes = 0;
		settings.OnChanged += () => changes++;
		var saving = settings.SaveAsync(cancellation.Token);
		try {
			await provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(settings.Secret, Is.EqualTo("plain"));
			settings.Secret = "edited";
			cancellation.Cancel();
			await Assert.ThatAsync(async () => await saving.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		} finally {
			release.TrySetResult();
		}

		// Cancellation must not decrypt the caller's later edit or publish a saved-value notification.
		Assert.That(settings.Secret, Is.EqualTo("edited"));
		Assert.That(settings.ID, Is.EqualTo("cancelled"));
		Assert.That(settings.Provider, Is.SameAs(cache));
		Assert.That(await cache.LoadSettingAsync(typeof(Preferences), "cancelled"), Is.SameAs(settings));
		Assert.That(changes, Is.Zero);
		Assert.That(await provider.ContainsSettingAsync(typeof(Preferences), settings.ID), Is.False);

		// The same root remains usable after cancellation and saves its current cleartext normally.
		await settings.SaveAsync();
		Assert.That(((Preferences)await provider.LoadSettingAsync(typeof(Preferences), "cancelled")).Secret, Is.EqualTo("edited"));
		Assert.That(changes, Is.EqualTo(1));
	}

	// A synchronous primitive cannot be interrupted after its asynchronous adapter has started it.
	[Test]
	public async Task SyncSourceCancellationAfterStorageStartsRetainsLaterEditsAndWaitsForAcceptance() {
		using var provider = new SyncStorageProvider();
		var cache = new CachedSettingsProvider(provider);
		var settings = (Preferences)await cache.NewSettingAsync(typeof(Preferences), "profile");
		settings.Provider = cache;
		settings.Secret = "original";
		var changes = 0;
		settings.OnChanged += () => changes++;
		using var cancellation = new CancellationTokenSource();
		provider.ReleaseSave.Reset();

		// Cancellation after entry cannot preempt the synchronous write or transform a later edit.
		var saving = settings.SaveAsync(cancellation.Token);
		try {
			await provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(settings.Secret, Is.EqualTo("original"));
			settings.Secret = "edited";
			cancellation.Cancel();
			Assert.That(saving.IsCompleted, Is.False);
			Assert.That(changes, Is.Zero);
		} finally {
			provider.ReleaseSave.Set();
			await saving.WaitAsync(TimeSpan.FromSeconds(5));
		}

		Assert.That(settings.Secret, Is.EqualTo("edited"));
		Assert.That(settings.ID, Is.EqualTo("profile"));
		Assert.That(settings.Provider, Is.SameAs(cache));
		Assert.That(await cache.LoadSettingAsync(typeof(Preferences), "profile"), Is.SameAs(settings));
		Assert.That(((Preferences)await provider.LoadSettingAsync(typeof(Preferences), "profile")).Secret, Is.EqualTo("original"));
		Assert.That(changes, Is.EqualTo(1));
		await settings.SaveAsync();
		Assert.That(((Preferences)await provider.LoadSettingAsync(typeof(Preferences), "profile")).Secret, Is.EqualTo("edited"));
		Assert.That(changes, Is.EqualTo(2));
	}

	// Verify autosave completion precedes publication in the settings cache.
	[Test]
	public async Task NewSettingAsyncWaitsForAutoSaveBeforeTheCachePublishesItsIdentity() {
		// Suspend autosave before the cache can publish the newly created setting.
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new AsyncStorageProvider(autoSave: true) { SaveRelease = release.Task };
		var cache = new CachedSettingsProvider(provider);
		var creation = cache.NewSettingAsync(typeof(Preferences), "created");
		try {
			await provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(creation.IsCompleted, Is.False);
			Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "created"), Is.False);
		} finally {
			release.TrySetResult();
		}
		var created = (Preferences)await creation.WaitAsync(TimeSpan.FromSeconds(5));

		// Verify the cache publishes the successfully persisted instance.
		Assert.That(created.ID, Is.EqualTo("created"));
		Assert.That(created.Provider, Is.SameAs(cache));
		Assert.That(created.Secret, Is.EqualTo("initial"));
		Assert.That(provider.SaveCalls, Is.EqualTo(1));
		Assert.That(await cache.LoadSettingAsync(typeof(Preferences), "created"), Is.SameAs(created));
	}

	// Verify failed creation leaves the cache empty and the input decrypted.
	[Test]
	public async Task FailedAutoSaveDoesNotPublishANewCachedSetting() {
		// Reject autosave before a newly created setting becomes visible.
		var provider = new AsyncStorageProvider(autoSave: true) { SaveError = new InvalidOperationException("Write failed.") };
		var cache = new CachedSettingsProvider(provider);

		// Observe failed creation before checking cache membership.
		await Assert.ThatAsync(async () => await cache.NewSettingAsync(typeof(Preferences), "failed"), Throws.TypeOf<InvalidOperationException>());

		// Verify failure leaves no cached value and restores the settings input.
		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "failed"), Is.False);
		Assert.That(provider.SaveCalls, Is.EqualTo(1));
		Assert.That(provider.LastSaved.Secret, Is.EqualTo("initial"));
	}

	// Verify generic creation delegates autosave exactly once in both directions.
	[TestCase(false)]
	[TestCase(true)]
	public void GetCreatesAndAutomaticallySavesExactlyOnceThroughEitherDirection(bool asynchronous) {
		// Exercise the same generic helper with either storage direction.
		using var synchronous = new SyncStorageProvider(autoSave: true);
		var asynchronousProvider = new AsyncStorageProvider(autoSave: true);
		ISettingsProvider provider = asynchronous ? asynchronousProvider : synchronous;

		// Create through the generic interface helper, which delegates autosave to the provider.
		var settings = provider.Get<Preferences>("created");

		// Verify creation preserves identity and performs exactly one storage write.
		Assert.That(settings.ID, Is.EqualTo("created"));
		Assert.That(settings.Provider, Is.SameAs(provider));
		Assert.That(asynchronous ? asynchronousProvider.SaveCalls : synchronous.SaveCalls, Is.EqualTo(1));
		Assert.That(provider.ContainsSetting(typeof(Preferences), settings.ID), Is.True);
		Assert.That(((Preferences)provider.LoadSetting(typeof(Preferences), settings.ID)).Secret, Is.EqualTo("initial"));
	}

	#region Inner Types

	/// <summary>Uses an encrypted preference to exercise the provider lifecycle.</summary>
	public sealed class Preferences : SettingsObject {
		/// <summary>Exercises encryption and plaintext restoration through both directions.</summary>
		[SettingsProviderPublicationTests.ProbeEncryptionAttribute]
		public string Secret { get; set; } = "initial";
	}

	// A real synchronous primitive can block while its asynchronous adapter remains pending.
	private sealed class SyncStorageProvider : SyncSettingsProviderBase, IDisposable {
		private readonly Dictionary<(Type Type, object Id), string> _records = new();
		private readonly bool _autoSave;

		// Configure the fixture provider without performing storage work.
		public SyncStorageProvider(bool autoSave = false) {
			_autoSave = autoSave;
		}

		// Selects whether creation must persist before returning.
		public override bool AutoSaveNewSettings {
			get {
				return _autoSave;
			}
		}

		// Releases a pending synchronous storage write.
		public ManualResetEventSlim ReleaseSave { get; } = new(true);

		// Signals when the storage primitive has started.
		public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// Rejects writes before fixture storage is changed.
		public Exception SaveError { get; set; }

		// Records the context observed by the storage primitive.
		public SynchronizationContext PrimitiveContext { get; private set; }

		// Records the value received by storage before decryption.
		public string StoredSecret { get; private set; }

		// Counts calls that reach the storage primitive.
		public int SaveCalls { get; private set; }

		// Check whether this fixture has a persisted record.
		public override bool ContainsSetting(Type settingsObjectType, object id = null) {
			return _records.ContainsKey((settingsObjectType, id));
		}

		// Remove this fixture record.
		public override void DeleteSetting(SettingsObject settings) {
			_records.Remove((settings.GetType(), settings.ID));
		}

		// Clear this fixture storage.
		public override void ClearSettings() {
			_records.Clear();
		}

		// Release the synchronization gate owned by this fixture.
		public void Dispose() {
			ReleaseSave.Dispose();
		}

		// Read fixture storage without bypassing the base provider lifecycle.
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
			return (SettingsObject)Tools.Json.ReadFromString(settingsObjectType, _records[(settingsObjectType, id)]);
		}

		// Persist fixture values after the base provider prepares them.
		protected override void SaveInternal(SettingsObject settings) {
			// Capture the encrypted input and signal that storage was reached.
			SaveCalls++;
			PrimitiveContext = SynchronizationContext.Current;
			StoredSecret = ((Preferences)settings).Secret;
			SaveStarted.TrySetResult();

			// Publish only after the test releases storage and the write succeeds.
			ReleaseSave.Wait();
			if (SaveError != null)
				throw SaveError;
			_records[(settings.GetType(), settings.ID)] = Tools.Json.WriteToString(settings);
		}
	}

	// Async primitives deliberately suspend so synchronous adapters must join real completion outside the caller's context.
	private sealed class AsyncStorageProvider : AsyncSettingsProviderBase {
		private readonly Dictionary<(Type Type, object Id), string> _records = new();
		private readonly List<SynchronizationContext> _primitiveContexts = new();
		private readonly bool _autoSave;

		// Configure the fixture provider without performing storage work.
		public AsyncStorageProvider(bool autoSave = false) {
			_autoSave = autoSave;
		}

		// Selects whether creation must persist before returning.
		public override bool AutoSaveNewSettings {
			get {
				return _autoSave;
			}
		}

		// Delays asynchronous storage completion until the test releases it.
		public Task SaveRelease { get; set; } = Task.CompletedTask;

		// Signals when the storage primitive has started.
		public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// Rejects writes before fixture storage is changed.
		public Exception SaveError { get; set; }

		// Records the value received by storage before decryption.
		public string StoredSecret { get; private set; }

		// Retains the input used to inspect restoration after a write.
		public Preferences LastSaved { get; private set; }

		// Counts calls that reach the storage primitive.
		public int SaveCalls { get; private set; }

		// Returns the contexts observed by the asynchronous primitives.
		public SynchronizationContext[] PrimitiveContexts {
			get {
				return _primitiveContexts.ToArray();
			}
		}

		// Check record presence through the asynchronous fixture contract.
		public override async Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			_primitiveContexts.Add(SynchronizationContext.Current);
			await Task.Yield();
			cancellationToken.ThrowIfCancellationRequested();
			return _records.ContainsKey((settingsObjectType, id));
		}

		// Remove this fixture record through the asynchronous contract.
		public override async Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			_primitiveContexts.Add(SynchronizationContext.Current);
			await Task.Yield();
			cancellationToken.ThrowIfCancellationRequested();
			_records.Remove((settings.GetType(), settings.ID));
		}

		// Clear this fixture storage through the asynchronous contract.
		public override async Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			_primitiveContexts.Add(SynchronizationContext.Current);
			await Task.Yield();
			cancellationToken.ThrowIfCancellationRequested();
			_records.Clear();
		}

		// Read fixture storage through the asynchronous provider primitive.
		protected override async Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			_primitiveContexts.Add(SynchronizationContext.Current);
			await Task.Yield();
			cancellationToken.ThrowIfCancellationRequested();
			return (SettingsObject)Tools.Json.ReadFromString(settingsObjectType, _records[(settingsObjectType, id)]);
		}

		// Persist fixture values after the asynchronous provider prepares them.
		protected override async Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			// Capture the encrypted input before deliberately suspending the write.
			_primitiveContexts.Add(SynchronizationContext.Current);
			SaveCalls++;
			LastSaved = (Preferences)settings;
			StoredSecret = LastSaved.Secret;
			SaveStarted.TrySetResult();

			// Completion or cancellation determines whether storage can publish this value.
			await SaveRelease.WaitAsync(cancellationToken).ConfigureAwait(false);
			await Task.Yield();
			if (SaveError != null)
				throw SaveError;
			_records[(settings.GetType(), settings.ID)] = Tools.Json.WriteToString(settings);
		}
	}

	// Keep incorrectly captured continuations runnable, making context capture an assertion failure rather than a deadlock.
	private sealed class RecordingSynchronizationContext : SynchronizationContext {
		private int _posts;

		// Counts continuations sent to the caller context.
		public int Posts {
			get {
				return Volatile.Read(ref _posts);
			}
		}

		// Count captured continuations and dispatch them without blocking the test.
		public override void Post(SendOrPostCallback callback, object state) {
			Interlocked.Increment(ref _posts);
			ThreadPool.QueueUserWorkItem(_ => callback(state));
		}
	}

	#endregion
}
