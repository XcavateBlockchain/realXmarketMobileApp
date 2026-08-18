# Solana Wallet Standard injection into X25519WebView — Design

**Date:** 2026-07-28
**Status:** Approved, ready for implementation
**Builds on:** [Unified PlutoFrameworkSolanaAccount](2026-07-25-unified-solana-account-design.md)

## Goal

The messages dashboard hosted in `X25519WebView` already receives an injected Polkadot
wallet through `window.injectedWeb3`. Give it an injected **Solana** wallet as well, backed
by whichever Solana key the app holds, discoverable by dapps built on
[`@solana/wallet-adapter`](https://github.com/anza-xyz/wallet-adapter).

The two injections coexist. The Solana one must be indistinguishable from the Polkadot one
in the screens it shows the user.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Discovery mechanism | Wallet Standard registration, not a published adapter | wallet-adapter's own guidance: "For any wallet injected into the window in a browser, browser extension, or mobile app, you no longer need to publish an adapter at all." |
| Feature set | Varies by key type | `account.features` is what wallet-adapter gates each call on, so advertising per key type is the protocol's own mechanism, not a workaround. |
| `solana:signTransaction` under MWA | Not advertised | MWA 2.0 deprecated `sign_transactions`; `MwaClient` deliberately does not implement it. Advertising it would be a promise the wallet cannot keep. |
| Chains | All three, request wins | The dapp picks its own RPC endpoint. Declaring only the app-wide cluster would make a mainnet dapp silently fail whenever the app sits on Devnet. |
| Transaction confirmation UI | None beyond the key unlock | Matches `signPayload`, which shows no popup either. |
| Transport | The existing `mauiWallet` channel | A second channel would mean new `JavascriptInterface` and `WKScriptMessageHandler` types in both platform files for no gain. |
| Transaction parsing | Byte-level, not Solnet | Signing is over the message bytes regardless of transaction version, so a byte-level path supports v0 without Solnet needing to understand it. |

## Research findings

All verified against published source, not from memory.

### wallet-adapter ignores a wallet without a transaction feature

From `@solana/wallet-adapter-base/src/standard.ts`:

```ts
export function isWalletAdapterCompatibleStandardWallet(
    wallet: StandardWallet
): wallet is WalletAdapterCompatibleStandardWallet {
    return (
        StandardConnect in wallet.features &&
        StandardEvents in wallet.features &&
        (SolanaSignAndSendTransaction in wallet.features || SolanaSignTransaction in wallet.features)
    );
}
```

**A wallet exposing only `standard:connect`, `standard:events` and `solana:signMessage` is
invisible to wallet-adapter** — it never appears in the wallet list. `standard:disconnect`,
`solana:signMessage` and `solana:signIn` are all optional.

### Registration works regardless of load order

From `@wallet-standard/wallet/src/register.ts`:

```ts
export function registerWallet(wallet: Wallet): void {
    const callback: WindowRegisterWalletEventCallback = ({ register }) => register(wallet);
    try {
        (window as WalletEventsWindow).dispatchEvent(new RegisterWalletEvent(callback));
    } catch (error) {
        console.error('wallet-standard:register-wallet event could not be dispatched\n', error);
    }
    try {
        (window as WalletEventsWindow).addEventListener('wallet-standard:app-ready', ({ detail: api }) =>
            callback(api)
        );
    } catch (error) {
        console.error('wallet-standard:app-ready event listener could not be added\n', error);
    }
}
```

The `RegisterWalletEvent` is constructed with
`{ bubbles: false, cancelable: false, composed: false }` and overrides `preventDefault`,
`stopImmediatePropagation` and `stopPropagation` to throw.

**This is what makes injecting from `Navigated` safe.** The app dispatches
`wallet-standard:app-ready` and listens for `wallet-standard:register-wallet`; the wallet
does the mirror image. Registering after the page has loaded still lands. No move to a
document-start injection point is needed, and none is available on both platforms anyway.

### The adapter gates on the *account*, not only the wallet

From `@solana/wallet-standard-wallet-adapter-base/src/adapter.ts`:

```ts
if (SolanaSignAndSendTransaction in this.#wallet.features) {
    if (account.features.includes(SolanaSignAndSendTransaction)) {
        feature = SolanaSignAndSendTransaction;
    } else if (
        SolanaSignTransaction in this.#wallet.features &&
        account.features.includes(SolanaSignTransaction)
    ) {
        feature = SolanaSignTransaction;
    } else {
        throw new WalletAccountError();
    }
} else if (SolanaSignTransaction in this.#wallet.features) {
    if (!account.features.includes(SolanaSignTransaction)) throw new WalletAccountError();
    feature = SolanaSignTransaction;
} else {
    throw new WalletConfigError();
}

const chain = getChainForEndpoint(connection.rpcEndpoint);
if (!account.chains.includes(chain)) throw new WalletSendTransactionError();
```

Two consequences. Advertising a feature per key type is done through `account.features`, and
it is the mechanism the adapter already expects. And `account.chains` must contain whatever
`getChainForEndpoint` derives from the dapp's endpoint.

### Endpoint to chain mapping

`getChainForEndpoint` in `@solana/wallet-standard-util` matches on substrings: the literal
mainnet-beta URL, then `devnet`, `testnet`, `localhost`/`127.0.0.1`, defaulting to
`solana:mainnet`. A dapp's chain is therefore not something this app controls.

### The transaction arrives serialized and possibly part-signed

The adapter serializes with `{ requireAllSignatures: false, verifySignatures: false }` for
legacy transactions and `transaction.serialize()` for versioned ones, and may have applied
additional signers first. The payload is a wire-format transaction that may already carry
signatures in slots other than ours.

### Feature signatures

| Feature | Version | Method |
|---|---|---|
| `standard:connect` | `1.0.0` | `(input?: { silent?: boolean }) => Promise<{ accounts }>` |
| `standard:disconnect` | `1.0.0` | `() => Promise<void>` |
| `standard:events` | `1.0.0` | `on('change', listener) => () => void` |
| `solana:signMessage` | `1.1.0` | `(...inputs) => Promise<{ signedMessage, signature, signatureType? }[]>` |
| `solana:signTransaction` | `1.0.0` | `(...inputs) => Promise<{ signedTransaction }[]>`, plus `supportedTransactionVersions` |
| `solana:signAndSendTransaction` | `1.0.0` | `(...inputs) => Promise<{ signature }[]>`, plus `supportedTransactionVersions` |

Every method is variadic and returns an **array** of outputs, one per input. Getting this
wrong produces a wallet that appears to connect and then fails on first use.

### MWA 2.0 deprecated sign_transactions

Support for `sign_and_send_transactions` is mandatory in the Mobile Wallet Adapter 2.0
specification; `sign_transactions` is deprecated and wallets support it only for backwards
compatibility. This is why `MwaClient` never implemented it, and why the MWA account does not
advertise `solana:signTransaction`.

## Architecture

```
PlutoFrameworkCore/Solana/
  SolanaTransactionFramer.cs        # + Parse, FindSignerIndex, ApplySignature (pure, tested)

PlutoFramework/Model/Solana/
  PlutoFrameworkSolanaAccount.cs    # + wire-transaction signing surface
  MnemonicSolanaAccount.cs          # local signer
  MwaSolanaAccount.cs               # + explicit cluster on the authorized session

PlutoFramework/Components/WebView/
  SolanaWalletStandardBridge.cs     # new, sibling of PolkadotExtensionWalletBridge
  DAppApprovalModel.cs              # new, the approval logic both bridges share
  WebSignRawPopupViewModel.cs       # + pluggable signer, + real rejection

PlutoFramework/Components/Messages/
  X25519WebView.xaml.cs             # + routing, + the injected script

PlutoFramework/Resources/Raw/
  solanawalleticon.svg              # new, the wallet icon asset
```

`X25519WebView.android.cs` and `X25519WebView.ios.cs` are untouched.

## Transport

The Solana bridge reuses the `mauiWallet` `JavascriptInterface` / `WKScriptMessageHandler`
and the `window.__mauiWalletDeliver` reply path already in place. Methods are prefixed
`solana:`, and `X25519WebView.ProcessWalletRequestAsync` routes on that prefix:

| JS → native method | Handled by |
|---|---|
| `solana:connect` | `SolanaWalletStandardBridge` |
| `solana:disconnect` | `SolanaWalletStandardBridge` |
| `solana:signMessage` | `SolanaWalletStandardBridge` |
| `solana:signTransaction` | `SolanaWalletStandardBridge` |
| `solana:signAndSendTransaction` | `SolanaWalletStandardBridge` |
| everything else | `PolkadotExtensionWalletBridge`, unchanged |

Binary values cross the bridge as **base64 strings** and are converted to and from
`Uint8Array` on the JS side. JSON has no byte-array representation, and base64 avoids the
size cost of a number array.

Native pushes to JS go through `window.__plutoSolanaEmit(payload)`, dispatched with the
existing `DispatchScriptSafeAsync`.

## The injected wallet

Injected from `OnNavigated` alongside the existing scripts, guarded by
`window.__plutoSolanaInjected`. Self-contained vanilla JS: the injection point is a string
evaluated in the page, so no npm package can be imported and `registerWallet` is reproduced
inline from the source quoted above.

| Field | Value |
|---|---|
| `version` | `'1.0.0'` |
| `name` | `AppInfo.Name` |
| `icon` | `data:image/svg+xml;base64,…` |
| `chains` | `['solana:mainnet', 'solana:devnet', 'solana:testnet']` |
| `accounts` | `[]`, or the pre-authorized account — see [Autoconnect](#autoconnect) |
| `features` | the six below |

### The icon

Wallet Standard requires a data URI. `Resources/Images/logotransparentwhite.svg` is copied to
`Resources/Raw/solanawalleticon.svg` — `PlutoFramework.csproj` already registers
`Resources\Raw\**` wholesale as `MauiAsset` — and read once at runtime via
`FileSystem.OpenAppPackageFileAsync`, base64-encoded and cached. A minimal inline SVG is the
fallback if that read fails: a wallet with a malformed icon is worse than a plain one.

Copied rather than referenced because files under `Resources/Images` are processed into
platform image assets and are not readable as raw bytes at runtime. Not embedded as a source
constant either — that SVG is ~3.8 KB, which is ~5 KB of base64 noise in a C# file.

### Accounts

`standard:connect` returns exactly one account, since the app holds at most one Solana key.

| Field | Value |
|---|---|
| `address` | base58, from `PlutoFrameworkSolanaAccount.Address` |
| `publicKey` | the 32 raw bytes, base58-decoded from the address |
| `chains` | all three, as on the wallet |
| `features` | per key type, below |
| `label` | `DisplayName` |

```
MnemonicSolanaAccount            MwaSolanaAccount
  standard:connect                 standard:connect
  standard:disconnect              standard:disconnect
  standard:events                  standard:events
  solana:signMessage               solana:signMessage
  solana:signTransaction           solana:signAndSendTransaction
  solana:signAndSendTransaction
```

`supportedTransactionVersions` is `['legacy', 0]` on both transaction features.

### standard:events

The listener registry lives in JS. Native calls `window.__plutoSolanaEmit({ accounts: [...] })`
on connect and on disconnect, and the script fans that out to every `change` listener. `on`
returns its unsubscribe function.

Nothing else emits `change`. A Solana key changing while the WebView is open is not a flow
this app has, and inventing an observer for it would be speculative.

## UI/UX parity

| Wallet Standard feature | Native behaviour | Polkadot equivalent |
|---|---|---|
| `standard:connect` | `DAppApprovalModel.RequestAsync` → the same connection-request popup | `enable` |
| `standard:connect` with `silent: true` | approval cache only, never prompts | — |
| `solana:signMessage` | the same `WebSignRawPopupView` bottom sheet | `signRaw` |
| `solana:signTransaction` | no popup; `ResolveAsync(reason)` unlocks the key | `signPayload` |
| `solana:signAndSendTransaction` | as above, plus the wallet app's own screen under MWA | `signPayload` |
| `standard:disconnect` | clears the cached approval, empties `accounts`, emits `change` | — |

### DAppApprovalModel

`HandleEnableAsync`'s approval sequence is extracted verbatim so both bridges run the same
code rather than two copies that can drift:

1. `Application.Current.Resources["AllowedOrigins"]` contains the URL → approved.
2. `PlutoConfigurationModel.WhitelistedDApps` matches the host → approved.
3. `ExtensionWebViewModel.ApprovedUrls` has a cached answer for the host → use it.
4. Otherwise `DAppWebViewConnectionRequestPopupViewModel.ShowAsync(dAppInfo)`, and cache the
   result against the host.

`silent: true` stops after step 3 and returns no accounts rather than prompting. That is what
the flag exists for, and `autoConnect` relies on it not prompting.

### The sign-message popup

`WebSignRawPopupViewModel` gains a `Func<byte[], Task<byte[]>>? Signer`. When null it keeps
today's Substrate path — `KeysModel.GetAccountAsync`, Blake2 above 256 bytes, Ed25519 or
Sr25519 — so the Polkadot flow is byte-for-byte what it is now. The Solana bridge sets it to
`bytes => account.SignMessageAsync(bytes, reason, token)`.

The message is handed over as a `Plutonication.RawMessage` with `type = "bytes"`, `data` as
hex and `address` as the base58 Solana address, so the sheet renders the identical decoded
text and raw-bytes card.

**One deliberate fix.** `Reject` currently hides the sheet without completing
`SignatureTask`, so the dapp's promise never settles and the page hangs. Solana needs a real
rejection, and the same fix removes the same hang from the Polkadot path. `Reject` will fault
the task, and the bridge turns that into a rejected promise.

## Transaction signing

Solnet's deserializer does not handle versioned transactions well, and it does not need to:
a signature is over the message bytes whatever the version, so the transaction can be treated
as a frame around an opaque message.

```
shortvec(numSignatures) || numSignatures x 64 bytes || message
```

`SolanaTransactionFramer` gains three pure functions beside the existing `EncodeShortVectorLength`,
`FrameUnsigned` and `ExtractSignature`:

- **`Parse(byte[] wireTransaction)`** — decodes the shortvec count, slices out the signature
  slots and the message. Rejects a truncated or inconsistent payload.
- **`FindSignerIndex(byte[] message, byte[] publicKey)`** — walks the message header to the
  account-key array and returns our index, throwing when the key is absent or sits outside
  the signer range. Handles the v0 prefix byte: a leading byte with the high bit set is a
  version marker rather than `numRequiredSignatures`, and the header follows it.
- **`ApplySignature(parsed, index, signature)`** — writes the 64 bytes into that slot and
  reassembles, leaving every other slot as it arrived.

Signatures already present are preserved. The adapter may have applied extra signers before
handing the transaction over, and discarding those would produce a transaction that fails
submission for an unrelated-looking reason.

### The account surface

`PlutoFrameworkSolanaAccount` gains, beside the existing instruction-based `SendAsync`:

```csharp
/// <summary>Signs a serialized wire-format transaction, returning it with our signature filled in.</summary>
public abstract Task<byte[]> SignWireTransactionAsync(
    byte[] wireTransaction, string reason, CancellationToken token);

/// <summary>Signs and submits a serialized wire-format transaction on the given cluster.</summary>
public abstract Task<byte[]> SignAndSendWireTransactionAsync(
    byte[] wireTransaction, SolanaCluster cluster, string reason, CancellationToken token);
```

Both return raw bytes: the signed transaction and the 64-byte transaction signature
respectively, matching what the Wallet Standard outputs carry.

The cluster is a parameter rather than the `Cluster` property because the dapp's request
names it, and this design honours the request.

| | `SignWireTransactionAsync` | `SignAndSendWireTransactionAsync` |
|---|---|---|
| **Mnemonic** | parse, find index, `key.Account.Sign(message)`, apply | as left, then `SolanaRpcModel.SendTransactionAsync(cluster, …)`, base58-decoded to bytes |
| **MWA** | throws `NotSupportedException` — never reachable, since the feature is not advertised | `sign_and_send_transactions` with the payload as received, inside a session authorized on `cluster`; signature bytes returned as-is |

`MwaSolanaAccount.RunAuthorizedAsync` currently hardcodes the app-wide `Cluster`. It takes an
explicit cluster instead, with the existing callers passing `Cluster` so their behaviour does
not change. Reauthorizing onto the requested chain is exactly the mismatch-handling the
unified-account design already established, pointed at the dapp's choice rather than the
app's setting.

## Legacy Phantom-style provider

**Added 2026-07-29, after the Wallet Standard registration alone was found not to work
against the actual dashboard.**

The hosted dashboard does not consume the Wallet Standard. Its Solana path
(`app/services/wallet/solanaProvider.ts` and `walletCatalog.ts` in
[assetDidCommDashboard](https://github.com/rostislavlitovkin/assetDidCommDashboard)) probes
three globals directly:

```ts
if (w.phantom?.solana) return { provider: w.phantom.solana, name: 'Phantom' }
if (w.solflare)       return { provider: w.solflare,       name: 'Solflare' }
if (w.backpack)       return { provider: w.backpack,       name: 'Backpack' }
return null
```

and uses exactly three members of what it finds:

```ts
interface SolanaInjectedProvider {
  publicKey?: { toBase58(): string } | null
  connect(options?: { onlyIfTrusted?: boolean }): Promise<{ publicKey?: { toBase58(): string } } | void>
  signMessage(message: Uint8Array, display?: 'utf8' | 'hex'): Promise<{ signature: Uint8Array } | Uint8Array>
}
```

Its Polkadot path reads `window.injectedWeb3`, which is why the existing Polkadot injection
has always worked and why the mismatch was specific to Solana.

The injected script therefore installs a second surface at `window.phantom.solana`, backed by
the same bridge methods, alongside the Wallet Standard registration. Behaviour that the
dashboard's code depends on:

| Requirement | Why |
|---|---|
| `connect({ onlyIfTrusted: true })` must **reject** when unapproved | The dashboard reads a rejection as "not trusted yet" and leaves the stored session alone. Resolving without a key throws deeper in its own `resolveAddress` with a misleading message. |
| `connect()` resolves `{ publicKey: { toBase58() } }` | `resolveAddress` reads `connectResult.publicKey` first, falling back to `provider.publicKey`. |
| `provider.publicKey` non-null after connect | `signApiRequest` compares it against the address it was asked to sign for. |
| `signMessage` returns a real `Uint8Array` | The result goes straight into `@polkadot/util-crypto`'s `base58Encode`. |

The Wallet Standard registration is kept. It costs nothing, is already covered by tests, and
keeps the wallet usable by any other dapp built on `@solana/wallet-adapter`.

### Known limitation: the wallet is labelled "Phantom"

The dashboard's `WALLET_CATALOG` has exactly three Solana brand ids and takes the displayed
provider name from whichever global matched — there is no generic or first-party slot. Since
the app injects at `window.phantom.solana`, the dashboard shows the in-app wallet as
"Phantom".

Nothing on the injection side can change this; the name is decided by the dashboard's
detection order, not read from the provider. Fixing it properly means a change in the
dashboard: a fourth catalog entry detecting a first-party global, with `solanaProvider.ts`
probing it ahead of the three extensions.

## Autoconnect

At injection time, if the host already has a positive entry in
`ExtensionWebViewModel.ApprovedUrls`, `wallet.accounts` is pre-populated from
`KeysModel.GetSolanaAddress()` — the synchronous `Preferences` read that does **not** unlock
the key. `autoConnect` then works with no biometric prompt on page load, which is what the
Wallet Standard means by "this can be set by the Wallet so the app can use authorized
accounts on the initial page load".

`ResolveAsync` is called only when a signature is actually requested.

## Error handling

| Condition | Surface |
|---|---|
| No Solana key configured | `connect` rejects, "No Solana account is available" |
| User declines the connection popup | `connect` rejects |
| User rejects the sign-message sheet | `signMessage` rejects |
| MWA user declines in the wallet app | `MwaAuthorizationException` message, rejected |
| RPC submission failed | `SolanaRpcException` message, rejected |
| Malformed transaction bytes | `FormatException` message, rejected |
| Our key is not a required signer | `InvalidOperationException` message, rejected |

Errors travel back over the existing `{ id, error }` response shape, which the injected
script already turns into a promise rejection.

## Testing

Unit tests in `PlutoFrameworkTests`, on the byte-level work where a mistake produces a
plausible-looking but invalid signature rather than an obvious failure.

| Test | Guards |
|---|---|
| `Parse` round-trips a single-signature legacy transaction | the frame layout |
| `Parse` round-trips a multi-signature transaction | multi-byte shortvec counts |
| `Parse` rejects a truncated payload | a short read silently yielding a wrong message |
| `Parse` rejects a payload whose count exceeds its length | the same, from the other side |
| `FindSignerIndex` returns 0 for the fee payer | the common path |
| `FindSignerIndex` returns the right index for a second signer | the header walk |
| `FindSignerIndex` throws for a key present but not a signer | signing into a non-signer slot |
| `FindSignerIndex` throws for a key absent entirely | a mismatched account |
| `FindSignerIndex` handles a v0 message prefix byte | reading the version marker as a signer count |
| `ApplySignature` writes to the requested slot only | off-by-one across slots |
| `ApplySignature` preserves signatures already present | discarding a co-signer's signature |
| `ApplySignature` leaves the message region byte-identical | off-by-one into the message |

**Not unit tested**, matching this codebase's conventions: `SolanaWalletStandardBridge`,
`DAppApprovalModel` and the account subclasses depend on static MAUI models
(`Preferences`, `KeysModel`, `Application.Current.Resources`, `DependencyService`). They are
verified by compilation plus device testing. The end-to-end dapp flow needs a funded account
on a live cluster, and the MWA path additionally needs an Android device with a wallet
installed; both will be reported as untested rather than implied to work.

## Explicit exclusions

- **`solana:signIn` (SIWS).** It needs a domain-bound message builder and an approval screen
  with no Polkadot counterpart, so its UX could not be "identical" to anything existing.
- ~~**A legacy `window.solana` shim.**~~ **This assumption was wrong — see
  [Legacy Phantom-style provider](#legacy-phantom-style-provider).** The hosted dashboard
  does not use `@solana/wallet-adapter` or `@wallet-standard/app` at all, so the shim is
  required rather than optional and is now part of the implementation.
- **Multi-signature transactions where this account is not among the required signers.**
  `FindSignerIndex` throws rather than guessing.
- **Priority fees, compute-budget instructions, transaction simulation and confirmation
  polling.** Unchanged from the unified-account design.
- **The other WebViews.** `PolkadotExtensionWebView` and `AdvancedWebView` are out of scope.
  The bridge is written as a standalone class so wiring it into them later is routing only.
- **The Polkadot injection.** Untouched except for the shared approval extraction and the
  `Reject` fix, both of which preserve its behaviour or improve it identically.
