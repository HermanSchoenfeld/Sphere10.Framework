// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class JobScheduleInterfaceTests {
	[Test]
	public void DirectAndExistingSchedulesOrderByNextStartTime([Values(false, true)] bool useBaseClass, [Values(-1, 0, 1)] int offset) {
		var start = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var first = CreateSchedule(useBaseClass, start.AddMinutes(offset));
		var second = CreateSchedule(useBaseClass, start);

		var ordering = ((IComparable<IJobSchedule>)first).CompareTo(second);

		Assert.That(Math.Sign(ordering), Is.EqualTo(offset));
		Assert.That(Math.Sign(Comparer<IJobSchedule>.Default.Compare(second, first)), Is.EqualTo(-offset));
	}

	[Test]
	public void ExistingPublicApiUsesSameOrderingAsInterface() {
		var start = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var schedule = new OnIntervalSchedule<ActionJob>(start, TimeSpan.FromMinutes(1));
		var later = new DirectSchedule(start.AddMinutes(2));

		Assert.That(schedule.CompareTo(later), Is.LessThan(0));
		Assert.That(((IComparable<IJobSchedule>)schedule).CompareTo(later), Is.EqualTo(schedule.CompareTo(later)));
	}

	[Test]
	public void ComparisonPreservesNullFailure([Values(false, true)] bool useBaseClass) {
		var schedule = CreateSchedule(useBaseClass, DateTime.MaxValue);

		Assert.That(() => ((IComparable<IJobSchedule>)schedule).CompareTo(null), Throws.TypeOf<NullReferenceException>());
	}

	[Test]
	public void PublicClassComparisonUsesClassStartTimeWhenInterfacePropertyIsReimplemented() {
		var start = new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var schedule = new RemappedSchedule(start, start.AddMinutes(2));
		var middle = new DirectSchedule(start.AddMinutes(1));

		Assert.That(schedule.NextStartTime, Is.EqualTo(start));
		Assert.That(((IJobSchedule)schedule).NextStartTime, Is.EqualTo(start.AddMinutes(2)));
		Assert.That(schedule.CompareTo(middle), Is.LessThan(0));
		Assert.That(Comparer<IJobSchedule>.Default.Compare(schedule, middle), Is.LessThan(0));
		Assert.That(((IComparable<IJobSchedule>)middle).CompareTo(schedule), Is.LessThan(0));
	}

	private static IJobSchedule CreateSchedule(bool useBaseClass, DateTime start)
		=> useBaseClass ? new OnIntervalSchedule<ActionJob>(start, TimeSpan.FromMinutes(1)) : new DirectSchedule(start);

	private sealed class RemappedSchedule : OnIntervalSchedule<ActionJob>, IJobSchedule {
		private readonly DateTime _interfaceStartTime;

		public RemappedSchedule(DateTime classStartTime, DateTime interfaceStartTime) : base(classStartTime, TimeSpan.FromMinutes(1))
			=> _interfaceStartTime = interfaceStartTime;

		DateTime IJobSchedule.NextStartTime => _interfaceStartTime;
	}

	private sealed class DirectSchedule : IJobSchedule {
		public DirectSchedule(DateTime start) => NextStartTime = start;

		public IJob Job => null;

		public DateTime? LastStartTime => null;

		public DateTime? LastEndTime => null;

		public DateTime NextStartTime { get; }

		public DateTime? EndDate => null;

		public ReschedulePolicy ReschedulePolicy => ReschedulePolicy.OnStart;

		public uint IterationsRemaining => uint.MaxValue;

		public uint IterationsExecuted => 0;

		public void NotifyStart(DateTime start) { }

		public void NotifyExecution(DateTime start, DateTime end) { }

		public JobScheduleSerializableSurrogate ToSerializableSurrogate() => null;

		public void FromSerializableSurrogate(JobScheduleSerializableSurrogate scheduleSurrogate) { }
	}
}
