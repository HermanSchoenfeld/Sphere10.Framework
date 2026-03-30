// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using MySqlConnector;
using Sphere10.Framework.Data;

namespace Sphere10.Framework.Windows.Forms.MySQL;

public partial class MySQLConnectionPanel : ConnectionPanelBase {
	public MySQLConnectionPanel() {
		InitializeComponent();
	}

	protected override IDAC GetDACInternal() {
		return new MySQLDAC(ConnectionString);
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public override string ConnectionString {
		get {
			var builder = new MySqlConnectionStringBuilder();
			builder.Server = _serverTextBox.Text;
			builder.Database = _databaseTextBox.Text;
			builder.UserID = _usernameTextBox.Text;
			builder.Password = _passwordTextBox.Text;
			var port = Tools.Parser.SafeParse<int?>(_portTextBox.Text);
			if (port.HasValue)
				builder.Port = (uint)port.Value;
			return builder.ToString();
		}
		set {
			var builder = new MySqlConnectionStringBuilder(value);
			_serverTextBox.Text = builder.Server;
			_databaseTextBox.Text = builder.Database;
			_usernameTextBox.Text = builder.UserID;
			_passwordTextBox.Text = builder.Password;
			_portTextBox.Text = builder.Port != 3306 ? builder.Port.ToString() : string.Empty;
		}
	}

	public override string DatabaseName => Database;

	public string Server {
		get { return _serverTextBox.Text; }
		set { _serverTextBox.Text = value; }
	}

	public string Database {
		get { return _databaseTextBox.Text; }
		set { _databaseTextBox.Text = value; }
	}

	public string Username {
		get { return _usernameTextBox.Text; }
		set { _usernameTextBox.Text = value; }
	}

	public string Password {
		get { return _passwordTextBox.Text; }
		set { _passwordTextBox.Text = value; }
	}

	public string Port {
		get { return _portTextBox.Text; }
		set { _portTextBox.Text = value; }
	}
}
