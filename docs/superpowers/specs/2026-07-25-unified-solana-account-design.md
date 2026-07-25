# Unified PlutoFrameworkSolanaAccount — Design

**Date:** 2026-07-25
**Status:** Approved, ready for implementation
**Builds on:** [Solana wallet support](2026-07-25-solana-wallet-support-design.md)

## Goal

One type the app uses for Solana work regardless of how the key is held: a locally
derived BIP39 account, or a wallet app reached over Mobile Wallet Adapter. Call sites
should not branch on which.

## Why the Substrate precedent does not transfer

`KeysModel.GetAccountAsync()` hands roughly ten call sites a Substrate `Account` they sign
with directly. That works because both Substrate variants — Sr25519 and PolkadotJson — hold
a secret on the device, so a local signing object always exists.

Mobile Wallet Adapter holds no key locally. A unified type therefore cannot expose a
signable account. It has to expose **operations**, with two implementations behind them.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Surface | Identity, message signing, and transactions | Transactions were explicitly in scope, which requires `Solana.Rpc`. |
| Transaction API | Instructions in, signature out | The caller cannot build a transaction without a blockhash, and fetching one itself would duplicate the RPC work the abstraction exists to hide. |
| Network mismatch | Reauthorize on the app-wide selected network | What MWA 2.0 reauthorization is for. Keeps the existing "do not pre-emptively discard the key" policy while making the single app-wide setting authoritative. |
| Type form | Abstract base class, not an interface | The transaction build step is identical for both variants; only sign-and-submit differs. |

## Research findings

### Solana.Rpc 8.7.0 dependency graph

Verified from the nupkg. Contains `lib/net8.0/Solnet.Rpc.dll` and depends only on:

- `Solana.Wallet` 8.7.0 — already referenced.
- `Microsoft.Extensions.Logging` 9.0.0 and `.Console` 9.0.0 — resolve up to the 10.x
  already present via MAUI and `Microsoft.Extensions.Configuration.*`.

No repeat of the `Portable.BouncyCastle` conflict that ruled out the legacy `Solnet.*` ids.

### Solnet APIs this design relies on

Verified against source at `bmresearch/Solnet@master`:

- `ClientFactory.GetClient(Cluster)` → `IRpcClient`.
- `IRpcClient.GetLatestBlockHashAsync(Commitment)` → `RequestResult<ResponseValue<LatestBlockHash>>`.
- `IRpcClient.SendTransactionAsync(byte[], bool skipPreflight, …)` → `RequestResult<string>`,
  where the string is the base58 transaction signature.
- `TransactionBuilder`: `SetRecentBlockHash(string)`, `SetFeePayer(PublicKey)`,
  `AddInstruction(TransactionInstruction)`, `CompileMessage()`, `Build(Account)`.
- `Cluster` enum: `DevNet`, `TestNet`, `MainNet`.

**`RequestResult` reports failure through the result object rather than throwing.** A failed
blockhash fetch treated as success would yield a transaction that fails later for an
unrelated-looking reason, so results must be checked explicitly.

### ShortVectorEncoding is internal

`Solnet.Rpc.Utilities.ShortVectorEncoding` is declared `internal`, so its `EncodeLength` is
unavailable. The shortvec length prefix must be implemented here. It is a base-128 varint:
each byte carries seven bits of length with the high bit set while more bytes follow.

### MWA transaction payload framing

`TransactionBuilder.Serialize()` tolerates zero signatures, emitting `shortvec(0)` followed
by the message. That is not what Mobile Wallet Adapter wants: `sign_and_send_transactions`
expects a fully-formed wire-format transaction with a **zeroed signature slot per required
signer**, which the wallet then fills in. The payload must be framed manually:

```
shortvec(requiredSignatures) || requiredSignatures × 64 zero bytes || compiledMessage
```

### MWA has no persistent session

A session ends when the wallet app returns control, so every MWA operation is a fresh
association: intent, WebSocket, handshake. This is inherent to the protocol and shapes the
design in Section 3 — each operation is one session performing two calls.

## Architecture

```
PlutoFrameworkCore/Solana/
  SolanaRpcModel.cs                 # cluster -> IRpcClient, blockhash, submit, RequestResult checks
  SolanaTransactionFramer.cs        # shortvec + unsigned payload framing + signature extraction
  SolanaRpcException.cs
  SolanaCluster.cs                  # + ToSolnetCluster()

PlutoFramework/Model/Solana/
  PlutoFrameworkSolanaAccount.cs    # abstract base + static ResolveAsync
  MnemonicSolanaAccount.cs          # sealed, local signer
  MwaSolanaAccount.cs               # sealed, remote signer
```

The account classes live in the MAUI layer because they depend on `KeysModel`,
`SolanaNetworkModel` and `SolanaMwaModel`, all of which use MAUI `Preferences`. Everything
platform-agnostic and testable stays in Core.

Solnet types appear in this public API through `TransactionInstruction`. That is a third
Solnet touchpoint beyond `SolanaMnemonicsModel` and `SolanaAddress`, and it is acceptable:
`TransactionInstruction` collides with nothing in Substrate, so it carries none of the
ambiguity that keeps `Account`, `Wallet` and `Mnemonic` isolated.

