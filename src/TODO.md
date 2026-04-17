# TODO — Agent Work Instructions

## Task 1: Migrate All `ClassicAssert` Usages to Constraint-Model Assertions

**Scope:** All `*.cs` files under `tests/` and `src/` directories.

**What to do:**
- Search the entire codebase for `ClassicAssert` (including `using NUnit.Framework.Legacy;`).
- Replace every occurrence with the modern NUnit constraint-model equivalent via `Assert.That(...)`:
  - `ClassicAssert.IsTrue(x, msg)` → `Assert.That(x, Is.True, msg)`
  - `ClassicAssert.IsFalse(x, msg)` → `Assert.That(x, Is.False, msg)`
  - `ClassicAssert.AreEqual(expected, actual, msg)` → `Assert.That(actual, Is.EqualTo(expected), msg)`
  - `ClassicAssert.IsNotNull(x)` → `Assert.That(x, Is.Not.Null)`
  - `ClassicAssert.IsNull(x)` → `Assert.That(x, Is.Null)`
  - etc. — see `.github/copilot-instructions.md` § Unit Testing for the full list.
- Remove `using NUnit.Framework.Legacy;` from every file once all `ClassicAssert` calls are replaced.
- Also check `src/Sphere10.Framework.NUnit/AssertEx.cs` and `src/Sphere10.Framework.NUnit/NUnitTool.cs` — these utility files may use `ClassicAssert` internally.
- Build and run affected tests to confirm no regressions.

---

## Task 2: ObjectSpace Integration Tests — Freshly-Hydrated Round-Trip Verification

**File:** `tests/Sphere10.Framework.Tests/ObjectSpaces/ObjectSpaceIntegrationTests.cs`

**Problem:** The integration tests currently verify stored values by re-reading from the *same* `ObjectSpace` instance, which may return cached in-memory objects from the `InstanceTracker`. This does not prove the data was correctly persisted and can be deserialized.

**What to do:**
For every test that creates/saves objects and then verifies their values, **add a second verification phase** that:

1. Captures the raw bytes of the underlying stream: `var rawBytes = os.Streams.RootStream.ToArray();` (call `os.Flush()` first to ensure all data is written).
2. Creates a **brand-new** `ObjectSpace` from those bytes:
   ```csharp
   using var freshStream = new MemoryStream(rawBytes);
   using var freshOs = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, freshStream);
   ```
   - `SafeBoxTestHelper.CreateSafeBoxObjectSpace` may need an overload that accepts an existing `Stream` instead of creating a new `MemoryStream`. If the overload does not exist, add one.
3. Repeats the **same assertions** on `freshOs` — counts, unique-key lookups, property values, cross-dimension references, etc.
4. Ensures that objects loaded from `freshOs` are **not** the same instance (`Is.Not.SameAs`) as the original, but **are** value-equal.

Apply this pattern to every test region: Identity CRUD, Account CRUD, Block/Transaction CRUD, cross-dimension references, polymorphic identity hierarchy, complex object graph loops, etc.

---

## Task 3: Schnorr Implementation Review & Refactor

**Files:**
- `src/Sphere10.Framework.CryptoEx/EC/Schnorr/Schnorr.cs` (main implementation)
- Inner classes `Key`, `PrivateKey`, `PublicKey` (approx. lines 415–540)

### 3a: Remove Redundant `IFuture` Usage

The inner `Key` base class wraps `AsInteger` in `Tools.Values.Future.LazyLoad(...)`. This is pointless because `BytesToBigIntPositive` is a trivial computation on an already-available `byte[]`. Similarly:
- `PrivateKey.Parameters` wraps `ECPrivateKeyParameters` in an `IFuture`.
- `PublicKey.AsPoint` wraps the `ECPoint` passed directly into the constructor (literally `() => point`) — the value is already computed.
- `PublicKey.Parameters` wraps `ECPublicKeyParameters` in an `IFuture`.

**What to do:**
- Replace `IFuture<BigInteger> AsInteger` with a plain `BigInteger AsInteger { get; }` computed eagerly in the `Key` constructor.
- Replace `IFuture<ECPoint> AsPoint` with `ECPoint AsPoint { get; }` — assign directly from the constructor parameter.
- Replace `IFuture<ECPrivateKeyParameters> Parameters` and `IFuture<ECPublicKeyParameters> Parameters` with plain properties, computed eagerly in constructors.
- Update all call sites: change `AsInteger.Value`, `Parameters.Value`, `AsPoint.Value` to just `AsInteger`, `Parameters`, `AsPoint`.

### 3b: Replace `ThrowIfPointIsAtInfinity` with `Guard.Argument`

- Remove the `ThrowIfPointIsAtInfinity` method entirely.
- At every call site, replace with:
  ```csharp
  Guard.Argument(!IsPointInfinity(point), nameof(point), "Point is at infinity");
  ```
