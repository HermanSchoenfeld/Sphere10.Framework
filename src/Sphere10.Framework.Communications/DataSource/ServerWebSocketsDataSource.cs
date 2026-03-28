// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: David Price
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sphere10.Framework.Communications;

public class ServerWebSocketsDataSource<TItem> : ProtocolChannelDataSource<TItem> {

	private readonly Dictionary<string, TItem> Items = new Dictionary<string, TItem>();
	private InitializeDelegate InitializeItem { get; init; }
	private UpdateDelegate UpdateItem { get; set; }
	private IdDelegate IdItem { get; set; }
	private string ReceivedId { get; set; }
	private ServerWebSocketsChannelHub Hub { get; set; }

	public override Task<int> CountAsync => Task.FromResult(Items.Count);

	public ServerWebSocketsDataSource(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint, bool secure, InitializeDelegate initializeItem, UpdateDelegate updateItem, IdDelegate idItem)
		: base(new ServerWebSocketsChannel(localEndpoint, remoteEndpoint, secure, true)) {
		InitializeItem = initializeItem;
		UpdateItem = updateItem;
		IdItem = idItem;

		Hub = ((ServerWebSocketsChannel)ProtocolChannel).Hub;
		Hub.ReceivedBytes += ProtocolChannel_ReceivedBytes;
	}

	public string Report() => Hub.Report();

	public void CloseConnection(string id) => Hub.CloseConnection(id);

	public void Close() => Hub.Close();

	public void PublicReceiveBytes(ReadOnlyMemory<byte> bytes) {
		var packet = new WebSocketsPacket(bytes.ToArray());
		ReceivedId = packet.Id;

		if (!packet.Tokens.Any())
			return;

		switch (packet.Tokens[0]) {
			case "new":
				NewRange(int.Parse(packet.Tokens[1]));
				break;
			case "read": {
				var searchTerm = packet.Tokens[1];
				var pageLength = int.Parse(packet.Tokens[2]);
				var page = int.Parse(packet.Tokens[3]);
				var sortProperty = packet.Tokens[4];
				var sortDirection = (SortDirection)Enum.Parse(typeof(SortDirection), packet.Tokens[5]);
				Read(searchTerm, pageLength, ref page, sortProperty, sortDirection, out _);
			}
				break;
			case "update": {
				var toUpdate = JsonConvert.DeserializeObject<List<TItem>>(packet.JsonData);
				UpdateRange(toUpdate);
			}
				break;
			case "delete": {
				var toDelete = JsonConvert.DeserializeObject<List<TItem>>(packet.JsonData);
				DeleteRange(toDelete);
			}
				break;
			default:
				throw new Exception("Server received bad packet");
		}
	}

	private void ProtocolChannel_ReceivedBytes(ReadOnlyMemory<byte> bytes) => PublicReceiveBytes(bytes);

	public override IEnumerable<TItem> NewRange(int count) {
		var type = typeof(TItem);
		var newItems = new List<TItem>();
		for (var i = 0; i < count; i++) {
			var newInstance = (TItem)Activator.CreateInstance(type);
			var error = InitializeItem(newInstance, Items.Count + 1);
			if (!string.IsNullOrEmpty(error))
				continue;

			newItems.Add(newInstance);
			var id = IdItem(newInstance);
			Items[id] = newInstance;
			SystemLog.Info("New: " + JsonConvert.SerializeObject(newInstance));
		}

		var message = $"newreturn {Items.Count}";
		var jsonData = JsonConvert.SerializeObject(newItems);
		var returnPacket = new WebSocketsPacket(ReceivedId, message, jsonData);
		SendPacket(returnPacket, false);
		return newItems;
	}

