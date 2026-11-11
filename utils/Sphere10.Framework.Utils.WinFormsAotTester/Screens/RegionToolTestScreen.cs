// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Linq;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Explores installed culture and region information without changing the machine locale.</summary>
public partial class RegionToolTestScreen : ApplicationScreen {
	/// <summary>Creates the table and filter without querying an external service.</summary>
	public RegionToolTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Populates the runtime culture catalog on first display.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode)
			ShowCultures();
	}

	// Refresh and filtering share one deterministic catalog projection.
	private void Button1_Click(object sender, EventArgs args) => ShowCultures();

	// Local filtering neither persists input nor changes a system preference.
	private void FilterChanged(object sender, EventArgs args) => ShowCultures();

	// RegionInfo supports specific cultures; neutral languages intentionally have no region.
	private void ShowCultures() {
		var query = _filter.Text.Trim();
		using (_cultureGrid.EnterRenderingScope()) {
			_cultureGrid.Rows.Clear();
			foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).Where(culture => culture.Name.Length > 0).OrderBy(culture => culture.EnglishName)) {
				var regionName = "Neutral culture";
				var regionCode = string.Empty;
				if (!culture.IsNeutralCulture) {
					try {
						var region = new RegionInfo(culture.Name);
						regionName = region.EnglishName;
						regionCode = region.TwoLetterISORegionName + " / " + region.ThreeLetterISORegionName;
					} catch (ArgumentException) {
						regionName = "No region information";
					}
				}
				var text = culture.EnglishName + " " + culture.Name + " " + regionName + " " + regionCode;
				if (query.Length == 0 || text.Contains(query, StringComparison.OrdinalIgnoreCase))
					_cultureGrid.Rows.Add(culture.EnglishName, culture.Name, regionName, regionCode);
			}
		}
		_summary.Text = $"{_cultureGrid.Rows.Count:N0} cultures shown · Region information is unavailable for neutral cultures.";
	}
}
