# Solana Transfer — Design

**Date:** 2026-07-29
**Status:** Implemented. See "Corrections found during implementation" for the four places
where the design as written was wrong or was improved on.
**Builds on:** [Solana balances and Solana-first onboarding](2026-07-25-solana-balances-design.md),
[Solana token detail page](2026-07-26-solana-token-detail-design.md),
[Unified PlutoFrameworkSolanaAccount](2026-07-25-unified-solana-account-design.md)

## Goal

1. A Transfer button on `SolanaBalancesPage` and on `SolanaTokenDetailPage`.
2. A Transfer popup that sends SOL and any whitelisted SPL token.
3. Both entry points into the QR scanner wired up: an in-popup scan button for the
   recipient field, and a `solana:` branch in the global scanner.
4. A token picker listing every whitelisted token for the current cluster, with balances
   queried from the cluster and refreshed while the picker is open.
5. Submitted transactions tracked in a toast stack, mirroring the Substrate extrinsic stack.

Both prior Solana specs listed transfer under "Explicit exclusions" because the framework
had no transaction building. That is no longer true — see "The signing layer already
exists" below. This spec closes that gap.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Instruction source | `Solana.Programs` 8.7.0 | The only source of `SystemProgram` / `TokenProgram` / `AssociatedTokenAccountProgram`. Matches the pinned `Solana.Wallet` / `Solana.Rpc` and adds no conflicting transitive dependency. |
| SPL transfer instruction | `TransferChecked`, not `Transfer` | It carries decimals, so the chain rejects a decimals mistake. Plain `Transfer` would silently send 1000× on a wrong-decimals bug. |
| Picker balance freshness | Poll every 10s while open | Chosen by the product owner. Reuses the existing balance query, adds no RPC surface, and stops with the popup. |
| Picker balance figure | The derived ATA's amount, not the row's sum | An SPL transfer spends from one account. The balances row sums every account for a mint, so Max off the sum could fill an unsendable amount. See "The balances row can overstate what is spendable". |
| Missing recipient ATA | Created, sender pays the rent, not disclosed | Chosen by the product owner, with the ~0.00204 SOL cost understood. A failure caused by insufficient SOL is still reported in plain words. |
| Amount validation | Balance-checked, with a Max button | Chosen by the product owner. Catches the two commonest failures before a round trip. |
| SOL Max reserve | 0.001 SOL | 200× the 5000-lamport signature fee, leaving headroom for a later ATA-creating send. SPL Max takes the full balance — an SPL send spends no SPL on fees. |
| Base-unit conversion | Truncates | A user must never submit more than the number they typed. |
| Address validation | base58 decode to exactly 32 bytes | Solana addresses are 32–44 characters. The Substrate `Length == 48` rule accepts garbage and rejects valid addresses. |
| Tracking UI | A parallel Solana stack | Chosen by the product owner over generalizing `ExtrinsicInfo`, which is coupled to `Substrate.NetApi` types. The overlap this creates is mitigated by offsetting, below. |
| Tracking mechanism | Poll `getSignatureStatuses` | Solana has no `SubmitAndWatchExtrinsic` equivalent. Solnet's streaming client exists but would mean a second transport for one screen. |
| `processed` status | Folded into Pending | Keeps the toast vocabulary 1:1 with the Substrate stack, which is what "the same way" means here. |
| Solana Pay parameters | Recipient only; `amount` / `spl-token` ignored | Prefilling a token means mapping an arbitrary mint against the whitelist and deciding what to do when it is absent. That is a feature of its own. |
| Explorer | Solscan | Mirrors the Subscan deep link the Substrate toast already offers. |

## Research findings

### The signing layer already exists

`PlutoFramework/Model/Solana/PlutoFrameworkSolanaAccount.cs:97`:

```csharp
public async Task<string> SendAsync(
    IEnumerable<TransactionInstruction> instructions, string reason, CancellationToken token)
{
    var blockHash = await SolanaRpcModel.GetLatestBlockHashAsync(cluster, token);
    var builder = new TransactionBuilder()
        .SetRecentBlockHash(blockHash)
        .SetFeePayer(new SolanaPublicKey(Address));
    foreach (var instruction in instructionList) builder.AddInstruction(instruction);
    return await SignAndSubmitAsync(builder, cluster, reason, token);
}
```

It builds, fetches a blockhash, signs and submits for both key variants — `MnemonicSolanaAccount`
signs offline and submits through `SolanaRpcModel`, `MwaSolanaAccount` hands the compiled
message to the wallet via `sign_and_send_transactions`. Both return the base58 signature,
and the doc comment states it does not wait for confirmation.

