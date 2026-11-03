// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Communications;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the integration workbenches without external mail, audio, hooks or network traffic.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TesterIntegrationShowcaseTests {

	[Test]
	public void CsvConversionPreservesQuotedAndEmptyFieldsAndRejectsAnInvalidDraftAtomically() {
		RunWithMessageLoop(async owner => {
			using var screen = new CBACSVConverterScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var input = Find<TextBox>(screen, "_CBACSVTextBox");
			var output = Find<TextBox>(screen, "_outputTextBox");
			input.Text = "01/11/2026,-1,\"Coffee, lunch\",,tail";
			Find<Button>(screen, "_convertButton").PerformClick();
			Assert.That(output.Text.TrimEnd(), Is.EqualTo("2026-11-01,-1,\"Coffee, lunch\",,tail"));
			var accepted = output.Text;
			input.Text = "02/11/2026,2,valid,3" + Environment.NewLine + "31/02/2026,4,invalid,5";
			Find<Button>(screen, "_convertButton").PerformClick();
			Assert.That(Find<Label>(screen, "_status").Text, Does.Contain("Record 2 has an invalid date"));
			Assert.That(output.Text, Is.EqualTo(accepted));
			input.Clear();
			Find<Button>(screen, "_convertButton").PerformClick();
			Assert.That(output.Text, Is.Empty);
		});
	}

	[Test]
	public void ParagraphClearResetsFormattingAndUrlGenerationRejectsOverflow() {
		RunWithMessageLoop(async owner => {
			using var paragraph = new ParagraphBuilderScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(paragraph);
			await Task.Yield();
			Find<TextBox>(paragraph, "_sentenceTextBox").Text = "hello world";
			Find<Button>(paragraph, "_appendSentenceButton").PerformClick();
			Assert.That(Find<TextBox>(paragraph, "_paragraphTextBox").Text, Is.EqualTo("Hello world."));
			var previous = paragraph.Builder;
			Find<Button>(paragraph, "_clearButton").PerformClick();
			Assert.That(paragraph.Builder, Is.Not.SameAs(previous));
			Find<TextBox>(paragraph, "_sentenceTextBox").Text = "a fresh sentence";
			Find<Button>(paragraph, "_appendSentenceButton").PerformClick();
			Assert.That(Find<TextBox>(paragraph, "_paragraphTextBox").Text, Is.EqualTo("A fresh sentence."));

			owner.Controls.Remove(paragraph);
			using var urls = new UrlIDTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(urls);
			await Task.Yield();
			Find<NumericUpDown>(urls, "_start").Value = 7;
			Find<NumericUpDown>(urls, "_count").Value = 2;
			Find<ComboBox>(urls, "_format").SelectedIndex = 2;
			Find<Button>(urls, "_generateButton").PerformClick();
			var generated = Find<TextBox>(urls, "_textBoxEx").Text;
			Assert.That(generated, Does.Contain("7\t" + UrlID.Generate(7, UrlID.Format.HexBE)));
			Assert.That(generated, Does.Contain("8\t" + UrlID.Generate(8, UrlID.Format.HexBE)));
			Find<NumericUpDown>(urls, "_start").Value = uint.MaxValue;
			Find<Button>(urls, "_generateButton").PerformClick();
			Assert.That(Find<Label>(urls, "_status").Text, Does.Contain("UInt32.MaxValue"));
			Assert.That(Find<TextBox>(urls, "_textBoxEx").Text, Is.EqualTo(generated));
		});
	}

	[Test]
	public void ImageMatrixReleasesEveryReplacedAndFinalBitmap() {
		RunWithMessageLoop(async owner => {
			using var screen = new ImageResizeScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var boxes = Field<PictureBoxEx[,]>(screen, "_boxes");
			var original = Field<Image>(screen, "_original");
			var images = boxes.Cast<PictureBoxEx>().Select(box => box.Image).ToArray();
			Assert.That(images, Has.Length.EqualTo(36));
			Assert.That(images.All(image => image != null), Is.True);
			Find<NumericUpDown>(screen, "_widthNumeric").Value = 96;
			foreach (var image in images)
				Assert.That(() => _ = image.Width, Throws.TypeOf<ArgumentException>());
			var replacements = boxes.Cast<PictureBoxEx>().Select(box => box.Image).ToArray();
			screen.Dispose();
			foreach (var image in replacements)
				Assert.That(() => _ = image.Width, Throws.TypeOf<ArgumentException>());
			Assert.That(() => _ = original.Width, Throws.TypeOf<ArgumentException>());
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void EmptyCompressionRoundTripVerifiesContentAndRemovesOwnedTemporaryFiles(bool encrypted) {
		var directory = Path.Combine(Path.GetTempPath(), "Sphere10-integration-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var previousTemp = Environment.GetEnvironmentVariable("TEMP");
		var previousTmp = Environment.GetEnvironmentVariable("TMP");
		try {
			Environment.SetEnvironmentVariable("TEMP", directory);
			Environment.SetEnvironmentVariable("TMP", directory);
			Assert.That(Path.TrimEndingDirectorySeparator(Path.GetTempPath()), Is.EqualTo(directory).IgnoreCase);
			var input = Path.Combine(directory, "empty.bin");
			File.WriteAllBytes(input, Array.Empty<byte>());
			RunWithMessageLoop(async owner => {
				using ApplicationScreen screen = encrypted ? new EncryptedCompressionTestScreen() : new CompressionTestScreen();
				screen.Dock = DockStyle.Fill;
				owner.Controls.Add(screen);
				await Task.Yield();
				Find<PathSelectorControl>(screen, "fileSelectorControl1").Path = input;
				if (encrypted)
					Find<TextBox>(screen, "_passwordTextBox").Text = "session-only-test-password";
				Find<Button>(screen, "_testButton").PerformClick();
				await WaitUntilAsync(() => Find<Button>(screen, "_testButton").Enabled);
				Assert.That(Find<Label>(screen, "_status").Text, Does.StartWith("Completed."));
				Assert.That(Find<TextBox>(screen, "textBox1").Text, Does.Contain("n/a (empty input)").And.Contain("content verified"));
			});
			Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(new[] { input }));
			File.Delete(input);
		} finally {
			Environment.SetEnvironmentVariable("TEMP", previousTemp);
			Environment.SetEnvironmentVariable("TMP", previousTmp);
			Directory.Delete(directory, true);
		}
	}

	[Test]
	public void ActivatingNetworkHooksAndAudioWorkbenchesDoesNotStartExternalActions() {
		RunWithMessageLoop(async owner => {
			using var network = new CommunicationsTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(network);
			await Task.Yield();
			Assert.That(Field<ProtocolChannel>(network, "_channel"), Is.Null);
			Assert.That(Field<object>(network, "_dataSource"), Is.Null);
			Assert.That(Field<System.Windows.Forms.Timer>(network, "_reportTimer").Enabled, Is.False);
			owner.Controls.Remove(network);

			using var hooks = new HooksScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(hooks);
			await Task.Yield();
			Assert.That(Field<WindowsKeyboardHook>(hooks, "_keyHook").Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
			Assert.That(Field<WindowsMouseHook>(hooks, "_mouseHook").Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
			Assert.That(Field<System.Windows.Forms.Timer>(hooks, "_refreshTimer").Enabled, Is.False);
			owner.Controls.Remove(hooks);

			using var sounds = new TestSoundsScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(sounds);
			await Task.Yield();
			Assert.That(Field<object>(sounds, "_device"), Is.Null);
			Assert.That(Field<object>(sounds, "_player"), Is.Null);
		});
	}

	// Named controls exercise the actual Designer surface without production testing accessors.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// Ownership assertions inspect private resources only where no public testing seam is appropriate.
	private static T Field<T>(object owner, string name) {
		return (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
	}

	// Await a semantic native result while keeping the owning dispatcher responsive.
	private static async Task WaitUntilAsync(Func<bool> predicate) {
		var elapsed = Stopwatch.StartNew();
		while (!predicate() && elapsed.Elapsed < TimeSpan.FromSeconds(8))
			await Task.Delay(10);
		Assert.That(predicate(), Is.True, "The integration workflow did not complete.");
	}

	// Each case owns its dispatcher, native window and restored synchronization context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1100, 800)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The integration showcase test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
