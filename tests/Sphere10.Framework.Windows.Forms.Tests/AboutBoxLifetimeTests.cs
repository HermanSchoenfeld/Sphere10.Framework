// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class AboutBoxLifetimeTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RepeatedHostedAboutCallsRespectTransientAndSingletonContainerOwnership(bool singleton) => RunWithMessageLoop(async () => {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		var dialogs = new List<ProbeAboutBox>();
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		})) {
			Sphere10Framework.Instance
				.Build()
				.ConfigureServices(services => {
					services.AddMainForm<LiteMainForm>();
					IAboutBox Create(IServiceProvider _) {
						var dialog = new ProbeAboutBox();
						dialogs.Add(dialog);
						return dialog;
					}
					if (singleton)
						services.AddSingleton<IAboutBox>(Create);
					else
						services.AddTransient<IAboutBox>(Create);
				})
				.Start();
			var mainForm = (LiteMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();
			_ = mainForm.Handle;
			for (var call = 0; call < 2; call++) {
				await mainForm.ShowAboutBox();
				Assert.That(dialogs.Count, Is.EqualTo(singleton ? 1 : call + 1));
				Assert.That(dialogs.Last().IsDisposed, Is.EqualTo(!singleton), "A per-call scope owns transient dialogs; a reusable singleton remains owned by the root container.");
				Assert.That(dialogs.Sum(dialog => dialog.Presentations), Is.EqualTo(call + 1), "Both calls must present and close a real native modal dialog.");
			}
		}
		Assert.That(dialogs.All(dialog => dialog.IsDisposed), Is.True, "Root shutdown must eventually dispose the factory-created singleton too.");
	});

	private static void RunWithMessageLoop(Func<Task> test) {
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The About lifetime test timed out.");
			foreach (var dialog in System.Windows.Forms.Application.OpenForms.OfType<ProbeAboutBox>().ToArray())
				dialog.Dispose();
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var closeOwner = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	private sealed class ProbeAboutBox : Form, IAboutBox {
		public ProbeAboutBox() {
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
		}

		public int Presentations { get; private set; }

		protected override void OnVisibleChanged(EventArgs args) {
			base.OnVisibleChanged(args);
			if (Visible) {
				Presentations++;
				BeginInvoke((Action)Close);
			}
		}
	}
}
