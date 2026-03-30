// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using Oracle.ManagedDataAccess.Client;
using Sphere10.Framework.Data;

namespace Sphere10.Framework.Windows.Forms.Oracle;

public partial class OracleConnectionBar : ConnectionBarBase {
	public OracleConnectionBar() {
		InitializeComponent();
	}

	protected override IDAC GetDACInternal() {
		return new OracleDAC(ConnectionString);
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public override string ConnectionString {
		get {
			var host = _serverTextBox.Text;
			var service = _databaseTextBox.Text;
			var userId = _usernameTextBox.Text;
			var password = _passwordTextBox.Text;
			var port = Tools.Parser.Parse<int?>(_portTextBox.Text);
			return Tools.Oracle.CreateConnectionString(dataSource: host, serviceName: service, userId: userId, password: password, port: port);
		}
		set {
			var builder = new OracleConnectionStringBuilder(value);
			_usernameTextBox.Text = builder.UserID;
			_passwordTextBox.Text = builder.Password;
			// DataSource may contain TNS descriptor; display as-is
			_serverTextBox.Text = builder.DataSource;
			_databaseTextBox.Text = string.Empty;
			_portTextBox.Text = string.Empty;
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
