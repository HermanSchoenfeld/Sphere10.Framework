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
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies the split container's layout, position binding and remembered position.</summary>
[TestFixture]
[NonParallelizable]
public class SplitContainerTests {

	[TestCase(SplitOrientation.Horizontal, "horizontal")]
	[TestCase(SplitOrientation.Vertical, "vertical")]
	public async Task RendersBothPanelsAroundAnAccessibleSplitter(SplitOrientation orientation, string name) {
		var parameters = Parameters(orientation, 30);
		parameters["class"] = "records";
		parameters["style"] = "height: 400px";
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, $"class=\"sphere10-split sphere10-split-{name} records\""), Is.True, "A supplied class is added to the container's own.");
			Assert.That(html, Does.Contain("style=\"height: 400px; --sphere10-split-position: 30; --sphere10-split-panel1-minimum: 60px; --sphere10-split-panel2-minimum: 40px; --sphere10-splitter-width: 5px;\""));
			Assert.That(Regex.IsMatch(html, $"role=\"separator\" tabindex=\"0\" aria-orientation=\"{name}\" aria-label=\"Resize panels\"\\s+aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"30\""), Is.True);
			Assert.That(html.IndexOf("Master", StringComparison.Ordinal), Is.LessThan(html.IndexOf("Detail", StringComparison.Ordinal)));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task MovingTheSplitterClampsReportsAndKeepsThePositionAgainstAnUnchangedBinding() {
		var reported = new List<double>();
		var parameters = Parameters(SplitOrientation.Horizontal, 50);
		parameters[nameof(SplitContainer.SplitterPositionChanged)] = EventCallback.Factory.Create<double>(new object(), position => reported.Add(position));
		await RenderAsync(parameters, async (split, host, rendered) => {
			await split.SetSplitterPositionAsync(72.456);
			Assert.That(split.Position, Is.EqualTo(72.46));
			Assert.That(reported, Is.EqualTo(new[] { 72.46 }));
			await split.SetSplitterPositionAsync(150);
			Assert.That(split.Position, Is.EqualTo(100));
			await host.UpdateAsync(new Dictionary<string, object>(parameters));
			await rendered.QuiescenceTask;
			Assert.That(split.Position, Is.EqualTo(100), "An unchanged supplied value does not undo a move.");
			parameters[nameof(SplitContainer.SplitterPosition)] = 25D;
			await host.UpdateAsync(new Dictionary<string, object>(parameters));
			await rendered.QuiescenceTask;
			Assert.That(split.Position, Is.EqualTo(25));
			Assert.That(rendered.ToHtmlString(), Does.Contain("--sphere10-split-position: 25;"));
		});
	}

	[Test]
	public async Task RemembersThePositionUnderItsSettingsKey() {
		var settings = new MemorySettingsProvider();
		using var scope = UserSettings.UseProvider(settings);
		var parameters = Parameters(SplitOrientation.Vertical, 50);
		parameters[nameof(SplitContainer.SettingsKey)] = "fixture.split";
		await RenderAsync(parameters, async (split, _, _) => {
			await split.SetSplitterPositionAsync(64);
			Assert.That(settings.Saves, Is.EqualTo(1));
		});
		await RenderAsync(parameters, async (split, _, rendered) => {
			Assert.That(split.Position, Is.EqualTo(50), "Storage is read once the component is interactive.");
			await ((IHandleAfterRender)split).OnAfterRenderAsync();
			await rendered.QuiescenceTask;
			Assert.That(split.Position, Is.EqualTo(64));
			Assert.That(rendered.ToHtmlString(), Does.Contain("--sphere10-split-position: 64;"));
		});
	}

	[Test]
	public async Task RejectsAPositionOutsideZeroToOneHundred() =>
		await Assert.ThatAsync(() => RenderAsync(Parameters(SplitOrientation.Horizontal, 120), (_, _, _) => Task.CompletedTask), Throws.ArgumentException);

	private static Dictionary<string, object> Parameters(SplitOrientation orientation, double position) => new() {
		[nameof(SplitContainer.Orientation)] = orientation,
		[nameof(SplitContainer.SplitterPosition)] = position,
		[nameof(SplitContainer.Panel1MinSize)] = 60,
		[nameof(SplitContainer.Panel1)] = (RenderFragment)(builder => builder.AddContent(0, "Master")),
		[nameof(SplitContainer.Panel2)] = (RenderFragment)(builder => builder.AddContent(0, "Detail"))
	};

	private static async Task RenderAsync(Dictionary<string, object> parameters, Func<SplitContainer, ComponentHost, HtmlRootComponent, Task> assertions) {
		await using var provider = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(new GalleryRenderingTests.TestJsRuntime()).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(SplitContainer),
				[nameof(ComponentHost.Parameters)] = parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(value => host = value)
			}));
			await assertions((SplitContainer)host.Component, host, rendered);
		});
	}

	#region Inner Types

	// Keeps saved settings in memory for the duration of one scenario.
	private sealed class MemorySettingsProvider : SyncSettingsProviderBase {
		private readonly Dictionary<(Type, object), SettingsObject> _settings = new();

		// Counts completed saves.
		public int Saves { get; private set; }

		// Keeps this fixture storage readable for its assertions.
		public override bool EncryptSettings => false;

		// Checks whether this fixture has a saved record.
		public override bool ContainsSetting(Type settingsType, object id = null) => _settings.ContainsKey((settingsType, id));

		// Removes a fixture record.
		public override void DeleteSetting(SettingsObject settings) => _settings.Remove((settings.GetType(), settings.ID));

		// Clears this fixture storage.
		public override void ClearSettings() => _settings.Clear();

		// Returns the saved record for the requested key.
		protected override SettingsObject LoadInternal(Type settingsType, object id = null) => _settings[(settingsType, id)];

		// Stores the record for a later read.
		protected override void SaveInternal(SettingsObject settings) {
			_settings[(settings.GetType(), settings.ID)] = settings;
			Saves++;
		}
	}

	#endregion
}
