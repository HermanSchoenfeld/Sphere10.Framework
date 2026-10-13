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

namespace Sphere10.Framework.Utils.Profiler.Caches;

public sealed class CacheProfilerSettings {
	private const string EnvironmentKey = "SPHERE10_CACHE_PROFILER_SETTINGS";
	private static readonly Lazy<CacheProfilerSettings> CurrentSettings = new(ReadCurrent);

	public static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public int OperationCount { get; set; } = 8192;

	public int HotSetPercent { get; set; } = 10;

	public int HotAccessPercent { get; set; } = 80;

	public int FetchWork { get; set; }

	public string RunId { get; set; } = Guid.NewGuid().ToString("N");

	public string OutputDirectory { get; set; }

	public CacheProfile[] Profiles { get; set; } = Array.Empty<CacheProfile>();

	public static CacheProfilerSettings Current => CurrentSettings.Value;

	public CacheProfile GetProfile(string name) => Profiles.Single(profile => profile.Name == name);

	public string[] GetProfileNames() => Array.ConvertAll(Profiles, profile => profile.Name);

	public void Validate() {
		Guard.ArgumentInRange(OperationCount, 1, 1048576, nameof(OperationCount));
		Guard.ArgumentInRange(HotSetPercent, 1, 100, nameof(HotSetPercent));
		Guard.ArgumentInRange(HotAccessPercent, 0, 100, nameof(HotAccessPercent));
		Guard.ArgumentInRange(FetchWork, 0, 100000, nameof(FetchWork));
		Guard.Argument(RunId is { Length: 32 } && RunId.All(Uri.IsHexDigit), nameof(RunId), "A run ID must contain 32 hexadecimal digits.");
		Guard.ArgumentNotNullOrWhitespace(OutputDirectory, nameof(OutputDirectory));
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

	public static CacheProfilerSettings Parse(string[] args, out string[] benchmarkArgs, out bool showConfiguration) {
		var settings = new CacheProfilerSettings();
		var suite = "access";
		var capacity = 1024;
		var sizePattern = CacheSizePattern.Unit;
		var profileFile = (string)null;
		var profileNames = (string)null;
		var forwarded = new List<string>();
		showConfiguration = false;
		for (var index = 0; index < args.Length; index++) {
			var argument = args[index];
			string Value() {
				Guard.Argument(index + 1 < args.Length, nameof(args), $"A value is required after '{argument}'.");
				return args[++index];
			}
			switch (argument) {
				case "--suite": suite = Value().ToLowerInvariant(); break;
				case "--capacity": capacity = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--sizes": sizePattern = Tools.Enums.ParseEnum<CacheSizePattern>(Value(), true); break;
				case "--operations": settings.OperationCount = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--hot-set": settings.HotSetPercent = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--hot-access": settings.HotAccessPercent = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--fetch-work": settings.FetchWork = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--config": profileFile = Path.GetFullPath(Value()); break;
				case "--profiles": profileNames = Value(); break;
				case "--output": settings.OutputDirectory = Path.GetFullPath(Value()); break;
				case "--show-config": showConfiguration = true; break;
				default: forwarded.Add(argument); break;
			}
		}
		settings.Profiles = profileFile == null ? CacheProfile.CreateSuite(suite, capacity, sizePattern)
			: JsonSerializer.Deserialize<CacheProfile[]>(File.ReadAllText(profileFile), JsonOptions);
		Guard.ArgumentNotNullOrEmpty(settings.Profiles, nameof(args));
		Guard.Argument(settings.Profiles.All(profile => profile != null), nameof(args), "Null profiles are not supported.");
		if (profileNames != null) {
			var names = profileNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			Guard.Argument(names.All(name => settings.Profiles.Any(profile => profile.Name == name)), nameof(args), "A selected profile does not exist.");
			settings.Profiles = settings.Profiles.Where(profile => names.Contains(profile.Name, StringComparer.Ordinal)).ToArray();
		}
		settings.OutputDirectory ??= Path.GetFullPath(Path.Combine("BenchmarkDotNet.Artifacts", $"caches-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
		settings.Validate();
		benchmarkArgs = forwarded.ToArray();
		return settings;
	}

	private static CacheProfilerSettings ReadCurrent() {
		var json = Environment.GetEnvironmentVariable(EnvironmentKey);
		Guard.Ensure(!string.IsNullOrWhiteSpace(json), "Configure the cache profiler through its domain runner.");
		var settings = JsonSerializer.Deserialize<CacheProfilerSettings>(json, JsonOptions);
		Guard.Ensure(settings != null, "Cache profiler settings could not be read.");
		settings.Validate();
		return settings;
	}
}