**It has zero callers.** Verified by grep across `PlutoFramework/`, `PlutoFrameworkCore/`
and `PlutoFrameworkTests/`: the only hit is the definition. No `SystemProgram`,
`TokenProgram` or `AssociatedTokenAccountProgram` reference exists anywhere in the repo.
The gap is instructions and UI, not signing.

The comment at `PlutoFramework/Components/Solana/SolanaTokenDetailPage.xaml:230-231` —
*"Receive only. The framework has no Solana transaction building, so a Transfer button here
could not do anything"* — is stale. That file dates from 2026-07-26; signing landed in
`85afc536` on 2026-07-28. It is deleted by this work.

### `Solana.Programs` is a separate package and is safe to add

`PlutoFrameworkCore.csproj` references `Solana.Wallet` and `Solana.Rpc` at 8.7.0 but not
`Solana.Programs`, which is where the instruction builders live. Checked against
nuget.org on 2026-07-29: `Solana.Programs` publishes 8.7.0, and its nuspec declares exactly
two dependencies:

```xml
<dependency id="Solana.Rpc" version="8.7.0" />
<dependency id="System.Runtime.Extensions" version="4.3.1" />
```

No BouncyCastle. The conflict documented in the csproj comment — the legacy `Solnet.*` ids
pulling `Portable.BouncyCastle` 1.9.0 against the `bc26`-aliased `BouncyCastle.Cryptography`
2.6.0 — does not recur.

### Verified instruction signatures

Read from `bmresearch/Solnet@master` on 2026-07-29:

```csharp
// SystemProgram.cs:81
public static TransactionInstruction Transfer(PublicKey fromPublicKey, PublicKey toPublicKey, ulong lamports);

// TokenProgram.cs:91
public static TransactionInstruction TransferChecked(
    PublicKey source, PublicKey destination, ulong amount, int decimals,
    PublicKey authority, PublicKey tokenMint, IEnumerable<PublicKey> signers = null);

// AssociatedTokenAccountProgram.cs:55, :99
public static TransactionInstruction CreateAssociatedTokenAccount(
    PublicKey payer, PublicKey owner, PublicKey mint, PublicKey tokenProgramId);
public static PublicKey DeriveAssociatedTokenAccount(
    PublicKey owner, PublicKey mint, PublicKey tokenProgramId);
```

Both ATA methods take an explicit `tokenProgramId`, so the whitelist's `ProgramId` field
carries through to Token-2022 mints without a code change — which is what that field was
added for.

`CreateAssociatedTokenAccount` returns **null** when the address cannot be derived. The
plan builder must treat a null instruction as a failure rather than adding it to the list.

### The balances row can overstate what is spendable

`SolanaBalanceAssembler` sums every token account for a mint into one row, deliberately —
the balances spec records this as guarding against under-reporting a split balance. But an
SPL transfer draws on **one** account. A wallet holding USDC across two accounts shows 40 on
the balances page while the derived ATA holds 25, and a Max button reading the row would
fill an amount that cannot send.

The transfer path therefore resolves the **derived ATA's own amount** as the spendable
figure, and the picker displays that. Everything inside the transfer flow then consistently
answers "what can I send". The divergence from the balances page only appears for wallets
holding a mint in a non-ATA token account, which requires deliberate action to create.

No new RPC call is needed: `SolanaRpcModel.GetTokenAccountsAsync` already returns each
account's `PublicKey` alongside its parsed `TokenAmount`; the assembler simply discards the
pubkeys. `SolanaTransferModel` keeps them and matches against the derived address.

### `getSignatureStatuses` returns null for unknown signatures

Verified signature, `Solnet.Rpc/IRpcClient.cs:658`:

```csharp
Task<RequestResult<ResponseValue<List<SignatureStatusInfo>>>> GetSignatureStatusesAsync(
    List<string> transactionHashes, bool searchTransactionHistory = false);
```

`SignatureStatusInfo` (`Solnet.Rpc/Models/Signature.cs:8`) carries `Slot`, `Confirmations`,
`Error` (mapped from `err`), `ConfirmationStatus` (`"processed"` / `"confirmed"` /
`"finalized"`), `Memo`, `Signature` and `BlockTime`.

The list contains a **null** at the index of any signature the node has not seen. Null means
"not yet observed", not "failed". Conflating them would paint a red Failed toast over every
healthy transaction during its first seconds. The mapper treats null as Pending, and this is
the single most important test in the suite.

### The Substrate tracking UI cannot be reused as-is

