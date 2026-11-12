// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;

namespace Sphere10.Framework.Application;

/// <summary>Persists each settings record as a JSON file in the configured directory.</summary>
public class DirectoryFileSettingsProvider : SyncSettingsProviderBase {

	private readonly IFuture<string> _baseDirectory;

	/// <summary>Uses a fixed directory and the requested file-naming policy.</summary>
	public DirectoryFileSettingsProvider(string directory, DirectorySettingsProviderPolicy policy = DirectorySettingsProviderPolicy.Default)
		: this(Tools.Values.Future.Explicit(directory), policy) {
	}

	/// <summary>Resolves the directory when settings are accessed and applies the requested file-naming policy.</summary>
	public DirectoryFileSettingsProvider(IFuture<string> directory, DirectorySettingsProviderPolicy policy = DirectorySettingsProviderPolicy.Default) {
		_baseDirectory = directory;
		Policy = policy;
	}

	/// <summary>The resolved directory containing this provider's settings files.</summary>
	public string BaseDirectory => _baseDirectory.Value;

	/// <summary>The policy used to derive file names from settings types and identities.</summary>
	public DirectorySettingsProviderPolicy Policy { get; protected set; }

	/// <inheritdoc />
	public override bool ContainsSetting(Type settingsObjectType, object id = null) {
		var path = DetermineFilepath(settingsObjectType, id);
		if (!File.Exists(path))
			return false;

		if (!Tools.FileSystem.IsFileEmpty(path))
			return true;

		// Empty records are unusable and should not survive an existence check.
		File.Delete(path);
		return false;
	}

	/// <inheritdoc />
	public override void DeleteSetting(SettingsObject settings) {
		var filename = DetermineFilepath(settings);
		if (File.Exists(filename))
			File.Delete(filename);

		FireChanged();
	}

	/// <inheritdoc />
	public override void ClearSettings() {
		if (Directory.Exists(BaseDirectory))
			foreach (var filePath in Directory.GetFiles(BaseDirectory))
				if (Path.GetExtension(filePath).ToUpper() == ".SETTING")
					File.Delete(filePath);

		FireChanged();
	}

	/// <inheritdoc />
	protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
		var file = DetermineFilepath(settingsObjectType, id);
		if (Tools.Xml.IsXmlFile(file))
			return Tools.Xml.DeepReadFromFile<SettingsObject>(file);

		using var stream = new FileStream(file, FileMode.Open);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return (SettingsObject)new SettingsObjectJsonSerializer().Deserialize(reader, settingsObjectType);
	}

	/// <inheritdoc />
	protected override SettingsObject LoadInternal(SettingsObject defaults) {
		// Populate known defaults so omitted values retain registration-specific initialization.
		var file = DetermineFilepath(defaults);
		if (Tools.Xml.IsXmlFile(file))
			return LoadInternal(defaults.GetType(), defaults.ID);

		using var stream = new FileStream(file, FileMode.Open);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		using var jsonReader = new Newtonsoft.Json.JsonTextReader(reader);
		new SettingsObjectJsonSerializer().Populate(jsonReader, defaults);
		return defaults;
	}

	/// <inheritdoc />
	protected override void SaveInternal(SettingsObject settings) {
		if (Policy.HasFlag(DirectorySettingsProviderPolicy.CreateDirectoryIfNotExists) && !Directory.Exists(BaseDirectory))
			Directory.CreateDirectory(BaseDirectory);

		var filePath = DetermineFilepath(settings);
		var fileExists = File.Exists(filePath);
		try {
			using var stream = new FileStream(filePath, FileMode.Create);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			new SettingsObjectJsonSerializer { Formatting = Newtonsoft.Json.Formatting.Indented }.Serialize(writer, settings);
		} catch {
			// A failed first save must not leave a partial record that appears to exist.
			if (!fileExists && File.Exists(filePath))
				Tools.Exceptions.ExecuteIgnoringException(() => File.Delete(filePath));

			throw;
		}
	}

	#region Auxiliary Methods

	#region Auxillary methods

	// Resolve a settings value through the same type-and-identity file naming policy.
	protected string DetermineFilepath(SettingsObject settings) => DetermineFilepath(settings.GetType(), settings.ID);

	// Combine the current directory with the file name derived from a settings identity.
	protected string DetermineFilepath(Type settingsObjectType, object id = null) => Path.Combine(BaseDirectory, DetermineFilename(settingsObjectType, id));

	// Resolve the stored file name for an existing settings value.
	protected virtual string DetermineFilename(SettingsObject settingsObject) {
		Guard.ArgumentNotNull(settingsObject, "settingsObject");

		return DetermineFilename(settingsObject.GetType(), settingsObject.ID);
	}

	// Apply the configured type naming policy, optional identity and settings extension.
	protected virtual string DetermineFilename(Type settingsObjectType, object id) {
		Guard.ArgumentNotNull(settingsObjectType, "settingsObjectType");

		var filename = Policy.HasFlag(DirectorySettingsProviderPolicy.UseFullyQualifiedTypeNameInFilename)
			? settingsObjectType.FullName
			: settingsObjectType.GetShortName();
		if (id != null)
			filename = CombineExtensionlessFilenameWithID(filename, id.ToString());

		return AddFileExtension(filename);
	}

	// Append a file-system-safe record identity to its type-derived file name.
	protected virtual string CombineExtensionlessFilenameWithID(string extensionlessFilename, string id)
		=> extensionlessFilename + "_" + Tools.FileSystem.ToWellFormedFileName(id);

	// Add the extension used to identify this provider's settings files.
	protected virtual string AddFileExtension(string partialFilename) => partialFilename + ".setting";

	#endregion

	#endregion
}
