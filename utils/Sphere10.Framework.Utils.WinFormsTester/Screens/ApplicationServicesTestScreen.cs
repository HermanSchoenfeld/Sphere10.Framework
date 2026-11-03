// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Demonstrates the current application-owned services rather than an obsolete remote stub.</summary>
public partial class ApplicationServicesTestScreen : WinFormsApplicationScreen {
	/// <summary>Creates a resource-free service inspection surface.</summary>
	public ApplicationServicesTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Query the actual owning graph; inspection never constructs or takes ownership of services.
	// HS: 2019-02-24 disabled due to .net standard upgrade
	//var services = new SoftwareService2WebServiceClient();
	//services.Test();
	// The retired remote-service sample is replaced by inspection of the current application services.
	private void _callTestMethodButton_Click(object sender, EventArgs e) {
		if (Block?.Parent == null) {
			_status.Text = "Open this screen from the Tester to inspect its owning application.";
			return;
		}
		var application = Block.Parent;
		var output = new StringBuilder();
		output.AppendLine("Application: " + application.GetType().FullName);
		output.AppendLine("Logger: " + application.Logger.GetType().FullName);
		output.AppendLine("Declaring block: " + Block.Name);
		output.AppendLine("Plugin: " + Block.Source?.Name);
		output.AppendLine();
		foreach (var block in application.Blocks)
			output.AppendLine($"{block.Name}: {block.ScreenDefinitions.Length} screen registration(s)");
		_output.Text = output.ToString();
		_status.Text = "Inspected the existing application, blocks and logger.";
	}

	// The normal application logger observes the message through its configured sinks.
	private void LogClicked(object sender, EventArgs e) {
		if (Block?.Parent == null) {
			_status.Text = "An owning application is required for this example.";
			return;
		}
		Block.Parent.Logger.Info("Application Services demonstration: message written through the owning application.");
		_status.Text = "Message written through the application logger.";
	}
}
