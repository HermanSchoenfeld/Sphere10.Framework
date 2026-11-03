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
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Retains the lifecycle demonstration and optionally hosts typed inspector preferences and grids.</summary>
[Settings(typeof(ScreenHostingDesignTestScreen.ScreenHostingPreferences))]
public partial class ScreenHostingDesignTestScreen : ScreenHostingTestScreen {
	/// <summary>Creates the ordinary Design registration without declared preference behavior.</summary>
	public ScreenHostingDesignTestScreen()
		: this("Design") {
		if (Tools.Runtime.IsDesignMode)
			InitializePreferencesDemo();
	}

	/// <summary>Lets independent registrations supply their own construction-time workspace profile.</summary>
	public ScreenHostingDesignTestScreen(string profile)
		: base(profile, ScreenActivationMode.MultiInstance) {
	}

	/// <summary>Adds the typed preference demonstration while preserving the registration's original constructor argument.</summary>
	public ScreenHostingDesignTestScreen(string profile, bool includePreferences)
		: this(profile) {
		if (includePreferences)
			InitializePreferencesDemo();
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ScreenHostingPreferences ScreenSettings {
		get {
			return (ScreenHostingPreferences)base.ScreenSettings;
		}
	}

	#region Inner Types

	/// <summary>Plain inspector preferences declared independently for the CPU and GPU registrations.</summary>
	[UserEditable]
	public class ScreenHostingPreferences : SettingsObject, IValidatableObject {
		/// <summary>The heading shown in this registration's preference demonstration.</summary>
		[DefaultValue("Inspector summary")]
		[Category("Summary")]
		public string Heading { get; set; } = "Inspector summary";

		/// <summary>Whether the inspector displays its summary beneath the heading.</summary>
		[DefaultValue(true)]
		[Category("Summary")]
		[DisplayName("Show summary")]
		public bool ShowSummary { get; set; } = true;

		/// <summary>The amount of saved-view and grid detail in the live summary.</summary>
		[DefaultValue(DetailLevel.Compact)]
		[Category("Summary")]
		[DisplayName("Detail level")]
		public DetailLevel Details { get; set; } = DetailLevel.Compact;

		/// <summary>Nested presentation values, separate from the operational notes.</summary>
		[Category("Presentation")]
		public Presentation Appearance { get; set; } = new();

		/// <summary>The named views shown by the inspector's summary.</summary>
		[Category("Saved views")]
		[DisplayName("Saved views")]
		public SavedView[] SavedViews { get; set; } = new[] { new SavedView() };

		/// <summary>The primary table's independent preference role.</summary>
		[Category("Grids")]
		[DisplayName("Records grid")]
		public VisualGridSettings Grid { get; set; }

		/// <summary>The second table's independent preference role.</summary>
		[Category("Grids")]
		[DisplayName("Details grid")]
		public VisualGridSettings DetailsGrid { get; set; }

		/// <summary>The optional output's plain remembered preferences.</summary>
		[Category("Logging")]
		[DisplayName("Log output")]
		public LogOutputSelectorPreferences LogOutput { get; set; } = new();

		/// <summary>Restores scalar attributes and this inspector's explicit nested presentation defaults.</summary>
		public override void RestoreDefaultValues() {
			base.RestoreDefaultValues();
			if (Appearance != null)
				Tools.Object.SetDefaultValues(Appearance);
			SavedViews = new[] { new SavedView() };
			Grid = CreateGrid(5);
			DetailsGrid = CreateGrid(3);
			LogOutput?.RestoreDefaultValues();
		}

		/// <summary>Rejects unusable options while preserving the reusable adapters' portable value constraints.</summary>
		public override Result Validate() {
			var result = new Result();
			if (string.IsNullOrWhiteSpace(Heading) || Heading.Length > 60)
				result.AddError("Inspector heading must contain between 1 and 60 characters.");
			if (!Enum.IsDefined(Details))
				result.AddError("Select a supported inspector detail level.");
			if (Appearance == null || string.IsNullOrWhiteSpace(Appearance.Prefix))
				result.AddError("The summary presentation requires a prefix.");
			if (SavedViews == null || SavedViews.Any(view => view == null || string.IsNullOrWhiteSpace(view.Name)))
				result.AddError("Every saved view requires a name.");
			if (Grid == null || DetailsGrid == null || LogOutput == null)
				result.AddError("Both grid roles and logging require preferences.");
			else {
				result.Merge(Grid.Validate());
				result.Merge(DetailsGrid.Validate());
				result.Merge(LogOutput.Validate());
			}
			return result;
		}

		IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext context) {
			foreach (var error in Validate().ErrorMessages)
				yield return new ValidationResult(error);
		}

		// Column schema is available to unopened Options pages and supplies the same Reset defaults as first use.
		private static VisualGridSettings CreateGrid(int pageSize) {
			return new VisualGridSettings {
				PageMode = VisualGridPageMode.Paged,
				PageSize = pageSize,
				SortDirection = SortDirection.None,
				Columns = new[] {
					new ApplicationGridColumnPreferences { Key = "Id", Title = "ID" },
					new ApplicationGridColumnPreferences { Key = "Name", Title = "Name" },
					new ApplicationGridColumnPreferences { Key = "Summary", Title = "Summary" }
				}
			};
		}

		#region Inner Types

		/// <summary>Controls the amount of live summary detail without changing screen identity or drafts.</summary>
		public enum DetailLevel {
			[Description("Compact summary")]
			Compact,

			[Description("Detailed summary")]
			Detailed
		}

		/// <summary>Nested presentation values, edited provisionally by the shared Options editor.</summary>
		public class Presentation {
			/// <summary>The prefix used in the live summary.</summary>
			[DefaultValue("Current view")]
			public string Prefix { get; set; } = "Current view";

			/// <summary>Whether the heading uses the demonstration highlight color.</summary>
			[DefaultValue(true)]
			[DisplayName("Highlight heading")]
			public bool HighlightHeading { get; set; } = true;
		}

		/// <summary>One editable saved view in the declared collection.</summary>
		public class SavedView {
			/// <summary>The readable saved-view name.</summary>
			[DefaultValue("Overview")]
			public string Name { get; set; } = "Overview";

			/// <summary>A plain description editable in the nested collection.</summary>
			[DefaultValue("The inspector's initial view.")]
			public string Description { get; set; } = "The inspector's initial view.";
		}
		#endregion
	}
	#endregion
}
