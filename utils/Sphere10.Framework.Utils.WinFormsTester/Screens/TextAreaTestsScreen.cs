// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tuple = System.Tuple;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Compares native text rendering and preserves the original character/EPASA generation examples.</summary>
public partial class TextAreaTestsScreen : WinFormsApplicationScreen {
	public const string Pascal64CharSet = "abcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-+{}[]_:`|<>,.?/~";
	public const string Pascal64StartCharSet = "abcdefghijklmnopqrstuvwxyz!@#$%^&*()-+{}[]_:`|<>,.?/~";
	public const string HexStringCharSet = "0123456789abcdef";
	public const string Base58CharSet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
	public const string SafeUnescapedAnsiCharSet = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|~";
	public string SafeEscapedAnsiCharSet = "\"\\}";
	public const char EscapeChar = '\\';

	public TextAreaTestsScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// The normal path intentionally appends line by line to expose native rendering cost.
	private void FillStandardButton_Click(object sender, EventArgs args) {
		FillText(false);
	}

	// The former locked stub now uses the Framework's existing rendering scope.
	private void FillLockedButton_Click(object sender, EventArgs args) {
		FillText(true);
	}

	// Both modes use identical generated text; only their rendering boundary differs.
	private void FillText(bool deferRendering) {
		var text = GenerateText((int)_sizeKilobytes.Value * 1024);
		var watch = Stopwatch.StartNew();
		if (deferRendering) {
			using (_standardTextBox.EnterRenderingScope())
				AppendLines(text);
		} else
			AppendLines(text);
		_timing.Text = $"{text.Length * sizeof(char):N0} UTF-16 bytes appended in {watch.Elapsed.TotalMilliseconds:N1} ms · {(deferRendering ? "Deferred rendering" : "Ordinary rendering")}";
	}

	// UI-owned text assignment stays on the dispatcher in every mode.
	private void AppendLines(string text) {
		_standardTextBox.Clear();
		using var reader = new StringReader(text);
		while (reader.ReadLine() is { } line)
			_standardTextBox.AppendText(line + Environment.NewLine);
	}