`PlutoFramework/Components/Extrinsic/ExtrinsicInfo.cs` carries a
`Substrate.NetApi.Model.Types.Base.Hash`, a `PlutoFramework.Constants.Endpoint` (which
`ExtrinsicStatusView` reads to set the chain icon, `ExtrinsicStatusView.xaml.cs:139`), and a
`TaskCompletionSource<EventsListViewModel>` whose `.Task` the tap handler awaits
(`ExtrinsicStatusView.xaml.cs:295`) before pushing `ExtrinsicDetailPage`.

A Solana row has none of those. Populating them with synthetic values would leave the tap
handler awaiting a `TaskCompletionSource` that is never completed — a toast that hangs when
tapped. Hence the parallel stack.

### Both stacks would occupy the same bounds

`ExtrinsicStatusStackLayout` is mounted in the shared control template at
`PlutoFramework/Templates/PageTemplate/Page.xaml:72` (`ZIndex="5"`), so **every**
`PageTemplate` page — including both Solana pages — already renders it. Its bounds come from
`ExtrinsicStatusStackViewModel.LayoutBounds`, `new Rect(0.5, 0, 1, HeightRequest)` with
`PositionProportional,WidthProportional`.

A second stack mounted the same way lands on top of the first. The mitigation is in
Part D.

### The scanner has no `solana:` branch

`PlutoFramework/Model/NavigationModel.cs:83-162` handles `plutonication:`, `plutolayout:`,
`substrate:` and a raw Parity-Signer hex payload, and falls through to an "Unable to read QR
code" popup.

The app already **emits** `solana:{address}` QR codes from four places —
`SolanaBalancesPageViewModel.cs:47`, `SolanaTokenDetailPageViewModel.cs:304`,
`SolanaMnemonicKeyDetailPageViewModel.cs:24` and `SolanaMwaKeyDetailPageViewModel.cs:31`.
Scanning one of the app's own QR codes therefore fails today. This work fixes that as a
side effect of wiring the transfer flow.

### `ReceiveAndTransferModel` cannot serve these pages

`PlutoFramework/Model/ReceiveAndTransferModel.cs:67-83` opens `Transfer()` with a
`KeysModel.HasSubstrateKey()` guard that shows a "no account" popup. New accounts are
Solana-only, so routing through it would refuse the very user the page serves — the same
reason `SolanaTokenDetailPageViewModel.Receive()` already drives `AddressQrCodeViewModel`
directly.

## Architecture

```
PlutoFrameworkCore/Solana/                   (no MAUI, unit tested)
  SolanaTransferModel.cs        new — spendable balances, ATA probe, plan building
  SolanaTransferPlan.cs         new — instructions + spendable amount + creates-ATA flag
  SolanaTransferBalance.cs      new — one picker row
  SolanaAddressValidator.cs     new — base58 decode to exactly 32 bytes
  SolanaFees.cs                 new — lamport constants
  SolanaTransactionStatus.cs    new — enum
  SolanaSignatureStatusMapper.cs new — pure: SignatureStatusInfo? -> status
  SolanaAmount.cs               + ToBaseUnits
  SolanaRpcModel.cs             + GetAccountInfoAsync, GetSignatureStatusesAsync
  Constants/Solscan.cs          new — explorer URLs

PlutoFramework/Components/Solana/Transfer/    (MAUI)
  SolanaTransferView.xaml(.cs), SolanaTransferViewModel.cs
  SolanaTokenSelectView.xaml(.cs), SolanaTokenSelectViewModel.cs
  SolanaTokenSelectRowView.xaml(.cs)

PlutoFramework/Components/Solana/Status/      (MAUI)
  SolanaTransactionInfo.cs
  SolanaTransactionStatusStackViewModel.cs
  SolanaTransactionStatusStackLayout.xaml(.cs)
  SolanaTransactionStatusView.xaml(.cs)
  SolanaTransactionTracker.cs

PlutoFramework/
  Components/Solana/SolanaBalancesPage.xaml(.cs)     + Transfer button, popups
  Components/Solana/SolanaBalancesPageViewModel.cs   + confirmed-event reload
  Components/Solana/SolanaTokenDetailPage.xaml       + Transfer button, popups
  Components/Solana/SolanaTokenDetailPageViewModel.cs + TransferCommand
  Components/Solana/SolanaBalanceCellView.xaml.cs    + confirmed-event reload
  Model/NavigationModel.cs                           + solana: branch
  Templates/PageTemplate/Page.xaml                   + Solana status stack
  MauiAppBuilderExtensions.cs                        + DI registrations

PlutoFrameworkCore/PlutoFrameworkCore.csproj         + Solana.Programs 8.7.0
```

Everything platform-agnostic stays in Core and is unit tested. The MAUI layer holds only
what depends on navigation, `Preferences` or `SecureStorage`.

## Part A — Core transfer model

### Constants

