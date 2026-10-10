// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Wizards;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GuidedWorkspaceWizardTests {
	[TestCase("Solo", false)]
	[TestCase("Team", true)]
	public async Task SettingsBuildsTheChosenRouteAndBackPreservesTheDraft(string mode, bool hasReviewers) {
		var draft = new WorkspaceSetupDraft { Name = "Design studio", Mode = mode, Capacity = 14 };
		var wizard = CreateWizard(draft);
		var details = CreateStep<WorkspaceDetailsStep>(wizard);
		Assert.That((await details.OnNextAsync()).IsSuccess, Is.True);
		wizard.Next();
		var settings = CreateStep<WorkspaceSettingsStep>(wizard);
		Assert.That((await settings.OnNextAsync()).IsSuccess, Is.True);
		Assert.That(Array.IndexOf(wizard.Steps, typeof(WorkspaceReviewersStep)) >= 0, Is.EqualTo(hasReviewers));
		wizard.Previous();
		Assert.That(wizard.Model, Is.SameAs(draft));
		Assert.That(draft.Name, Is.EqualTo("Design studio"));
		Assert.That(draft.Capacity, Is.EqualTo(14));
	}

	[Test]
	public async Task ChangingFromTeamToSoloRemovesTheUnusedBranch() {
		var draft = new WorkspaceSetupDraft { Name = "Design studio", Mode = "Team", Reviewers = "Avery" };
		var wizard = CreateWizard(draft);
		await CreateStep<WorkspaceDetailsStep>(wizard).OnNextAsync();
		wizard.Next();
		await CreateStep<WorkspaceSettingsStep>(wizard).OnNextAsync();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WorkspaceReviewersStep)));
		wizard.Previous();
		wizard.Previous();
		draft.Mode = "Solo";
		await CreateStep<WorkspaceDetailsStep>(wizard).OnNextAsync();
		wizard.Next();
		await CreateStep<WorkspaceSettingsStep>(wizard).OnNextAsync();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WorkspaceReviewStep)));
		Assert.That(wizard.Steps, Has.None.EqualTo(typeof(WorkspaceReviewersStep)));
		Assert.That(draft.Reviewers, Is.EqualTo("Avery"), "The removed step must not erase the draft if the user later selects Team again.");
	}

	[Test]
	public async Task BackFromPresetDetourReturnsToSettingsWithoutChangingValuesOrLeavingDuplicateSteps() {
		var draft = new WorkspaceSetupDraft { Name = "Studio", Mode = "Team", Capacity = 17, Access = "Invitation only" };
		var wizard = CreateWizard(draft);
		wizard.Next();
		WorkspaceSetupFlow.SetRemainingSettingsSteps(wizard);
		WorkspaceSetupFlow.OpenPresetChooser(wizard);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WorkspacePresetStep)));
		await CreateStep<WorkspacePresetStep>(wizard).OnPreviousAsync();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WorkspaceSettingsStep)));
		Assert.That(wizard.Steps, Is.EqualTo(new[] {
			typeof(WorkspaceDetailsStep), typeof(WorkspaceSettingsStep), typeof(WorkspaceReviewersStep), typeof(WorkspaceReviewStep)
		}));
		Assert.That(draft.Capacity, Is.EqualTo(17));
		Assert.That(draft.Access, Is.EqualTo("Invitation only"));
		Assert.That(draft.Preset, Is.EqualTo("Custom"));
	}

	[TestCase(0)]
	[TestCase(101)]
	public async Task CapacityValidationKeepsTheSettingsStepAndDraft(int capacity) {
		var draft = new WorkspaceSetupDraft { Name = "Studio", Capacity = capacity };
		var wizard = CreateWizard(draft);
		wizard.Next();
		var result = await CreateStep<WorkspaceSettingsStep>(wizard).OnNextAsync();
		Assert.That(result.IsSuccess, Is.False);
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WorkspaceSettingsStep)));
		Assert.That(draft.Capacity, Is.EqualTo(capacity));
	}

	[Test]
	public async Task ReviewRequiresAcknowledgementBeforeTheCompletionSnapshotIsAccepted() {
		var draft = new WorkspaceSetupDraft { Name = "Studio" };
		var wizard = CreateWizard(draft);
		var review = CreateStep<WorkspaceReviewStep>(wizard);
		Assert.That((await review.OnNextAsync()).IsSuccess, Is.False);
		draft.Acknowledged = true;
		Assert.That((await review.OnNextAsync()).IsSuccess, Is.True);
		var snapshot = draft.Copy();
		draft.Name = "Changed draft";
		Assert.That(snapshot.Name, Is.EqualTo("Studio"));
		Assert.That(snapshot, Is.Not.SameAs(draft), "A completion summary must own its values independently of further draft edits.");
	}

	private static TStep CreateStep<TStep>(IBlazorWizard<WorkspaceSetupDraft> wizard)
		where TStep : BlazorWizardStep<WorkspaceSetupDraft>, new() {
		var step = new TStep();
		// These workflow tests invoke step validation without a renderer. Browser tests cover parameter binding and rendered navigation.
#pragma warning disable BL0005
		step.Wizard = wizard;
#pragma warning restore BL0005
		return step;
	}

	private static IBlazorWizard<WorkspaceSetupDraft> CreateWizard(WorkspaceSetupDraft draft) => new BlazorWizardBuilder<WorkspaceSetupDraft>()
		.NewWizard("Workspace setup")
		.WithModel(draft)
		.AddStep<WorkspaceDetailsStep>()
		.AddStep<WorkspaceSettingsStep>()
		.AddStep<WorkspaceReviewStep>()
		.Build();
}
