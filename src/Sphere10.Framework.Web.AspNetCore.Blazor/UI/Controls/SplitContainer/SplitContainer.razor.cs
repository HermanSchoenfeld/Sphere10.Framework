// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

/// <summary>Shows two panels separated by a draggable splitter, such as a master grid above or beside its detail panel.</summary>
/// <remarks>
/// <see cref="SplitterPosition"/> is the first panel's share in percent and can be bound two-way. Dragging the splitter, or the arrow,
/// Home and End keys while it has focus, moves it within the panels' minimum sizes. With a <see cref="SettingsKey"/> the position is
/// remembered through <c>UserSettings</c> as a <see cref="SplitContainerSettings"/>. Give the container a definite height, for example
/// <c>style="height: 100%"</c> inside a sized parent or <c>style="height: 70vh"</c>.
/// </remarks>
public partial class SplitContainer : ComponentBase, IAsyncDisposable {
	private ElementReference _element;
	private IJSObjectReference _module;
	private IJSObjectReference _behavior;
	private DotNetObjectReference<SplitContainer> _reference;
	private Task _initializationTask;
	private Task _disposalTask;
	private double? _suppliedPosition;
	private bool _settingsRead;
	private bool _disposed;

	[Inject] private IJSRuntime JSRuntime { get; set; }

	/// <summary>The splitter's direction. Horizontal, the default, places the first panel above the second.</summary>
	[Parameter] public SplitOrientation Orientation { get; set; } = SplitOrientation.Horizontal;

	/// <summary>The first panel's share of the container in percent, from 0 to 100. Defaults to 50.</summary>
	[Parameter] public double SplitterPosition { get; set; } = 50;

	/// <summary>Reports a position chosen by dragging or the keyboard, for <c>@bind-SplitterPosition</c>.</summary>
	[Parameter] public EventCallback<double> SplitterPositionChanged { get; set; }

	/// <summary>The first panel's minimum size in CSS pixels. Defaults to 40.</summary>
	[Parameter] public int Panel1MinSize { get; set; } = 40;

	/// <summary>The second panel's minimum size in CSS pixels. Defaults to 40.</summary>
	[Parameter] public int Panel2MinSize { get; set; } = 40;

	/// <summary>The splitter's thickness in CSS pixels. Defaults to 5.</summary>
	[Parameter] public int SplitterWidth { get; set; } = 5;

	/// <summary>The first panel, above or to the left of the splitter.</summary>
	[Parameter] public RenderFragment Panel1 { get; set; }

	/// <summary>The second panel, below or to the right of the splitter.</summary>
	[Parameter] public RenderFragment Panel2 { get; set; }

	/// <summary>The splitter's accessible name.</summary>
	[Parameter] public string SplitterLabel { get; set; } = "Resize panels";

	/// <summary>An application-qualified key that remembers the position for this browser's user. Null keeps the position for the component's lifetime only.</summary>
	[Parameter] public string SettingsKey { get; set; }

	/// <summary>Attributes for the container element; a class is added to the container's own classes and a style precedes its layout variables.</summary>
	[Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> AdditionalAttributes { get; set; }

	/// <summary>The displayed position of the first panel in percent.</summary>
	public double Position { get; private set; } = 50;

	private string OrientationName => Orientation == SplitOrientation.Horizontal ? "horizontal" : "vertical";

	private string ContainerClass => $"sphere10-split sphere10-split-{OrientationName}"
		+ (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out var extra) && extra != null ? " " + extra : null);

	private string ContainerStyle {
		get {
			var style = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("style", out var extra) && extra != null ? extra.ToString().TrimEnd() : null;
			if (!string.IsNullOrEmpty(style) && !style.EndsWith(';'))
				style += ";";
			return FormattableString.Invariant($"{style} --sphere10-split-position: {Position}; --sphere10-split-panel1-minimum: {Panel1MinSize}px; --sphere10-split-panel2-minimum: {Panel2MinSize}px; --sphere10-splitter-width: {SplitterWidth}px;").TrimStart();
		}
	}

	/// <summary>Moves the splitter, reports the change and remembers it when a settings key is set.</summary>
	[JSInvokable]
	public async Task SetSplitterPositionAsync(double position) {
		if (_disposed || double.IsNaN(position))
			return;
		position = Math.Round(Math.Clamp(position, 0, 100), 2);
		if (position == Position)
			return;
		Position = position;
		StateHasChanged();
		await SplitterPositionChanged.InvokeAsync(position);
		await SaveSettingsAsync(position);
	}