```csharp
public static class SolanaFees
{
    /// <summary>Lamports per signature. Every transaction here has exactly one.</summary>
    public const ulong LamportsPerSignature = 5_000;

    /// <summary>
    /// Held back by Max on a SOL send. 200x the signature fee, so a subsequent
    /// ATA-creating SPL send (~0.00204 SOL of rent) still clears.
    /// </summary>
    public const ulong MaxReserveLamports = 1_000_000;
}
```

### Address validation

```csharp
public static bool IsValidAddress(string? address);
```

Base58-decodes with the existing `SolanaBase58` and requires exactly 32 bytes. Deliberately
not a length check: the Substrate `Length == 48` rule duplicated across `TransferViewModel`
and `NftTransferViewModel` would accept a 48-character non-address and reject a valid
32-character Solana one.

### Spendable balances

```csharp
public static Task<IReadOnlyList<SolanaTransferBalance>> GetTransferableBalancesAsync(
    string owner, SolanaCluster cluster, CancellationToken token);
```

Follows `SolanaBalancesModel.GetBalancesAsync`'s shape: one `GetLamportBalanceAsync`, then
one `GetTokenAccountsAsync` per distinct `ProgramId` in the cluster's whitelist. It then
derives each mint's ATA and takes **that account's** amount, rather than summing.

```csharp
public sealed record SolanaTransferBalance
{
    public required string Symbol { get; init; }
    public required string Mint { get; init; }
    public required int Decimals { get; init; }
    public required string ProgramId { get; init; }
    public required bool IsNative { get; init; }

    /// <summary>Base units held in the derived ATA — what a transfer can actually spend.</summary>
    public required BigInteger SpendableBaseUnits { get; init; }
}
```

SOL first, then whitelist order, zeros included — the same ordering the balances page uses.

### The plan

```csharp
public static Task<SolanaTransferPlan> BuildAsync(
    string senderAddress, string recipientAddress, SolanaTransferBalance token,
    BigInteger baseUnits, SolanaCluster cluster, CancellationToken cancellationToken);
```

| Token | Instructions |
|---|---|
| SOL | `SystemProgram.Transfer(sender, recipient, lamports)` |
| SPL, recipient ATA exists | `TokenProgram.TransferChecked(senderAta, recipientAta, amount, decimals, sender, mint)` |
| SPL, recipient ATA absent | `CreateAssociatedTokenAccount(payer: sender, owner: recipient, mint, programId)` **then** the above |

Existence is probed with `GetAccountInfoAsync` on the derived recipient ATA. A null
`AccountInfo` means absent; an RPC failure throws `SolanaRpcException` rather than assuming
absent, because assuming absent on a network blip would add a create instruction that fails
against an account that already exists.

```csharp
public sealed record SolanaTransferPlan
{
    public required IReadOnlyList<TransactionInstruction> Instructions { get; init; }
    public required bool CreatesRecipientAccount { get; init; }
}
```

`CreatesRecipientAccount` is not shown to the user — per the decision above, the rent is not
disclosed — but it is carried so the failure path can say "Not enough SOL to complete this
transfer" instead of surfacing a raw RPC string. Concealing a cost is a product decision;
concealing a *failure* is a bug.

### Base units

`SolanaAmount.ToBaseUnits(decimal amount, int decimals)`, the inverse of the existing
`FromBaseUnits`. It **truncates**:

```csharp
var scaled = amount * (decimal)BigInteger.Pow(10, decimals);
return (BigInteger)decimal.Truncate(scaled);
```

Rounding could turn a Max-filled balance into one base unit more than the wallet holds.
`BigInteger.Pow`, not `Math.Pow`, so no double ever touches a money figure.

## Part B — The popup

### Placement

`SolanaBalancesPage` gains a full-width Transfer button below the address card. The existing
`AddressView` already covers receive, so no button pair is introduced. The button binds to
`HasAccount`, so the no-key empty state does not offer a transfer it cannot perform.

`SolanaTokenDetailPage`'s `PageBottomBarView` becomes a two-column Receive / Transfer pair,
matching `AssetDetailPage.xaml:152-182`. Its Transfer **preselects the token being viewed**.
The stale "no Solana transaction building" comment is deleted.

### Layout

```
┌ Transfer ────────────────────────────┐
│  [ Recipient address        ]  [ QR ]│
│  ⚠ Not a valid Solana address        │
│                                      │
│  [ 0.5241         ] [Max]  ( SOL ▾ ) │
│  Balance: 0.5241 SOL                 │
│  ⚠ Insufficient balance              │
│                                      │
│  [ Cancel ]          [ Transfer ]    │
└──────────────────────────────────────┘
```

