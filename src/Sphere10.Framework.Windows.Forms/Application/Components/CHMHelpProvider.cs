// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.IO;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public class CHMHelpProvider : IHelpServices {
	private static readonly object SyncObject;
	private string _chmFile;

	static CHMHelpProvider() {
		SyncObject = new object();
	}

	public CHMHelpProvider(IUserInterfaceServices userInterfaceServices, IProductInformationProvider productInformationProvider) {

		UserInterfaceServices = userInterfaceServices;
		ProductInformationProvider = productInformationProvider;
		CHMFile = null;


	}

	public IUserInterfaceServices UserInterfaceServices { get; private set; }

	public IProductInformationProvider ProductInformationProvider { get; private set; }

	public string CHMFile {
		get {
			if (_chmFile == null) {
				lock (SyncObject) {
					if (_chmFile == null) {
						var chmQuery = ProductInformationProvider.ProductInformation.HelpResources.Where(hr => hr.Item1 == HelpType.CHM);
						if (!chmQuery.Any()) {
							throw new SoftwareException("No default CHM help file is defined");
						}
						CHMFile = chmQuery.First().Item2;
					}
				}
			}
			return _chmFile;
		}
		private set {
			if (value == null) {
				_chmFile = null;
			} else {
				_chmFile = StringFormatter.FormatEx(value);
				if (!File.Exists(_chmFile)) {
					throw new SoftwareException("File does not exist '{0}'", _chmFile);
				}
			}
		}
	}

	public void ShowContextHelp(IHelpableObject helpableObject) {
		System.Windows.Forms.Help.ShowHelp(
			UserInterfaceServices.PrimaryUIController as Control,
			File.Exists(helpableObject.FileName) ? helpableObject.FileName : CHMFile,
			System.Windows.Forms.HelpNavigator.TopicId,
			helpableObject.HelpTopicID.Value.ToString()
		);

	}

	public void ShowHelp() {
		System.Windows.Forms.Help.ShowHelp(
			UserInterfaceServices.PrimaryUIController as Control,
			CHMFile
		);
	}


}