- Similarly, review `ValidatePoint` — it calls `ThrowIfPointIsAtInfinity` then checks `IsEven`. Refactor to use `Guard.Argument` for both checks.

### 3c: Replace Other Throw-Based Validation with `Guard` / `TryParse` Patterns

- `ValidatePrivateKeyRange(name, scalar)` — use `Guard.Argument(ValidatePrivateKeyRangeNoThrow(scalar), name, "...")`.
- `ValidatePublicKeyRange(name, publicKey)` — same pattern.
- `ValidateSignature(r, s)` — use `Guard.Argument`.
- `ValidateArray` / `ValidateJaggedArray` / `ValidateBuffer` — use `Guard.ArgumentNotNull` and `Guard.Argument`.
- In `LiftX`, the `throw new ArgumentException` when `c != y^2` should use `Guard.Argument`.
- In `VerifyDigest`, it currently calls `ValidatePublicKeyRange` which throws — but `VerifyDigest` should never throw on bad input, it should return `false`. Wrap validation in a try/catch or use the `NoThrow` variant and return false on failure.

### 3d: General Code Quality

- The `SignDigestWithAuxRandomData` method has a `try/finally` that manually `Array.Clear`s intermediates. This is fine for security but consider using `Span<byte>` / `stackalloc` where possible (see Task 4).
- `VerifyDigest` converts `ReadOnlySpan<byte>` to `byte[]` immediately (`sig.ToArray()`, `messageDigest.ToArray()`, `publicKey.ToArray()`). Minimize allocations where BouncyCastle APIs allow.
- `BatchVerifyDigest` takes `byte[][]` — consider accepting `ReadOnlySpan<byte>[]` or at minimum ensure the public API is consistent with the rest of the scheme.

---

## Task 4: Optimize with `stackalloc` / `Span<byte>` to Minimize Allocations

**Scope:** Framework-wide, focusing on hot paths in:
1. **Schnorr** (`Schnorr.cs`) — `TaggedHash`, `ComputeSha256Hash`, `SignDigestWithAuxRandomData`, `VerifyDigest`, `BytesOfBigInt`, `BytesOfXCoord`.
2. **Cryptography** (`src/Sphere10.Framework.CryptoEx/`) — any hashing, key derivation, or signing code that allocates `new byte[N]` for small fixed-size buffers.
3. **ClusteredStreams** (`src/Sphere10.Framework/Streams/`) — cluster read/write operations that allocate temp buffers.
4. **Collections / Paged collections** (`src/Sphere10.Framework/Collections/`) — serialization/deserialization buffers in `StreamMappedList`, `StreamMappedDictionary`, etc.

**Guidelines:**
- Use `stackalloc byte[N]` for buffers ≤ 256 bytes with known compile-time or small runtime sizes.
- Use `Span<byte>` / `ReadOnlySpan<byte>` parameters and locals instead of `byte[]` where downstream APIs accept spans.
- For larger or variable-size buffers, use `ArrayPool<byte>.Shared.Rent/Return`.
- Be careful with `stackalloc` in loops — the allocation must not grow unbounded.
- BouncyCastle APIs mostly require `byte[]`, so `stackalloc` benefits are limited to pre-BouncyCastle computation and post-BouncyCastle result handling.
- Target **.NET 8** code paths (some projects are .NET Standard 2.1 where `stackalloc` in expressions is available but `Span` interop is more limited).

---

## Task 5: Deterministic Key Derivation Across All Signature Schemes

**Requirement:** `GeneratePrivateKey(ReadOnlySpan<byte> seed)` must be fully deterministic — same seed always produces the same private key, across instances, runs, and machines.

**Schnorr status (already done):** `Schnorr.GeneratePrivateKey` uses `DigestRandomGenerator` seeded with the provided seed, which is deterministic. The test `GeneratePrivateKey_SameSeed_ProducesSameKey` in `SchnorrSecurityTests.cs` covers this.

**What to do:**
1. Audit **every** `IDigitalSignatureScheme` / `StatelessDigitalSignatureScheme` implementation in `src/Sphere10.Framework.CryptoEx/` (ECDSA variants, Ed25519, any others) for the same pattern.
2. Ensure each scheme's `GeneratePrivateKey(ReadOnlySpan<byte> seed)` method:
   - Uses a deterministic PRNG (e.g., `DigestRandomGenerator` seeded exclusively with the seed).
   - Does **not** mix in `SecureRandom` or system entropy when a seed is provided.
   - The parameterless `GeneratePrivateKey()` (no seed) should still use system entropy.
3. Add or verify a test `GeneratePrivateKey_SameSeed_ProducesSameKey` for each scheme:
   - Create two fresh instances of the scheme.
   - Generate private keys from the same 32-byte seed on both.
   - Assert the raw bytes are identical.