A `card:BottomPopupCard` titled "Transfer", mirroring `TransferView.xaml`'s structure, listed
in each host page's `PageTemplate.PopupContent`. `SolanaTokenSelectView` is a second
`BottomPopupCard` stacked over it, exactly as `AssetSelectView` stacks over `TransferView`.

Both view models are DI singletons registered in `MauiAppBuilderExtensions`, matching
`TransferViewModel` and `AssetSelectViewModel`.

The popup binds its own `BindingContext` in its constructor. `PageTemplate`'s popup layout
inherits the *page's* BindingContext (`PageTemplate.cs:202-218`), so a popup bound to a
singleton must set its own — the same reason `TransferView` does.

### Validation

`ConfirmButtonState` is `Enabled` only when all of:

- `SolanaAddressValidator.IsValidAddress(Recipient)`
- `decimal.TryParse(Amount, …)` and the result is `> 0`
- base units `<= SpendableBaseUnits`
- for SOL only, base units `+ SolanaFees.LamportsPerSignature <= SpendableBaseUnits`

Address and amount are hand-written setters that re-run validation on every keystroke,
matching `TransferViewModel.cs:16-48`. Messages are specific — "Not a valid Solana address"
and "Insufficient balance" are different problems and read differently.

### Max

Fills `SpendableBaseUnits` for an SPL token, and `SpendableBaseUnits - MaxReserveLamports`
for SOL, floored at zero. An SPL send spends no SPL on fees, so reserving there would strand
tokens for no reason.

### The picker

`SolanaTokenSelectViewModel.Appear()` starts a 10-second poll of
`GetTransferableBalancesAsync`, cancel-and-replace per tick using the `ReplaceLoadingToken`
pattern `SolanaBalancesPageViewModel.cs:77-88` established. `Disappear()` cancels it. Rows
update in place; the selected row stays selected across refreshes.

Every whitelisted token for the current cluster appears, plus SOL, zeros included — the page
is "the tokens this app deals in", the same reasoning the balances spec records.

A failed poll leaves the previous figures on screen and shows an inline message. A picker
that blanks itself on one bad round trip is worse than one showing a 10-second-old number.

Rows show the amount and symbol only, **no USD value** — unlike Substrate's
`AssetSelectorView`. Pricing would mean a Jupiter call inside a loop that already runs every
10 seconds, and the figure that decides a transfer is the token amount. The USD value stays
one tap away on the balances page.

### Submit

```csharp
var plan = await SolanaTransferModel.BuildAsync(sender, recipient, token, baseUnits, cluster, ct);

var info = stack.Register(description, cluster);   // toast at Submitting, before anything slow
SetToDefault();                                    // close the popup

var account = await PlutoFrameworkSolanaAccount.ResolveAsync(reason, ct);
if (account is null)
{
    info.Status = SolanaTransactionStatus.Error;   // key unavailable, or the unlock was declined
    return;
}

var signature = await account.SendAsync(plan.Instructions, reason, ct);
info.Signature = signature;
_ = SolanaTransactionTracker.TrackAsync(signature, cluster, info, ct);
```

The toast is registered **first**, so the user sees something the moment they tap rather
than during the unlock prompt. The popup closes **before** submitting: an MWA key launches
an intent and backgrounds the app, and returning to a stale popup sitting over a toast that
already says "Submitting" reads as a transfer that did not happen.

`ResolveAsync` returns null when there is no key *or* when the user declines the unlock
prompt. The Transfer button is gated on `HasAccount`, so in practice this is the declined
case, and it must land on `Error` rather than leaving a toast stuck at `Submitting`
forever. `SendAsync` throwing lands on `Error` the same way.

## Part C — QR scanner

### In the popup

The recipient field's QR icon pushes `UniversalScannerPage` with an `OnScannedMethod`
callback, mirroring `IdentityAddressView.xaml.cs:240-271`. The callback parses, assigns, and
pops.

### In the global scanner

`NavigationModel.OnScanned` gains a `solana:` branch beside the `substrate:` one at
`NavigationModel.cs:110-128`. It navigates to `SolanaBalancesPage` **first**, then opens the
popup pre-filled.

Navigating first is a deliberate difference from the Substrate branch, which sets
`TransferViewModel.IsVisible = true` from whatever page is showing — and `TransferView` is
only listed in some pages' `PopupContent`, so on the others nothing appears. Navigating to a
page known to host the popup makes the scan always do something visible.

### Parsing

One shared helper, so the two call sites cannot drift:

```csharp
public static string? TryParseRecipient(string scanned);
```

- `solana:<address>` → the address
- `solana:<address>?amount=1&spl-token=<mint>` → the address, query discarded
- a bare base58 address → itself
- anything else → null

