// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// The update presenter's decisions: what follows an accepted, declined, refused or failed notice, how the checker's announcement
/// reaches the user interface, and which updates are handed back to the checker. The notice (which installs through the updater) and
/// the user interface services are substituted, so no window is created.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdatePresenterTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task AnUpdateWhoseInstallerStartedAsksTheApplicationToExit() {
		var scenario = new Scenario();
		scenario.Presenter.Notice = _ => Task.FromResult<ProductUpdateInstallResult?>(ProductUpdateInstallResult.ExitRequired);
		Assert.That(await scenario.Presenter.ShowAsync(Update("2.0.0.0")), Is.EqualTo(ProductUpdateNoticeResult.Installing));
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0", "exit" }));
	}

	[Test]
	public async Task AnUpdateTheChannelInstalledItselfRequestsNoExit() {
		// The Windows Store closes and restarts the application itself, and the notice shows its update without release notes.
		var scenario = new Scenario();
		scenario.Presenter.Notice = _ => Task.FromResult<ProductUpdateInstallResult?>(ProductUpdateInstallResult.Completed);
		var storeUpdate = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), IsMandatory = true };
		Assert.That(await scenario.Presenter.ShowAsync(storeUpdate), Is.EqualTo(ProductUpdateNoticeResult.Installing));
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
		Assert.That(scenario.Presenter.Shown, Is.SameAs(storeUpdate), "The notice is given the update as found, with no release notes added.");
	}

	[Test]
	public async Task ADeclinedUpdateLeavesTheApplicationRunning() {
		var scenario = new Scenario();
		scenario.Presenter.Notice = _ => Task.FromResult<ProductUpdateInstallResult?>(null);
		Assert.That(await scenario.Presenter.ShowAsync(Update("2.0.0.0")), Is.EqualTo(ProductUpdateNoticeResult.Declined));
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
	}

	[Test]
	public async Task ANoticeThatFailsLeavesTheApplicationRunning() {
		var scenario = new Scenario();
		var failure = new InvalidOperationException("The notice could not be shown.");
		scenario.Presenter.Notice = _ => Task.FromException<ProductUpdateInstallResult?>(failure);
		await Assert.ThatAsync(async () => await scenario.Presenter.ShowAsync(Update("2.0.0.0")), Throws.Exception.SameAs(failure));
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }), "No exit is requested.");
		scenario.Presenter.Notice = _ => Task.FromResult<ProductUpdateInstallResult?>(ProductUpdateInstallResult.ExitRequired);
		Assert.That(await scenario.Presenter.ShowAsync(Update("2.0.0.0")), Is.EqualTo(ProductUpdateNoticeResult.Installing), "A failed presentation does not block the next one.");
	}

	[Test]
	public void ThePresenterNeedsTheUpdater() {
		var services = new ServiceCollection().BuildServiceProvider();
		var checker = new ProductUpdateChecker(new ReleaseServerProductUpdater(new SubstitutedService()));
		Assert.That(() => new ProductUpdatePresenter(services, checker, null), Throws.ArgumentNullException);
	}

	[Test]
	public async Task OneNoticeIsShownAtATimeAndNoneWhileTheApplicationIsExiting() {
		var scenario = new Scenario();
		var answer = new TaskCompletionSource<ProductUpdateInstallResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
		scenario.Presenter.Notice = _ => answer.Task;
		var first = scenario.Presenter.ShowAsync(Update("2.0.0.0"));
		Assert.That(await scenario.Presenter.ShowAsync(Update("3.0.0.0")), Is.EqualTo(ProductUpdateNoticeResult.NotShown));
		answer.SetResult(null);
		Assert.That(await first.WaitAsync(Deadline), Is.EqualTo(ProductUpdateNoticeResult.Declined));
		scenario.UserInterface.ApplicationExiting = true;
		Assert.That(await scenario.Presenter.ShowAsync(Update("3.0.0.0")), Is.EqualTo(ProductUpdateNoticeResult.NotShown));
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
	}

	[Test]
	public async Task TheCheckersAnnouncementIsPostedToTheUserInterfaceAndTheCheckDoesNotWaitForTheUser() {
		var scenario = new Scenario();
		var answer = new TaskCompletionSource<ProductUpdateInstallResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
		scenario.Presenter.Notice = _ => answer.Task;
		scenario.Presenter.Start();
		scenario.Presenter.Start();
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1), "One subscription posts one notice.");
		Assert.That(scenario.Events, Is.Empty, "The check has ended although the notice has not been shown yet.");
		// The user interface thread runs the posted work; shutdown can still drain the checker while the notice is open.
		scenario.UserInterface.RunPosted();
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
		await scenario.Checker.StopAsync().WaitAsync(Deadline);
		answer.SetResult(null);
	}

	[Test]
	public async Task AStoppedPresenterNoLongerObservesTheChecker() {
		var scenario = new Scenario();
		scenario.Presenter.Start();
		scenario.Presenter.Stop();
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.Zero);
	}

	[Test]
	public async Task ADeclinedUpdateIsNotOfferedAgain() {
		var scenario = new Scenario();
		scenario.Presenter.Start();
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1));
		scenario.UserInterface.RunPosted();
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.Zero, "The user saw the notice and chose Later.");
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
	}

	[Test]
	public async Task AnUpdateRefusedWhileTheApplicationWasExitingIsOfferedAgainByTheNextCheck() {
		var scenario = new Scenario();
		scenario.Presenter.Start();
		scenario.UserInterface.ApplicationExiting = true;
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1));
		scenario.UserInterface.RunPosted();
		Assert.That(scenario.Events, Is.Empty, "No notice is shown over an exit that is being confirmed.");
		// The exit was declined, so the application is still running at the next check.
		scenario.UserInterface.ApplicationExiting = false;
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1), "The version that was not shown is announced again.");
		scenario.UserInterface.RunPosted();
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.Zero);
	}

	[Test]
	public async Task ANewerVersionFoundWhileANoticeIsOpenIsOfferedAgainAndShownAfterThatNoticeCloses() {
		var scenario = new Scenario();
		var older = new TaskCompletionSource<ProductUpdateInstallResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
		scenario.Presenter.Notice = update => update.Version.Major == 2 ? older.Task : Task.FromResult<ProductUpdateInstallResult?>(null);
		scenario.Presenter.Start();
		var olderNotice = scenario.Presenter.ShowAsync(Update("2.0.0.0"));
		// The newer version cannot be shown over the open notice, so each check offers it again.
		for (var i = 0; i < 2; i++) {
			Assert.That(await scenario.CheckAsync("3.0.0.0"), Is.EqualTo(1));
			scenario.UserInterface.RunPosted();
		}
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0" }));
		older.SetResult(null);
		Assert.That(await olderNotice.WaitAsync(Deadline), Is.EqualTo(ProductUpdateNoticeResult.Declined));
		Assert.That(await scenario.CheckAsync("3.0.0.0"), Is.EqualTo(1));
		scenario.UserInterface.RunPosted();
		Assert.That(scenario.Events, Is.EqualTo(new[] { "notice 2.0.0.0", "notice 3.0.0.0" }));
		Assert.That(await scenario.CheckAsync("3.0.0.0"), Is.Zero);
	}

	[Test]
	public async Task APresentationThatFailsIsLoggedReportedToTheUserAndOfferedAgain() {
		var scenario = new Scenario();
		scenario.Presenter.Notice = _ => Task.FromException<ProductUpdateInstallResult?>(new InvalidOperationException("The notice could not be shown."));
		scenario.Presenter.Start();
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1));
		scenario.UserInterface.RunPosted();
		await scenario.UserInterface.Reported.Task.WaitAsync(Deadline);
		Assert.That(scenario.UserInterface.Reported.Task.Result, Is.EqualTo("Update Not Installed: The notice could not be shown."));
		Assert.That(scenario.Errors, Has.Count.EqualTo(1));
		Assert.That(scenario.Errors.ToArray()[0], Does.Contain("The notice could not be shown."));
		Assert.That(scenario.Events, Does.Not.Contain("exit"));
		Assert.That(await scenario.CheckAsync("2.0.0.0"), Is.EqualTo(1), "The user can try the same update again after the next check.");
	}

	private static ReleaseServerProductUpdate Update(string version) => new() { Version = ApplicationVersion.Parse(version), DownloadUrl = "https://updates.example.com/setup.exe", FileName = "setup.exe" };

	#region Inner Types

	/// <summary>A presenter over a real checker and release-server updater, with a substituted service, notice, logger and user interface.</summary>
	private sealed class Scenario {

		public Scenario() {
			Events = new List<string>();
			Errors = new ConcurrentQueue<string>();
			Service = new SubstitutedService();
			UserInterface = new SubstitutedUserInterface(Events);
			Updater = new ReleaseServerProductUpdater(Service);
			Checker = new ProductUpdateChecker(Updater);
			var services = new ServiceCollection();
			services.AddSingleton<IUserInterfaceServices>(UserInterface);
			services.AddSingleton<ILogger>(new ActionLogger(_ => { }, _ => { }, _ => { }, Errors.Enqueue));
			Presenter = new SubstitutedPresenter(services.BuildServiceProvider(), Checker, Updater, Events);
		}

		public List<string> Events { get; }

		public ConcurrentQueue<string> Errors { get; }

		public SubstitutedService Service { get; }

		public ReleaseServerProductUpdater Updater { get; }

		public SubstitutedUserInterface UserInterface { get; }

		public ProductUpdateChecker Checker { get; }

		public SubstitutedPresenter Presenter { get; }

		/// <summary>Runs one check that finds the version and answers how many notices that check posted to the user interface.</summary>
		public async Task<int> CheckAsync(string version) {
			var posted = UserInterface.Posted.Count;
			Service.Version = version;
			await Checker.CheckAsync().WaitAsync(Deadline);
			return UserInterface.Posted.Count - posted;
		}
	}

	private sealed class SubstitutedService : IProductUpdateService {
		public string Version { get; set; }

		public Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult(Version == null ? ProductUpdateCheckResult.NoUpdate : ProductUpdateCheckResult.Available(Update(Version)));
	}

	/// <summary>A presenter whose notice records the update it was given and answers what the case says the installation did.</summary>
	private sealed class SubstitutedPresenter : ProductUpdatePresenter {
		private readonly List<string> _events;

		public SubstitutedPresenter(IServiceProvider services, ProductUpdateChecker checker, ProductUpdaterBase updater, List<string> events)
			: base(services, checker, updater) {
			_events = events;
		}

		public Func<ProductUpdate, Task<ProductUpdateInstallResult?>> Notice { get; set; } = _ => Task.FromResult<ProductUpdateInstallResult?>(null);

		public ProductUpdate Shown { get; private set; }

		protected override Task<ProductUpdateInstallResult?> ShowNoticeAsync(Form owner, ProductUpdate update) {
			Assert.That(Updater, Is.Not.Null, "The notice installs through the checker's updater.");
			_events.Add($"notice {update.Version}");
			Shown = update;
			return Notice(update);
		}
	}

	/// <summary>Records exit requests and reports, and holds posted work until a case runs it as the user interface thread would.</summary>
	private sealed class SubstitutedUserInterface : IUserInterfaceServices {
		private readonly List<string> _events;
		private int _run;

		public SubstitutedUserInterface(List<string> events) => _events = events;

		public List<Action> Posted { get; } = new();

		public TaskCompletionSource<string> Reported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public bool ApplicationExiting { get; set; }

		public string Status { get; set; }

		public ILoadingIndicator LoadingIndicator { get; } = new LoadingIndicator();

		public object PrimaryUIController => null;

		/// <summary>Runs the work posted since the last call, once.</summary>
		public void RunPosted() {
			while (_run < Posted.Count)
				Posted[_run++]();
		}

		public void Exit(bool force = false) => _events.Add(force ? "forced exit" : "exit");

		public void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) {
			Assert.That(executeAsync, Is.True, "The announcement is posted, never sent.");
			Posted.Add(action);
		}

		public Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default) {
			Reported.TrySetResult($"{title}: {message}");
			return Task.CompletedTask;
		}

		public Task ReportErrorAsync(Exception error, CancellationToken cancellationToken = default) => ReportErrorAsync("Error", error.Message, cancellationToken);

		public Task ReportErrorAsync(string message, CancellationToken cancellationToken = default) => ReportErrorAsync("Error", message, cancellationToken);

		public Task ReportFatalErrorAsync(string title, string message, CancellationToken cancellationToken = default) => ReportErrorAsync(title, message, cancellationToken);

		public Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

		public Task ShowSendCommentDialog() => Task.CompletedTask;

		public Task ShowSubmitBugReportDialog() => Task.CompletedTask;

		public Task ShowRequestFeatureDialog() => Task.CompletedTask;

		public Task ShowAboutBox() => Task.CompletedTask;

		public bool AskYN(string question) => false;
	}

	#endregion
}