4. Add a test `GeneratePrivateKey_DifferentSeeds_ProduceDifferentKeys` for each scheme.
5. Use modern NUnit constraint-model assertions (`Assert.That`), not `ClassicAssert`.

---

## Task 6: Schnorr Security Tests — Exhaustive Bit-Flip Coverage

**File:** `tests/Sphere10.Framework.CryptoEx.Tests/SchnorrSecurityTests.cs`

**Current state:** `VerifyDigest_FlippedBitInR_Fails` and `VerifyDigest_FlippedBitInS_Fails` only flip bit 0 of byte 0 in the R and S components respectively (i.e., `tampered[0] ^= 0x01` and `tampered[schnorr.KeySize] ^= 0x01`).

**What to do:**
Replace these two tests with a comprehensive bit-flip test that iterates through **every bit** of the signature:

```csharp
[Test]
public void VerifyDigest_FlipEveryBit_AllFail() {
    var schnorr = CreateSchnorr();
    var sk = schnorr.GeneratePrivateKey();
    var pk = schnorr.DerivePublicKey(sk);
    var messageDigest = RandomMessageDigest();
    var sig = schnorr.SignDigest(sk, messageDigest);

    for (var byteIdx = 0; byteIdx < sig.Length; byteIdx++) {
        for (var bitIdx = 0; bitIdx < 8; bitIdx++) {
            var tampered = (byte[])sig.Clone();
            tampered[byteIdx] ^= (byte)(1 << bitIdx);
            bool result;
            try {
                result = schnorr.VerifyDigest(tampered, messageDigest, pk.RawBytes);
            } catch (Exception) {
                result = false;
            }
            Assert.That(result, Is.False,
                $"Flipping bit {bitIdx} of byte {byteIdx} must invalidate the signature");
        }
    }
}
```

- Remove the old `VerifyDigest_FlippedBitInR_Fails` and `VerifyDigest_FlippedBitInS_Fails` tests (they are subsumed).
- Keep `VerifyDigest_SwappedRAndS_Fails` as a separate test.
- Use `Assert.That` (constraint-model), not `ClassicAssert`.

---

## Task 7: InstanceTracker — Lookup by `ObjectSpaceObjectReference` Before Loading

**Files:**
- `src/Sphere10.Framework/ObjectSpaces/InstanceTracker.cs`
- `src/Sphere10.Framework/ObjectSpaces/ObjectSpaceBase.cs`

**Problem:** When `ObjectSpaceBase.TryGet<TItem>(long index, ...)` is called, it checks `_instanceTracker.TryGet<TItem>(index, ...)` which looks up by `(Type, rowIndex)`. However, the InstanceTracker also maintains a `_refToObject` dictionary keyed by `ObjectSpaceObjectReference(dimensionIndex, objectIndex)`. Currently, `TryGet` does **not** consult `_refToObject`, so if an object was loaded via a different code path (e.g., `ResolveExternalReference`) and tracked by ref but the type-based map was keyed differently, it could be missed or a duplicate instance could be created.

### 7a: `ObjectSpaceBase.TryGet<TItem>(long index, out TItem item)`

After the existing `_instanceTracker.TryGet(index, out item)` check, add a fallback that constructs the `ObjectSpaceObjectReference` and checks `_instanceTracker.TryResolveRef`:

```csharp
// Existing fast path
if (_instanceTracker.TryGet(index, out item))
    return true;

// Fallback: check by ObjectSpaceObjectReference ID
var dimIdx = GetDimensionIndex(typeof(TItem));
var objRef = new ObjectSpaceObjectReference(dimIdx, index);
if (_instanceTracker.TryResolveRef(objRef, out var cached)) {
    item = (TItem)cached;
    return true;
}
```

### 7b: `ResolveExternalReference`

This method already checks `TryResolveRef` first (good). After loading and tracking, ensure the type-based map and the ref-based map are both populated (they are, via `Track` + `TrackRef`). No change needed here, but **verify** correctness.

### 7c: `SaveInternal` / `DeleteInternal` / `AcceptNewInternal`

Review each to ensure that any mutation of the InstanceTracker (re-indexing after save, untracking on delete) keeps both maps (`_objectsByType` and `_refToObject`) consistent. In particular:
- When `Track(item, newIndex)` is called after a provisional→real index change, the old `(type, provisionalIndex)` entry must be removed. Verify `Track` handles this via the bijection update path.
- When `Untrack(item)` is called on delete, both `_objectsByType` and `_refToObject` are cleaned up (already done — confirm).

### 7d: Stale State Prevention

Add a method to `InstanceTracker` that verifies consistency between `_objectsByType` and `_refToObject`:
```csharp
[Conditional("DEBUG")]
internal void AssertConsistency() {
    // Every object in _objectsByType should have a corresponding _objectRefs entry
    // Every entry in _refToObject should have a corresponding _objectsByType entry
}
```
Call this at the end of `SaveInternal` and `DeleteInternal` in DEBUG builds.