Solana Pay's `amount` and `spl-token` are discarded on purpose. Honouring `spl-token` means
resolving an arbitrary mint against the whitelist and deciding what to do when it is absent —
a feature with its own design, not a parser detail. Discarding is safe: the user still gets
the right recipient and fills the rest in.

## Part D — Tracking

### Status

```csharp
public enum SolanaTransactionStatus
{
    Submitting,        // signing, or handed to the wallet; no signature yet
    Pending,           // signature returned; status null or "processed"
    ConfirmedSuccess,  // "confirmed", err == null
    ConfirmedFailed,   // "confirmed", err != null
    FinalizedSuccess,  // "finalized", err == null
    FinalizedFailed,   // "finalized", err != null
    Dropped,           // blockhash expired without the signature ever being seen
    Error,             // SendAsync threw; never reached the network
}
```

`processed` folds into `Pending` so the toast vocabulary maps 1:1 onto
`ExtrinsicStatusEnum`, which is what "track it the same way" means. Unlike Substrate's
`ExtrinsicStatusEnum.Error`, which is assigned nowhere in the codebase, `Error` here is
reachable.

### The mapper

```csharp
public static SolanaTransactionStatus Map(SignatureStatusInfo? status);
```

Pure and exhaustively tested:

| Input | Result |
|---|---|
| `null` | `Pending` |
| `ConfirmationStatus == "processed"` | `Pending` |
| `"confirmed"`, `Error == null` | `ConfirmedSuccess` |
| `"confirmed"`, `Error != null` | `ConfirmedFailed` |
| `"finalized"`, `Error == null` | `FinalizedSuccess` |
| `"finalized"`, `Error != null` | `FinalizedFailed` |
| unrecognised or absent status string | `Pending` |

Unrecognised maps to `Pending`, not to a failure. A status string this client does not know
is not evidence a transaction failed.

### The tracker

`SolanaTransactionTracker.TrackAsync(signature, cluster, info, token)` polls
`GetSignatureStatusesAsync([signature], searchTransactionHistory: false)` every 2 seconds.

- Still `Pending` after **90 s** → `Dropped`. A blockhash is valid for roughly 150 slots; past
  that the transaction can no longer land, so "Dropped" is a fact and not a guess.
- Stops at either finalized state, or at **120 s** total, leaving the last known status on
  screen. A transaction confirmed but not yet finalized is a real state, not an error.
- A failed poll is skipped, not fatal. Losing one 2-second tick to a network blip must not
  end tracking.

### The stack

`SolanaTransactionInfo` mirrors `ExtrinsicInfo`'s shape without the Substrate types:
`Status` (raising `PropertyChanged`), `Cluster`, `Description` (e.g. `"Transfer 0.5 SOL"`),
and a **mutable, initially null** `Signature`. It is null on purpose: the toast exists at
`Submitting` before a signature is known, and an MWA send may never return one.

`SolanaTransactionStatusStackViewModel` mirrors `ExtrinsicStatusStackViewModel`: a
dictionary as the source of truth, an `ObservableCollection` rebuilt in `Update()`,
`IsVisible` and `HeightRequest`. It differs in one way — `Register(description, cluster)`
returns a new `SolanaTransactionInfo` keyed by a generated id rather than by the signature,
since the Substrate stack can key on a hash computed before submission and this one cannot.
`Signature` is filled in afterwards and is only used for the Solscan link, which is hidden
while it is null.

`SolanaTransactionStatusView` mirrors `ExtrinsicStatusView`: swipe-to-dismiss, a close
button, and a 5-second auto-dismiss after `FinalizedSuccess`. Where the Substrate toast
reads its chain icon from `Endpoint.Icon`, this one uses the `solana.png` that
`Constants/Assets.cs` already maps — a static image, since there is only one Solana network
family and the cluster is not a different chain. Labels and colours parallel it:

| Status | Label | Colour |
|---|---|---|
| `Submitting` | Submitting | Gray |
| `Pending` | Pending | Orange |
| `ConfirmedSuccess` | Confirmed - Success | Green |
| `ConfirmedFailed` | Confirmed - Failed | DarkRed |
| `FinalizedSuccess` | Finalized - Success | Green |
| `FinalizedFailed` | Finalized - Failed | DarkRed |
| `Dropped` | Dropped | DarkRed |
| `Error` | Error | DarkRed |

Tapping opens `WebViewPage` on the Solscan URL, mirroring the Subscan deep link at
`ExtrinsicDetailPage.xaml.cs:24-31`:

```csharp
public static class Solscan
{
    // Solscan defaults to mainnet and takes the cluster as a query parameter.
    public static string TransactionUrl(string signature, SolanaCluster cluster) => cluster switch
    {
        SolanaCluster.Mainnet => $"https://solscan.io/tx/{signature}",
        _ => $"https://solscan.io/tx/{signature}?cluster={cluster.GetName().ToLower()}",
    };
}
```

