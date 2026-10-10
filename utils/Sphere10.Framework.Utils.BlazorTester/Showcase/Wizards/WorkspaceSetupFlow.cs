// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Wizards;

/// <summary>Composes dynamic branches through the framework's shared wizard engine.</summary>
public static class WorkspaceSetupFlow {
	public static void SetRemainingSettingsSteps(IBlazorWizard<WorkspaceSetupDraft> wizard) {
		Guard.ArgumentNotNull(wizard, nameof(wizard));
		wizard.UpdateSteps(WizardStepUpdateType.ReplaceAllNext, wizard.Model.Mode == "Team"
			? new[] { typeof(WorkspaceReviewersStep), typeof(WorkspaceReviewStep) }
			: new[] { typeof(WorkspaceReviewStep) });
	}

	public static void OpenPresetChooser(IBlazorWizard<WorkspaceSetupDraft> wizard) {
		Guard.ArgumentNotNull(wizard, nameof(wizard));
		wizard.UpdateSteps(WizardStepUpdateType.ReplaceAllNext, new[] { typeof(WorkspacePresetStep) });
		wizard.Next();
	}

	public static void ReturnFromPresetChooser(IBlazorWizard<WorkspaceSetupDraft> wizard) {
		Guard.ArgumentNotNull(wizard, nameof(wizard));
		wizard.Previous();
		SetRemainingSettingsSteps(wizard);
	}

	public static string StepTitle(Type step) {
		Guard.ArgumentNotNull(step, nameof(step));
		return step == typeof(WorkspaceDetailsStep) ? "Details"
			: step == typeof(WorkspaceSettingsStep) ? "Settings"
			: step == typeof(WorkspaceReviewersStep) ? "Reviewers"
			: step == typeof(WorkspacePresetStep) ? "Preset chooser"
			: step == typeof(WorkspaceReviewStep) ? "Review"
			: step.Name;
	}
}
