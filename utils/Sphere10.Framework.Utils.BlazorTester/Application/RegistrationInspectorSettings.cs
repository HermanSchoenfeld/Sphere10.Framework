// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Plain inspector display preferences used by two independent registrations of the same screen.</summary>
[UserEditable]
public class RegistrationInspectorSettings : IValidatableObject {
	[Category("Summary")]
	[DisplayName("Summary heading")]
	[Description("The heading shown inside this inspector; its navigation identity remains unchanged.")]
	public string Caption { get; set; } = "Inspector summary";

	[Category("Summary")]
	[DisplayName("Show summary")]
	public bool ShowSummary { get; set; } = true;

	[Category("Summary")]
	[DisplayName("Detail level")]
	public InspectorDetailLevel DetailLevel { get; set; } = InspectorDetailLevel.Compact;

	[Category("Presentation")]
	[Description("Nested presentation values are provisional until Apply or OK.")]
	public InspectorPresentation Presentation { get; set; } = new();

	[Category("Saved views")]
	[DisplayName("Saved views")]
	[Description("Add, edit or remove named views without changing the retained screen draft.")]
	public InspectorSavedView[] SavedViews { get; set; } = new[] { new InspectorSavedView() };

	/// <summary>Rejects unusable display values before an accepted profile is published.</summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
		if (string.IsNullOrWhiteSpace(Caption) || Caption.Length > 60)
			yield return new ValidationResult("Summary heading must contain between 1 and 60 characters.", new[] { nameof(Caption) });
		if (Presentation == null || Presentation.FontScale is < 80 or > 150)
			yield return new ValidationResult("Font scale must be between 80 and 150 percent.", new[] { nameof(Presentation) });
		if (!Enum.IsDefined(DetailLevel))
			yield return new ValidationResult("Select a supported detail level.", new[] { nameof(DetailLevel) });
		if (SavedViews == null || Array.Exists(SavedViews, view => view == null || string.IsNullOrWhiteSpace(view.Name)))
			yield return new ValidationResult("Every saved view needs a name.", new[] { nameof(SavedViews) });
	}

	/// <summary>Controls how much summary content the demonstration displays.</summary>
	public enum InspectorDetailLevel {
		[Description("Compact summary")]
		Compact,

		[Description("Detailed summary")]
		Detailed
	}

	/// <summary>Nested value preferences, saved only through the inspector's root profile.</summary>
	public class InspectorPresentation {
		[DisplayName("Font scale")]
		[Description("Inspector summary text size as a percentage, from 80 to 150.")]
		public int FontScale { get; set; } = 100;

		[DisplayName("Emphasize heading")]
		public bool EmphasizeHeading { get; set; } = true;
	}

	/// <summary>One editable item in the demonstration's detached collection.</summary>
	public class InspectorSavedView {
		public string Name { get; set; } = "Overview";

		public string Description { get; set; } = "The inspector's default summary.";
	}
}
