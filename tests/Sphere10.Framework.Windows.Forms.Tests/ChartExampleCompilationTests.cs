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
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Explicit("Requires An Installed .NET SDK To Compile The Exported C# Example")]
[Category("WindowsIntegration")]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartExampleCompilationTests {
	[Test]
	public void GeneratedAdvancedExampleCompilesAndRecreatesItsDataThresholdAndAnchoredCallout() => RunWithMessageLoop(async owner => {
		using var original = new Chart { Dock = DockStyle.Fill };
		owner.Controls.Add(original);
		ChartGalleryRenderer.Render(original, ChartGallerySamples.All.Single(sample => sample.Id == "Thresholds"));
		var example = ChartGalleryRenderer.CreateCSharpExample(original);
		const string factoryDeclaration = "private static Chart CreateExampleChart()";
		var factoryOffset = example.IndexOf(factoryDeclaration, StringComparison.Ordinal);
		Assert.That(factoryOffset, Is.GreaterThan(0), "The Export Must Include Its Using Directives And Factory Method");
		var compilableExample = example[..factoryOffset] + "\npublic static class CompiledChartExample {\n" + example[factoryOffset..] + "\n}\n";
		var temporaryRoot = Path.GetFullPath(Path.GetTempPath());
		var directoryName = "Sphere10.ChartExampleCompilation-" + Guid.NewGuid().ToString("N");
		var directory = Path.Combine(temporaryRoot, directoryName);
		Directory.CreateDirectory(directory);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			// Only this test's freshly created directory can be recursively removed.
			var resolved = Path.GetFullPath(directory);
			Guard.Ensure(string.Equals(Path.GetDirectoryName(resolved), Path.TrimEndingDirectorySeparator(temporaryRoot), StringComparison.OrdinalIgnoreCase)
				&& string.Equals(Path.GetFileName(resolved), directoryName, StringComparison.Ordinal), "Unexpected Chart Compilation Cleanup Target");
			Directory.Delete(resolved, true);
		});
		var assemblyName = "CompiledChartExample_" + Guid.NewGuid().ToString("N");
		var projectPath = Path.Combine(directory, "ChartExample.csproj");
		var project = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
			new XElement("PropertyGroup",
				new XElement("TargetFramework", "net10.0-windows"),
				new XElement("UseWindowsForms", "true"),
				new XElement("OutputType", "Library"),
				new XElement("AssemblyName", assemblyName),
				new XElement("NuGetAudit", "false")),
			new XElement("ItemGroup", new XElement("Reference", new XAttribute("Include", typeof(Chart).Assembly.GetName().Name),
				new XElement("HintPath", typeof(Chart).Assembly.Location),
				new XElement("Private", "false")))));
		project.Save(projectPath);
		new XDocument(new XElement("configuration", new XElement("packageSources", new XElement("clear"))))
			.Save(Path.Combine(directory, "NuGet.Config"));
		await File.WriteAllTextAsync(Path.Combine(directory, "ChartExample.cs"), compilableExample);
		var outputDirectory = Path.Combine(directory, "output");
		await BuildExampleAsync(projectPath, outputDirectory, directory);
		using var assemblyStream = File.OpenRead(Path.Combine(outputDirectory, assemblyName + ".dll"));
		var compiled = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
		var factory = compiled.GetType("CompiledChartExample").GetMethod("CreateExampleChart", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.That(factory, Is.Not.Null);
		using var recreated = (Chart)factory.Invoke(null, null);
		owner.Controls.Remove(original);
		owner.Controls.Add(recreated);
		owner.PerformLayout();
		Assert.That(recreated.Dock, Is.EqualTo(DockStyle.Fill));
		Assert.That(recreated.ChartAreas.Select(area => area.Name), Is.EqualTo(original.ChartAreas.Select(area => area.Name)));
		Assert.That(recreated.Series.Select(series => (series.Name, series.ChartType, series.ChartArea, series.Color.ToArgb())),
			Is.EqualTo(original.Series.Select(series => (series.Name, series.ChartType, series.ChartArea, series.Color.ToArgb()))));
		for (var seriesIndex = 0; seriesIndex < original.Series.Count; seriesIndex++) {
			var expectedSeries = original.Series[seriesIndex];
			var actualSeries = recreated.Series[seriesIndex];
			Assert.That(actualSeries.Points.Select(point => point.XValue), Is.EqualTo(expectedSeries.Points.Select(point => point.XValue)));
			for (var pointIndex = 0; pointIndex < expectedSeries.Points.Count; pointIndex++)
				Assert.That(actualSeries.Points[pointIndex].YValues, Is.EqualTo(expectedSeries.Points[pointIndex].YValues));
		}
		var originalArea = original.ChartAreas.Single();
		var recreatedArea = recreated.ChartAreas.Single();
		Assert.That(recreatedArea.AxisX.Title, Is.EqualTo(originalArea.AxisX.Title));
		Assert.That(recreatedArea.AxisY.Title, Is.EqualTo(originalArea.AxisY.Title));
		Assert.That(recreatedArea.AxisY.Maximum, Is.EqualTo(originalArea.AxisY.Maximum));
		var originalBand = originalArea.AxisY.StripLines.Single();
		var recreatedBand = recreatedArea.AxisY.StripLines.Single();
		Assert.That(recreatedBand.IntervalOffset, Is.EqualTo(originalBand.IntervalOffset));
		Assert.That(recreatedBand.StripWidth, Is.EqualTo(originalBand.StripWidth));
		Assert.That(recreatedBand.Text, Is.EqualTo(originalBand.Text));
		Assert.That(recreatedBand.BackColor, Is.EqualTo(originalBand.BackColor));
		var originalCallout = original.Annotations.OfType<CalloutAnnotation>().Single();
		var recreatedCallout = recreated.Annotations.OfType<CalloutAnnotation>().Single();
		Assert.That(recreatedCallout.Text, Is.EqualTo(originalCallout.Text));
		Assert.That(recreatedCallout.CalloutStyle, Is.EqualTo(originalCallout.CalloutStyle));
		Assert.That(recreatedCallout.AnchorDataPointName, Is.EqualTo(originalCallout.AnchorDataPointName));
		Assert.That(recreatedCallout.AnchorDataPoint, Is.SameAs(recreated.Series[0].Points[15]));
		using var image = new MemoryStream();
		recreated.SaveImage(image, ChartImageFormat.Png);
		Assert.That(image.Length, Is.GreaterThan(3000));
		image.Position = 0;
		using var decoded = Image.FromStream(image);
		Assert.That(decoded.Size, Is.EqualTo(recreated.Size));
	});

	private static async Task BuildExampleAsync(string projectPath, string outputDirectory, string directory) {
		var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet") {
			WorkingDirectory = directory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		foreach (var argument in new[] { "build", projectPath, "--configuration", "Release", "--output", outputDirectory, "--nologo", "--verbosity", "minimal" })
			start.ArgumentList.Add(argument);
		start.Environment["DOTNET_CLI_HOME"] = Path.Combine(directory, "cli");
		start.Environment["NUGET_PACKAGES"] = Path.Combine(directory, "packages");
		start.Environment["NUGET_HTTP_CACHE_PATH"] = Path.Combine(directory, "http-cache");
		start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
		start.Environment["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false";
		start.Environment["DOTNET_NOLOGO"] = "1";
		start.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
		start.Environment["TEMP"] = directory;
		start.Environment["TMP"] = directory;
		using var process = new Process { StartInfo = start };
		Assert.That(process.Start(), Is.True, "The Installed .NET SDK Must Start");
		await using var stop = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			if (!process.HasExited) {
				process.Kill(true);
				await process.WaitForExitAsync();
			}
		});
		var standardOutput = process.StandardOutput.ReadToEndAsync();
		var standardError = process.StandardError.ReadToEndAsync();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		var cancellation = await Tools.Exceptions.ExecuteCapturingExceptionAsync<OperationCanceledException>(() => process.WaitForExitAsync(timeout.Token));
		if (cancellation != null) {
			if (!process.HasExited)
				process.Kill(true);
			await process.WaitForExitAsync();
		}
		var log = await standardOutput + Environment.NewLine + await standardError;
		TestContext.Progress.WriteLine(log);
		Assert.That(cancellation, Is.Null, "The Generated C# Example Did Not Compile Within 45 Seconds" + Environment.NewLine + log);
		Assert.That(process.ExitCode, Is.EqualTo(0), log);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false, ClientSize = new Size(850, 530), StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 65000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The Chart Example Compilation Test Timed Out"); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			watchdog.Stop();
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