### Avoiding the overlap

`SolanaTransactionStatusStackLayout` mounts in `Page.xaml`'s control template beside the
Substrate stack. Its code-behind subscribes to `ExtrinsicStatusStackViewModel.PropertyChanged`
for `HeightRequest` and offsets its own `TranslationY` to sit below it, unsubscribing in
`OnHandlerChanged` when detached.

That is a one-way read of an existing singleton — no edit to `ExtrinsicStatusStackViewModel`
or `ExtrinsicStatusView`, so the Substrate path cannot regress. Both stacks can be non-empty
at once without covering each other.

### Refreshing after a send

A static `SolanaTransactionConfirmed` event is raised on the first transition into
`ConfirmedSuccess`. `SolanaBalancesPageViewModel`, `SolanaBalanceCellView` and an open
`SolanaTokenSelectViewModel` subscribe and reload — the Solana counterpart of the
`MainPageLayoutUpdater.ReloadAsync` call the Substrate tracker makes on in-block
(`PlutoFrameworkSubstrateClient.cs:206`).

Every subscriber unsubscribes in its existing `Unsubscribe()`, alongside `ClusterChanged`.
The balances spec already records that these static events leak a view model per page visit
if this is skipped.

## Testing

NUnit 4 in `PlutoFrameworkTests`, matching the existing Solana suites.

| Test | Guards |
|---|---|
| Validator rejects a 48-character SS58 address | The `Length == 48` rule being copied across |
| Validator accepts a real 32-byte base58 address | — |
| Base58 decoding to a length other than 32 is rejected | Garbage passing a length check |
| Non-base58 characters are rejected without throwing | A malformed scan crashing the popup |
| `ToBaseUnits` truncates rather than rounds | Submitting one base unit more than is held |
| `ToBaseUnits` round-trips with `FromBaseUnits` at 6 and 9 decimals | Off-by-10^n |
| SOL plan is exactly one `SystemProgram.Transfer` | — |
| SPL plan with a present recipient ATA has no create instruction | Paying rent for an account that exists |
| SPL plan with an absent ATA orders create **before** transfer | Transferring into an account that does not exist yet |
| `CreatesRecipientAccount` reflects the probe | A failure that cannot be explained |
| Derived ATA matches a known owner / mint / program triple | Wrong seeds sending funds to a dead address |
| A Token-2022 program id reaches both derivation and `TransferChecked` | The whitelist `ProgramId` field being ignored |
| Spendable is the derived ATA's amount, not the sum across accounts | Max filling an unsendable amount |
| A mint held only in a non-ATA account reports zero spendable | Silently spending from the wrong account |
| Mapper: `null` → `Pending` | **A red Failed toast on every healthy transaction** |
| Mapper: `"processed"` → `Pending` | The same, one step later |
| Mapper: confirmed + err → `ConfirmedFailed` | A failed transfer reading as success |
| Mapper: finalized + err → `FinalizedFailed` | The same, at finality |
| Mapper: unknown status string → `Pending` | Treating an unrecognised state as failure |
| Max subtracts the reserve for SOL but not for SPL | A SOL send with nothing left for the fee |
| Max floors at zero on a dust balance | A negative amount reaching the builder |
| `solana:<a>`, `solana:<a>?amount=1`, bare `<a>` all yield the address | Scanner regressions |
| `TryParseRecipient` returns null for `substrate:…` and for junk | A Substrate address entering a Solana transfer |
| Solscan URL carries `?cluster=devnet` off-mainnet and omits it on mainnet | A devnet transaction linking to a mainnet page |

**Not unit tested, and to be reported as untested rather than implied to work:** every live
RPC call (`getAccountInfo`, `getSignatureStatuses`, `sendTransaction`); the tracker against a
real cluster; submission through Mobile Wallet Adapter, which needs an Android device with a
wallet installed; the toast stack's visual offset; and the picker's 10-second poll behaviour
over time.

## Explicit exclusions

- **Transaction history.** No `getSignaturesForAddress`. The toast stack is in-memory and
  lost on restart, exactly as the Substrate stack is.
- **Fee estimation shown in the popup.** The Substrate popup's fee display is commented-out
  dead code with `IsVisible="False"`; this one does not add what that one does not have.
- **Disclosing the ATA rent.** Decided against. Only the failure it can cause is reported.
- **Solana Pay `amount` / `spl-token` prefill.** Parsed out and discarded; see Part C.
- **Transaction simulation before submit.** `simulateTransaction` is not on `SolanaRpcModel`,
  and the Substrate equivalent — the Chopsticks dry run in
  `TransactionAnalyzerConfirmationViewModel` — is itself unreachable behind an early return
  at `TransactionAnalyzerConfirmationViewModel.cs:152-154`.
