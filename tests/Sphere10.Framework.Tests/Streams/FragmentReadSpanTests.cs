// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FragmentReadSpanTests {

	[Test]
	public void ExistingProvidersUseOneFragmentReadPerSegment() {
		var provider = new LegacyFragmentProvider();
		using var stream = new FragmentedStream(provider);
		Span<byte> destination = stackalloc byte[5];
		Assert.That(stream.Read(destination), Is.EqualTo(5));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
		Assert.That(provider.FragmentReads, Is.EqualTo(2));
		stream.Position = 1;
		stream.Write(new byte[] { 8, 9, 10 }.AsSpan());
		Assert.That(provider.FragmentReads, Is.EqualTo(4));
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 8, 9, 10, 5 }));
	}

	[Test]
	public void DirectProvidersAvoidMaterializingFragmentsForReadsAndWrites() {
		var provider = new DirectFragmentProvider();
		using var stream = new FragmentedStream(provider);
		Span<byte> destination = stackalloc byte[7];
		destination.Fill(255);
		Assert.That(stream.Read(destination), Is.EqualTo(5));
		Assert.That(destination[..5].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
		Assert.That(destination[5..].ToArray(), Is.All.EqualTo(255));
		stream.Position = 1;
		stream.Write(new byte[] { 8, 9, 10 }.AsSpan());
		Assert.That(provider.LengthReads, Is.EqualTo(2));
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 8, 9, 10, 5 }));
	}

	[Test]
	public void ByteArrayProviderRetainsReimplementedFragmentProjection() {
		IStreamFragmentProvider provider = new ProjectedByteArrayProvider();
		using var stream = new FragmentedStream(provider);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 2, 3 }));
	}

	[Test]
	public void ByteArrayProviderLengthsAndIOFollowLengthChangingInterfaceProjection() {
		var projected = new LengthProjectedByteArrayProvider();
		IStreamFragmentProvider provider = projected;
		Assert.That(provider.TotalBytes, Is.EqualTo(5));
		Assert.That(provider.GetFragmentLength(0), Is.EqualTo(2));
		Assert.That(projected.GetFragmentLength(1), Is.EqualTo(3));
		Span<byte> destination = stackalloc byte[4];
		destination.Fill(255);
		Assert.That(provider.ReadFragment(0, 1, destination.Slice(1, 2)), Is.EqualTo(1));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 255, 2, 255, 255 }));
		using var stream = new FragmentedStream(provider);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
		stream.Position = 1;
		stream.Write(new byte[] { 8, 9, 10 }.AsSpan());
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 8, 9, 10, 5 }));
		Assert.That(projected.GetBackingFragment(0).ToArray(), Is.EqualTo(new byte[] { 238, 1, 8 }));
		Assert.That(projected.GetBackingFragment(1).ToArray(), Is.EqualTo(new byte[] { 238, 9, 10, 5 }));
	}

	[Test]
	public void ByteArrayProviderAndTrivialSubclassHaveTheSameFragmentBehavior([Values] bool subclass) {
		IStreamFragmentProvider provider = subclass
			? new TrivialByteArrayProvider()
			: new ByteArrayStreamFragmentProvider(new[] { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } }, () => new byte[3]);
		Assert.That(provider.GetFragmentLength(0), Is.EqualTo(2));
		Assert.That(provider.GetFragmentLength(1), Is.EqualTo(3));
		using var stream = new FragmentedStream(provider);
		var destination = new byte[] { 255, 255, 255, 255, 255, 255, 255 };
		Assert.That(stream.Read(destination.AsSpan(1, 5)), Is.EqualTo(5));
		Assert.That(destination, Is.EqualTo(new byte[] { 255, 1, 2, 3, 4, 5, 255 }));
		stream.Position = 1;
		stream.Write(new byte[] { 8, 9, 10 }, 0, 3);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 8, 9, 10, 5 }));
	}

	[Test]
	public void MissingFragmentBytesFailInsteadOfRepeatingAZeroLengthRead() {
		using var stream = new FragmentedStream(new EmptyFragmentProvider());
		Assert.That(() => stream.Read(new byte[1].AsSpan()), Throws.TypeOf<EndOfStreamException>());
		Assert.That(() => stream.ToArray(), Throws.TypeOf<EndOfStreamException>());
	}

	private sealed class LegacyFragmentProvider : IStreamFragmentProvider {
		private readonly ByteArrayStreamFragmentProvider _inner = new(new[] { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } }, () => new byte[3]);

		public int FragmentReads { get; private set; }
		public long TotalBytes => _inner.TotalBytes;

		public ReadOnlySpan<byte> GetFragment(long fragmentID) {
			FragmentReads++;
			return _inner.GetFragment(fragmentID);
		}

		public void UpdateFragment(long fragmentID, long fragmentPosition, ReadOnlySpan<byte> updateSpan) => _inner.UpdateFragment(fragmentID, fragmentPosition, updateSpan);
		public void MapStreamPosition(long position, out long fragmentID, out long fragmentPosition) => _inner.MapStreamPosition(position, out fragmentID, out fragmentPosition);
		public void SetTotalBytes(long length) => _inner.SetTotalBytes(length);
	}

	private sealed class DirectFragmentProvider : IStreamFragmentProvider {
		private readonly ByteArrayStreamFragmentProvider _inner = new(new[] { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } }, () => new byte[3]);

		public int LengthReads { get; private set; }
		public long TotalBytes => _inner.TotalBytes;

		public ReadOnlySpan<byte> GetFragment(long fragmentID) => throw new InvalidOperationException("Fragment content must not be materialized.");

		public int GetFragmentLength(long fragmentID) {
			LengthReads++;
			return _inner.GetFragmentLength(fragmentID);
		}

		public int ReadFragment(long fragmentID, long fragmentPosition, Span<byte> destination) => _inner.ReadFragment(fragmentID, fragmentPosition, destination);
		public void UpdateFragment(long fragmentID, long fragmentPosition, ReadOnlySpan<byte> updateSpan) => _inner.UpdateFragment(fragmentID, fragmentPosition, updateSpan);
		public void MapStreamPosition(long position, out long fragmentID, out long fragmentPosition) => _inner.MapStreamPosition(position, out fragmentID, out fragmentPosition);
		public void SetTotalBytes(long length) => _inner.SetTotalBytes(length);
	}

	private sealed class ProjectedByteArrayProvider : ByteArrayStreamFragmentProvider, IStreamFragmentProvider {
		public ProjectedByteArrayProvider()
			: base(new[] { new byte[] { 1, 2 } }, () => new byte[2]) {
		}

		public new ReadOnlySpan<byte> GetFragment(long fragmentID) {
			var result = base.GetFragment(fragmentID).ToArray();
			for (var index = 0; index < result.Length; index++)
				result[index]++;
			return result;
		}
	}

	private sealed class EmptyFragmentProvider : IStreamFragmentProvider {
		public long TotalBytes => 1;
		public ReadOnlySpan<byte> GetFragment(long fragmentID) => ReadOnlySpan<byte>.Empty;
		public void UpdateFragment(long fragmentID, long fragmentPosition, ReadOnlySpan<byte> updateSpan) => throw new NotSupportedException();
		public void SetTotalBytes(long length) => throw new NotSupportedException();

		public void MapStreamPosition(long position, out long fragmentID, out long fragmentPosition) {
			fragmentID = 0;
			fragmentPosition = 0;
		}
	}

	private sealed class LengthProjectedByteArrayProvider : ByteArrayStreamFragmentProvider, IStreamFragmentProvider {
		public LengthProjectedByteArrayProvider()
			: base(new[] { new byte[] { 238, 1, 2 }, new byte[] { 238, 3, 4, 5 } }, () => throw new NotSupportedException()) {
		}

		long IStreamFragmentProvider.TotalBytes => TotalBytes - FragmentCount;

		public ReadOnlySpan<byte> GetBackingFragment(long fragmentID) => base.GetFragment(fragmentID);

		ReadOnlySpan<byte> IStreamFragmentProvider.GetFragment(long fragmentID) => base.GetFragment(fragmentID)[1..];

		void IStreamFragmentProvider.UpdateFragment(long fragmentID, long fragmentPosition, ReadOnlySpan<byte> updateSpan)
			=> base.UpdateFragment(fragmentID, fragmentPosition + 1, updateSpan);

		void IStreamFragmentProvider.SetTotalBytes(long length) => throw new NotSupportedException();

		void IStreamFragmentProvider.MapStreamPosition(long position, out long fragmentID, out long fragmentPosition) {
			IStreamFragmentProvider provider = this;
			for (fragmentID = 0; fragmentID < FragmentCount; fragmentID++) {
				var length = provider.GetFragmentLength(fragmentID);
				if (position < length) {
					fragmentPosition = position;
					return;
				}
				position -= length;
			}
			fragmentPosition = position;
		}
	}

	private sealed class TrivialByteArrayProvider : ByteArrayStreamFragmentProvider {
		public TrivialByteArrayProvider()
			: base(new[] { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } }, () => new byte[3]) {
		}
	}
}
