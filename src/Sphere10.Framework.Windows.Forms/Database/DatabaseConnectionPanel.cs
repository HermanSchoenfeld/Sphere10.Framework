// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;

namespace Sphere10.Framework.Windows.Forms;

public partial class DatabaseConnectionPanel : ConnectionPanelBase, IDatabaseConnectionProvider {
	private DBMSType? _currentDBMSType;

	public event EventHandlerEx<DatabaseConnectionPanel, DBMSType> DBMSTypeChanged;

	public DatabaseConnectionPanel() {
		InitializeComponent();
		_dbmsCombo.Filter = _dbmsCombo.Filter = x => Sphere10Framework.Instance.ServiceProvider.HasNamedService<ConnectionPanelBase>(x.ToString());
		_dbmsCombo.EnumType = typeof(DBMSType);
		SelectDefaultPanel();
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

	//[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	//[Browsable(false)]
	public override string ConnectionString {
		get => CurrentConnectionPanel.ConnectionString;
		set => CurrentConnectionPanel.ConnectionString = value;
	}

	public virtual DBReference Database => new DBReference {
		DBMSType = SelectedDBMSType,
		ConnectionString = ConnectionString
	};

	public override string DatabaseName => CurrentConnectionPanel.DatabaseName;

	protected virtual void OnDBMSTypeChanged() {
	}

	protected virtual void SelectDefaultPanel() {
		if (_dbmsCombo.Items.Count > 0)
			_dbmsCombo.SelectedIndex = 0;
	}

	protected override IDAC GetDACInternal() {
		return CurrentConnectionPanel.GetDAC();
	}

	public override Task<Result> TestConnection() {
		return CurrentConnectionPanel.TestConnection();
	}

	protected ConnectionPanelBase CurrentConnectionPanel { get; set; }

	protected virtual void _dbmsCombo_SelectedIndexChanged(object sender, EventArgs e) {
		var DbmsType = (DBMSType)_dbmsCombo.SelectedEnum;
		if (CurrentConnectionPanel != null && _currentDBMSType == DbmsType)
			return;
		_currentDBMSType = DbmsType;
		ChangeConnectionPanel(DbmsType);
	}

	protected void ChangeConnectionPanel(DBMSType dbmsType) {
		if (Tools.Runtime.IsDesignMode)
			return;
		var Lookup = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<INamedLookup<ConnectionPanelBase>>();
		var ConnectionPanel = Lookup[dbmsType.ToString()];
		if (ConnectionPanel == null)
			throw new InvalidOperationException($"No ConnectionPanel registered for DBMS type: {dbmsType}");
		ChangeConnectionPanel(ConnectionPanel);
	}

	protected void ChangeConnectionPanel(ConnectionPanelBase connectionPanel) {
		if (CurrentConnectionPanel != null)
			_connectionProviderPanel.Controls.Remove(CurrentConnectionPanel);
		CurrentConnectionPanel = connectionPanel;
		CurrentConnectionPanel.Dock = DockStyle.Fill;
		CurrentConnectionPanel.DockPadding.All = 0;
		CurrentConnectionPanel.Margin = Padding.Empty;
		_connectionProviderPanel.Controls.Add(CurrentConnectionPanel);
		RaiseDBMSTypeChangedEvent();
	}

	private void RaiseDBMSTypeChangedEvent() {
		OnDBMSTypeChanged();
		DBMSTypeChanged?.Invoke(this, SelectedDBMSType);
	}
	
}