- **Sending to a non-whitelisted mint.** The picker is the whitelist.
- **Multi-account sends.** A transfer spends from the derived ATA only; consolidating split
  balances is a separate feature.
- **Priority fees / compute budget instructions.** Not needed at current Solana load, and a
  wrong compute-unit price is a way to overpay silently.
- **Transferring NFTs or Token-2022 mints with transfer hooks.** The whitelist has no
  Token-2022 entry today; the `ProgramId` field means adding a plain one is configuration.
- **A Solana `TransactionAnalyzer` equivalent.** No confirmation screen with decoded effects.

## Corrections found during implementation

### The `ProgramId` field could not simply "carry through"

"Verified instruction signatures" above was read from `bmresearch/Solnet@master`. The
**published** `Solana.Programs` 8.7.0 — the newest release — ships only:

```
AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, mint)
AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(payer, owner, mint)
```

Both hardcode the legacy SPL token program, as does `TokenProgram.TransferChecked`, which
stamps `TokenProgram.ProgramIdKey` onto the instruction it returns. The `tokenProgramId`
overloads exist on master and have never been released.

So the claim that a Token-2022 mint is configuration rather than a code change was false as
written. `SolanaAssociatedTokenAccount` now does the derivation and the create instruction
with an explicit token program, and the planner corrects the program id on the transfer
instruction. `LegacyDerivationMatchesSolnetsOwn` pins the derivation against Solnet's for
the legacy case, so the seed order is checked against an independent implementation rather
than against itself.

The second half of this — `TransferChecked` hardcoding the program too — was found by a test
rather than by reading, which is what the Token-2022 case in the plan tests is for.

### The SOL reserve does not cover token-account rent

The reserve was justified as "leaving headroom for a subsequent ATA-creating send". It does
not: 0.001 SOL is 1,000,000 lamports and associated-token-account rent is about 2,039,280.
The value is kept, with the honest justification — 200 times the signature fee, so a Max SOL
send pays for itself with room to spare. Sizing it to fund a hypothetical later SPL transfer
would quietly send less SOL than the user asked to.

### The popup is hosted by the page template, not per page

Part C had the global scanner navigate to `SolanaBalancesPage` before opening the popup,
because the popup was to be listed in each page's `PopupContent`. Putting
`SolanaTransferView` and `SolanaTokenSelectView` in `Page.xaml`'s control template instead —
where `ConnectMwaPopupView` and `EnterPasswordPopupView` already live — makes the popup
available on every page, so the scan opens it in place with no navigation at all.

### One view model, not two

The design named a separate `SolanaTokenSelectViewModel`. The Substrate side splits
`TransferViewModel` from `AssetSelectViewModel` because that picker is also used by the NFT
and Xcavate flows; this one serves a single flow, and splitting it would mean keeping a
balance list and a selection in step across two singletons. Both views share
`SolanaTransferViewModel`.

Two further structural changes, both for testability rather than correctness:
`SolanaTransferPlanner` (pure) is split from `SolanaTransferModel` (which does the RPC
probe), and `SolanaTransferBalanceAssembler` holds the spendable rule, so neither needs a
cluster to test.

## What was built and verified

90 new unit tests, all passing; the suite went from 240 to 330 passing with the 26
pre-existing network-dependent Substrate failures unchanged. `PlutoFramework` and
`XcavateMobileApp` both build for `net10.0-android`.

**Untested, and stated as such rather than implied to work:** every live RPC call
(`getAccountInfo`, `getSignatureStatuses`, `sendTransaction`); the tracker against a real
cluster; submission through Mobile Wallet Adapter, which needs an Android device with a
wallet installed; the toast stack's visual offset; and the ten-second poll over time. No
transfer has been sent on any cluster.

## Sources

- [bmresearch/Solnet](https://github.com/bmresearch/Solnet) — `SystemProgram.cs`,
  `TokenProgram.cs`, `AssociatedTokenAccountProgram.cs`, `IRpcClient.cs`,
  `Models/Signature.cs`, read at `master` on 2026-07-29
- [Solana.Programs on nuget.org](https://www.nuget.org/packages/Solana.Programs/8.7.0) —
  nuspec dependencies verified 2026-07-29
- [Solana JSON-RPC](https://solana.com/docs/rpc) — `getSignatureStatuses`, `getAccountInfo`,
  `sendTransaction`
- [Solana Pay specification](https://docs.solanapay.com/spec) — the `solana:` URI scheme
