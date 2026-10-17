// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AppxPackageSentryTests {
	[Test]
	public async Task InventoryPreservesExactIdentityAndUnknownRemovabilityWithoutLoadingProfiles() {
		var sentry = new TranscriptSentry("""
			[{"Name":"Example.App","FullName":"Example.App_1.2.3.0_x64__publisher","FamilyName":"Example.App_publisher",
			"DisplayName":"Example application","NonRemovable":null,"IsFramework":false,"MetadataError":"Localized metadata denied"}]
			""");
		var rows = await sentry.GetPackagesAsync();
		Assert.That(rows.Single().FullName, Is.EqualTo("Example.App_1.2.3.0_x64__publisher"));
		Assert.That(rows.Single().DisplayName, Is.EqualTo("Example application"));
		Assert.That(rows.Single().NonRemovable, Is.Null);
		Assert.That(rows.Single().MetadataError, Does.Contain("denied"));
		Assert.That(sentry.Arguments, Does.Contain("-NoProfile").And.Contain("-NonInteractive").And.Contain("-Command").And.Not.Contain("-EncodedCommand"));
		Assert.That(sentry.Script, Does.Contain("Main,Framework,Resource,Bundle,Xap,Optional").And.Not.Contain("-AllUsers"));
	}

	[Test]
	public async Task ARealEmptyInventoryIsDifferentFromInvalidOrFailedOutput() {
		Assert.That(await new TranscriptSentry("[]").GetPackagesAsync(), Is.Empty);
		await Assert.ThatAsync(() => new TranscriptSentry(string.Empty).GetPackagesAsync(), Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("no structured package inventory"));
		await Assert.ThatAsync(() => new TranscriptSentry("null").GetPackagesAsync(), Throws.InstanceOf<InvalidOperationException>());
		await Assert.ThatAsync(() => new TranscriptSentry("[{\"Name\":\"Missing identity\"}]").GetPackagesAsync(), Throws.InstanceOf<InvalidOperationException>());
		await Assert.ThatAsync(() => new TranscriptSentry("", 1, "Appx access denied").GetPackagesAsync(),
			Throws.InstanceOf<ProcessSentryException>().With.Message.Contains("Appx access denied"));
	}

	[Test]
	public async Task RemovalEncodesOneExactCurrentUserIdentityAndChecksCompletion() {
		var fullName = "Example.App_1.2.3.0_x64__publisher";
		var sentry = new TranscriptSentry(string.Empty);
		await sentry.RemovePackageAsync(fullName);
		Assert.That(sentry.Script, Does.Contain(Convert.ToBase64String(Encoding.UTF8.GetBytes(fullName))));
		Assert.That(sentry.Script, Does.Contain("PackageFullName -ceq $fullName").And.Contain("Remove-AppxPackage -Package $fullName"));
		Assert.That(sentry.Script, Does.Contain("$remaining.Count -ne 0").And.Not.Contain("-AllUsers").And.Not.Contain("Remove-AppxProvisionedPackage"));
	}

	[TestCase("*")]
	[TestCase("Example.App'; Remove-Item C:\\ -Recurse; '")]
	[TestCase("Example.App\r\n")]
	[TestCase("")]
	public async Task InvalidPackageIdentitiesNeverLaunchTheHelper(string fullName) {
		var sentry = new TranscriptSentry(string.Empty);
		await Assert.ThatAsync(() => sentry.RemovePackageAsync(fullName), Throws.InstanceOf<ArgumentException>());
		Assert.That(sentry.Arguments, Is.Null);
	}

	[Test]
	[Explicit("Read-only inventory of packages actually registered for the current Windows account; never removes a package.")]
	[Category("NativeIntegration")]
	public async Task CurrentUserNativeInventoryIncludesAllPackageTypesAndReadableProductNames() {
		var rows = await new AppxPackageSentry().GetPackagesAsync();
		Assert.That(rows, Is.Not.Null);
		Assert.That(rows.Select(row => row.FullName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(rows.Length));
		Assert.That(rows.All(row => !string.IsNullOrWhiteSpace(row.Name) && !string.IsNullOrWhiteSpace(row.FullName)), Is.True);
		TestContext.WriteLine($"Current-user Windows packages: {rows.Length}; localized names: {rows.Count(row => !string.IsNullOrWhiteSpace(row.DisplayName))}.");
		foreach (var application in rows.Where(row => row.Name.Contains("AutoMouse", StringComparison.OrdinalIgnoreCase)
			|| row.DisplayName.Contains("Auto Mouse", StringComparison.OrdinalIgnoreCase)))
			TestContext.WriteLine($"Auto Mouse package: {application.DisplayName} | {application.FullName} | Non-removable: {application.NonRemovable}");
	}

	private sealed class TranscriptSentry : AppxPackageSentry {
		private readonly ProcessExecutionResult _result;

		public TranscriptSentry(string output, int exitCode = 0, string error = "") {
			_result = new ProcessExecutionResult(exitCode, output, error);
		}

		public string[] Arguments { get; private set; }
		public string Script => Arguments[^1];

		public override Task<ProcessExecutionResult> RunCapturedAsync(string[] arguments, CancellationToken cancellationToken = default) {
			Arguments = arguments;
			return Task.FromResult(_result);
		}
	}
}