	private void EpasaPwdCharsButton_Click(object sender, EventArgs args) {
		const string regexEscapes = @"[\^$.|?*+(){}";
		const string epasaEscapes = @":\""[]()<>{}";
		const string csharpEscapes = @"""";
		const string pascal64charset = @"abcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-+{}[]_:`|<>,.?/~";

		var sb = new StringBuilder();
		for (var index = 32; index <= 126; index++)
			sb.Append($"\"{Encoding.ASCII.GetString(new byte[] { (byte)index })}\" | ");

		sb.AppendLine();
		sb.Append("ANSICHAR = (");
		for (var i = 32; i <= 126; i++) {
			var charStr = Encoding.ASCII.GetString(new byte[] { (byte)i });
			var regexEscaped = regexEscapes.Contains(charStr);
			var epasaEscaped = epasaEscapes.Contains(charStr);
			var csharpEscaped = csharpEscapes.Contains(charStr);
			var prefix = string.Empty;
			if (regexEscaped)
				prefix = @"\";
			if (epasaEscaped)
				prefix = @"\\" + prefix;
			if (csharpEscaped)
				charStr = @"""""";

			charStr = prefix + charStr;
			sb.Append($"{charStr}|");
		}

		sb.Append(")");

		sb.AppendLine();
		sb.Append("PASCAL64 = (");
		foreach (var ch in pascal64charset) {
			var charStr = ch.ToString();
			var regexEscaped = regexEscapes.Contains(charStr);
			var epasaEscaped = epasaEscapes.Contains(charStr);
			var csharpEscaped = csharpEscapes.Contains(charStr);
			var prefix = string.Empty;
			if (regexEscaped)
				prefix = @"\";
			if (epasaEscaped)
				prefix = @"\\" + prefix;
			if (csharpEscaped)
				charStr = @"""""";

			charStr = prefix + charStr;
			sb.Append($"{charStr}|");
		}

		sb.Append(")");

		sb.AppendLine();
		sb.Append(@"EscapedAnsiCharSet = @""");
		for (var i = 32; i <= 126; i++) {
			var ch = Encoding.ASCII.GetString(new byte[] { (byte)i }).ToString();
			if (csharpEscapes.Contains(ch))
				ch = @"""";
			if (epasaEscapes.Contains(ch))
				sb.Append(ch);
		}
		sb.Append(@""";");

		sb.AppendLine();
		sb.Append(@"UnescapedAnsiCharSet = @""");
		for (var i = 32; i <= 126; i++) {
			var ch = Encoding.ASCII.GetString(new byte[] { (byte)i }).ToString();

			if (csharpEscapes.Contains(ch))
				ch = @"""";
			if (!epasaEscapes.Contains(ch))
				sb.Append(ch);
		}
		sb.Append(@""";");

		sb.AppendLine();
		sb.Append(@"FullAnsiCharSet = @""");
		for (var i = 32; i <= 126; i++) {
			var ch = Encoding.ASCII.GetString(new byte[] { (byte)i }).ToString();
			if (csharpEscapes.Contains(ch))
				ch = @"""";
			sb.Append(ch);
		}
		sb.Append(@""";");
		_standardTextBox.Clear();
		_standardTextBox.AppendText(sb.ToString());
	}

	// Generation-only timing does not include native editor updates.
	private async void GenOnlyButton_Click(object sender, EventArgs args) {
		await GenerateAsync(false);
	}

	// Background generation returns a detached string before touching the native editor.
	private async void FillLockedAsyncButton_Click(object sender, EventArgs args) {
		await GenerateAsync(true);
	}

	// Contain native-event failures and reject delivery after screen disposal.
	private async Task GenerateAsync(bool display) {
		_actions.Enabled = false;
		try {
			var size = (int)_sizeKilobytes.Value * 1024;
			var watch = Stopwatch.StartNew();
			var text = await Task.Run(() => GenerateText(size));
			if (IsDisposed || Disposing)
				return;
			if (display)
				_standardTextBox.Text = text;
			_timing.Text = $"{text.Length * sizeof(char):N0} UTF-16 bytes generated in {watch.Elapsed.TotalMilliseconds:N1} ms · {(display ? "Published to editor" : "Generation only")}";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_timing.Text = "Generation could not complete: " + error.Message;
		} finally {
			if (!IsDisposed && !Disposing)
				_actions.Enabled = true;
		}
	}

	// A small default keeps the example responsive while allowing explicit larger experiments.
	private static string GenerateText(int byteLimit) {
		var text = new StringBuilder();
		while (text.Length * sizeof(char) < byteLimit)
			text.AppendLine(Tools.Text.GenerateRandomString(50));
		return text.ToString();
	}

	private async void EpasaRegexButton_Click(object sender, EventArgs args) {

		try {
			//var unescapedSafeAnsiCharPattern =
			//	RegexPattern.With.Set(RegexPattern.With.Literal(SafeUnescapedAnsiCharSet));

			var unescapedSafeAnsiCharPatterns = SafeUnescapedAnsiCharSet
				.Select(c => RegexPattern.With.Literal(c.ToString()));

			var escapedSafeAnsiCharPatterns = SafeEscapedAnsiCharSet
				.Select(c => RegexPattern.With.Literal("\\").Literal(c.ToString()));

			var safeAnsiCharPattern = RegexPattern.With.Choice.EitherAny(
unescapedSafeAnsiCharPatterns.Concat(escapedSafeAnsiCharPatterns).ToArray());

			var safeAnsiStringPattern = new RegexPattern(safeAnsiCharPattern)
				.Repeat.OneOrMore;

			var pascal64StartPattern = RegexPattern.With
				.Set(RegexPattern.With.Literal(Pascal64StartCharSet, true));

			// account name needs to omit brackets!

			var pascal64StringPattern =
				new RegexPattern(pascal64StartPattern)
					.Set(RegexPattern.With.Literal(Pascal64CharSet, true)).Repeat.InRange(2, 63);

			var hexNibblePattern = RegexPattern.With
				.Set(RegexRange.OfMany(Tuple.Create('0', '9'), Tuple.Create('a', 'f')));

			var hexBytePattern = RegexPattern.With
				.Group(
new RegexPattern(hexNibblePattern).Repeat.Exactly(2));

			var hexStringPattern = new RegexPattern(hexBytePattern).Repeat.OneOrMore;

			var base58CharPattern = RegexPattern.With
				.Set(RegexPattern.With.Literal(Base58CharSet));

			var base58StringPattern = new RegexPattern(base58CharPattern).Repeat.OneOrMore;

			var integerPattern = RegexPattern
				.With.Digit.Repeat.OneOrMore;

			var accountNamePattern = RegexPattern
				.With.NamedGroup("AccountName", pascal64StringPattern);

			var accountNumberPattern = RegexPattern
				.With.NamedGroup("AccountNumber", integerPattern)
				.Literal("-")
				.NamedGroup("Checksum", RegexPattern.With.Digit.Repeat.Exactly(2));

			var pasaPattern = RegexPattern
				.With.Choice.Either(accountNamePattern, accountNumberPattern);

			var payloadPattern = RegexPattern.With
				.Choice.EitherAny(
					RegexPattern.With.NamedGroup("ASCIIPayload", RegexPattern.With.Literal("\"").Group(safeAnsiStringPattern).Literal("\"")).Repeat.Optional,
					RegexPattern.With.NamedGroup("HexPayload", RegexPattern.With.Literal("0x").Group(hexStringPattern)).Repeat.Optional,
					RegexPattern.With.NamedGroup("Base58Payload", base58StringPattern).Repeat.Optional
				);

			var publicPayloadPattern = RegexPattern.With
				.Literal("[")
				.NamedGroup("NoEncryption", payloadPattern).Repeat.Optional
				.Literal("]", true);

			var receiverPayloadPattern = RegexPattern.With
				.Literal("(")
				.NamedGroup("ReceiverEncrypted", payloadPattern).Repeat.Optional
				.Literal(")");

			var senderPayloadPattern = RegexPattern.With
				.Literal("<")
				.NamedGroup("SenderEncrypted", payloadPattern).Repeat.Optional
				.Literal(">");

			var aesPayloadPattern = RegexPattern.With
				.Literal("<")
				.NamedGroup("AESEncrypted", payloadPattern).Repeat.Optional
				.Literal(">")
				.Literal(":")
				.NamedGroup("Password", safeAnsiStringPattern);

			var extendedAddressPattern = RegexPattern.With
				.Choice.EitherAny(
publicPayloadPattern, receiverPayloadPattern, senderPayloadPattern, aesPayloadPattern);

			var epasaChecksumPattern = new RegexPattern(hexBytePattern)
				.Repeat.Exactly(2);

			var epasaPattern = RegexPattern
				.With.AtBeginning
				.NamedGroup("PASA", pasaPattern)
				.RegEx(new RegexPattern(extendedAddressPattern)).Repeat.Optional
				.Group(
RegexPattern.With
						.Literal(":")
						.NamedGroup("EPASAChecksum", epasaChecksumPattern)
						.AtEnd).Repeat.Optional;

			_standardTextBox.Clear();
			_standardTextBox.AppendText(epasaPattern.ToString());
			{
				//subregex = new Regex(unescapedSafeAnsiCharPattern, RegexOptions.None).Match("77-44[0x1234]:121f");
				_standardTextBox.AppendLine("const string safeAnsiCharPattern = \"" + safeAnsiCharPattern + "\"");
				_standardTextBox.AppendLine("const string safeAnsiStringPattern = \"" + safeAnsiStringPattern + "\"");
				_standardTextBox.AppendLine("const string pascal64StartPattern = \"" + pascal64StartPattern + "\"");
				_standardTextBox.AppendLine("const string pascal64StringPattern = \"" + pascal64StringPattern + "\"");
				_standardTextBox.AppendLine("const string hexNibblePattern = \"" + hexNibblePattern + "\"");
				_standardTextBox.AppendLine("const string hexBytePattern = \"" + hexBytePattern + "\"");
				_standardTextBox.AppendLine("const string hexStringPattern = \"" + hexStringPattern + "\"");
				_standardTextBox.AppendLine("const string base58CharPattern = \"" + base58CharPattern + "\"");
				_standardTextBox.AppendLine("const string base58StringPattern = \"" + base58StringPattern + "\"");
				_standardTextBox.AppendLine("const string integerPattern = \"" + integerPattern + "\"");
				_standardTextBox.AppendLine("const string accountNamePattern = \"" + accountNamePattern + "\"");
				_standardTextBox.AppendLine("const string accountNumberPattern = \"" + accountNumberPattern + "\"");
				_standardTextBox.AppendLine("const string pasaPattern = \"" + pasaPattern + "\"");
				_standardTextBox.AppendLine("const string payloadPattern = \"" + payloadPattern + "\"");
				_standardTextBox.AppendLine("const string publicPayloadPattern = \"" + publicPayloadPattern + "\"");
				_standardTextBox.AppendLine("const string receiverPayloadPattern = \"" + receiverPayloadPattern + "\"");
				_standardTextBox.AppendLine("const string senderPayloadPattern = \"" + senderPayloadPattern + "\"");
				_standardTextBox.AppendLine("const string aesPayloadPattern = \"" + aesPayloadPattern + "\"");
				_standardTextBox.AppendLine("const string extendedAddressPattern = \"" + extendedAddressPattern + "\"");
				_standardTextBox.AppendLine("const string epasaChecksumPattern = \"" + epasaChecksumPattern + "\"");
				_standardTextBox.AppendLine("const string epasaPattern = \"" + epasaPattern + "\"");
			}

			_standardTextBox.AppendLine("");

			var regex = new Regex(epasaPattern, RegexOptions.None);

			var result = regex.Match("77-44[0x123]:121f");
			result = regex.Match("77-44[0x123]");
			result = regex.Match("77-44[B58xdf]:121f");
			result = regex.Match("77-44");
			result = regex.Match("account-name");
			result = regex.Match("account-name:abcd");
			_standardTextBox.AppendLine(result.ToString());
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}
}