	/// <summary>Releases the browser behavior after any pending initialization.</summary>
	public ValueTask DisposeAsync() => new(_disposalTask ??= DisposeCoreAsync());

	/// <summary>Validates the parameters and applies a changed supplied position.</summary>
	protected override void OnParametersSet() {
		Guard.Argument(Enum.IsDefined(Orientation), nameof(Orientation), "Select a supported orientation.");
		Guard.Argument(SplitterPosition is >= 0 and <= 100, nameof(SplitterPosition), "The splitter position is a percentage from 0 to 100.");
		Guard.Argument(Panel1MinSize >= 0, nameof(Panel1MinSize), "A minimum size cannot be negative.");
		Guard.Argument(Panel2MinSize >= 0, nameof(Panel2MinSize), "A minimum size cannot be negative.");
		Guard.ArgumentGT(SplitterWidth, 0, nameof(SplitterWidth));

		// A supplied value applies when it changes, so its echo through a two-way binding does not undo a drag or a remembered position.
		if (_suppliedPosition != SplitterPosition) {
			_suppliedPosition = SplitterPosition;
			Position = SplitterPosition;
		}
	}

	/// <summary>Attaches the splitter behavior and restores a remembered position once interactive storage is available.</summary>
	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (_disposed)
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			_initializationTask ??= InitializeJavascriptAsync();
			await _initializationTask;
			if (!_settingsRead && !_disposed) {
				_settingsRead = true;
				await RestoreSettingsAsync();
			}
		}, exception => exception is JSDisconnectedException || exception is OperationCanceledException && _disposed);
		if (error is JSDisconnectedException)
			_initializationTask = null;
	}

	// Reads the remembered position; a missing or unreadable record keeps the supplied one.
	private async Task RestoreSettingsAsync() {
		var key = SettingsKey;
		if (string.IsNullOrWhiteSpace(key))
			return;
		SplitContainerSettings settings = null;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			if (await UserSettings.HasAsync<SplitContainerSettings>(key))
				settings = await UserSettings.GetAsync<SplitContainerSettings>(key);
		}, exception => exception is JSException or InvalidOperationException or Newtonsoft.Json.JsonException);
		if (error != null)
			SystemLog.Exception(error);
		if (settings == null || _disposed || key != SettingsKey || double.IsNaN(settings.SplitterPosition))
			return;
		Position = Math.Clamp(settings.SplitterPosition, 0, 100);
		StateHasChanged();
	}

	// Saves a completed move through the existing user settings provider.
	private async Task SaveSettingsAsync(double position) {
		var key = SettingsKey;
		if (string.IsNullOrWhiteSpace(key) || _disposed)
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			var settings = await UserSettings.GetAsync<SplitContainerSettings>(key);
			settings.SplitterPosition = position;
			await settings.SaveAsync();
		}, exception => exception is JSException or InvalidOperationException or Newtonsoft.Json.JsonException);
		if (error != null)
			SystemLog.Exception(error);
	}

	// Imports the module and attaches the pointer and keyboard behavior to the rendered splitter.
	private async Task InitializeJavascriptAsync() {
		_module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import",
			"./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/SplitContainer/SplitContainer.razor.js");
		if (_disposed || _module == null)
			return;
		_reference = DotNetObjectReference.Create(this);
		_behavior = await _module.InvokeAsync<IJSObjectReference>("Initialize", _element, _reference);
	}

	// Waits for initialization, then releases the behavior, the module and the callback reference.
	private async Task DisposeCoreAsync() {
		_disposed = true;
		using (Tools.Scope.ExecuteOnDispose(() => _reference?.Dispose())) {
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				if (_initializationTask != null)
					await _initializationTask;
			}, error => error is OperationCanceledException or JSDisconnectedException);
			foreach (var reference in new[] { _behavior, _module }) {
				if (reference == null)
					continue;
				await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
					if (ReferenceEquals(reference, _behavior))
						await reference.InvokeVoidAsync("Dispose");
					await reference.DisposeAsync();
				}, error => error is OperationCanceledException or JSDisconnectedException);
			}
		}
	}
}
