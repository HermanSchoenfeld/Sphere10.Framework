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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public sealed class ProfilerSettings {
	private const string EnvironmentKey = "SPHERE10_OBJECTSPACE_PROFILER_SETTINGS";
	private static readonly Lazy<ProfilerSettings> CurrentSettings = new(ReadCurrent);

	public static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public int RecordCount { get; set; } = 1000;

	public string RunId { get; set; } = Guid.NewGuid().ToString("N");

	public int PayloadBytes { get; set; } = 128;

	public int OperationCount { get; set; } = 256;

	public bool ShuffledInsertion { get; set; }

	public ObjectSpaceLookupPattern LookupPattern { get; set; } = ObjectSpaceLookupPattern.Shuffled;

	public int HotSetPercent { get; set; } = 10;

	public ObjectSpacePayloadPattern PayloadPattern { get; set; }

	public int CategoryCount { get; set; } = 16;

	public int HotCategoryPercent { get; set; }

	public ObjectSpaceWorkloadMix WorkloadMix { get; set; }

	public int MixedReadPercentage => WorkloadMix switch {
		ObjectSpaceWorkloadMix.ReadHeavy => 90,
		ObjectSpaceWorkloadMix.WriteHeavy => 10,
		_ => 50
	};

	public string OutputDirectory { get; set; }

	public string DataDirectory { get; set; }

	public bool Trace { get; set; }

	public ObjectSpaceProfile[] Profiles { get; set; } = Array.Empty<ObjectSpaceProfile>();

	public static ProfilerSettings Current => CurrentSettings.Value;

	public ObjectSpaceProfile GetProfile(string name) => Profiles.Single(profile => profile.Name == name);

	public string[] GetProfileNames(bool fileOnly = false, bool fullIndexesOnly = false)
		=> Profiles.Where(profile => (!fileOnly || profile.Configuration.Storage == ObjectSpaceLabStorage.File)
			&& (!fullIndexesOnly || profile.Configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full)).Select(profile => profile.Name).ToArray();

	public void Validate() {
		Guard.ArgumentInRange(RecordCount, 1, 1000000, nameof(RecordCount));
		Guard.ArgumentInRange(PayloadBytes, 0, 1048576, nameof(PayloadBytes));
		Guard.ArgumentInRange(OperationCount, 1, RecordCount, nameof(OperationCount));
		Guard.Argument(Enum.IsDefined(LookupPattern), nameof(LookupPattern), "Unknown lookup pattern.");
		Guard.Argument(Enum.IsDefined(PayloadPattern), nameof(PayloadPattern), "Unknown payload pattern.");
		Guard.Argument(Enum.IsDefined(WorkloadMix), nameof(WorkloadMix), "Unknown workload mix.");
		Guard.ArgumentInRange(HotSetPercent, 1, 100, nameof(HotSetPercent));
		Guard.ArgumentInRange(CategoryCount, 1, 1000000, nameof(CategoryCount));
		Guard.ArgumentInRange(HotCategoryPercent, 0, 99, nameof(HotCategoryPercent));
		Guard.Argument(HotCategoryPercent == 0 || CategoryCount > 1, nameof(CategoryCount), "A hot category requires at least two categories.");
		Guard.Argument(RunId is { Length: 32 } && RunId.All(Uri.IsHexDigit), nameof(RunId), "A run ID must contain 32 hexadecimal digits.");
		Guard.ArgumentNotNullOrWhitespace(OutputDirectory, nameof(OutputDirectory));
		Guard.ArgumentNotNullOrWhitespace(DataDirectory, nameof(DataDirectory));
		Guard.ArgumentNotNullOrEmpty(Profiles, nameof(Profiles));
		Guard.Argument(Profiles.All(profile => profile != null), nameof(Profiles), "Profiles must not contain null entries.");
		Guard.Argument(Profiles.Select(profile => profile.Name).Distinct(StringComparer.Ordinal).Count() == Profiles.Length, nameof(Profiles), "Profile names must be unique.");
		foreach (var profile in Profiles) {
			profile.Validate();
			Guard.Argument(profile.Configuration.SchemaProfile == ObjectSpaceLabSchemaProfile.PayloadOnly, nameof(Profiles),
				"The ObjectSpaces benchmarks measure the PayloadOnly record schema. DeveloperLab requires separate multi-dimension workloads.");
		}
	}

	public void Publish() {
		Validate();
		// BenchmarkDotNet children inherit the exact selected configuration, without consulting a mutable external file.
		Environment.SetEnvironmentVariable(EnvironmentKey, JsonSerializer.Serialize(this, JsonOptions));
	}

	public static ProfilerSettings Parse(string[] args, out string[] benchmarkArgs, out bool showConfiguration) {
		var settings = new ProfilerSettings();
		var suite = "baseline";
		var storage = ObjectSpaceLabStorage.Memory;
		var profileFile = (string)null;
		var profileNames = (string)null;
		var forwarded = new List<string>();
		var operationsSpecified = false;
		showConfiguration = false;
		for (var index = 0; index < args.Length; index++) {
			var argument = args[index];
			string Value() {
				Guard.Argument(index + 1 < args.Length, nameof(args), $"A value is required after '{argument}'.");
				return args[++index];
			}
			switch (argument) {
				case "--suite": suite = Value().ToLowerInvariant(); break;
				case "--storage": storage = Tools.Enums.ParseEnum<ObjectSpaceLabStorage>(Value(), true); break;
				case "--records": settings.RecordCount = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--payload": settings.PayloadBytes = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--operations": settings.OperationCount = int.Parse(Value(), CultureInfo.InvariantCulture); operationsSpecified = true; break;
				case "--insertion": {
					var insertion = Value().ToLowerInvariant();
					Guard.Argument(insertion is "sequential" or "shuffled", nameof(args), "Insertion must be sequential or shuffled.");
					settings.ShuffledInsertion = insertion == "shuffled";
					break;
				}
				case "--lookup": settings.LookupPattern = Tools.Enums.ParseEnum<ObjectSpaceLookupPattern>(Value(), true); break;
				case "--hot-set-percent": settings.HotSetPercent = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--payload-mode": settings.PayloadPattern = Tools.Enums.ParseEnum<ObjectSpacePayloadPattern>(Value(), true); break;
				case "--categories": settings.CategoryCount = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--hot-category-percent": settings.HotCategoryPercent = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--mix": settings.WorkloadMix = Tools.Enums.ParseEnum<ObjectSpaceWorkloadMix>(Value(), true); break;
				case "--config": profileFile = Path.GetFullPath(Value()); break;
				case "--profiles": profileNames = Value(); break;
				case "--output": settings.OutputDirectory = Path.GetFullPath(Value()); break;
				case "--data-root": settings.DataDirectory = Path.GetFullPath(Value()); break;
				case "--trace": settings.Trace = true; break;
				case "--show-config": showConfiguration = true; break;
				default: forwarded.Add(argument); break;
			}
		}
		settings.Profiles = profileFile == null ? ObjectSpaceProfile.CreateSuite(suite, storage)
			: JsonSerializer.Deserialize<ObjectSpaceProfile[]>(File.ReadAllText(profileFile), JsonOptions);
		Guard.ArgumentNotNullOrEmpty(settings.Profiles, nameof(args), "At least one configuration profile is required.");
		Guard.Argument(settings.Profiles.All(profile => profile != null), nameof(args), "Profiles must not contain null entries.");
		if (profileNames != null) {
			var selectedNames = profileNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			Guard.Argument(selectedNames.All(name => settings.Profiles.Any(profile => profile.Name == name)), nameof(args), "A selected profile does not exist in the suite or configuration file.");
			settings.Profiles = settings.Profiles.Where(profile => selectedNames.Contains(profile.Name, StringComparer.Ordinal)).ToArray();
		}
		if (!operationsSpecified)
			settings.OperationCount = Math.Min(settings.RecordCount, settings.OperationCount);
		settings.OutputDirectory ??= Path.GetFullPath(Path.Combine("BenchmarkDotNet.Artifacts", $"objectspaces-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
		settings.DataDirectory ??= Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Sphere10-ObjectSpaceProfiler"));
		settings.Validate();
		benchmarkArgs = forwarded.ToArray();
		return settings;
	}

	private static ProfilerSettings ReadCurrent() {
		var json = Environment.GetEnvironmentVariable(EnvironmentKey);
		Guard.Ensure(!string.IsNullOrWhiteSpace(json), "Run the profiler executable to configure benchmarks before executing them.");
		var settings = JsonSerializer.Deserialize<ProfilerSettings>(json, JsonOptions);
		Guard.Ensure(settings != null, "Profiler settings could not be read.");
		settings.Validate();
		return settings;
	}
}
