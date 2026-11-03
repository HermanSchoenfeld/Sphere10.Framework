// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Composes real SMTP messages while keeping credentials and drafts out of persisted preferences.</summary>
public partial class EmailTestScreen : ApplicationScreen {
	private bool _sending;

	/// <summary>Creates the composer without reading settings or contacting a server.</summary>
	public EmailTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override EmailScreenSettings ScreenSettings => (EmailScreenSettings)base.ScreenSettings;

	// Restoring transport choices does not replace message or password drafts.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings || _sending)
			return;
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_smtpServerTextBox.Text = values.Server;
			_smtpUsernameTextBox.Text = values.Username;
			_smtpPortIntBox.Value = Math.Clamp(values.Port, 1, 65535);
			_sslCheckBox.Checked = values.UseTls;
			_isHtmlCheckBox.Checked = values.HtmlBody;
		}
	}

	protected override void CopyUIToModel() {
		base.CopyUIToModel();
		if (Updating || !HasSettings)
			return;
		var values = ScreenSettings;
		var changed = values.Server != _smtpServerTextBox.Text
			|| values.Username != _smtpUsernameTextBox.Text
			|| values.Port != decimal.ToInt32(_smtpPortIntBox.Value)
			|| values.UseTls != _sslCheckBox.Checked
			|| values.HtmlBody != _isHtmlCheckBox.Checked;
		if (!changed)
			return;
		values.Server = _smtpServerTextBox.Text;
		values.Username = _smtpUsernameTextBox.Text;
		values.Port = decimal.ToInt32(_smtpPortIntBox.Value);
		values.UseTls = _sslCheckBox.Checked;
		values.HtmlBody = _isHtmlCheckBox.Checked;
		MarkSettingsChanged();
	}

	private void PreferencesChanged(object sender, EventArgs e) {
		CopyUIToModel();
	}

	private async void _sendButtonAsync_Click(object sender, EventArgs e) {
		await SendMessageAsync(false);
	}

	private async void _sendButton_Click(object sender, EventArgs e) {
		await SendMessageAsync(true);
	}

	// Capture all native input before either mail API runs; the synchronous API runs away from the dispatcher.
	private async Task SendMessageAsync(bool synchronous) {
		if (_sending)
			return;
		var request = new EmailerSettings {
			SMTPServer = _smtpServerTextBox.Text.Trim(), SMTPPort = decimal.ToInt32(_smtpPortIntBox.Value),
			SMTPUsername = _smtpUsernameTextBox.Text, SMTPPassword = _smtpPasswordTextBox.Text,
			From = _fromTextBox.Text.Trim(), To = _toTextBox.Text, CC = _ccTextBox.Text, BCC = _bccTextBox.Text,
			ReplyTo = _replyToTextBox.Text, Subject = _subjectTextBox.Text, Body = _bodyTextBox.Text,
			SSL = _sslCheckBox.Checked, IsHtml = _isHtmlCheckBox.Checked
		};
		var recipients = Tools.Mail.ParseEmailsFromCommaSeparatedList(request.To).ToArray();
		if (string.IsNullOrWhiteSpace(request.SMTPServer) || !Tools.Mail.IsValidEmail(request.From)
			|| recipients.Length == 0 || recipients.Any(address => !Tools.Mail.IsValidEmail(address))) {
			_status.Text = "Enter an SMTP server, a valid From address and at least one valid To address.";
			return;
		}

		_sending = true;
		try {
			_status.Text = "Sending message…";
			using (_loadingCircle.BeginAnimationScope(this)) {
				if (synchronous)
					await Task.Run(() => Tools.Mail.SendEmail(request.SMTPServer, request.From, request.Subject, request.Body, recipients,
						Tools.Mail.ParseEmailsFromCommaSeparatedList(request.CC), Tools.Mail.ParseEmailsFromCommaSeparatedList(request.BCC),
						Tools.Mail.ParseEmailsFromCommaSeparatedList(request.ReplyTo), requiresSSL: request.SSL, username: request.SMTPUsername,
						password: request.SMTPPassword, port: request.SMTPPort, bodyHtml: request.IsHtml));
				else
					await Tools.Mail.SendEmailAsync(request.SMTPServer, request.From, request.Subject, request.Body, recipients,
						Tools.Mail.ParseEmailsFromCommaSeparatedList(request.CC), Tools.Mail.ParseEmailsFromCommaSeparatedList(request.BCC),
						Tools.Mail.ParseEmailsFromCommaSeparatedList(request.ReplyTo), requiresSSL: request.SSL, username: request.SMTPUsername,
						password: request.SMTPPassword, port: request.SMTPPort, bodyHtml: request.IsHtml);
			}
			if (!IsDisposed && !Disposing)
				_status.Text = "SMTP send completed successfully.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "SMTP send failed: " + error.Message;
		} finally {
			request.SMTPPassword = null;
			_sending = false;
			if (!IsDisposed && !Disposing)
				Refresh();
		}
	}

	#region Inner Types

	public class EmailerSettings : SettingsObject {
		[DefaultValue(null)]
		public string From { get; set; }

		[DefaultValue(null)]
		public string To { get; set; }

		[DefaultValue(null)]
		public string CC { get; set; }

		[DefaultValue(null)]
		public string BCC { get; set; }

		[DefaultValue(null)]
		public string ReplyTo { get; set; }

		[DefaultValue(null)]
		public string SMTPServer { get; set; }

		[DefaultValue(null)]
		public int? SMTPPort { get; set; }

		[DefaultValue(null)]
		public string SMTPUsername { get; set; }

		[DefaultValue(null)]
		public string SMTPPassword { get; set; }

		[DefaultValue(false)]
		public bool SSL { get; set; }

		[DefaultValue(false)]
		public bool IsHtml { get; set; }

		[DefaultValue(null)]
		public string Subject { get; set; }

		[DefaultValue(null)]
		public string Body { get; set; }

	}

	/// <summary>Non-secret SMTP transport preferences for the mail composer.</summary>
	[UserEditable]
	public class EmailScreenSettings : SettingsObject {
		/// <summary>The SMTP server name.</summary>
		[DefaultValue("localhost")]
		[Required]
		public string Server { get; set; } = "localhost";

		/// <summary>The SMTP port.</summary>
		[DefaultValue(25)]
		[Range(1, 65535)]
		public int Port { get; set; } = 25;

		/// <summary>The SMTP user name; the password remains a session-only draft.</summary>
		[DefaultValue("")]
		public string Username { get; set; } = string.Empty;

		/// <summary>Whether the connection uses TLS.</summary>
		[DefaultValue(false)]
		public bool UseTls { get; set; } = false;

		/// <summary>Whether the body is HTML.</summary>
		[DefaultValue(false)]
		public bool HtmlBody { get; set; } = false;
	}
	#endregion
}
