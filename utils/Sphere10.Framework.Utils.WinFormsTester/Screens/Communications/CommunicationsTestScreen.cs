// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Communications;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Demonstrates channel messaging and WebSocket data sources with UI-owned reporting.</summary>
public partial class CommunicationsTestScreen : ApplicationScreen {
	private readonly System.Windows.Forms.Timer _reportTimer;
	private readonly ConcurrentQueue<string> _received = new();
	private ProtocolChannel _channel;
	private ServerWebSocketsDataSource<TestClass> _dataSource;
	private Task _closing = Task.CompletedTask;
	private bool _opening;

	/// <summary>Creates inactive endpoint controls without global logging, DNS or network side effects.</summary>
	public CommunicationsTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_reportTimer = new System.Windows.Forms.Timer { Interval = 150 };
		_reportTimer.Tick += RefreshReport;
	}

	// Pause UI reporting while WinForms recreates this screen's native handle.
	protected override void OnHandleDestroyed(EventArgs args) {
		_reportTimer?.Stop();
		base.OnHandleDestroyed(args);
	}

	// Resume an active demonstration after its native handle becomes available again.
	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);
		if (!Disposing && !IsDisposed && (_channel != null || _dataSource != null))
			_reportTimer?.Start();
	}

	/// <summary>Stops reporting and requests channel closure before an accepted close saves preferences.</summary>
	public override async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		await CloseOwnedAsync().WaitAsync(cancellationToken);
		await base.SaveSettingsAsync(cancellationToken);
	}

	protected override void OnDestroyScreen() {
		_reportTimer.Stop();
		_reportTimer.Tick -= RefreshReport;
		_reportTimer.Dispose();
		_closing = ObserveCloseAsync(CloseOwnedAsync());
		base.OnDestroyScreen();
	}

	private async void OpenClicked(object sender, EventArgs e) {
		await OpenChannelAsync();
	}

	// Capture the endpoint controls and retain the current demonstration channel.
	private async Task OpenChannelAsync() {
		if (_channel != null || _dataSource != null || _opening)
			return;
		try {
			var (local, remote) = ReadEndpoints();
			var role = _role.SelectedIndex == 1 ? CommunicationRole.Server : CommunicationRole.Client;
			var protocol = _protocol.SelectedItem as string;
			_channel = protocol switch {
				"TCP" => new TCPChannel(local, remote, role),
				"WebSockets" when role == CommunicationRole.Server => new ServerWebSocketsChannel(local, remote, false),
				"WebSockets" => new ClientWebSocketsChannel($"ws://{remote.Address}:{remote.Port}", false),
				_ => new UDPChannel(local, remote, role)
			};
			var channel = _channel;
			channel.ReceivedBytes += ReceivedBytes;
			_opening = true;
			SetAvailability();
			_reportTimer.Start();
			_status.Text = "Opening channel. A WebSocket server waits for a client.";
			await Task.Run(channel.Open);
			if (!IsDisposed && !Disposing && ReferenceEquals(_channel, channel))
				_status.Text = "Channel opened. Send transmits the captured UTF-8 message.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Channel opening failed: " + error.Message;
			await ObserveCloseAsync(CloseOwnedAsync());
		} finally {
			_opening = false;
			if (!IsDisposed && !Disposing)
				SetAvailability();
		}
	}

	// Retain the original multi-client data-source demonstration as a separate explicit server command.
	private void DataServerClicked(object sender, EventArgs e) {
		if (_channel != null || _dataSource != null || _opening)
			return;
		try {
			var (local, remote) = ReadEndpoints();
			_dataSource = new ServerWebSocketsDataSource<TestClass>(local, remote, false,
				(item, id) => { item.FillWithTestData(id); return null; }, item => null, item => item.Id.ToString());
			_reportTimer.Start();
			SetAvailability();
			_status.Text = "WebSocket data-source server started. Client connections appear below.";
		} catch (Exception error) {
			_status.Text = "Data-source server could not start: " + error.Message;
		}
	}

	private async void SendClicked(object sender, EventArgs e) {
		await SendMessageAsync();
	}

	// Sending observes the actual transport result and reports errors on the native dispatcher.
	private async Task SendMessageAsync() {
		var channel = _channel;
		if (channel == null || channel.State != ProtocolChannelState.Open)
			return;
		try {
			var sent = await channel.TrySendBytes(Encoding.UTF8.GetBytes(_message.Text));
			if (!IsDisposed && !Disposing)
				_status.Text = sent ? "Message sent." : "The channel did not accept the message.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Send failed: " + error.Message;
		}
	}

	private async void StopClicked(object sender, EventArgs e) {
		await StopOwnedAsync();
	}

	// A user-requested stop reports cleanup failures rather than claiming success.
	private async Task StopOwnedAsync() {
		try {
			await CloseOwnedAsync();
			if (!IsDisposed && !Disposing) {
				_clients.Rows.Clear();
				_status.Text = "Channel closure requested; reporting stopped.";
			}
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Channel shutdown failed: " + error.Message;
		} finally {
			if (!IsDisposed && !Disposing)
				SetAvailability();
		}
	}

	private async void CloseSelectedClicked(object sender, EventArgs e) {
		await CloseSelectedAsync();
	}

	// A selected data-source row denotes a client; a raw-channel row denotes its sole transport.
	private async Task CloseSelectedAsync() {
		if (_clients.SelectedRows.Count == 0)
			return;
		if (_dataSource != null) {
			var id = Convert.ToString(_clients.SelectedRows[0].Cells[0].Value);
			try {
				_dataSource.CloseConnection(id);
			} catch (Exception error) {
				if (!IsDisposed && !Disposing)
					_status.Text = "Connection close failed: " + error.Message;
			}
		} else {
			await ObserveCloseAsync(CloseOwnedAsync());
		}
	}

	// Native callbacks only queue detached text; the owning UI timer updates controls.
	private void ReceivedBytes(ReadOnlyMemory<byte> bytes) {
		if (_received.Count < 1000)
			_received.Enqueue(Encoding.UTF8.GetString(bytes.Span));
	}

	private void RefreshReport(object sender, EventArgs e) {
		if (IsDisposed || Disposing)
			return;
		while (_received.TryDequeue(out var message))
			_output.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
		if (_output.TextLength > 24000)
			_output.Text = _output.Text[^18000..];
		var selected = _clients.SelectedRows.Count == 0 ? null : Convert.ToString(_clients.SelectedRows[0].Cells[0].Value);
		_clients.Rows.Clear();
		if (_dataSource != null) {
			foreach (var line in _dataSource.Report().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
				var match = Regex.Match(line, @"^Key:\s+(\S+)\s+State:\s+(\S+)\s+Connection:\s+(\S+)");
				if (match.Success)
					_clients.Rows.Add(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
			}
		} else if (_channel != null) {
			_clients.Rows.Add("Current channel", _channel.State, _channel.State == ProtocolChannelState.Open && _channel.IsConnectionAlive());
		}
		_clients.ClearSelection();
		foreach (DataGridViewRow row in _clients.Rows) {
			if (string.Equals(Convert.ToString(row.Cells[0].Value), selected, StringComparison.Ordinal))
				row.Selected = true;
		}
		SetAvailability();
	}

	private void InfoClicked(object sender, EventArgs e) {
		RefreshReport(sender, e);
	}

	private void SelectionChanged(object sender, EventArgs e) {
		_closeSelected.Enabled = _clients.SelectedRows.Count != 0;
	}

	private void ClearClicked(object sender, EventArgs e) {
		_received.Clear();
		_output.Clear();
	}

	private (IPEndPoint Local, IPEndPoint Remote) ReadEndpoints() {
		if (!IPAddress.TryParse(_localAddress.Text, out var local) || !IPAddress.TryParse(_remoteAddress.Text, out var remote))
			throw new FormatException("Enter literal local and remote IP addresses, such as 127.0.0.1.");
		return (new IPEndPoint(local, decimal.ToInt32(_localPort.Value)), new IPEndPoint(remote, decimal.ToInt32(_remotePort.Value)));
	}

	private void SetAvailability() {
		var active = _channel != null || _dataSource != null || _opening || !_closing.IsCompleted;
		_openButton.Enabled = !active;
		_dataServerButton.Enabled = !active;
		_stopButton.Enabled = active;
		_sendButton.Enabled = _channel?.State == ProtocolChannelState.Open;
		_protocol.Enabled = !active;
		_role.Enabled = !active;
		_closeSelected.Enabled = _clients.SelectedRows.Count != 0;
	}

	// Clear the live owner first; repeated stop/dispose callers join the same captured cleanup task.
	private Task CloseOwnedAsync() {
		var channel = _channel;
		var dataSource = _dataSource;
		_channel = null;
		_dataSource = null;
		if (channel == null && dataSource == null)
			return _closing;
		if (!IsDisposed && !Disposing)
			_reportTimer.Stop();
		if (channel != null)
			channel.ReceivedBytes -= ReceivedBytes;
		_closing = Task.Run(async () => {
			if (dataSource != null)
				dataSource.Close();
			if (channel != null) {
				try {
					await channel.DisposeAsync();
				} finally {
					if (channel is UDPChannel udp)
						udp.Dispose();
				}
			}
		});
		return _closing;
	}

	private static async Task ObserveCloseAsync(Task operation) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => operation);
		if (error != null)
			SystemLog.Exception(error, "Could not completely close the communications demonstration.");
	}
}
