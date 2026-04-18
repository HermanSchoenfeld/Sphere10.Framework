using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamMappedMerkleRecyclableListWithIndexTests : RecyclableListTestsBase {

	protected override IDisposable CreateList<T>(IItemSerializer<T> serializer, IEqualityComparer<T> comparer, out IRecyclableList<T> list) {
		var stream = new MemoryStream();
		var smrlist = new StreamMappedMerkleRecyclableList<T>(
			stream,
			CHF.SHA2_256,
			32, 
			serializer,
			itemChecksummer: new ObjectHashCodeChecksummer<T>(), 
			reservedStreams: 3, 
			autoLoad: true
		);
		list = smrlist;
		return new Disposables(smrlist, stream);
	}

	[Test]
	public void WalkThrough_CheckMerkleTree() {
		var serializer = new StringSerializer();
		var recycledHash = Hashers.ZeroHash(CHF.SHA2_256);
		using var disposables = CreateList(serializer, StringComparer.InvariantCultureIgnoreCase, out var rlist);

		byte[] TreeHash(params string[] text) => MerkleTree.ComputeMerkleRoot(text.Select(x => x is not null ? Hashers.Hash(CHF.SHA2_256, serializer.SerializeBytesLE(x)) : recycledHash), CHF.SHA2_256);

		var mlist = (StreamMappedMerkleRecyclableList<string>)rlist;

		// verify empty
		Assert.That(rlist.Count, Is.EqualTo(0));
		Assert.That(rlist.ListCount, Is.EqualTo(0));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(mlist.MerkleTree.Root, Is.Null);

		// Enumerate empty
		Assert.That(Array.Empty<string>(), Is.EqualTo(rlist));

		// add "A"
		rlist.Add("A");
		Assert.That(rlist.Count, Is.EqualTo(1));
		Assert.That(rlist.ListCount, Is.EqualTo(1));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A")));

		// try insert
		Assert.That(() => rlist.Insert(0, "text"), Throws.InstanceOf<NotSupportedException>());

		// add "B" and "C"
		rlist.AddRange(new[] { "B", "C" });
		Assert.That(rlist.Count, Is.EqualTo(3));
		Assert.That(rlist.ListCount, Is.EqualTo(3));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A", "B", "C")));

		// remove "B"
		Assert.That(rlist.Remove("B"), Is.True);
		Assert.That(rlist.Count, Is.EqualTo(2));
		Assert.That(rlist.ListCount, Is.EqualTo(3));
		Assert.That(rlist.RecycledCount, Is.EqualTo(1));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A", null, "C")));
		
		// Ensure B is not found
		Assert.That(rlist.IndexOf("B"), Is.EqualTo(-1));

		// try read recycled
		Assert.That(() => rlist.Read(1), Throws.ArgumentException);
		Assert.That(() => rlist.Update(1, "X"), Throws.ArgumentException);
		Assert.That(() => rlist.RemoveAt(1), Throws.ArgumentException);

		// Enumerate 
		Assert.That(new[] { "A", "C" }, Is.EqualTo(rlist));
		Assert.That(rlist.Count, Is.EqualTo(2));
		Assert.That(rlist.ListCount, Is.EqualTo(3));
		Assert.That(rlist.RecycledCount, Is.EqualTo(1));

		// add "B1" (verify used recycled index)
		rlist.Add("B1");
		Assert.That(rlist.Count, Is.EqualTo(3));
		Assert.That(rlist.ListCount, Is.EqualTo(3));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(rlist.IndexOf("B1"), Is.EqualTo(1));
		Assert.That(rlist.Read(1), Is.EqualTo("B1"));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A", "B1", "C")));

		// Update B1 to B2
		rlist.Update(1, "B2");
		Assert.That(rlist.IndexOf("B2"), Is.EqualTo(1));
		Assert.That(rlist.Count, Is.EqualTo(3));
		Assert.That(rlist.ListCount, Is.EqualTo(3));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(rlist.IndexOf("B2"), Is.EqualTo(1));
		Assert.That(rlist.Read(1), Is.EqualTo("B2"));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A", "B2", "C")));

		// Enumeration check
		Assert.That(new[] { "A", "B2", "C" }, Is.EqualTo(rlist));

		// add another "A" (verify used new index)
		rlist.Add("A");
		Assert.That(rlist.ListCount, Is.EqualTo(4));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(rlist.Count, Is.EqualTo(4));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash("A", "B2", "C", "A")));

		// verify index of first "A"
		Assert.That(rlist.IndexOf("A"), Is.EqualTo(0));

		// Remove first A
		rlist.RemoveAt(0);
		Assert.That(rlist.ListCount, Is.EqualTo(4));
		Assert.That(rlist.RecycledCount, Is.EqualTo(1));
		Assert.That(rlist.Count, Is.EqualTo(3));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash(null, "B2", "C", "A")));

		// Verify index of second "A"
		Assert.That(rlist.IndexOf("A"), Is.EqualTo(3));

		// Remove second "A"
		rlist.RemoveAt(3);
		Assert.That(rlist.ListCount, Is.EqualTo(4));
		Assert.That(rlist.RecycledCount, Is.EqualTo(2));
		Assert.That(rlist.Count, Is.EqualTo(2));
		Assert.That(mlist.MerkleTree.Root, Is.EqualTo(TreeHash(null, "B2", "C", null)));

		// Verify recycled indices
		Assert.That(rlist.IsRecycled(0), Is.True);
		Assert.That(rlist.IsRecycled(1), Is.False);
		Assert.That(rlist.IsRecycled(2), Is.False);
		Assert.That(rlist.IsRecycled(3), Is.True);

		// Verify not "A" is found
		Assert.That(rlist.IndexOf("A"), Is.EqualTo(-1));
		Assert.That(rlist.Contains("A"), Is.False);


		// Verify index of B2 and C are (1, 2) respectively and other indices are recycled
		Assert.That(rlist.IndexOf("B2"), Is.EqualTo(1));
		Assert.That(rlist.IndexOf("C"), Is.EqualTo(2));

		// Enumerate "B2" and "C"
		Assert.That(new[] { "B2", "C" }, Is.EqualTo(rlist));

		// Clear
		rlist.Clear();
		Assert.That(rlist.ListCount, Is.EqualTo(0));
		Assert.That(rlist.RecycledCount, Is.EqualTo(0));
		Assert.That(rlist.Count, Is.EqualTo(0));
		Assert.That(mlist.MerkleTree.Root, Is.Null);
	}


}

