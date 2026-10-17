// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Category("Integration")]
[NonParallelizable] // These tests temporarily set the process-wide SMTP certificate policy and restore it afterwards.
public class MailTlsIntegrationTests {

	[TestCase(false)]
	[TestCase(true)]
	public Task PlainSmtpDeliversMessage(bool useAsync) => VerifyDelivery(useAsync, false, false, false);

	[TestCase(false)]
	[TestCase(true)]
	public Task StartTlsDeliversWithExplicitTestCertificateTrust(bool useAsync) => VerifyDelivery(useAsync, true, true, true);

	[TestCase(false)]
	[TestCase(true)]
	public Task StartTlsRejectsUntrustedCertificateByDefault(bool useAsync) => VerifyDelivery(useAsync, true, true, false);

	[TestCase(false)]
	[TestCase(true)]
	public Task RequiredTlsRejectsServerWithoutStartTls(bool useAsync) => VerifyDelivery(useAsync, true, false, false);

	[TestCase(false)]
	[TestCase(true)]
	public async Task SendPreservesBorrowedContentDuringAndAfterDelivery(bool useAsync) {
		// Hold a real loopback SMTP exchange before its greeting to observe pending-send ownership.
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		using var cancelOperations = Tools.Scope.ExecuteOnDispose(cancellation.Cancel);
		var listener = new TcpListener(IPAddress.Loopback, 0);
		using var stopListener = Tools.Scope.ExecuteOnDispose(listener.Stop);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		var connectionAccepted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var allowGreeting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releaseGreeting = Tools.Scope.ExecuteOnDispose(() => allowGreeting.TrySetResult(true));
		using var attachmentStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic attachment"));
		using var attachment = new Attachment(attachmentStream, "attachment.txt");
		using var viewStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic alternate view"));
		using var view = new AlternateView(viewStream, "text/plain");
		var receiveTask = ReceiveDeferredMessage(listener, connectionAccepted, allowGreeting.Task, cancellation.Token);
		var sendTask = useAsync
			? Tools.Mail.SendEmailAsync("localhost", "sender@example.test", "Ownership regression", "Synthetic local message", new[] { "recipient@example.test" },
				attachments: new[] { attachment }, alternativeViews: new[] { view }, port: port)
			: Task.Run(() => Tools.Mail.SendEmail("localhost", "sender@example.test", "Ownership regression", "Synthetic local message", new[] { "recipient@example.test" },
				attachments: new[] { attachment }, alternativeViews: new[] { view }, port: port));
		await connectionAccepted.Task.WaitAsync(cancellation.Token);
		Assert.That(sendTask.IsCompleted, Is.False, "The send cannot complete before the SMTP server greeting.");
		Assert.That(attachmentStream.CanRead, Is.True, "The message must retain its attachment stream throughout the pending send.");
		Assert.That(viewStream.CanRead, Is.True, "The message must retain its alternate view throughout the pending send.");

		// Complete delivery and retain the supplied content for reuse by its caller.
		allowGreeting.SetResult(true);
		var message = await receiveTask;
		await sendTask.WaitAsync(cancellation.Token);
		Assert.That(message, Does.Contain("Subject: Ownership regression").And.Contain("attachment.txt"));
		Assert.That(attachmentStream.CanRead, Is.True, "Send completion must preserve caller-owned attachments.");
		Assert.That(viewStream.CanRead, Is.True, "Send completion must preserve caller-owned alternate views.");
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public async Task ConfigurationFailurePreservesBorrowedContent(bool useAsync, bool invalidPort) {
		// Fail after borrowed content enters the message, before any network connection is attempted.
		using var attachmentStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic attachment"));
		using var attachment = new Attachment(attachmentStream, "attachment.txt");
		using var viewStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic alternate view"));
		using var view = new AlternateView(viewStream, "text/plain");
		var views = invalidPort ? new[] { view } : new[] { view, null };
		var port = invalidPort ? 65536 : Tools.Mail.SMTPPort;
		IResolveConstraint expectedFailure = invalidPort && useAsync
			? Throws.TypeOf<SmtpException>().With.InnerException.TypeOf<InvalidOperationException>()
			: Throws.TypeOf(invalidPort ? typeof(InvalidOperationException) : typeof(ArgumentNullException));
		if (useAsync)
			await Assert.ThatAsync(() => Tools.Mail.SendEmailAsync("localhost", "sender@example.test", "Ownership regression", "Synthetic local message",
				new[] { "recipient@example.test" }, attachments: new[] { attachment }, alternativeViews: views, port: port), expectedFailure);
		else
			Assert.That(() => Tools.Mail.SendEmail("localhost", "sender@example.test", "Ownership regression", "Synthetic local message",
				new[] { "recipient@example.test" }, attachments: new[] { attachment }, alternativeViews: views, port: port), expectedFailure);
		Assert.That(attachmentStream.CanRead, Is.True, "A later configuration failure must preserve already accepted caller-owned attachments.");
		Assert.That(viewStream.CanRead, Is.True, "A later configuration failure must preserve partially populated caller-owned alternate views.");
	}

	private static async Task VerifyDelivery(bool useAsync, bool requiresTls, bool serverOffersTls, bool trustTestCertificate) {
		// Certificate creation needs X509/BCL primitives; there is no framework certificate factory.
		using var privateKey = RSA.Create(2048);
		var request = new CertificateRequest("CN=localhost", privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var names = new SubjectAlternativeNameBuilder();
		names.AddDnsName("localhost");
		request.CertificateExtensions.Add(names.Build());
		using var generatedCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
		// Reimport so Windows Schannel can use the private key; disposal deletes the temporary key container.
		using var certificate = X509CertificateLoader.LoadPkcs12(generatedCertificate.Export(X509ContentType.Pfx), null);
		RemoteCertificateValidationCallback validationPolicy = null;
		var validationCalls = 0;
		if (trustTestCertificate) {
			// Trust only this temporary certificate in the test process. Do not modify OS certificate stores or accept arbitrary certificates.
			validationPolicy = (sender, presentedCertificate, chain, errors) => {
				Interlocked.Increment(ref validationCalls);
				return presentedCertificate != null && (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None
					&& presentedCertificate.GetCertHashString() == certificate.GetCertHashString();
			};
		}
#pragma warning disable SYSLIB0014 // Regression coverage: production code must not overwrite the application's legacy SMTP certificate policy.
		var previousPolicy = ServicePointManager.ServerCertificateValidationCallback;
		using var restorePolicy = Tools.Scope.ExecuteOnDispose(() => ServicePointManager.ServerCertificateValidationCallback = previousPolicy);
		ServicePointManager.ServerCertificateValidationCallback = validationPolicy;
#pragma warning restore SYSLIB0014

		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		using var cancelOperations = Tools.Scope.ExecuteOnDispose(cancellation.Cancel);
		var listener = new TcpListener(IPAddress.Loopback, 0);
		using var stopListener = Tools.Scope.ExecuteOnDispose(listener.Stop);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		var receiveTask = ReceiveEmail(listener, serverOffersTls, certificate, cancellation.Token);
		using var attachmentStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic attachment"));
		using var attachment = new Attachment(attachmentStream, "attachment.txt");
		using var viewStream = new MemoryStream(Encoding.ASCII.GetBytes("Synthetic alternate view"));
		using var view = new AlternateView(viewStream, "text/plain");
		Exception sendError = null;
		try {
			var sendTask = useAsync
				? Tools.Mail.SendEmailAsync("localhost", "sender@example.test", "TLS regression", "Synthetic local message", new[] { "recipient@example.test" },
					attachments: new[] { attachment }, alternativeViews: new[] { view }, requiresSSL: requiresTls, port: port)
				: Task.Run(() => Tools.Mail.SendEmail("localhost", "sender@example.test", "TLS regression", "Synthetic local message", new[] { "recipient@example.test" },
					attachments: new[] { attachment }, alternativeViews: new[] { view }, requiresSSL: requiresTls, port: port));
			await sendTask.WaitAsync(cancellation.Token);
		} catch (Exception error) {
			sendError = error;
		}
		var message = await receiveTask;
		Assert.That(attachmentStream.CanRead, Is.True, "Successful sends and TLS failures must preserve caller-owned attachments.");
		Assert.That(viewStream.CanRead, Is.True, "Successful sends and TLS failures must preserve caller-owned alternate views.");
		var shouldDeliver = !requiresTls || serverOffersTls && trustTestCertificate;
		if (shouldDeliver) {
			Assert.That(sendError, Is.Null);
			Assert.That(message, Does.Contain("Subject: TLS regression").And.Contain("Synthetic local message"));
		} else {
			Assert.That(message, Is.Null, "No message may be sent after TLS negotiation fails.");
			if (serverOffersTls) {
				// Send can surface AuthenticationException directly; SendMailAsync wraps it in SmtpException.
				var certificateError = sendError is SmtpException ? sendError.InnerException : sendError;
				Assert.That(certificateError, Is.InstanceOf<AuthenticationException>(), "Rejection must result from certificate validation.");
			} else {
				Assert.That(sendError, Is.InstanceOf<SmtpException>());
			}
		}
		if (trustTestCertificate)
			Assert.That(validationCalls, Is.GreaterThan(0), "The configured certificate policy must run during the real TLS handshake.");
#pragma warning disable SYSLIB0014
		Assert.That(ServicePointManager.ServerCertificateValidationCallback, Is.SameAs(validationPolicy), "Mail helpers must not install a certificate bypass.");
#pragma warning restore SYSLIB0014
	}

	private static async Task<string> ReceiveDeferredMessage(TcpListener listener, TaskCompletionSource<bool> connectionAccepted, Task allowGreeting, CancellationToken cancellationToken) {
		using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
		using var stream = connection.GetStream();
		using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
		using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
		connectionAccepted.SetResult(true);
		await allowGreeting.WaitAsync(cancellationToken);
		await writer.WriteLineAsync("220 localhost test SMTP".AsMemory(), cancellationToken);
		var message = await ReceiveMessage(reader, writer, cancellationToken);

		// The helper owns its SMTP client and must close the connection without the server doing so first.
		var closingCommand = await reader.ReadLineAsync(cancellationToken);
		Assert.That(closingCommand, Is.Null.Or.EqualTo("QUIT"), "Client disposal must end the SMTP session after delivery.");
		if (closingCommand == "QUIT")
			await writer.WriteLineAsync("221 Bye".AsMemory(), cancellationToken);
		return message;
	}

	private static async Task<string> ReceiveEmail(TcpListener listener, bool offersTls, X509Certificate2 certificate, CancellationToken cancellationToken) {
		using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
		using var stream = connection.GetStream();
		using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
		using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
		await writer.WriteLineAsync("220 localhost test SMTP".AsMemory(), cancellationToken);
		if (!offersTls)
			return await ReceiveMessage(reader, writer, cancellationToken);

		Assert.That(await reader.ReadLineAsync(cancellationToken), Does.StartWith("EHLO "));
		await writer.WriteLineAsync("250-localhost\r\n250 STARTTLS".AsMemory(), cancellationToken);
		Assert.That(await reader.ReadLineAsync(cancellationToken), Is.EqualTo("STARTTLS"));
		await writer.WriteLineAsync("220 Ready for TLS".AsMemory(), cancellationToken);
		using var tlsStream = new SslStream(stream, leaveInnerStreamOpen: true);
		try {
			await tlsStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, cancellationToken);
			Assert.That(tlsStream.IsEncrypted, Is.True);
			using var tlsReader = new StreamReader(tlsStream, Encoding.ASCII, false, 1024, leaveOpen: true);
			using var tlsWriter = new StreamWriter(tlsStream, Encoding.ASCII, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
			return await ReceiveMessage(tlsReader, tlsWriter, cancellationToken);
		} catch (AuthenticationException error) {
			TestContext.WriteLine(error);
			return null;
		} catch (IOException error) {
			TestContext.WriteLine(error);
			// A client rejecting the certificate may close the connection after the server completes its half of the handshake.
			return null;
		}
	}

	private static async Task<string> ReceiveMessage(StreamReader reader, StreamWriter writer, CancellationToken cancellationToken) {
		while (await reader.ReadLineAsync(cancellationToken) is { } command) {
			if (command.StartsWith("EHLO ", StringComparison.Ordinal) || command.StartsWith("HELO ", StringComparison.Ordinal)) {
				await writer.WriteLineAsync("250 localhost".AsMemory(), cancellationToken);
			} else if (command.StartsWith("MAIL FROM:", StringComparison.Ordinal) || command.StartsWith("RCPT TO:", StringComparison.Ordinal)) {
				await writer.WriteLineAsync("250 OK".AsMemory(), cancellationToken);
			} else if (command == "DATA") {
				await writer.WriteLineAsync("354 End with a dot".AsMemory(), cancellationToken);
				var message = new StringBuilder();
				while (await reader.ReadLineAsync(cancellationToken) is { } line && line != ".")
					message.AppendLine(line);
				await writer.WriteLineAsync("250 Message accepted".AsMemory(), cancellationToken);
				return message.ToString();
			} else if (command == "QUIT") {
				await writer.WriteLineAsync("221 Bye".AsMemory(), cancellationToken);
				return null;
			} else {
				Assert.Fail($"Unexpected SMTP command: {command}");
			}
		}
		return null;
	}
}
