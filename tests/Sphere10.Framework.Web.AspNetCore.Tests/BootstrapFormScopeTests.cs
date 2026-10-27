// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.MVC;

namespace Sphere10.Framework.Web.AspNetCore.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BootstrapFormScopeTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task FormCompletionWritesItsMarkerAndConfiguredClientOptions(bool asynchronous) {
		using var writer = new StringWriter();
		var helper = CreateHelper(writer);
		var scope = helper.BeginBootstrapForm("/submit", new TestFormModel { ID = "owned-form" }, options: FormScopeOptions.OmitFormTag | FormScopeOptions.UseGet);
		Assert.That(scope.FormID, Is.EqualTo("owned-form"));
		scope.MessageMarker().WriteTo(writer, HtmlEncoder.Default);
		Assert.That(() => scope.MessageMarker(), Throws.InstanceOf<InvalidOperationException>());
		if (asynchronous)
			await scope.DisposeAsync();
		else
			scope.Dispose();
		var completed = writer.ToString();
		Assert.That(completed, Does.Contain("owned-form_message_marker"));
		Assert.That(completed, Does.Contain("F_Init('owned-form',"));
		Assert.That(completed, Does.Contain("\"httpMethod\":\"GET\""));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedFormCompletionCanBeRetried(bool asynchronous) {
		using var writer = new RetryWriter();
		var scope = CreateHelper(writer).BeginBootstrapForm("/submit", new TestFormModel { ID = "retry-form" }, options: FormScopeOptions.OmitFormTag);
		if (asynchronous)
			await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.TypeOf<IOException>());
		else
			Assert.That(() => scope.Dispose(), Throws.TypeOf<IOException>());
		Assert.That(() => scope.Dispose(), Throws.Nothing);
		var completed = writer.ToString();
		Assert.That(completed, Does.Contain("retry-form_message_marker"));
		Assert.That(completed, Does.Contain("F_Init('retry-form',"));
	}

	[Test]
	public async Task AsynchronousFormCompletionAwaitsTheOutputWriterWithoutSynchronousOutput() {
		using var writer = new GatedWriter();
		var scope = CreateHelper(writer).BeginBootstrapForm("/submit", new TestFormModel { ID = "async-form" }, options: FormScopeOptions.OmitFormTag);
		var disposal = scope.DisposeAsync();
		try {
			Assert.That(writer.Writes, Is.EqualTo(1));
			Assert.That(disposal.IsCompleted, Is.False);
			Assert.That(writer.ToString(), Is.Empty);
		} finally {
			writer.Release.SetResult();
		}
		await disposal;
		Assert.That(writer.Writes, Is.EqualTo(2));
		Assert.That(writer.ToString(), Does.Contain("async-form_message_marker").And.Contain("F_Init('async-form',"));
	}

	private static IHtmlHelper<TestFormModel> CreateHelper(TextWriter writer) {
		var helper = DispatchProxy.Create<IHtmlHelper<TestFormModel>, HtmlHelperProxy>();
		((HtmlHelperProxy)(object)helper).Context = new ViewContext { Writer = writer, FormContext = new FormContext() };
		return helper;
	}

	[Test]
	public async Task AsynchronousFormCompletionBuffersTheSynchronousMvcEndFormAndRestoresItsWriter() {
		using var constructorOutput = new StringWriter();
		using var writer = new GatedWriter();
		var helper = CreateHelper(constructorOutput);
		var scope = helper.BeginBootstrapForm("/submit", new TestFormModel { ID = "tagged-form" }, options: 0);
		helper.ViewContext.Writer = writer;
		var disposal = scope.DisposeAsync();
		try {
			Assert.That(disposal.IsCompleted, Is.False);
		} finally {
			writer.Release.SetResult();
		}
		await disposal;
		Assert.That(writer.Writes, Is.EqualTo(3), "Marker, client initialization and the buffered MVC form ending must all use asynchronous response output.");
		Assert.That(writer.ToString(), Does.EndWith("</form>"));
		Assert.That(helper.ViewContext.Writer, Is.SameAs(writer));
	}

	public sealed class TestFormModel : FormModelBase {
	}

	public class HtmlHelperProxy : DispatchProxy {
		public ViewContext Context { get; set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args) {
			switch (targetMethod.Name) {
				case "get_ViewContext":
					return Context;
				case "BeginForm":
					Context.Writer.Write("<form>");
					return new MvcForm(Context, HtmlEncoder.Default);
				case "HiddenFor":
					return HtmlString.Empty;
				case "EndForm":
					new MvcForm(Context, HtmlEncoder.Default).EndForm();
					return null;
				default:
					throw new NotSupportedException(targetMethod.Name);
			}
		}
	}

	private sealed class RetryWriter : StringWriter {
		private bool _fail = true;

		public override void Write(string value) {
			if (_fail && value.Contains("F_Init(", StringComparison.Ordinal)) {
				_fail = false;
				throw new IOException("Owned form writer failure.");
			}
			base.Write(value);
		}

		public override Task WriteAsync(string value) {
			Write(value);
			return Task.CompletedTask;
		}
	}

	private sealed class GatedWriter : TextWriter {
		private readonly StringBuilder _output = new();
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Writes { get; private set; }
		public override Encoding Encoding => Encoding.UTF8;
		public override void Write(string value) => throw new AssertionException("Asynchronous form completion must not use synchronous response output.");
		public override async Task WriteAsync(string value) {
			Writes++;
			await Release.Task.ConfigureAwait(false);
			_output.Append(value);
		}
		public override string ToString() => _output.ToString();
	}

}