	public Task<IEnumerable<TItem>> Read(string searchTerm, int pageLength, ref int page, string sortProperty, SortDirection sortDirection, out int totalItems) {
		var usePage = page;
		var list = Items.Values.ToList();
		totalItems = list.Count;

		return Task.Run(() => {
			var first = pageLength * usePage;
			if (first > list.Count) {
				first = list.Count - pageLength;
				if (first < 0)
					first = 0;
			}

			var count = pageLength;
			if (first + count > list.Count - 1)
				count = list.Count - first;

			var readItems = list.GetRange(first, count);
			var message = $"readreturn {list.Count}";
			var jsonData = JsonConvert.SerializeObject(readItems);
			var returnPacket = new WebSocketsPacket(ReceivedId, message, jsonData);
			SendPacket(returnPacket, false);
			SystemLog.Info($"Read: {jsonData}");
			return (IEnumerable<TItem>)readItems;
		});
	}

	public override Task CreateRangeAsync(IEnumerable<TItem> entities) {
		return Task.Run(() => {
			var created = new List<TItem>();
			foreach (var entity in entities) {
				var id = IdItem(entity);
				Items[id] = entity;
				created.Add(entity);
			}
			var packet = new WebSocketsPacket(ReceivedId, $"createreturn {Items.Count}", JsonConvert.SerializeObject(created));
			SendPacket(packet, true);
		});
	}

	public override Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		return Task.Run(() => {
			var list = Items.Values.ToList();
			var first = pageLength * page;
			if (first > list.Count) {
				first = list.Count - pageLength;
				if (first < 0)
					first = 0;
			}

			var count = pageLength;
			if (first + count > list.Count - 1)
				count = list.Count - first;

			if (count < 0)
				count = 0;

			var items = count == 0 ? new List<TItem>() : list.GetRange(first, count);
			return new DataSourceItems<TItem> {
				Items = items,
				Page = page,
				TotalCount = list.Count
			};
		});
	}

	public override Task RefreshRangeAsync(TItem[] entities) {
		return Task.CompletedTask;
	}

	public override Task UpdateRangeAsync(IEnumerable<TItem> entities) {
		return Task.Run(() => {
			var updatedEntities = new List<TItem>();
			foreach (var entity in entities) {
				var id = IdItem(entity);
				if (!Items.ContainsKey(id))
					continue;
				Items[id] = entity;
				updatedEntities.Add(entity);
				SystemLog.Info("Updated: " + JsonConvert.SerializeObject(entity));
			}

			var message = $"updatereturn {Items.Count}";
			var jsonData = JsonConvert.SerializeObject(updatedEntities);
			var returnPacket = new WebSocketsPacket(ReceivedId, message, jsonData);
			SendPacket(returnPacket, true);
		});
	}

	public override Task DeleteRangeAsync(IEnumerable<TItem> entities) {
		return Task.Run(() => {
			var deletedEntities = new List<TItem>();
			foreach (var entity in entities) {
				var id = IdItem(entity);
				if (!Items.ContainsKey(id))
					continue;
				Items.Remove(id);
				deletedEntities.Add(entity);
				SystemLog.Info("Deleted: " + JsonConvert.SerializeObject(entity));
			}

			var message = $"deletereturn {Items.Count}";
			var jsonData = JsonConvert.SerializeObject(deletedEntities);
			var returnPacket = new WebSocketsPacket(ReceivedId, message, jsonData);
			SendPacket(returnPacket, true);
		});
	}

	public override Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions) {
		throw new NotImplementedException();
	}

	private void SendPacket(WebSocketsPacket packet, bool all) {
		if (Hub != null)
			Hub.TrySendBytes(packet, all);
		else
			ProtocolChannel.TrySendBytes(packet.ToBytes());
	}

	public void NewDelayed(int count) => throw new NotImplementedException();
	public void CreateDelayed(IEnumerable<TItem> entities) => throw new NotImplementedException();
	public void RefreshDelayed(IEnumerable<TItem> entities) => throw new NotImplementedException();
	public void UpdateDelayed(IEnumerable<TItem> entities) => throw new NotImplementedException();
	public void DeleteDelayed(IEnumerable<TItem> entities) => throw new NotImplementedException();
	public void ValidateDelayed(IEnumerable<(TItem entity, CrudAction action)> actions) => throw new NotImplementedException();
	public void CountDelayed() => throw new NotImplementedException();
}

