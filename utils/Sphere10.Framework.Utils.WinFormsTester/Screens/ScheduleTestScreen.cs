// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates reusable scheduler policies with screen-owned jobs, subscriptions and log delivery.</summary>
public partial class ScheduleTestScreen : ApplicationScreen {
	private readonly ConcurrentQueue<string> _pending = new();
	private readonly Timer _refreshTimer;
	private readonly ILogger _logger;
	private Scheduler _asynchronousScheduler;
	private Scheduler _synchronousScheduler;
	private volatile bool _acceptLogs = true;

	/// <summary>Creates an inactive example without touching the process-global schedulers.</summary>
	public ScheduleTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_logger = new ActionLogger(QueueLog);
		_refreshTimer = new Timer { Interval = 80 };
		_refreshTimer.Tick += RefreshLog;
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override SchedulingScreenSettings ScreenSettings => (SchedulingScreenSettings)base.ScreenSettings;

	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (!Tools.Runtime.IsDesignMode)
			_refreshTimer.Start();
	}

	// Jobs only use the detached queue logger, so disposal never leaves a worker invoking a destroyed HWND.
	protected override void OnDestroyScreen() {
		_acceptLogs = false;
		_refreshTimer.Stop();
		_refreshTimer.Tick -= RefreshLog;
		_refreshTimer.Dispose();
		ReleaseScheduler(ref _asynchronousScheduler);
		ReleaseScheduler(ref _synchronousScheduler);
		_pending.Clear();
		base.OnDestroyScreen();
	}

	private void _job1Button_Click(object sender, EventArgs e) {
		CreateScheduler(false, actionBased: true);
		_asynchronousScheduler.Start();
		_status.Text = "Two screen-owned asynchronous sample jobs are running.";
	}

	private void TestButton_Click(object sender, EventArgs e) {
		CreateScheduler(true);
	}

	private void AsyncTestButton_Click(object sender, EventArgs e) {
		CreateScheduler(false);
	}

	// The same policy-aware composition serves the two independent scheduler owners.
	private void CreateScheduler(bool synchronous, bool actionBased = false) {
		var current = synchronous ? _synchronousScheduler : _asynchronousScheduler;
		if (current != null) {
			_status.Text = "That scheduler already exists. Use Start or Stop.";
			return;
		}
		var values = !HasSettings ? new SchedulingScreenSettings() : ScreenSettings;
		var scheduler = new Scheduler(SchedulerPolicy.DontThrow | SchedulerPolicy.RemoveJobOnError
			| (synchronous ? SchedulerPolicy.ForceSyncronous : 0), _logger);
		scheduler.JobStatusChanged += JobStatusChanged;
		scheduler.OnBeforeJobBegin = job => QueueLog(job.Name + " [OnBeforeJobBegin]");
		scheduler.OnAfterJobEnd = job => QueueLog(job.Name + " [OnAfterJobEnd]");
		scheduler.OnJobError = (job, error) => QueueLog(job.Name + ": " + error.Message);
		for (var index = 1; index <= 2; index++) {
			var name = (synchronous ? "Sync " : "Async ") + index;
			var builder = actionBased
				? JobBuilder.For(() => QueueLog("Executed action " + name))
				: JobBuilder.For(typeof(TestJob));
			builder.Called(name);
			if (synchronous)
				builder.RunSyncronously();
			else
				builder.RunAsyncronously();
			var job = builder.Repeat.OnInterval(TimeSpan.FromMilliseconds(values.IntervalMilliseconds * index), totalIterations: (uint)values.Iterations).Build();
			scheduler.AddJob(job);
		}
		if (synchronous)
			_synchronousScheduler = scheduler;
		else
			_asynchronousScheduler = scheduler;
		_status.Text = $"Created {(synchronous ? "synchronous" : "asynchronous")} scheduler: two jobs, {values.Iterations} interval execution(s) each.";
	}

	private void SyncStartButton_Click(object sender, EventArgs e) {
		_synchronousScheduler?.Start();
		_status.Text = _synchronousScheduler == null ? "Create the synchronous scheduler first." : "Synchronous scheduler started.";
	}

	private void SyncStopButton_Click(object sender, EventArgs e) {
		_synchronousScheduler?.Stop();
		_status.Text = "Synchronous scheduling stopped. A running bounded job finishes normally.";
	}

	private void AsyncStartButton_Click(object sender, EventArgs e) {
		_asynchronousScheduler?.Start();
		_status.Text = _asynchronousScheduler == null ? "Create the asynchronous scheduler first." : "Asynchronous scheduler started.";
	}

	private void AsyncStopButton_Click(object sender, EventArgs e) {
		_asynchronousScheduler?.Stop();
		_status.Text = "Asynchronous scheduling stopped. Running bounded jobs finish normally.";
	}

	private void SyncSerializeButton_Click(object sender, EventArgs e) {
		try {
			using var original = CreateSerializableExample();
			using var restored = new Scheduler(SchedulerPolicy.ForceSyncronous | SchedulerPolicy.DontThrow, _logger);
			var surrogate = original.ToSerializableSurrogate();
			restored.FromSerializableSurrogate(surrogate);
			QueueLog($"Surrogate restored {restored.ToSerializableSurrogate().Jobs.Length} job(s). Neither scheduler was started.");
			_status.Text = "Surrogate round trip completed.";
		} catch (Exception error) {
			_status.Text = "Surrogate round trip failed: " + error.Message;
		}
	}

	private void XmlSerializeButton_Click(object sender, EventArgs e) {
		string path = null;
		try {
			path = Path.GetTempFileName();
			using var scheduler = CreateSerializableExample();
			using var restored = new Scheduler(SchedulerPolicy.ForceSyncronous | SchedulerPolicy.DontThrow, _logger);
			var serializer = new XmlSchedulerSerializer(path);
			serializer.Serialize(scheduler.ToSerializableSurrogate());
			restored.FromSerializableSurrogate(serializer.Deserialize());
			QueueLog(File.ReadAllText(path));
			_status.Text = $"XML restored {restored.ToSerializableSurrogate().Jobs.Length} job(s). The temporary file was removed.";
		} catch (Exception error) {
			_status.Text = "XML round trip failed: " + error.Message;
		} finally {
			if (path != null)
				File.Delete(path);
		}
	}

	private Scheduler CreateSerializableExample() {
		var scheduler = new Scheduler(SchedulerPolicy.ForceSyncronous | SchedulerPolicy.DontThrow, _logger);
		scheduler.AddJob(JobBuilder.For(typeof(TestJob))
			.Called("Serialized one-shot")
			.RunOnce(DateTime.Now.AddMinutes(1))
			.RunSyncronously()
			.Build());
		scheduler.AddJob(JobBuilder.For(typeof(TestJob))
			.Called("Serialized sample")
			.RunSyncronously()
			.Repeat
				.OnInterval(TimeSpan.FromSeconds(1), totalIterations: 3)
			.Build());
		return scheduler;
	}

	private void JobStatusChanged(IJob job, JobStatus from, JobStatus to) {
		QueueLog($"{job.Name}: {from} → {to}");
	}

	// Callbacks may arrive on worker threads and never access native controls.
	private void QueueLog(string text) {
		if (_acceptLogs && _pending.Count < 2000)
			_pending.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {text}");
	}

	private void RefreshLog(object sender, EventArgs e) {
		var output = new StringBuilder();
		while (_pending.TryDequeue(out var line))
			output.AppendLine(line);
		if (output.Length == 0)
			return;
		textBox1.AppendText(output.ToString());
		if (textBox1.TextLength > 40000)
			textBox1.Text = textBox1.Text[^30000..];
	}

	private void ClearClicked(object sender, EventArgs e) {
		_pending.Clear();
		textBox1.Clear();
	}

	// Stop future schedules and remove callbacks before releasing the screen-owned scheduler.
	private void ReleaseScheduler(ref Scheduler scheduler) {
		var owned = scheduler;
		scheduler = null;
		if (owned == null)
			return;
		owned.Stop();
		owned.JobStatusChanged -= JobStatusChanged;
		owned.OnBeforeJobBegin = null;
		owned.OnAfterJobEnd = null;
		owned.OnJobError = null;
		owned.Dispose();
	}

	#region Inner Types

	/// <summary>Defaults for new screen-owned scheduler examples.</summary>
	[UserEditable]
	public class SchedulingScreenSettings : SettingsObject {
		/// <summary>The first job interval; the second job uses twice this interval.</summary>
		[DefaultValue(1000)]
		[Range(100, 60000)]
		public int IntervalMilliseconds { get; set; } = 1000;

		/// <summary>The finite number of interval executions per job.</summary>
		[DefaultValue(5)]
		[Range(1, 100)]
		public int Iterations { get; set; } = 5;
	}
	#endregion
}
