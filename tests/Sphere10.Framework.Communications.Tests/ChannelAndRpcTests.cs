// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sphere10.Framework.Communications.RPC;

namespace Sphere10.Framework.Communications.Tests;

[TestFixture]
[NonParallelizable] // TcpSecurityPolicies holds process-wide state
public class ChannelAndRpcTests {

	[Test]
	public async Task AnonymousPipe_PlainTextLineCarriesNoBytes() {
		using var sender = new AnonymousPipeServerStream(PipeDirection.Out);
		using var read = new AnonymousPipeClientStream(PipeDirection.In, sender.ClientSafePipeHandle);
		using var writeServer = new AnonymousPipeServerStream(PipeDirection.Out);
		await using var channel = new ReceivingAnonymousPipe(read, writeServer);
		await channel.Open();
		var receivedStrings = new List<string>();
		channel.ReceivedString += receivedStrings.Add;

		await using (var writer = new StreamWriter(sender, leaveOpen: true) { AutoFlush = true }) {
			await writer.WriteLineAsync("plain text");
			await writer.WriteLineAsync(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
		}

		Assert.That(await channel.ReceiveBytes(CancellationToken.None), Is.Empty);
		Assert.That(await channel.ReceiveBytes(CancellationToken.None), Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(receivedStrings, Has.Member("plain text"));
	}

	[Test]
	public void TcpEndPointListener_AcceptedEndPointUsesListenerMessageSize() {
		var listener = new TcpEndPointListener(true, 0, 1) { MaxMessageSize = 8192 };
		listener.Start();
		try {
			var port = int.Parse(listener.GetDescription().Split(':')[1]);
			using var client = new TcpClient();
			client.Connect(IPAddress.Loopback, port);
			var accepted = (TcpEndPoint)listener.WaitForMessage();
			Assert.That(accepted.MaxMessageSize, Is.EqualTo(8192));
			accepted.Stop();
		} finally {
			listener.Stop();
		}
	}

	[Test]
	public void TcpSecurityPolicies_RejectedConnectionReleasesItsSlot() {
		var maxConnections = TcpSecurityPolicies.MaxSimultanousConnecitons;
		try {
			TcpSecurityPolicies.MaxSimultanousConnecitons = 0;
			Assert.That(() => TcpSecurityPolicies.ValidateConnectionCount(TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionOpen), Throws.TypeOf<TooManyConnectionsException>());
			TcpSecurityPolicies.MaxSimultanousConnecitons = 1;
			Assert.That(() => TcpSecurityPolicies.ValidateConnectionCount(TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionOpen), Throws.Nothing);
			TcpSecurityPolicies.ValidateConnectionCount(TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionClose);
		} finally {
			TcpSecurityPolicies.MaxSimultanousConnecitons = maxConnections;
		}
	}

	[Test]
	public void EndpointMessage_ToSafeStringDropsControlCharactersButKeepsLineBreaks() {
		var message = new EndpointMessage(new byte[] { (byte)'a', 1, (byte)'b', 10, (byte)'c', 27, (byte)'d' });
		Assert.That(message.ToSafeString(), Is.EqualTo("ab\ncd"));
	}

	[Test]
	public void ApiService_ServiceExceptionMessageIsReported() {
		var api = new ApiService();
		api.AddApi(new FailingService());
		var response = api.Call("failing.fail", new List<object> { JToken.FromObject(1) });
		Assert.That(response.Error, Is.Not.Null);
		Assert.That(response.Error.code, Is.EqualTo(-9));
		Assert.That(response.Error.text, Does.Contain(FailingService.FailureMessage));
	}

	[Test]
	public void JsonResponse_UsesJsonRpc2ErrorAndIdMembers() {
		var json = JsonConvert.SerializeObject(new JsonResponse { Error = new JsonRpcException(-9, "failure"), Id = "request-1" });
		Assert.That(json, Does.Contain("\"message\":\"failure\""));
		Assert.That(json, Does.Contain("\"id\":\"request-1\""));
		Assert.That(JsonConvert.DeserializeObject<JsonResponse>(json).Error.text, Is.EqualTo("failure"));
	}

	[Test]
	public async Task UDPServer_RunsAndStops() {
		Assert.That(new UDPServer(45678).Port, Is.EqualTo(45678));
		await using var server = new UDPServer(0);
		var run = server.Run();
		Assert.That(server.Running, Is.True);
		server.Stop();
		Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(run));
		await run;
		Assert.That(server.Running, Is.False);
	}

	[RpcAPIService("failing")]
	private sealed class FailingService {
		public const string FailureMessage = "The service failed.";

		[RpcAPIMethod]
		public int Fail(int value) => throw new InvalidOperationException(FailureMessage);
	}

	private sealed class ReceivingAnonymousPipe(PipeStream read, PipeStream write) : AnonymousPipe {
		public override CommunicationRole LocalRole => CommunicationRole.Client;

		public Task<byte[]> ReceiveBytes(CancellationToken cancellationToken) => ReceiveBytesInternal(cancellationToken);

		protected override Task<(AnonymousPipeEndpoint, PipeStream, PipeStream)> OpenPipeInternal()
			=> Task.FromResult((AnonymousPipeEndpoint.Empty, read, write));

		protected override void StartReceiveLoop() {
		}

		protected override Task StopReceiveLoop() => Task.CompletedTask;
	}
}
