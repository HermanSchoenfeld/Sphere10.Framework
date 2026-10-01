// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Wizard;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class DefaultWizardTests {
	[Test]
	public void Initialized() {
		IWizard wizard =
			new DefaultWizard<bool>("test", new List<Type> { typeof(object) }, true, null, null);

		Assert.That(wizard.CurrentStep, Is.Not.Null);
		Assert.That(wizard.HasNext, Is.False);
		Assert.That(wizard.HasPrevious, Is.False);
	}

	[Test]
	public void NextAsync() {
		IWizard wizard =
			new DefaultWizard<bool>("test",
				new List<Type> { typeof(object), typeof(object), typeof(object) },
				true,
				null,
				null);

		Assert.That(wizard.HasNext, Is.True);
		Assert.That(wizard.HasPrevious, Is.False);

		wizard.Next();

		Assert.That(wizard.HasNext, Is.True);
		Assert.That(wizard.HasPrevious, Is.True);
		Assert.That(wizard.CurrentStep, Is.Not.Null);

		wizard.Next();

		Assert.That(wizard.HasNext, Is.False);
		Assert.That(wizard.HasPrevious, Is.True);
		Assert.That(wizard.CurrentStep, Is.Not.Null);
	}

	[Test]
	public void InjectStep() {
		IWizard wizard =
			new DefaultWizard<object>("Test", new List<Type> { typeof(int) }, new object(), null, null);


		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(int)));
		wizard.Next();

		Assert.That(wizard.HasNext, Is.False);
		wizard.UpdateSteps(StepUpdateType.Inject, new[] { typeof(double) });
		Assert.That(wizard.HasNext, Is.True);

		bool result = wizard.Next();
		Assert.That(result, Is.True);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(double)));
		Assert.That(wizard.HasNext, Is.False);
	}

	[Test]
	public void InjectStepTwiceDedupe() {
		IWizard wizard =
			new DefaultWizard<object>("Test", new List<Type> { typeof(int) }, new object(), null, null);

		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(int)));
		Assert.That(wizard.HasNext, Is.False);

		wizard.UpdateSteps(StepUpdateType.Inject, new[] { typeof(double) });
		wizard.UpdateSteps(StepUpdateType.Inject, new[] { typeof(double) });
		Assert.That(wizard.HasNext, Is.True);

		bool result = wizard.Next();
		bool secondResult = wizard.Next();

		Assert.That(result, Is.True);
		Assert.That(secondResult, Is.False);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(double)));
		Assert.That(wizard.HasNext, Is.False);
	}

	[Test]
	public void ReplaceAllNextSteps() {
		IWizard wizard =
			new DefaultWizard<object>("Test",
				new List<Type> { typeof(int), typeof(decimal), typeof(double) },
				new object(),
				null,
				null);

		wizard.UpdateSteps(StepUpdateType.ReplaceAllNext, new[] { typeof(bool) });

		bool result = wizard.Next();
		Assert.That(result, Is.True);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(bool)));
		Assert.That(wizard.HasNext, Is.False);
	}

	[Test]
	public void RemoveNext() {
		IWizard wizard =
			new DefaultWizard<object>("Test",
				new List<Type> { typeof(int), typeof(decimal), typeof(double) },
				new object(),
				null,
				null);

		wizard.UpdateSteps(StepUpdateType.RemoveNext, new[] { typeof(decimal), typeof(double) });

		Assert.That(wizard.HasNext, Is.False);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(int)));
	}

	[Test]
	public void ReplaceAll() {
		IWizard wizard =
			new DefaultWizard<object>("Test",
				new List<Type> { typeof(int) },
				new object(),
				null,
				null);

		wizard.UpdateSteps(StepUpdateType.ReplaceAll, new[] { typeof(decimal), typeof(double) });

		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(decimal)));
		Assert.That(wizard.HasNext, Is.True);
	}

	[Test]
	public async Task FinishAsyncFalse() {
		IWizard wizard =
			new DefaultWizard<bool>("Test",
				new List<Type> { typeof(int) },
				false,
				x => Task.FromResult<Result<bool>>(x),
				null);

		bool result = await wizard.FinishAsync();
		Assert.That(result, Is.False);
	}

	[Test]
	public async Task FinishAsyncTrue() {
		IWizard wizard =
			new DefaultWizard<bool>("Test",
				new List<Type> { typeof(int) },
				true,
				x => Task.FromResult<Result<bool>>(x),
				null);

		bool result = await wizard.FinishAsync();
		Assert.That(result, Is.True);
	}

	[Test]
	public async Task CancelAsyncFalse() {
		IWizard wizard =
			new DefaultWizard<bool>("Test",
				new List<Type> { typeof(int) },
				false,
				null,
				x => Task.FromResult<Result<bool>>(x));

		bool result = await wizard.CancelAsync();
		Assert.That(result, Is.False);
	}

	[Test]
	public async Task CancelAsyncTrue() {
		IWizard wizard =
			new DefaultWizard<bool>("Test",
				new List<Type> { typeof(int) },
				true,
				null,
				x => Task.FromResult<Result<bool>>(x));

		bool result = await wizard.CancelAsync();
		Assert.That(result, Is.True);
	}
}


