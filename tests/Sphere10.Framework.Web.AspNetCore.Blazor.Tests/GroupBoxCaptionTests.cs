// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>
/// Verifies the group box caption pieces (owner, 22 November 2026: group boxes carry their commands as icons; states are icons in a
/// fixed place): a screen command's caption icon shares the toolbar item's state, tooltip and action, the caption lays out its heading
/// and icons, and a state icon keeps its slot.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GroupBoxCaptionTests {
	private const string ButtonPattern = "<button\\b[^>]*class=\"sphere10-command-icon\"[^>]*>";

	[Test]
	public async Task AnEnabledCommandShowsItsIconTitleAndName() {
		var screen = new CaptionScreen();
		await RenderIconAsync(screen.Edit, (_, rendered) => {
			var html = rendered.ToHtmlString();
			var button = Regex.Match(html, ButtonPattern).Value;
			Assert.That(button, Does.Contain("title=\"Change License Template\"").And.Contain("aria-label=\"Change License Template\"")
				.And.Contain("data-command-id=\"template-change\"").And.Not.Contain("disabled").And.Not.Contain("aria-description"));
			Assert.That(Regex.IsMatch(html, ButtonPattern + "\\s*<span\\b[^>]*class=\"fas fa-pen\" aria-hidden=\"true\""), Is.True);
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task ABlockedCommandIsDisabledAndItsTooltipNamesTheBlockerAsTheToolbarDoes() {
		var screen = new CaptionScreen { Locked = true };
		await RenderIconAsync(screen.Edit, (_, rendered) => {
			var button = Regex.Match(rendered.ToHtmlString(), ButtonPattern).Value;
			var toolbar = screen.ToolBarItems.Single(item => item.Id == "template-change");
			Assert.That(button, Does.Contain($"title=\"{toolbar.Title}\"").And.Contain("title=\"Change License Template (Product: Locked)\""));
			Assert.That(button, Does.Contain("aria-label=\"Change License Template\"").And.Contain("aria-description=\"Product: Locked\""),
				"The accessible name is the command's text; the blocker is its description.");
			Assert.That(button, Does.Contain("disabled"));
			Assert.That(toolbar.IsEnabled, Is.False);
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task TheIconIsDisabledExactlyWhenTheToolbarItemIs() {
		foreach (var (busy, dirty) in new[] { (false, false), (false, true), (true, true) }) {
			var screen = new CaptionScreen { Busy = busy, Dirty = dirty };
			await RenderIconAsync(screen.Save, (_, rendered) => {
				var button = Regex.Match(rendered.ToHtmlString(), ButtonPattern).Value;
				var toolbar = screen.ToolBarItems.Single(item => item.Id == "template-save");
				Assert.That(button.Contains("disabled"), Is.EqualTo(!toolbar.IsEnabled), $"busy {busy}, dirty {dirty}");
				Assert.That(screen.Save.IsEnabled, Is.EqualTo(toolbar.IsEnabled));
				return Task.CompletedTask;
			});
		}
	}

	[Test]
	public async Task AHiddenCommandRendersNothing() {
		var screen = new CaptionScreen { CanDelete = false };
		await RenderIconAsync(screen.Delete, (_, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("<button"));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task ClickingRunsTheCommandThroughTheScreensHookOnlyWhileEnabled() {
		var screen = new CaptionScreen();
		await RenderIconAsync(screen.Edit, async (icon, _) => {
			await Click(icon);
			Assert.That(screen.Log, Is.EqualTo(new[] { "hook:begin", "change", "hook:end" }));
			screen.Locked = true;
			await Click(icon);
			Assert.That(await screen.Edit.RunAsync(), Is.False);
			Assert.That(screen.Log, Has.Count.EqualTo(3), "A disabled command never runs.");
		});
	}

	[Test]
	public async Task TheIconRefreshesWhenTheScreenNotifiesAChange() {
		var screen = new CaptionScreen();
		await RenderIconAsync(screen.Edit, async (_, rendered) => {
			Assert.That(Regex.Match(rendered.ToHtmlString(), ButtonPattern).Value, Does.Not.Contain("disabled"));
			screen.Locked = true;
			screen.Changed();
			await rendered.QuiescenceTask;
			Assert.That(Regex.Match(rendered.ToHtmlString(), ButtonPattern).Value, Does.Contain("disabled").And.Contain("(Product: Locked)"));
		});
	}

	[Test]
	public async Task TheCaptionHoldsItsHeadingStateIconsAndCommandIcons() {
		var screen = new CaptionScreen { Locked = true };
		var html = await RenderAsync(builder => {
			builder.OpenComponent<GroupBoxCaption>(0);
			builder.AddAttribute(1, nameof(GroupBoxCaption.Text), "License Template");
			builder.AddAttribute(2, nameof(GroupBoxCaption.Commands), new[] { screen.Edit, screen.Save });
			builder.AddAttribute(3, nameof(GroupBoxCaption.ChildContent), (RenderFragment)(state => {
				state.OpenComponent<StateIcon>(0);
				state.AddAttribute(1, nameof(StateIcon.Icon), "fas fa-lock");
				state.AddAttribute(2, nameof(StateIcon.Text), "Locked");
				state.AddAttribute(3, nameof(StateIcon.Shown), true);
				state.CloseComponent();
			}));
			builder.CloseComponent();
		});
		Assert.That(Regex.IsMatch(html, "^<div\\b[^>]*class=\"sphere10-group-caption\"[^>]*>\\s*<h2\\b[^>]*class=\"sphere10-group-caption-text\"[^>]*>License Template</h2>\\s*<div\\b[^>]*class=\"sphere10-group-caption-icons\"[^>]*>"), Is.True,
			"The heading comes first, then the icons, in one caption element.");
		var icons = Regex.Match(html, "class=\"sphere10-group-caption-icons\"[^>]*>(?<icons>.*)</div>\\s*$", RegexOptions.Singleline).Groups["icons"].Value;
		Assert.That(Regex.Matches(icons, "data-command-id=\"(?<id>[^\"]+)\"").Select(match => match.Groups["id"].Value), Is.EqualTo(new[] { "template-change", "template-save" }));
		Assert.That(icons.IndexOf("aria-label=\"Locked\"", StringComparison.Ordinal), Is.LessThan(icons.IndexOf("data-command-id", StringComparison.Ordinal)), "State icons come before the commands.");
	}

	[Test]
	public async Task ACaptionWithoutIconsIsItsHeadingAtTheChosenLevel() {
		var html = await RenderAsync(builder => {
			builder.OpenComponent<GroupBoxCaption>(0);
			builder.AddAttribute(1, nameof(GroupBoxCaption.Text), "Terms");
			builder.AddAttribute(2, nameof(GroupBoxCaption.Level), 3);
			builder.AddAttribute(3, nameof(GroupBoxCaption.Class), "terms-caption");
			builder.CloseComponent();
		});
		Assert.That(Regex.IsMatch(html, "^<div\\b[^>]*class=\"sphere10-group-caption terms-caption\"[^>]*>\\s*<h3\\b[^>]*class=\"sphere10-group-caption-text\"[^>]*>Terms</h3>\\s*</div>$"), Is.True);
	}

	[Test]
	public void ACaptionRejectsAnInvalidLevel() {
		Assert.That(async () => await RenderAsync(builder => {
			builder.OpenComponent<GroupBoxCaption>(0);
			builder.AddAttribute(1, nameof(GroupBoxCaption.Text), "Terms");
			builder.AddAttribute(2, nameof(GroupBoxCaption.Level), 7);
			builder.CloseComponent();
		}), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public async Task AStateIconKeepsItsSlotWhetherShownOrNot() {
		var shown = await RenderStateIconAsync(true);
		var hidden = await RenderStateIconAsync(false);
		Assert.That(Regex.IsMatch(shown, "^<span\\b[^>]*class=\"sphere10-state-icon\" role=\"img\" title=\"Locked\" aria-label=\"Locked\"[^>]*>\\s*<span\\b[^>]*class=\"fas fa-lock\" aria-hidden=\"true\""), Is.True);
		Assert.That(Regex.IsMatch(hidden, "^<span\\b[^>]*class=\"sphere10-state-icon\" aria-hidden=\"true\" data-state-shown=\"false\"[^>]*></span>$"), Is.True,
			"The empty slot has the same class, and so the same size, but nothing to announce.");
	}

	// Renders one StateIcon for the Locked state.
	private static Task<string> RenderStateIconAsync(bool shown) => RenderAsync(builder => {
		builder.OpenComponent<StateIcon>(0);
		builder.AddAttribute(1, nameof(StateIcon.Icon), "fas fa-lock");
		builder.AddAttribute(2, nameof(StateIcon.Text), "Locked");
		builder.AddAttribute(3, nameof(StateIcon.Shown), shown);
		builder.CloseComponent();
	});

	// Runs the icon's click handler on the renderer, as the browser's click would.
	private static async Task Click(ScreenCommandIcon icon) {
		var handler = typeof(ScreenCommandIcon).GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic);
		await (Task)handler.Invoke(icon, null);
	}

	// Renders one ScreenCommandIcon for the command in a host that captures the component.
	private static async Task RenderIconAsync(ScreenCommand command, Func<ScreenCommandIcon, HtmlRootComponent, Task> assertions) {
		await using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(ScreenCommandIcon),
				[nameof(ComponentHost.Parameters)] = new Dictionary<string, object> { [nameof(ScreenCommandIcon.Command)] = command },
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(value => host = value)
			}));
			await rendered.QuiescenceTask;
			await assertions((ScreenCommandIcon)host.Component, rendered);
		});
	}

	// Renders a fragment and returns its markup.
	private static async Task<string> RenderAsync(RenderFragment fragment) {
		await using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		return await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<FragmentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(FragmentHost.Content)] = fragment
			}));
			await rendered.QuiescenceTask;
			return rendered.ToHtmlString().Trim();
		});
	}

	#region Inner Types

	/// <summary>A License Template screen whose caption commands mirror its toolbar.</summary>
	private sealed class CaptionScreen : ApplicationScreen {
		public CaptionScreen() {
			Edit = Command("template-change", "Change License Template", "fas fa-pen", () => Record("change")).BlockedBy(() => Locked ? "Product: Locked" : string.Empty);
			Save = Command("template-save", "Save License Template", "fas fa-save", () => Record("save")).EnabledWhen(() => Dirty);
			Delete = Command("template-delete", "Delete License Template", "fas fa-trash-alt", () => Record("delete")).VisibleWhen(() => CanDelete);
			UseCommands(new ScreenCommandLayout {
				MenuId = "template",
				MenuText = () => "License Template",
				Menu = [[Edit, Save], [Delete]],
				ToolBar = [[Edit, Save], [Delete]]
			});
		}

		public ScreenCommand Edit { get; }

		public ScreenCommand Save { get; }

		public ScreenCommand Delete { get; }

		public List<string> Log { get; } = new();

		public bool Busy { get; set; }

		public bool Dirty { get; set; }

		public bool Locked { get; set; }

		public bool CanDelete { get; set; } = true;

		protected override bool IsBusy => Busy;

		// Reports a change made without a render, as a screen does after a background update.
		public void Changed() => NotifyScreenChanged();

		protected override async Task RunCommandAsync(Func<Task> action) {
			Log.Add("hook:begin");
			await action();
			Log.Add("hook:end");
		}

		private Task Record(string entry) {
			Log.Add(entry);
			return Task.CompletedTask;
		}
	}

	/// <summary>Renders a supplied fragment as its whole content.</summary>
	private sealed class FragmentHost : ComponentBase {
		[Parameter] public RenderFragment Content { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Content);
	}

	#endregion
}
