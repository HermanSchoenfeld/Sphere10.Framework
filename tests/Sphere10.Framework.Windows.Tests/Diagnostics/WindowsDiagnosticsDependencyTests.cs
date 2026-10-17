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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.EventLogs;
using Sphere10.Framework.Windows.Files;
using Sphere10.Framework.Windows.Networking;
using Sphere10.Framework.Windows.Services;
using Sphere10.Framework.Windows.Storage;
using Sphere10.Framework.Windows.Startup;
using Sphere10.Framework.Windows.Security;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsDiagnosticsDependencyTests {
	private static readonly string[] _appTypeNames = {
		"IActionObserver", "ActionObserverLogger", "WindowsProcessCollector", "WindowsPerformanceCollector", "WindowsCatalogCollector", "WindowsHardwareCollector",
		"LivePerformanceStatistics", "LiveMetricSnapshot", "SystemPerformanceSample", "ProcessSnapshot", "ProcessDetails", "ProcessThreadSnapshot", "ProcessModuleSnapshot",
		"ServiceSnapshot", "DriverSnapshot", "ServiceActionPlan", "DriverActionPlan", "NetworkAdapterSnapshot", "NetworkEndpointSnapshot", "StorageSnapshot",
		"EnvironmentSnapshot", "StartupEntrySnapshot", "RegistryExplorerService", "RegistryLocation", "RegistryKeySnapshot", "RegistryValueSnapshot",
		"InstalledApplicationsService", "InstalledApplicationSnapshot", "InstalledApplicationIconCache", "FileSystemWatcherService", "FileWatchOptions",
		"FileWatchSnapshot", "FileWatchRecord", "WindowMessageService", "WindowMessageRequest", "WindowMessageResult", "TaskManagerReplacementService",
		"ApplicationElevationService", "NTFSFileCopier", "NTFSFileCopyOptions", "NTFSFileCopyProgress", "NTFSFileCopyResult", "UserCopier", "GroupCopier", "ShareCopier",
		"SIDSearcher", "SIDRenamer", "SidTranslator", "SidNameResolver"
	};

	public static Type[] NativeDataTypes => new[] {
		typeof(RegistryAddress), typeof(RegistryValue), typeof(RegistryTree), typeof(RegistrySubTree), typeof(ProcessIdentity), typeof(WindowsProcessInfo),
		typeof(WindowsProcessCounters), typeof(WindowsProcessInventoryEntry), typeof(WindowsThreadInfo), typeof(WindowsModuleInfo), typeof(WindowsSystemCounters), typeof(WindowsWindowInfo),
		typeof(WindowsServiceInfo), typeof(WindowsServiceConfiguration), typeof(WindowsDriverPackageInfo), typeof(WindowsNetworkEndpoint),
		typeof(WindowsNetworkInterface), typeof(WindowsInterfaceCounters), typeof(WindowsDriveInfo), typeof(WindowsPhysicalDiskInfo), typeof(WindowsPartitionInfo),
		typeof(WindowsEventLogEntry), typeof(FileHandleInfo), typeof(FileHandleCapture), typeof(WindowsFileInspection)
	};

	[TestCase("Sphere10.Framework.Application")]
	[TestCase("Sphere10.Framework.Data")]
	[TestCase("Sphere10.Framework.Windows.Forms")]
	[TestCase("System.Windows.Forms")]
	[TestCase("SystemExpert")]
	public void DiagnosticLibraryDoesNotDependOnApplicationOrPresentationAssemblies(string forbiddenPrefix) {
		var root = typeof(WinAPI).Assembly;
		var references = FrameworkDependencyClosure(root);
		Assert.That(references.Any(name => name.StartsWith(forbiddenPrefix, StringComparison.Ordinal)), Is.False,
			"Reusable Windows diagnostics must not depend on application-specific or presentation assemblies.");
	}

	[Test]
	public void ReusableWindowAndOwnedWorkerOperationsAreDeclaredByTheWindowsLibrary() {
		var library = typeof(WinAPI).Assembly;
		Assert.That(typeof(WindowsWindow).Assembly, Is.SameAs(library));
		Assert.That(typeof(WindowMessageSender).Assembly, Is.SameAs(library));
		Assert.That(typeof(WindowsProcessWorkerHost).Assembly, Is.SameAs(library));
		Assert.That(typeof(FileHandleService).Assembly, Is.SameAs(library));
		Assert.That(typeof(ProcessSignalService).Assembly, Is.SameAs(library));
		Assert.That(library.GetType("Sphere10.Framework.Windows.Diagnostics.TaskManagerReplacementService"), Is.Null, "Task Manager replacement policy belongs to its application.");
		Assert.That(FileHandleService.WorkerArgument, Does.StartWith("--sphere10-").And.Not.Contain("systemexpert"));
		Assert.That(ProcessSignalService.WorkerArgument, Does.StartWith("--sphere10-").And.Not.Contain("systemexpert"));
	}

	[Test]
	public void ServiceSentryWrapsAnExecutableAndNativeServiceUtilityRemainsSeparate() {
		Assert.That(typeof(ServiceControlManagerSentry).BaseType, Is.EqualTo(typeof(ProcessSentry)));
		Assert.That(typeof(NssmSentry).BaseType, Is.EqualTo(typeof(ProcessSentry)));
		Assert.That(ServiceControlManagerSentry.ServiceControlExecutable, Is.EqualTo("sc.exe"));
		Assert.That(new ServiceControlManagerSentry().ExecutableFileName, Is.EqualTo("sc.exe"));
		Assert.That(typeof(ProcessSentry).IsAssignableFrom(typeof(ServiceUtil)), Is.False);
		Assert.That(typeof(Tools.Windows).GetProperty(nameof(Tools.Windows.Services)).PropertyType, Is.EqualTo(typeof(ServiceUtil)));
		var nativeMethods = new[] { "GetServices", "GetServicesAsync", "GetDrivers", "GetDriversAsync", "ReadService", "ReadServiceAsync", "ReadConfiguration",
			"ReadConfigurationAsync", "SetStartMode", "SetStartModeAsync", "StartService", "StopService", "PauseService", "ResumeService" };
		Assert.That(typeof(ServiceControlManagerSentry).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
			.Select(method => method.Name).Where(nativeMethods.Contains), Is.Empty, "sc.exe commands and native SCM operations must have distinct owners.");
	}

	[Test]
	public void NewWindowsUtilitiesUseInstanceFacadesAndDoNotExportApiAliases() {
		var utilities = new[] { (Property: "Network", Type: typeof(WindowsNetworkUtil)), (Property: "Drives", Type: typeof(WindowsDriveUtil)),
			(Property: "Startup", Type: typeof(WindowsStartupUtil)), (Property: "EventLogs", Type: typeof(WindowsEventLogUtil)), (Property: "Services", Type: typeof(ServiceUtil)),
			(Property: "Environment", Type: typeof(EnvironmentUtil)), (Property: "SecurityDescriptors", Type: typeof(SecurityDescriptorUtil)) };
		foreach (var utility in utilities) {
			var property = typeof(Tools.Windows).GetProperty(utility.Property);
			Assert.That(property, Is.Not.Null, utility.Property);
			Assert.That(property.PropertyType, Is.EqualTo(utility.Type));
			Assert.That(utility.Type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly), Is.Empty,
				utility.Type.Name + " exposes operations through its instance facade.");
		}
		var removedNames = new[] { "ServicesUtil", "WindowsNetworkApi", "WindowsDriveApi", "WindowsStartupApi", "WindowsEventLogApi", "EnvironmentVariableService", "SecurityDescriptorTools" };
		Assert.That(typeof(WinAPI).Assembly.GetExportedTypes().Where(type => removedNames.Contains(type.Name)).Select(type => type.FullName), Is.Empty);
	}

	[Test]
	public void UpdatedWindowsOperationMethodsAcceptTokensOnlyWithAsyncCompletion() {
		var types = new[] { typeof(RegistryUtil), typeof(ServiceUtil), typeof(WindowsNetworkUtil), typeof(WindowsDriveUtil), typeof(WindowsStartupUtil),
			typeof(WindowsEventLogUtil), typeof(EnvironmentUtil), typeof(SecurityDescriptorUtil), typeof(WindowsUninstallRegistration), typeof(DriverManager), typeof(WindowsFile), typeof(WindowsVolume),
			typeof(WindowsProcess), typeof(ProcessSignaler), typeof(WindowsSystemInformation), typeof(WindowsComputer), typeof(FileHandleService),
			typeof(WindowsFileInspectionService), typeof(ProcessCommandService), typeof(ProcessSignalService), typeof(WindowMessageSender), typeof(WindowsWindow) };
		var violations = types.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)) && !HasAsyncCompletion(method.ReturnType))
			.Select(method => method.DeclaringType.FullName + "." + method.Name).OrderBy(name => name).ToArray();
		Assert.That(violations, Is.Empty, "Synchronous operation methods, including worker helpers, must not expose a CancellationToken parameter.");
	}

	private static bool HasAsyncCompletion(Type type)
		=> typeof(Task).IsAssignableFrom(type) || type == typeof(ValueTask) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);

	[Test]
	public void WindowsLibraryDoesNotExportApplicationCollectorsModelsOrPlans() {
		var misplaced = typeof(WinAPI).Assembly.GetExportedTypes().Where(type => _appTypeNames.Contains(type.Name, StringComparer.Ordinal)).Select(type => type.FullName).ToArray();
		Assert.That(misplaced, Is.Empty, "Collectors, presentation rows, migration workflows and action plans belong to their consuming application.");
	}

	[TestCaseSource(nameof(NativeDataTypes))]
	public void NativeContractsHaveNoPresentationOrSerializationNavigationAttributes(Type type) {
		var forbidden = new[] { "DisplayNameAttribute", "DescriptionAttribute", "ReadOnlyAttribute", "TypeConverterAttribute", "EditorAttribute", "CategoryAttribute", "BrowsableAttribute", "JsonIgnoreAttribute" };
		var members = new MemberInfo[] { type }.Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)).ToArray();
		var decorations = members.SelectMany(member => member.CustomAttributes.Where(attribute => forbidden.Contains(attribute.AttributeType.Name))
			.Select(attribute => member.Name + ": " + attribute.AttributeType.Name)).ToArray();
		Assert.That(decorations, Is.Empty, "Native data represents Windows contracts; application projections own display and serialization policy.");
		Assert.That(type.Assembly, Is.SameAs(typeof(WinAPI).Assembly));
	}

	[Test]
	public void NativeFacadeContainersUseTheirDllNamesAndDoNotRetainDomainAliases() {
		var removed = new[] { "WINDOW_MESSAGES", "ENVIRONMENT_NOTIFICATIONS", "REGISTRY_EDITING", "CONSOLE_SIGNALS", "FILE_COPY", "SECURITY_TOKENS", "SYSTEM_DIAGNOSTICS", "FILE_DIAGNOSTICS", "PIPE_IDENTITY" };
		var containers = typeof(WinAPI).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
		Assert.That(containers.Where(type => removed.Contains(type.Name)).Select(type => type.FullName), Is.Empty);
		foreach (var container in containers.Where(type => type.IsClass))
			foreach (var method in container.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				var import = method.GetCustomAttribute<DllImportAttribute>();
				if (import == null)
					continue;
				Assert.That(container.Name, Is.EqualTo(Path.GetFileNameWithoutExtension(import.Value).ToUpperInvariant()),
					$"WinAPI.{container.Name}.{method.Name} must be declared under the DLL it imports ({import.Value}).");
			}
	}

	private static string[] FrameworkDependencyClosure(Assembly root) {
		var references = new HashSet<string>(StringComparer.Ordinal);
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var pending = new Queue<Assembly>();
		pending.Enqueue(root);
		while (pending.Count > 0) {
			var assembly = pending.Dequeue();
			var name = assembly.GetName().Name;
			references.Add(name);
			if (!visited.Add(name))
				continue;
			foreach (var reference in assembly.GetReferencedAssemblies()) {
				// Record direct references and traverse framework libraries without loading unrelated platform assemblies.
				references.Add(reference.Name);
				if (reference.Name.StartsWith("Sphere10.", StringComparison.Ordinal))
					pending.Enqueue(Assembly.Load(reference));
			}
		}
		return references.OrderBy(name => name, StringComparer.Ordinal).ToArray();
	}
}
