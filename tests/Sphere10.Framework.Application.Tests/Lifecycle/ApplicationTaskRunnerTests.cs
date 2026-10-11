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
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationTaskRunnerTests {
	[Test]
	public void InitializersRunSequentialPriorityOrderBeforeTheParallelPhase() {
		var calls = new ConcurrentQueue<string>();
		var tasks = new IApplicationInitializer[] {
			new RecordingInitializer(() => calls.Enqueue("parallel early"), -100, true),
			new RecordingInitializer(() => calls.Enqueue("late"), 20),
			new RecordingInitializer(() => calls.Enqueue("early first"), 1),
			new RecordingInitializer(() => calls.Enqueue("early second"), 1),
			new RecordingInitializer(() => calls.Enqueue("parallel late"), 100, true)
		};
		Tools.Application.ExecuteInitializers(tasks);
		Assert.That(calls.Take(3), Is.EqualTo(new[] { "early first", "early second", "late" }));
		Assert.That(calls.Skip(3), Is.EquivalentTo(new[] { "parallel early", "parallel late" }));
	}

	[Test]
	public void InitializerProgressAndLogsIdentifyTasksAndReachOneHundredPercent() {
		var progress = new List<(string Title, string Message, int? Completed, int? Total, decimal Percent)>();
		var logger = new RecordingLogger();
		var tasks = new IApplicationInitializer[] {
			new RecordingInitializer(() => { }),
			new RecordingInitializer(() => { }, parallelizable: true),
			new RecordingInitializer(() => { }, parallelizable: true)
		};
		Tools.Application.ExecuteInitializers(tasks, logger, (title, message, completed, total, percent) => progress.Add((title, message, completed, total, percent)));
		Assert.That(progress, Has.Count.EqualTo(6));
		Assert.That(progress.All(item => item.Title == "Initializing" && item.Message == nameof(RecordingInitializer) && item.Total == 3), Is.True);
		Assert.That(progress.Select(item => item.Completed), Is.Ordered);
		Assert.That(progress.First().Completed, Is.Zero);
		Assert.That(progress.Last().Completed, Is.EqualTo(3));
		Assert.That(progress.First().Percent, Is.Zero);
		Assert.That(progress.Last().Percent, Is.EqualTo(100M));
		Assert.That(progress.All(item => item.Percent == 100M * item.Completed / item.Total), Is.True);
		Assert.That(logger.Messages.Count(message => message == "Initializing RecordingInitializer..."), Is.EqualTo(3));
		Assert.That(logger.Messages.Count(message => message == "Initialized RecordingInitializer."), Is.EqualTo(3));
	}

	[Test]
	public void FailedSequentialInitializerPreservesItsExceptionAndStopsFollowingTasks() {
		var failure = new InvalidOperationException("startup failed");
		var calls = new List<string>();
		var tasks = new IApplicationInitializer[] {
			new RecordingInitializer(() => { calls.Add("first"); throw failure; }, 1),
			new RecordingInitializer(() => calls.Add("later"), 2),
			new RecordingInitializer(() => calls.Add("parallel"), 0, true)
		};
		Tools.Exceptions.ExecuteCapturingException(() => Tools.Application.ExecuteInitializers(tasks), out var error);
		Assert.That(error, Is.SameAs(failure));
		Assert.That(calls, Is.EqualTo(new[] { "first" }));
	}

	[Test]
	public void FailedParallelInitializerRetainsParallelForEachExceptionBehavior() {
		var failure = new InvalidOperationException("parallel startup failed");
		Tools.Exceptions.ExecuteCapturingException(() => Tools.Application.ExecuteInitializers(new[] {
			new RecordingInitializer(() => throw failure, parallelizable: true)
		}), out var error);
		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(((AggregateException)error).InnerExceptions, Does.Contain(failure));
	}

	[Test]
	public void FinalizersPreserveRegistrationOrderAndReportEveryCompletion() {
		var calls = new List<string>();
		var progress = new List<(int? Completed, int? Total, decimal Percent)>();
		var logger = new RecordingLogger();
		var tasks = new IApplicationFinalizer[] {
			new RecordingFinalizer(() => calls.Add("first"), 100),
			new RecordingFinalizer(() => calls.Add("second"), -100)
		};
		Tools.Application.ExecuteFinalizers(tasks, logger, (_, message, completed, total, percent) => {
			Assert.That(message, Is.EqualTo(nameof(RecordingFinalizer)));
			progress.Add((completed, total, percent));
		});
		Assert.That(calls, Is.EqualTo(new[] { "first", "second" }), "Finalizer priorities have never changed their registration order.");
		Assert.That(progress.Select(item => item.Completed), Is.EqualTo(new[] { 0, 1, 1, 2 }));
		Assert.That(progress.All(item => item.Total == 2 && item.Percent == 50M * item.Completed), Is.True);
		Assert.That(logger.Messages, Is.EqualTo(new[] {
			"Finalizing RecordingFinalizer...", "Finalized RecordingFinalizer.",
			"Finalizing RecordingFinalizer...", "Finalized RecordingFinalizer."
		}));
	}

	[Test]
	public void FinalizationCollectsFailuresAfterAttemptingEveryTask() {
		var firstFailure = new InvalidOperationException("first failure");
		var lastFailure = new ArgumentException("last failure");
		var calls = new List<string>();
		var logger = new RecordingLogger();
		var progress = new List<int?>();
		var tasks = new IApplicationFinalizer[] {
			new RecordingFinalizer(() => { calls.Add("first"); throw firstFailure; }),
			new RecordingFinalizer(() => calls.Add("middle")),
			new RecordingFinalizer(() => { calls.Add("last"); throw lastFailure; })
		};
		Tools.Exceptions.ExecuteCapturingException(() => Tools.Application.ExecuteFinalizers(tasks, logger,
			(_, _, completed, _, _) => progress.Add(completed)), out var error);
		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(calls, Is.EqualTo(new[] { "first", "middle", "last" }));
		Assert.That(((AggregateException)error).InnerExceptions, Is.EqualTo(new Exception[] { firstFailure, lastFailure }));
		Assert.That(logger.Errors, Is.EqualTo(new Exception[] { firstFailure, lastFailure }));
		Assert.That(progress.Last(), Is.EqualTo(3), "Failed tasks still count as attempted shutdown work.");
	}

	[Test]
	public void ProgressFailuresCannotSuppressFinalizationTasks() {
		var calls = new List<string>();
		var failure = new InvalidOperationException("progress observer failed");
		var tasks = new IApplicationFinalizer[] {
			new RecordingFinalizer(() => calls.Add("first")),
			new RecordingFinalizer(() => calls.Add("second"))
		};
		Tools.Exceptions.ExecuteCapturingException(() => Tools.Application.ExecuteFinalizers(tasks, progress: (_, _, _, _, _) => throw failure), out var error);
		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
		Assert.That(((AggregateException)error).InnerExceptions, Has.Count.EqualTo(4));
		Assert.That(((AggregateException)error).InnerExceptions.All(item => ReferenceEquals(item, failure)), Is.True);
	}

	[Test]
	public void LoggingFailuresCannotSuppressFinalizationTasks() {
		var calls = new List<string>();
		var tasks = new IApplicationFinalizer[] {
			new RecordingFinalizer(() => calls.Add("first")),
			new RecordingFinalizer(() => calls.Add("second"))
		};
		Tools.Exceptions.ExecuteCapturingException(() => Tools.Application.ExecuteFinalizers(tasks, new FailingLogger()), out var error);
		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
		Assert.That(((AggregateException)error).InnerExceptions, Has.Count.EqualTo(4));
		Assert.That(((AggregateException)error).InnerExceptions.All(item => item.Message == "logging failed"), Is.True);
	}

	[Test]
	public void InvalidInventoriesAreRejectedBeforeAnyTaskRuns() {
		var calls = 0;
		Assert.That(() => Tools.Application.ExecuteInitializers(null), Throws.ArgumentNullException);
		Assert.That(() => Tools.Application.ExecuteFinalizers(null), Throws.ArgumentNullException);
		Assert.That(() => Tools.Application.ExecuteInitializers(new IApplicationInitializer[] { new RecordingInitializer(() => calls++), null }), Throws.ArgumentException);
		Assert.That(() => Tools.Application.ExecuteFinalizers(new IApplicationFinalizer[] { new RecordingFinalizer(() => calls++), null }), Throws.ArgumentException);
		Assert.That(calls, Is.Zero);
	}

	[Test]
	public void EmptyInventoriesDoNotEmitTaskProgressOrLogs() {
		var logger = new RecordingLogger();
		var progressCount = 0;
		Tools.Application.ExecuteInitializers(Array.Empty<IApplicationInitializer>(), logger, (_, _, _, _, _) => progressCount++);
		Tools.Application.ExecuteFinalizers(Array.Empty<IApplicationFinalizer>(), logger, (_, _, _, _, _) => progressCount++);
		Assert.That(progressCount, Is.Zero);
		Assert.That(logger.Messages, Is.Empty);
	}

	private sealed class RecordingInitializer : SyncApplicationInitializerBase {
		private readonly Action _initialize;

		public RecordingInitializer(Action initialize, int priority = 100, bool parallelizable = false) {
			_initialize = initialize;
			Priority = priority;
			Parallelizable = parallelizable;
		}

		public override int Priority { get; }

		public override bool Parallelizable { get; }

		public override void Initialize() => _initialize();
	}

	private sealed class RecordingFinalizer : SyncApplicationFinalizerBase {
		private readonly Action _finalize;

		public RecordingFinalizer(Action finalize, int priority = 100) {
			_finalize = finalize;
			Priority = priority;
		}

		public override int Priority { get; }

#pragma warning disable CS0465 // Implements the existing lifecycle task contract, not a CLR finalizer.
		public override void Finalize() => _finalize();
#pragma warning restore CS0465
	}

	private sealed class RecordingLogger : LoggerBase, ILogger {
		public RecordingLogger() => Options = LogOptions.VerboseProfile;

		public ConcurrentQueue<string> Messages { get; } = new();

		public ConcurrentQueue<Exception> Errors { get; } = new();

		protected override void Log(LogLevel logLevel, string message) => Messages.Enqueue(message);

		// LoggerBase.Exception formats exception details, so retain original exceptions through the logger contract in this fixture.
		void ILogger.Exception(Exception exception, string message) => Errors.Enqueue(exception);
	}

	private sealed class FailingLogger : LoggerDecorator {
		public FailingLogger()
			: base(new NoOpLogger()) {
		}

		public override void Info(string message) => throw new InvalidOperationException("logging failed");
	}
}
