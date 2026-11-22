// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies the field layouts: labelled controls, labelled read-only values and the stacked and inline grid containers.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FieldGridTests {

	[Test]
	public async Task RowsPairALabelWithItsControlOrAReadOnlyValue() {
		var html = await RenderAsync(builder => {
			builder.OpenComponent<FieldGrid>(0);
			builder.AddAttribute(1, nameof(FieldGrid.Label), "Product");
			builder.AddAttribute(2, nameof(FieldGrid.ChildContent), (RenderFragment)(rows => {
				rows.OpenComponent<FieldRow>(0);
				rows.AddAttribute(1, nameof(FieldRow.Label), "Name");
				rows.AddAttribute(2, nameof(FieldRow.For), "product-name");
				rows.AddAttribute(3, nameof(FieldRow.ChildContent), (RenderFragment)(control => {
					control.OpenElement(0, "input");
					control.AddAttribute(1, "id", "product-name");
					control.CloseElement();
				}));
				rows.CloseComponent();
				rows.OpenComponent<FieldRow>(4);
				rows.AddAttribute(5, nameof(FieldRow.Label), "License Count");
				rows.AddAttribute(6, nameof(FieldRow.Value), "412");
				rows.CloseComponent();
				rows.OpenComponent<FieldRow>(7);
				rows.AddAttribute(8, nameof(FieldRow.Label), "Notes");
				rows.AddAttribute(9, nameof(FieldRow.Wide), true);
				rows.AddAttribute(10, nameof(FieldRow.Value), "Long text");
				rows.CloseComponent();
			}));
			builder.CloseComponent();
		});
		Assert.That(Regex.IsMatch(html, "<div\\b[^>]*class=\"sphere10-field-grid sphere10-field-grid-stacked sphere10-field-grid-compact\" style=\"--sphere10-field-columns: 2\" role=\"group\" aria-label=\"Product\"[^>]*>"), Is.True,
			"Stacked, two columns and compact are the defaults.");
		Assert.That(Regex.IsMatch(html, "<div\\b[^>]*class=\"sphere10-field\"[^>]*>\\s*<label\\b[^>]*class=\"sphere10-field-label\" for=\"product-name\"[^>]*>Name</label>\\s*<div\\b[^>]*class=\"sphere10-field-value\"[^>]*>\\s*<input id=\"product-name\""), Is.True,
			"A control row is one field element holding its label, then the supplied control.");
		var readOnly = Regex.Match(html, "<label\\b[^>]*for=\"(?<id>sphere10-field-[0-9a-f]{32})\"[^>]*>License Count</label>\\s*<div\\b[^>]*class=\"sphere10-field-value\"[^>]*>\\s*<output\\b[^>]*id=\"(?<output>[^\"]+)\" class=\"sphere10-field-readonly\"[^>]*>412</output>");
		Assert.That(readOnly.Success, Is.True);
		Assert.That(readOnly.Groups["output"].Value, Is.EqualTo(readOnly.Groups["id"].Value), "A read-only value is labelled by its row.");
		Assert.That(html, Does.Contain("class=\"sphere10-field sphere10-field-wide\""));
	}

	[Test]
	public async Task AGridWithoutALabelHasNoGroupRoleAndCompactCanBeTurnedOff() {
		var html = await RenderAsync(builder => {
			builder.OpenComponent<FieldGrid>(0);
			builder.AddAttribute(1, nameof(FieldGrid.Class), "detail");
			builder.AddAttribute(2, nameof(FieldGrid.Compact), false);
			builder.AddAttribute(3, nameof(FieldGrid.Columns), 3);
			builder.CloseComponent();
		});
		Assert.That(Regex.IsMatch(html, "<div\\b[^>]*class=\"sphere10-field-grid sphere10-field-grid-stacked detail\" style=\"--sphere10-field-columns: 3\"[^>]*></div>"), Is.True, "Compact is the default and can be turned off.");
		Assert.That(html, Does.Not.Contain("role=\"group\""));
	}

	[Test]
	public async Task InlineLayoutPutsLabelsBesideTheirControls() {
		var html = await RenderAsync(builder => {
			builder.OpenComponent<FieldGrid>(0);
			builder.AddAttribute(1, nameof(FieldGrid.Layout), FieldLayout.Inline);
			builder.CloseComponent();
		});
		Assert.That(Regex.IsMatch(html, "<div\\b[^>]*class=\"sphere10-field-grid sphere10-field-grid-inline sphere10-field-grid-compact\"[^>]*></div>"), Is.True);
		Assert.That(html, Does.Not.Contain("--sphere10-field-columns"), "Columns apply to the stacked layout only.");
	}

	private static async Task<string> RenderAsync(RenderFragment content) {
		await using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		return await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<FragmentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(FragmentHost.Content)] = content
			}));
			return rendered.ToHtmlString();
		});
	}

	#region Inner Types

	/// <summary>Renders a supplied fragment as the test's root component.</summary>
	public sealed class FragmentHost : ComponentBase {

		/// <summary>The fragment to render.</summary>
		[Parameter] public RenderFragment Content { get; set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Content);
	}

	#endregion
}
