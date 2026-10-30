// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Adds the shared Log Output workflow and bounded asynchronous observation logging to a chart.</summary>
public sealed class ChartLogOutputBehavior : IDisposable, IAsyncDisposable {
	private readonly Control _surface;
	private readonly int _ownerThreadId;
	private readonly Func<ChartLogSample[]> _samples;
	private readonly Func<string> _titleProvider;
	private readonly ContextMenuStrip _menu;
	private readonly ToolStripMenuItem _logOutputCommand;
	private readonly List<(Control Control, ContextMenuStrip Previous)> _surfaces = new();
	private readonly Dictionary<ContextMenuStrip, ToolStripMenuItem> _extendedMenus = new();
	private readonly Dictionary<(string Series, string Identity), string> _published = new();
	private LogOutputSelectorControl _ownedSelector;
	private LogOutputSelectorControl _outputSelector;
	private Task _retiredOutput = Task.CompletedTask;
	private Task _completion = Task.CompletedTask;
	private Exception _lastError;
	private long _droppedSamples;
	private bool _captureQueued;
	private bool _configuring;
	private bool _disposed;
	private bool _captureEnabled = true;
	private ContextMenuStrip _sharedMenu;
	private WinFormsApplicationScreen _ownerScreen;
	private string _logTitle = string.Empty;
	private string _ownedScreenTitle;

	/// <summary>Attaches to a native chart without changing the chart engine or taking ownership of it.</summary>
	public ChartLogOutputBehavior(Chart chart)
		: this(chart, () => ReadNativeSamples(chart)) {
		chart.Invalidated += ChartInvalidated;
	}

	/// <summary>Uses application-supplied stable observation identities, captured only on the owning UI thread.</summary>
	public ChartLogOutputBehavior(Control surface, Func<ChartLogSample[]> samples, Func<string> titleProvider = null) {
		Guard.ArgumentNotNull(surface, nameof(surface));
		Guard.ArgumentNotNull(samples, nameof(samples));
		_surface = surface;
		_ownerThreadId = Environment.CurrentManagedThreadId;
		_samples = samples;
		_titleProvider = titleProvider;
		_logOutputCommand = new ToolStripMenuItem("Log Output");
		_logOutputCommand.Click += LogOutputClicked;
		_menu = new ContextMenuStrip();
		_menu.Items.Add(_logOutputCommand);
		_menu.Opening += MenuOpening;
		_sharedMenu = surface.ContextMenuStrip ?? _menu;
		AttachSurface(surface);
		surface.Disposed += SurfaceDisposed;
		surface.HandleDestroyed += SurfaceHandleDestroyed;
	}

	/// <summary>Borrow a shared selector, or assign null to return to the behavior's own output. The caller owns a borrowed selector.</summary>
	public LogOutputSelectorControl OutputSelector {
		get {
			EnsureOwnerThread();
			ObjectDisposedException.ThrowIf(_disposed, this);
			if (_outputSelector != null)
				return _outputSelector;
			if (_ownedSelector == null) {
				_ownedSelector = new LogOutputSelectorControl { Name = "ChartLogOutput", Configuration = new LogOutputConfiguration { TimestampEnabled = false } };
				_ownedScreenTitle = _ownedSelector.ScreenTitle;
				_ownedSelector.LoggingStarting += OwnedLoggingStarting;
				UpdateOwnedScreenTitle();
			}
			SetSelector(_ownedSelector);
			return _outputSelector;
		}
		set {
			EnsureOwnerThread();
			ObjectDisposedException.ThrowIf(_disposed, this);
			SetSelector(value);
		}
	}

	/// <summary>Disable when the shared output is fed by a caller-owned raw sampler; the context menu remains available.</summary>
	public bool CaptureEnabled {
		get => _captureEnabled;
		set {
			EnsureOwnerThread();
			_captureEnabled = value;
			if (!value) {
				_published.Clear();
				_published.TrimExcess();
			}
		}
	}

	/// <summary>Optional descriptive chart title. Empty uses the title provider or native chart/owning screen context.</summary>
	public string LogTitle {
		get => _logTitle;
		set { EnsureOwnerThread(); _logTitle = value ?? string.Empty; }
	}

