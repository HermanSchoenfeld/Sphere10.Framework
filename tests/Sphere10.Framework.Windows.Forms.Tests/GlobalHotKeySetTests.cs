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
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GlobalHotKeySetTests {
	private const Keys Toggle = Keys.Control | Keys.Alt | Keys.T;
	private const Keys Next = Keys.Control | Keys.Alt | Keys.N;

	[Test]
	public void RegistersBindingsRoutesPressesToTheLatestActionAndReportsFailures() {
		var created = new List<FakeHotKey>();
		using var set = new RecordingHotKeySet(created, Next);
		var calls = new List<string>();

		set.Apply(new[] {
			new GlobalHotKeyBinding("toggle", "Toggle", Toggle, () => calls.Add("first")),
			new GlobalHotKeyBinding("next", "Next effect", Next, () => calls.Add("next")),
			new GlobalHotKeyBinding("previous", "Previous effect", Keys.None, () => calls.Add("previous"))
		});

		Assert.That(created, Has.Count.EqualTo(2), "An unset binding creates no hot key.");
		Assert.That(set.RegisteredKeys, Is.EqualTo(new[] { "toggle" }));
		Assert.That(set.Errors, Is.EqualTo(new[] { "Next effect (Ctrl+Alt+N): unavailable" }));
		Assert.That(created[1].Disposed, Is.True, "A new binding that failed to register is released.");
		created[0].Press();

		// A second application with a new action and the same shortcut keeps the registration and routes to the new action.
		set.Apply(new[] { new GlobalHotKeyBinding("TOGGLE", "Toggle", Toggle, () => calls.Add("second")) });
		created[0].Press();

		Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
		Assert.That(created[0].Registrations, Is.EqualTo(1), "An unchanged shortcut is not registered again.");
		Assert.That(set.Errors, Is.Empty);
	}

	[Test]
	public void AChangedShortcutReRegistersAndAFailedChangeKeepsThePreviousShortcut() {
		var created = new List<FakeHotKey>();
		using var set = new RecordingHotKeySet(created, Next);

		set.Apply(new[] { new GlobalHotKeyBinding("toggle", null, Toggle, () => { }) });
		set.Apply(new[] { new GlobalHotKeyBinding("toggle", null, Next, () => { }) });

		Assert.That(created, Has.Count.EqualTo(1));
		Assert.That(created[0].Registered, Is.EqualTo(Toggle), "The failed change keeps the working registration.");
		Assert.That(set.Errors.Single(), Does.StartWith("toggle (Ctrl+Alt+N)"), "Without a caption the key names the binding.");
		set.Apply(new[] { new GlobalHotKeyBinding("toggle", null, Keys.Control | Keys.Alt | Keys.X, () => { }) });
		Assert.That(created[0].Registered, Is.EqualTo(Keys.Control | Keys.Alt | Keys.X));
	}

	[Test]
	public void RemovedOrClearedBindingsAndDisposalReleaseTheirHotKeys() {
		var created = new List<FakeHotKey>();
		var set = new RecordingHotKeySet(created, Keys.None);
		set.Apply(new[] {
			new GlobalHotKeyBinding("a", "A", Toggle, () => { }),
			new GlobalHotKeyBinding("b", "B", Next, () => { }),
			new GlobalHotKeyBinding("c", "C", Keys.Control | Keys.Alt | Keys.C, () => { })
		});

		set.Apply(new[] { new GlobalHotKeyBinding("a", "A", Toggle, () => { }), new GlobalHotKeyBinding("b", "B", Keys.None, () => { }) });

		Assert.That(created.Select(hotKey => hotKey.Disposed), Is.EqualTo(new[] { false, true, true }));
		Assert.That(set.RegisteredKeys, Is.EqualTo(new[] { "a" }));
		set.Dispose();
		Assert.That(created, Has.All.Property(nameof(FakeHotKey.Disposed)).True);
		Assert.That(() => set.Apply(Array.Empty<GlobalHotKeyBinding>()), Throws.TypeOf<ObjectDisposedException>());
		set.Dispose();
	}

	[Test]
	public void ExceptionsFromActionsAreIgnoredAndInvalidBindingsAreRejected() {
		var created = new List<FakeHotKey>();
		using var set = new RecordingHotKeySet(created, Keys.None);
		set.Apply(new[] { new GlobalHotKeyBinding("a", "A", Toggle, () => throw new InvalidOperationException()) });

		Assert.That(() => created[0].Press(), Throws.Nothing);
		Assert.That(() => set.Apply(new[] { new GlobalHotKeyBinding("a", "A", Toggle, () => { }), new GlobalHotKeyBinding("A", "A", Next, () => { }) }), Throws.ArgumentException);
		Assert.That(() => set.Apply(new[] { new GlobalHotKeyBinding(" ", "A", Toggle, () => { }) }), Throws.ArgumentException);
		Assert.That(() => set.Apply(new[] { new GlobalHotKeyBinding("a", "A", Toggle, null) }), Throws.ArgumentException);
	}

	[Test]
	public void CommandsRegisterAtTheirResolvedShortcutsAndReportErrorsByName() {
		var created = new List<FakeHotKey>();
		using var set = new RecordingHotKeySet(created, Next);
		var calls = new List<string>();
		var commands = new[] {
			new HotKeyCommand("toggle", "Toggle", Toggle, () => calls.Add("toggle")),
			new HotKeyCommand("next", "Next", Keys.None, () => calls.Add("next")),
			new HotKeyCommand("previous", "Previous", Keys.Control | Keys.Alt | Keys.P, () => calls.Add("previous"))
		};
		var assignments = new[] {
			new HotKeyAssignment { Command = "NEXT", Shortcut = Next },
			new HotKeyAssignment { Command = "previous", Shortcut = Keys.None }
		};

		set.Apply(commands, assignments);

		Assert.That(set.RegisteredKeys, Is.EqualTo(new[] { "toggle" }), "A user's None removes the default shortcut.");
		Assert.That(set.ErrorsByKey.Keys, Is.EqualTo(new[] { "next" }), "The assignment applies to the command named in any case, and it failed.");
		created[0].Press();
		Assert.That(calls, Is.EqualTo(new[] { "toggle" }));
	}

	private sealed class RecordingHotKeySet : GlobalHotKeySet {
		private readonly List<FakeHotKey> _created;
		private readonly Keys _failFor;

		public RecordingHotKeySet(List<FakeHotKey> created, Keys failFor) {
			_created = created;
			_failFor = failFor;
		}

		protected override IGlobalHotKey CreateHotKey() {
			var hotKey = new FakeHotKey { FailFor = _failFor };
			_created.Add(hotKey);
			return hotKey;
		}
	}

	private sealed class FakeHotKey : IGlobalHotKey {
		public event EventHandler Pressed;

		public Keys FailFor { get; init; } = Keys.None;

		public Keys Registered { get; private set; }

		public int Registrations { get; private set; }

		public bool Disposed { get; private set; }

		public bool TryRegister(Keys shortcut, out string error) {
			if (shortcut == FailFor) {
				error = "unavailable";
				return false;
			}
			error = null;
			Registered = shortcut;
			Registrations++;
			return true;
		}

		public void Unregister() => Registered = Keys.None;

		public void Press() => Pressed?.Invoke(this, EventArgs.Empty);

		public void Dispose() => Disposed = true;
	}
}
