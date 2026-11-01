// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using CommandLine.Core;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class DatabaseConnectionBar : ConnectionBarBase, IDatabaseConnectionProvider {
	private DBMSType? _currentDBMSType;

	public DatabaseConnectionBar() {
		InitializeComponent();
		_dbmsCombo.Filter = x => Sphere10Framework.Instance.ServiceProvider.HasNamedService<ConnectionBarBase>(x.ToString());
		_dbmsCombo.EnumType = typeof(DBMSType);
		SelectDefaultBar();
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DBMSType SelectedDBMSType {
		get => (DBMSType)_dbmsCombo.SelectedEnum;
		set => _dbmsCombo.SelectedEnum = value;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DBMSType[] IgnoreDBMS {
		get => _dbmsCombo.IgnoreEnums.Cast<DBMSType>().ToArray();
		set => _dbmsCombo.IgnoreEnums = (value ?? new DBMSType[0]).Cast<object>().ToArray();
	}


	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public override string ConnectionString {
		get => CurrentConnectionBar.ConnectionString;
		set => CurrentConnectionBar.ConnectionString = value;
	}


	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public override string DatabaseName => CurrentConnectionBar.DatabaseName;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public virtual DBReference Database {
		get => new() {
			DBMSType = SelectedDBMSType,
			ConnectionString = ConnectionString
		};
		set {
			this.SelectedDBMSType = value.DBMSType;
			this.ConnectionString = value.ConnectionString;
		}
	}

	protected virtual void SelectDefaultBar() {
		if (_dbmsCombo.Items.Count > 0)
			_dbmsCombo.SelectedIndex = 0;
	}

	protected override IDAC GetDACInternal() {
		return CurrentConnectionBar.GetDAC();
	}

	public override Task<Result> TestConnection() {
		return CurrentConnectionBar.TestConnection();
	}

	protected ConnectionBarBase CurrentConnectionBar { get; set; }

	protected virtual void _dbmsCombo_SelectedIndexChanged(object sender, EventArgs e) {
		var DbmsType = (DBMSType)_dbmsCombo.SelectedEnum;
		if (CurrentConnectionBar != null && _currentDBMSType == DbmsType)
			return;
		_currentDBMSType = DbmsType;
		ChangeConnectionBar(DbmsType);
	}

	protected void ChangeConnectionBar(DBMSType dbmsType) {
		if (Tools.Runtime.IsDesignMode)
			return;
		var Lookup = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<INamedLookup<ConnectionBarBase>>();
		var ConnectionBar = Lookup[dbmsType.ToString()];
		if (ConnectionBar == null)
			throw new InvalidOperationException($"No ConnectionBar registered for DBMS type: {dbmsType}");
		ChangeConnectionBar(ConnectionBar);
	}

	protected void ChangeConnectionBar(ConnectionBarBase connectionBar) {
		if (CurrentConnectionBar != null)
			_connectionProviderPanel.Controls.Remove(CurrentConnectionBar);
		CurrentConnectionBar = connectionBar;
		CurrentConnectionBar.Location = new Point(0, (_connectionProviderPanel.Height - CurrentConnectionBar.Height).ClipTo(0, int.MaxValue));
		CurrentConnectionBar.Dock = DockStyle.Fill;
		CurrentConnectionBar.DockPadding.All = 0;
		CurrentConnectionBar.Width = _connectionProviderPanel.Width;
		_connectionProviderPanel.Controls.Add(CurrentConnectionBar);
	}
	
}