	/// <summary>The current retained snapshot's logged identities; clearing or disposal releases this metadata.</summary>
	public int RetainedSampleCount => _published.Count;

	/// <summary>Number of observations rejected while the bounded output queue was full.</summary>
	public long DroppedSamples => Interlocked.Read(ref _droppedSamples);

	public Exception LastError => Volatile.Read(ref _lastError) ?? _outputSelector?.LastError;

	/// <summary>Completes after disposal drains accepted writes and releases the owned selector. Await before ending the owning UI loop.</summary>
	public Task Completion => _completion;

	/// <summary>Applies the same menu to another surface such as an empty state or telemetry heading.</summary>
	public void AttachSurface(Control surface) {
		EnsureOwnerThread();
		Guard.ArgumentNotNull(surface, nameof(surface));
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_surfaces.Any(item => ReferenceEquals(item.Control, surface)))
			return;
		_surfaces.Add((surface, surface.ContextMenuStrip));
		surface.ContextMenuStripChanged += SurfaceMenuChanged;
		if (surface.ContextMenuStrip == null)
			surface.ContextMenuStrip = _sharedMenu;
		else
			ExtendMenu(surface.ContextMenuStrip);
	}

	/// <summary>Captures new and revised observations once. No file or borrowed-logger IO runs on this thread.</summary>
	public void Capture() {
		EnsureOwnerThread();
		if (_disposed)
			return;
		if (!CaptureEnabled || Tools.Runtime.IsDesignMode || _outputSelector?.IsLogging != true) {
			_published.Clear();
			_published.TrimExcess();
			return;
		}
		var title = ResolvedLogTitle;
		var current = new Dictionary<(string Series, string Identity), string>();
		var publicationTime = DateTime.UtcNow;
		var samples = (_samples() ?? Array.Empty<ChartLogSample>()).Where(sample => sample != null)
			.GroupBy(sample => (CapturedAtUtc: sample.CapturedAtUtc.HasValue ? AsUtc(sample.CapturedAtUtc.Value) : (DateTime?)null,
				Identity: sample.CapturedAtUtc.HasValue ? null : sample.Identity ?? string.Empty));
		foreach (var sample in samples) {
			var measurements = sample.Select(reading => (Key: (reading.Series ?? string.Empty, reading.Identity ?? string.Empty),
				Details: FormatDetails(title, reading))).ToArray();
			var changed = false;
			foreach (var measurement in measurements) {
				var revision = title + "\n" + sample.Key.CapturedAtUtc?.Ticks.ToString(CultureInfo.InvariantCulture) + "\n" + measurement.Details;
				current[measurement.Key] = revision;
				changed |= !_published.TryGetValue(measurement.Key, out var previous) || previous != revision;
			}
			if (!changed)
				continue;
			var message = FormatSample(title, sample.Key.CapturedAtUtc ?? publicationTime, sample.ToArray());
			if (!_outputSelector.EnqueueInfo(message, includesTimestamp: true)) {
				if (_outputSelector.IsLogging)
					Interlocked.Increment(ref _droppedSamples);
				// Retry the entire related sample together while it remains in the retained snapshot.
				foreach (var measurement in measurements)
					if (_published.TryGetValue(measurement.Key, out var previous))
						current[measurement.Key] = previous;
					else
						current.Remove(measurement.Key);
			}
		}
		var previousCount = _published.Count;
		_published.Clear();
		foreach (var item in current)
			_published.Add(item.Key, item.Value);
		if (current.Count == 0 || current.Count < previousCount / 4)
			_published.TrimExcess();
	}

	/// <summary>Formats one related captured sample without reading controls or performing output IO. Safe for sampler workers.</summary>
	public static string FormatSample(string title, DateTime capturedAtUtc, ChartLogSample[] readings) {
		Guard.ArgumentNotNull(readings, nameof(readings));
		return AsUtc(capturedAtUtc).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
			+ " " + Clean(title) + " — " + string.Join("; ", readings.Where(reading => reading != null).Select(reading => FormatDetails(title, reading)));
	}

	/// <summary>Awaits every write accepted before this call, without stopping capture or blocking the UI thread.</summary>
	public async Task FlushAsync() {
		EnsureOwnerThread();
		if (_disposed) {
			await _completion;
			return;
		}
		await _retiredOutput;
		if (_outputSelector != null)
			await _outputSelector.FlushLoggingAsync();
	}

	public async Task ShowConfigurationAsync() {
		EnsureOwnerThread();
		if (_disposed || _configuring || Tools.Runtime.IsDesignMode)
			return;
		_configuring = true;
		using var configuration = Tools.Scope.ExecuteOnDispose(() => _configuring = false);
		var selector = OutputSelector;
		PrepareOwnedSelector();
		if (selector.HasConfiguration)
			await selector.ShowConfigurationAsync(_surface);
		else
			await selector.ConfigureAsync(_surface);
	}

	/// <summary>Restores chart-owned preferences after the hosting screen has attached its logger and screen host.</summary>
	public void RestoreSettings() {
		EnsureOwnerThread();
		if (!_disposed && !Tools.Runtime.IsDesignMode)
			PrepareOwnedSelector();
	}

	public void Dispose() {
		EnsureOwnerThread();
		if (_disposed)
			return;
		_disposed = true;
		_surface.Disposed -= SurfaceDisposed;
		_surface.HandleDestroyed -= SurfaceHandleDestroyed;
		if (_ownerScreen != null) {
			_ownerScreen.ScreenDisplayed -= OwnerScreenDisplayed;
			_ownerScreen.ScreenDestroyed -= OwnerScreenDestroyed;
		}
		if (_surface is Chart chart)
			chart.Invalidated -= ChartInvalidated;
		if (_outputSelector != null)
			_outputSelector.LoggingStateChanged -= LoggingStateChanged;
		if (_ownedSelector != null)
			_ownedSelector.LoggingStarting -= OwnedLoggingStarting;
		foreach (var item in _surfaces) {
			item.Control.ContextMenuStripChanged -= SurfaceMenuChanged;
			if (!item.Control.IsDisposed && ReferenceEquals(item.Control.ContextMenuStrip, _menu))
				item.Control.ContextMenuStrip = item.Previous;
		}
		foreach (var item in _extendedMenus) {
			if (!item.Key.IsDisposed)
				item.Key.Items.Remove(item.Value);
			item.Value.Dispose();
		}
		_menu.Dispose();
		_published.Clear();
		_published.TrimExcess();
		_surfaces.Clear();
		_extendedMenus.Clear();
		_ownerScreen = null;
		_completion = CompleteDisposalAsync();
	}

	public async ValueTask DisposeAsync() {
		Dispose();
		await _completion;
	}

	private void SetSelector(LogOutputSelectorControl selector) {
		if (ReferenceEquals(selector, _outputSelector))
			return;
		if (ReferenceEquals(_outputSelector, _ownedSelector) && _ownedSelector?.IsLogging == true)
			_retiredOutput = Task.WhenAll(_retiredOutput, _ownedSelector.StopLoggingAsync());
		if (_outputSelector != null)
			_outputSelector.LoggingStateChanged -= LoggingStateChanged;
		_outputSelector = selector;
		_published.Clear();
		_published.TrimExcess();
		if (selector != null)
			selector.LoggingStateChanged += LoggingStateChanged;
	}

	private string ResolvedLogTitle => !string.IsNullOrWhiteSpace(_logTitle) ? _logTitle : _titleProvider?.Invoke() ?? ResolveTitle(_surface);

	private void OwnedLoggingStarting(LogOutputConfiguration configuration) => UpdateOwnedScreenTitle();

	private void UpdateOwnedScreenTitle() {
		if (_ownedSelector != null && string.Equals(_ownedSelector.ScreenTitle, _ownedScreenTitle, StringComparison.Ordinal)) {
			_ownedScreenTitle = ResolvedLogTitle;
			_ownedSelector.ScreenTitle = _ownedScreenTitle;
		}
	}

	private void PrepareOwnedSelector() {
		// Borrowed selectors retain their consumer's preflight, application binding and preference lifecycle.
		if (_outputSelector != null && !ReferenceEquals(_outputSelector, _ownedSelector))
			return;
		WinFormsApplicationScreen owner = null;
		for (var current = _surface; current != null; current = current.Parent)
			if (current is WinFormsApplicationScreen screen) {
				owner = screen;
				break;
			}
		if (owner == null)
			return;
		if (!ReferenceEquals(owner, _ownerScreen)) {
			if (_ownerScreen != null) {
				_ownerScreen.ScreenDisplayed -= OwnerScreenDisplayed;
				_ownerScreen.ScreenDestroyed -= OwnerScreenDestroyed;
			}
			_ownerScreen = owner;
			_ownerScreen.ScreenDisplayed += OwnerScreenDisplayed;
			_ownerScreen.ScreenDestroyed += OwnerScreenDestroyed;
		}
		// Child Load can precede host attachment. A displayed owner retries after its borrowed services exist.
		if (owner.Host == null || owner.Block?.Parent == null)
			return;
		_ = OutputSelector;
		if (!ReferenceEquals(_outputSelector, _ownedSelector))
			return;
		UpdateOwnedScreenTitle();
		var path = new List<string>();
		for (var current = _surface; current != null; current = current.Parent) {
			path.Add(string.IsNullOrWhiteSpace(current.Name) ? current.GetType().Name : current.Name);
			if (current is WinFormsApplicationScreen screen) {
				_ownedSelector.ApplicationLogger ??= screen.Block?.Parent?.Logger;
				_ownedSelector.ScreenHost ??= screen.Host;
				path.Add(screen.GetType().FullName);
				break;
			}
		}
		if (string.IsNullOrEmpty(_ownedSelector.SettingsKey))
			_ownedSelector.SettingsKey = "ChartLogOutput:" + string.Join("/", path);
		_ownedSelector.RestoreSettings();
	}

	private async Task CompleteDisposalAsync() {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			// Detach the owned native selector on its UI thread before awaiting its session-owned IO drain.
			var disposeOwned = _ownedSelector?.DisposeAsync().AsTask() ?? Task.CompletedTask;
			var flushBorrowed = _outputSelector != null && !ReferenceEquals(_outputSelector, _ownedSelector)
				? _outputSelector.FlushLoggingAsync() : Task.CompletedTask;
			await Task.WhenAll(_retiredOutput, disposeOwned, flushBorrowed).ConfigureAwait(false);
		}).ConfigureAwait(false);
		if (error != null)
			Volatile.Write(ref _lastError, error);
	}

	private void LoggingStateChanged() {
		_published.Clear();
		_published.TrimExcess();
		Capture();
	}

	private void MenuOpening(object sender, System.ComponentModel.CancelEventArgs args) => _logOutputCommand.Enabled = !_disposed && !_configuring;

	private void SurfaceMenuChanged(object sender, EventArgs args) {
		if (_disposed || sender is not Control surface)
			return;
		if (surface.ContextMenuStrip == null)
			surface.ContextMenuStrip = _menu;
		else
			ExtendMenu(surface.ContextMenuStrip);
		var previous = _sharedMenu;
		_sharedMenu = surface.ContextMenuStrip;
		foreach (var item in _surfaces)
			if (!ReferenceEquals(item.Control, surface) && ReferenceEquals(item.Control.ContextMenuStrip, previous))
				item.Control.ContextMenuStrip = _sharedMenu;
	}

	private void EnsureOwnerThread() => Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Use chart capture and output configuration on the creating UI thread.");

	private void ExtendMenu(ContextMenuStrip menu) {
		if (ReferenceEquals(menu, _menu) || _extendedMenus.ContainsKey(menu))
			return;
		var command = new ToolStripMenuItem("Log Output");
		command.Click += LogOutputClicked;
		menu.Items.Add(command);
		_extendedMenus.Add(menu, command);
	}

	private async void LogOutputClicked(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(ShowConfigurationAsync);
		if (error != null && !_surface.IsDisposed)
			await ExceptionDialog.ShowAsync(_surface.FindForm(), "Log Output", error);
	}

	private void ChartInvalidated(object sender, InvalidateEventArgs args) {
		if (_disposed || _captureQueued || !CaptureEnabled || _outputSelector?.IsLogging != true || !_surface.IsHandleCreated)
			return;
		_captureQueued = true;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _surface.BeginInvoke((Action)(() => {
			_captureQueued = false;
			Capture();
		})), out var error);
		if (error != null)
			_captureQueued = false;
	}

	private void SurfaceHandleDestroyed(object sender, EventArgs args) => _captureQueued = false;

	private void OwnerScreenDisplayed(object sender, EventArgs args) => RestoreSettings();

	private void OwnerScreenDestroyed(object sender, EventArgs args) => Dispose();

	private void SurfaceDisposed(object sender, EventArgs args) => Dispose();

	private static ChartLogSample[] ReadNativeSamples(Chart chart) {
		var samples = new List<ChartLogSample>();
		foreach (var series in chart.Series) {
			var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
			var indexed = series.IsXValueIndexed || series.Points.All(point => point.XValue == 0);
			for (var index = 0; index < series.Points.Count; index++) {
				var point = series.Points[index];
				var position = !string.IsNullOrEmpty(point.AxisLabel) ? point.AxisLabel : (indexed ? index : point.XValue).ToString("R", CultureInfo.InvariantCulture);
				occurrences.TryGetValue(position, out var occurrence);
				occurrences[position] = occurrence + 1;
				var area = chart.ChartAreas.FirstOrDefault(candidate => candidate.Name == series.ChartArea) ?? chart.ChartAreas.FirstOrDefault();
				samples.Add(new ChartLogSample(series.Name, position + ":" + occurrence, position,
					point.IsEmpty ? new[] { "Unavailable" } : point.YValues.Select(value => value.ToString("R", CultureInfo.InvariantCulture)).ToArray()) {
					Units = ExtractUnits(area?.AxisY.Title), PositionLabel = string.IsNullOrWhiteSpace(area?.AxisX.Title) ? "X" : area.AxisX.Title
				});
			}
		}
		return samples.ToArray();
	}

	internal static string ResolveTitle(Control surface) {
		var native = surface as Chart ?? (surface as GraphChart)?.NativeChart;
		var nativeTitle = native?.Titles.FirstOrDefault(title => !string.IsNullOrWhiteSpace(title.Text))?.Text;
		if (!string.IsNullOrWhiteSpace(nativeTitle))
			return nativeTitle;
		for (var current = surface; current != null; current = current.Parent)
			if (current is WinFormsApplicationScreen screen && !string.IsNullOrWhiteSpace(screen.Title))
				return screen.Title;
		for (var current = surface; current != null; current = current.Parent)
			if (current is TelemetryChart telemetry && IsDescriptive(telemetry.ChartTitle))
				return telemetry.ChartTitle;
		return ResolveGroupTitle(surface) ?? (surface as GraphChart)?.Series.FirstOrDefault()?.Name ?? native?.Series.FirstOrDefault()?.Name ?? "Chart";
	}

	internal static string ResolveGroupTitle(Control surface) {
		for (var current = surface.Parent; current != null; current = current.Parent)
			if (current is GroupBox && !string.IsNullOrWhiteSpace(current.Text))
				return current.Text;
		return null;
	}

	internal static bool IsDescriptive(string title) => !string.IsNullOrWhiteSpace(title) && !string.Equals(title, "Metric", StringComparison.OrdinalIgnoreCase);

	internal static string ExtractUnits(string title) {
		if (string.IsNullOrWhiteSpace(title))
			return null;
		var opening = title.LastIndexOf('(');
		return opening >= 0 && title.EndsWith(')') ? title[(opening + 1)..^1].Trim() : title.Trim();
	}

	private static string FormatDetails(string title, ChartLogSample sample) {
		var series = IsDescriptive(sample.Series) ? sample.Series : title;
		var values = sample.Values == null || sample.Values.Length == 0 ? new[] { "Unavailable" } : sample.Values;
		var reading = string.Join(" / ", values.Select(value => Clean(value ?? "Unavailable")
			+ (!string.IsNullOrWhiteSpace(sample.Units) && value != "Unavailable" ? " " + Clean(sample.Units) : string.Empty)));
		var position = !sample.CapturedAtUtc.HasValue && !string.IsNullOrWhiteSpace(sample.Position)
			? $" ({Clean(sample.PositionLabel ?? "X")}: {Clean(sample.Position)})" : string.Empty;
		return $"{Clean(series)}: {reading}{position}";
	}

	private static DateTime AsUtc(DateTime time) => time.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : time.ToUniversalTime();

	private static string Clean(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
}