## The unified type

```csharp
public abstract class PlutoFrameworkSolanaAccount
{
    public abstract string Address { get; }        // base58
    public abstract string DisplayName { get; }
    public abstract KeyTypeEnum KeyType { get; }

    /// <summary>False for Mobile Wallet Adapter, where signing needs the wallet app.</summary>
    public abstract bool CanSignLocally { get; }

    /// <summary>The network the app operates on, never a per-account value.</summary>
    public SolanaCluster Cluster => SolanaNetworkModel.SelectedCluster;

    /// <summary>Null when no Solana key is configured.</summary>
    public static Task<PlutoFrameworkSolanaAccount?> ResolveAsync(string reason, CancellationToken token);

    public Task<byte[]> SignMessageAsync(byte[] message, string reason, CancellationToken token);

    public Task<string> SendAsync(
        IEnumerable<TransactionInstruction> instructions, string reason, CancellationToken token);
}
```

`ResolveAsync` reads whichever Solana key exists — the two are mutually exclusive — and
returns the matching subclass, or null.

Callers needing only an address for display should keep using
`KeysModel.GetSolanaAddressAsync()`, which reads the stored public key without unlocking
anything. `ResolveAsync` unlocks, so it requires a reason.

## Transaction flow

Implemented once in the base class:

1. `cluster` ← `SolanaNetworkModel.SelectedCluster`
2. `blockhash` ← `SolanaRpcModel.GetLatestBlockHashAsync(cluster, token)`
3. Build a `TransactionBuilder` with that blockhash, this account as fee payer, and the
   supplied instructions.
4. Delegate to `protected abstract SignAndSubmitAsync(builder, cluster, reason, token)`.

The only divergence is step 4:

- **Mnemonic** — `builder.Build(account)` yields signed bytes;
  `SolanaRpcModel.SendTransactionAsync` submits them and returns the base58 signature.
- **MWA** — `builder.CompileMessage()`, then `SolanaTransactionFramer.FrameUnsigned(message, 1)`,
  then `sign_and_send_transactions`. The wallet submits; this app makes no RPC call for the
  send. The returned signature bytes are converted to base58.

One required signature in both cases: the fee payer is this account and multi-signature
transactions are out of scope.

## Message signing

No network for either variant.

- **Mnemonic** — `account.Sign(message)` returns the 64-byte signature.
- **MWA** — `sign_messages` returns *signed payloads*: each is the message with its
  signature appended, so the signature is the trailing 64 bytes.

That extraction lives in `SolanaTransactionFramer.ExtractSignature`, tested, rather than as
an inline slice — an off-by-one here produces a plausible-looking but invalid signature.

## MWA reauthorization

Each MWA operation is a single session performing two calls:

1. Open the association and establish the session.
2. `authorize` with the **stored auth token** and the **currently selected chain**.
3. If the returned token or chain differs from what is stored, persist the refreshed key.
4. Perform the privileged call inside that same session.

One intent, one approval trip, and any network mismatch resolved as a side effect. A
declining user surfaces as `MwaAuthorizationException`, unchanged.

This requires extracting `MwaConnectFlow.WithAuthorizedSessionAsync<T>(identity, cluster,
authToken, operation, progress, token)` from the existing `ConnectAsync`, which currently
authorizes and immediately disposes the session with no way to do work inside it.
`ConnectAsync` becomes a thin caller of the new method.

## Error handling

| Condition | Surface |
|---|---|
| No Solana key configured | `ResolveAsync` returns null |
| User declines in wallet | `MwaAuthorizationException` |
| MWA wire fault, no wallet, wrong platform | `MwaProtocolException` and its existing subclasses |
| RPC call failed or returned an error result | `SolanaRpcException` |

## Testing

| Test | Guards |
|---|---|
| Shortvec encoding for 0, 1, 127, 128, 255 | The hand-written encoder Solnet keeps internal |
| `FrameUnsigned` total length and byte layout | `shortvec(n)` + n×64 zero bytes + message |
| Framed payload's message region is byte-identical to the input | Off-by-one in the signature slots |
| Framed payload's signature slots are all zero | The wallet needs empty slots to fill |
| `ExtractSignature` returns the trailing 64 bytes | The signed-payload convention |
| `ExtractSignature` rejects payloads under 64 bytes | Malformed wallet reply |
| `SolanaCluster.ToSolnetCluster()` for every value | Sending to the wrong network |

**Not unit tested:** the two account subclasses depend on static MAUI models
(`Preferences`, `KeysModel`), matching this codebase's conventions. They are verified by
compilation plus device testing. `SendAsync` end to end needs a funded account on a live
cluster; the MWA path additionally needs an Android device with a wallet installed. These
will be reported as untested rather than implied to work.

## Explicit exclusions

- Multi-signature transactions; the fee payer is the only signer.
- Priority fees, compute-budget instructions, and versioned transactions.
- Transaction simulation and confirmation polling. `SendAsync` returns once submitted.
- Balance and account-data queries. Nothing in scope needs them yet.
- Migrating existing call sites. Nothing consumes a Solana account today; this is the API
  the first consumer will use.
