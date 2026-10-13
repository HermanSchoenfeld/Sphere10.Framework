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

namespace Sphere10.Framework.Utils.Profiler.BTrees;

public sealed class BTreeProfilerSettings {
	private const string EnvironmentKey = "SPHERE10_BTREE_PROFILER_SETTINGS";
	private static readonly Lazy<BTreeProfilerSettings> CurrentSettings = new(ReadCurrent);

	public static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public int RecordCount { get; set; } = 10000;

	public int OperationCount { get; set; } = 256;

	public int GroupSize { get; set; } = 8;

	public bool ShuffledInsertion { get; set; } = true;

	public string Mix { get; set; } = "balanced";

	public string RunId { get; set; } = Guid.NewGuid().ToString("N");

	public string OutputDirectory { get; set; }

	public string DataDirectory { get; set; }

	public BTreeProfile[] Profiles { get; set; } = Array.Empty<BTreeProfile>();

	public static BTreeProfilerSettings Current => CurrentSettings.Value;

	public BTreeProfile GetProfile(string name) => Profiles.Single(profile => profile.Name == name);

	public string[] GetProfileNames(bool lookup = false, bool files = false, bool transactional = false)
		=> Profiles.Where(profile => (profile.Family == BTreeFamily.Lookup) == lookup
			&& (!files || profile.Storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory))
			&& (!transactional || profile.Storage == BTreeStorage.TransactionalFile)).Select(profile => profile.Name).ToArray();

	public void Validate() {
		Guard.ArgumentInRange(RecordCount, 1, 1000000, nameof(RecordCount));
		Guard.ArgumentInRange(OperationCount, 1, RecordCount, nameof(OperationCount));
		Guard.ArgumentInRange(GroupSize, 1, RecordCount, nameof(GroupSize));
		Guard.Argument(Mix is "balanced" or "read-heavy" or "write-heavy", nameof(Mix), "Mix must be balanced, read-heavy or write-heavy.");
		Guard.Argument(RunId is { Length: 32 } && RunId.All(Uri.IsHexDigit), nameof(RunId), "A run ID must contain 32 hexadecimal digits.");
		Guard.ArgumentNotNullOrWhitespace(OutputDirectory, nameof(OutputDirectory));
		Guard.ArgumentNotNullOrWhitespace(DataDirectory, nameof(DataDirectory));
		Guard.ArgumentNotNullOrEmpty(Profiles, nameof(Profiles));
		Guard.Argument(Profiles.All(profile => profile != null), nameof(Profiles), "Null profiles are not supported.");
		Guard.Argument(Profiles.Select(profile => profile.Name).Distinct(StringComparer.Ordinal).Count() == Profiles.Length, nameof(Profiles), "Profile names must be unique.");
		foreach (var profile in Profiles)
			profile.Validate();
	}

	public void Publish() {
		Validate();
		Environment.SetEnvironmentVariable(EnvironmentKey, JsonSerializer.Serialize(this, JsonOptions));
	}

	public static BTreeProfilerSettings Parse(string[] args, out string[] benchmarkArgs, out bool showConfiguration) {
		var settings = new BTreeProfilerSettings();
		var suite = "baseline";
		var storage = BTreeStorage.FileStream;
		var family = BTreeFamily.BTreePlus;
		var keyShape = BTreeKeyShape.Int64;
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
				case "--storage": storage = Tools.Enums.ParseEnum<BTreeStorage>(Value(), true); break;
				case "--family": family = Tools.Enums.ParseEnum<BTreeFamily>(Value(), true); break;
				case "--keys": keyShape = Tools.Enums.ParseEnum<BTreeKeyShape>(Value(), true); break;
				case "--records": settings.RecordCount = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--operations": settings.OperationCount = int.Parse(Value(), CultureInfo.InvariantCulture); operationsSpecified = true; break;
				case "--group-size": settings.GroupSize = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--mix": settings.Mix = Value().ToLowerInvariant(); break;
				case "--insertion": {
					var insertion = Value().ToLowerInvariant();
					Guard.Argument(insertion is "shuffled" or "sequential", nameof(args), "Insertion must be shuffled or sequential.");
					settings.ShuffledInsertion = insertion == "shuffled";
					break;
				}
				case "--config": profileFile = Path.GetFullPath(Value()); break;
				case "--profiles": profileNames = Value(); break;
				case "--output": settings.OutputDirectory = Path.GetFullPath(Value()); break;
				case "--data-root": settings.DataDirectory = Path.GetFullPath(Value()); break;
				case "--show-config": showConfiguration = true; break;
				default: forwarded.Add(argument); break;
			}
		}
		settings.Profiles = profileFile == null ? BTreeProfile.CreateSuite(suite, storage, family, keyShape)
			: JsonSerializer.Deserialize<BTreeProfile[]>(File.ReadAllText(profileFile), JsonOptions);
		Guard.ArgumentNotNullOrEmpty(settings.Profiles, nameof(args));
		Guard.Argument(settings.Profiles.All(profile => profile != null), nameof(args), "Null profiles are not supported.");
		if (profileNames != null) {
			var names = profileNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			Guard.Argument(names.All(name => settings.Profiles.Any(profile => profile.Name == name)), nameof(args), "A selected profile does not exist.");
			settings.Profiles = settings.Profiles.Where(profile => names.Contains(profile.Name, StringComparer.Ordinal)).ToArray();
		}
		if (!operationsSpecified)
			settings.OperationCount = Math.Min(settings.RecordCount, settings.OperationCount);
		settings.GroupSize = Math.Min(settings.GroupSize, settings.RecordCount);
		settings.OutputDirectory ??= Path.GetFullPath(Path.Combine("BenchmarkDotNet.Artifacts", $"btrees-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
		settings.DataDirectory ??= Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Sphere10-BTreeProfiler"));
		settings.Validate();
		benchmarkArgs = forwarded.ToArray();
		return settings;
	}

	private static BTreeProfilerSettings ReadCurrent() {
		var json = Environment.GetEnvironmentVariable(EnvironmentKey);
		Guard.Ensure(!string.IsNullOrWhiteSpace(json), "Configure the B-tree profiler through its domain runner.");
		var settings = JsonSerializer.Deserialize<BTreeProfilerSettings>(json, JsonOptions);
		Guard.Ensure(settings != null, "B-tree profiler settings could not be read.");
		settings.Validate();
		return settings;
	}
}
