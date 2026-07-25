# Solana Balances and Solana-First Onboarding — Design

**Date:** 2026-07-25
**Status:** Approved, ready for implementation planning
**Builds on:** [Solana wallet support](2026-07-25-solana-wallet-support-design.md),
[Unified PlutoFrameworkSolanaAccount](2026-07-25-unified-solana-account-design.md)

## Goal

1. A `SolanaBalancesPage` listing every whitelisted SPL token plus SOL, queried from the
   cluster selected in Settings.
2. The main page's Balance cell showing the real Solana balance and opening that page.
3. Onboarding creating or importing a **Solana** wallet instead of a Polkadot one, with an
   import-method popup offering Mobile Wallet Adapter or a seed phrase.

Item 3 removes the Substrate key from new accounts. Section "What this removes" states
exactly what stops working; that consequence was raised and accepted before this spec.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Whitelist location | `PlutoConfigurationModel.WhitelistedSolanaTokens`, set from `App.xaml.cs` | Mirrors the existing `WhitelistedTokens`. The framework stays free of realXmarket's token choices. |
| Whitelist granularity | Per cluster | A mint address identifies a different token on each cluster. One flat list would show mainnet USDC balances while pointed at devnet. |
| Zero balances | Shown | The page's purpose is "the tokens this app deals in". A token vanishing at zero reads as a bug. |
| SOL | Always present, not a whitelist entry | It is not an SPL token and cannot be misconfigured away; the fee currency must always be visible. |
| Pricing | Stablecoins pinned, SOL live from Jupiter | Chosen by the product owner. One HTTP call, and no price feed can misprice a stablecoin during an outage. |
| Devnet USD | Mainnet prices | Devnet exists to rehearse mainnet. The alternative — no USD on devnet — makes the two networks render differently and hides layout bugs until production. |
| Price failure | Amounts still shown, USD omitted | Balances are the feature; prices are decoration. A Jupiter outage must not blank the page. |
| Main page cell | Repurposed, not added | Keeps the 2×2 grid. Chosen by the product owner. |
| Onboarding keys | Solana only | Chosen by the product owner, with the regression in "What this removes" understood. |
| Existing users | Untouched | Their Substrate key stays in the database and keeps working. The regression is scoped to new onboardings. |

## Research findings

### Solnet RPC surface

Verified against `bmresearch/Solnet@master`, `src/Solnet.Rpc/IRpcClient.cs`:

```csharp
Task<RequestResult<ResponseValue<ulong>>> GetBalanceAsync(
    string pubKey, Commitment commitment = Commitment.Finalized);

Task<RequestResult<ResponseValue<List<TokenAccount>>>> GetTokenAccountsByOwnerAsync(
    string ownerPubKey, string tokenMintPubKey = null, string tokenProgramId = null,
    Commitment commitment = Commitment.Finalized);
```

From `src/Solnet.Rpc/Models/AccountData.cs`, the parsed shape reached through
`TokenAccount.Account`:

- `TokenAccountInfo.Data` → `TokenAccountData.Parsed` → `ParsedTokenAccountData.Info`
  (`TokenAccountInfoDetails`), carrying `Mint`, `Owner`, `State`, `IsNative` and
  `TokenAmount`.
- `TokenBalance` exposes `Amount` (raw, string), `Decimals`, `UiAmount`, `UiAmountString`,
  and the helpers `AmountUlong` / `AmountDecimal` / `AmountDouble`.

**The helper properties' scaling is not documented.** `Amount` is the raw base-unit string
and `UiAmount` is the scaled one; whether `AmountDecimal` scales is ambiguous from the
names alone. This design converts explicitly from `Amount` and `Decimals` rather than
trusting a helper whose semantics would be discovered only by a balance rendered a million
times too large.

`RequestResult` reports failure through the result object rather than throwing, so both
calls go through `SolanaRpcModel`'s existing `Unwrap`. A failed RPC call must not be
indistinguishable from an empty wallet.

### Token program id

`GetTokenAccountsByOwnerAsync` requires exactly one of mint or program id. Filtering by
program id — `TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA` — returns every SPL account in
one call, which is then joined against the whitelist locally. Filtering by mint would cost
one RPC round trip per whitelisted token against a rate-limited public endpoint.

Token-2022 (`TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb`) is a separate program and its
accounts are not returned by that call. No whitelisted token uses it today. The whitelist
entry therefore carries an optional `ProgramId`, and the model issues one call per distinct
program id present in the whitelist — one call while the list is legacy-only.

### Verified mint addresses

Checked live on 2026-07-25, not taken from memory:

| Token | Cluster | Mint | Decimals | Verified by |
|---|---|---|---|---|
| SOL | both | `So11111111111111111111111111111111111111112` | 9 | Jupiter price response |
| USDC | Mainnet | `EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v` | 6 | Jupiter price response, `usdPrice` ≈ 0.99996 |
| USDC | Devnet | `4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU` | 6 | `getTokenSupply` against `api.devnet.solana.com` |

### Jupiter price API

`GET https://lite-api.jup.ag/price/v3?ids=<comma-separated mints>`, no key, verified live:

```json
{"So11111111111111111111111111111111111111112":{"createdAt":"2024-06-05T08:55:25.527Z",
"liquidity":649200458.34,"usdPrice":74.15443178403174,"blockId":435157282,
"decimals":9,"priceChange24h":0.32589982}}
```

Keyed by mint, so the same call scales if a stablecoin is later unpinned. Only `usdPrice`
is consumed; an absent mint key means no price, not zero.

### `ToCurrencyString` is not a USD formatter

`ExchangeRateModel.cs:25` converts its argument with `GetExchangeRate("tGBP", currency)` —
it treats every input as tGBP regardless of the parameter name. `BalanceCellView` already
passes a USD sum through it. Feeding a genuine USD total into it would silently mislabel
the number, so a sibling `ToUsdCurrencyString` is added that goes through
`GetExchangeRate("USDT", currency)`, the pair the existing table already defines.

### Onboarding reaches `Finished` in exactly one place

`OnboardingStage.Finished` is set only in `ModifyUserProfilePageViewModel.cs:147`, at the
end of profile registration. `App.xaml.cs:116` routes to `XcavateAppShell` only when
`IsOnboardingCompleted()`. An onboarding that stops after the wallet must set `Finished`
itself, or the user re-enters onboarding on every launch.

### The profile API cannot accept a Solana address

`XcavateProfileService.RegisterProfileAsync` obtains a Substrate `Account` via
`KeysModel.GetAccountAsync` (line 34), passes it to `_client.UpdateProfileAsync` as the
signer (line 94), keys the record by `Ss58Address`, and requires an X25519 public key. The
identity is enforced by `profile-api.xcavate.io`, not by the client. No client-side change
substitutes a Solana address, which is why Solana-only onboarding cannot run the profile
step rather than merely choosing not to.

### Icon convention

`PlutoFrameworkCore/Constants/Assets.cs` maps symbols to file names, and MAUI compiles
`MauiImage` SVGs to PNG at build — `hydration.svg` is referenced as `"hydration.png"`. A
new `solana.svg` therefore registers as `{ "sol", "solana.png" }`.

## Architecture

```
PlutoFrameworkCore/
  Solana/
    SolanaTokenWhitelistEntry.cs     # record: cluster, mint, symbol, decimals, pinned price, program id
    SolanaNativeToken.cs             # SOL mint, decimals, symbol
    SolanaTokenProgram.cs            # program ids
    SolanaTokenBalance.cs            # one row: symbol, mint, amount, decimals, usd value, is native
    SolanaBalancesModel.cs           # address + cluster -> rows
    SolanaPriceModel.cs              # Jupiter, pinned prices honoured before any call
    SolanaRpcModel.cs                # + GetLamportBalanceAsync, GetTokenAccountsAsync
  Constants/Assets.cs                # + sol
  PlutoConfigurationModel.cs         # + WhitelistedSolanaTokens

PlutoFramework/
  Components/Solana/
    SolanaBalancesPage.xaml(.cs)
    SolanaBalancesPageViewModel.cs
    SolanaAssetView.xaml(.cs)
    SolanaBalanceCellView.xaml(.cs)
    SolanaNoAccountView.xaml(.cs)
    ImportMethodPopupView.xaml(.cs)
    ImportMethodPopupViewModel.cs
  Model/
    SolanaNetworkModel.cs            # + ClusterChanged
    KeysModel.cs                     # + HasSolanaKey(), GetSolanaAddress()
    PreferencesModel.cs              # + SOLANA_PUBLIC_KEY
    RequirementsModel.cs             # account check accepts either key
    Currency/ExchangeRateModel.cs    # + ToUsdCurrencyString
  Resources/Images/Assets/solana.svg

XcavateMobileApp/
  App.xaml.cs                        # start routing, whitelist config, GenerateNewAccountAsync
  Components/Account/ImportAccountCoordinator.cs
  Pages/InvestorMainPage.xaml        # cell swap
```

Everything platform-agnostic stays in Core and is unit tested. The MAUI layer holds only
what depends on `Preferences`, `SecureStorage` or navigation.

## Part A — Balance data

### Whitelist

```csharp
public sealed record SolanaTokenWhitelistEntry
{
    public required SolanaCluster Cluster { get; init; }
    public required string Mint { get; init; }
    public required string Symbol { get; init; }
    public required int Decimals { get; init; }

    /// <summary>A fixed USD price, for stablecoins. Null means priced from the live feed.</summary>
    public double? PinnedUsdPrice { get; init; }

    /// <summary>Defaults to the legacy SPL Token program. Token-2022 mints must say so.</summary>
    public string ProgramId { get; init; } = SolanaTokenProgram.Legacy;
}
```

Seeded in `App.xaml.cs` with the two verified USDC mints, both `PinnedUsdPrice = 1.00`.

Unlike `WhitelistedTokens`, an **empty Solana whitelist means "no SPL tokens"**, not "all of
them". The Substrate whitelist filters a set discovered on chain; this one *is* the set. An
empty list yields a page showing SOL alone, which is correct and legible.

### Query

`SolanaBalancesModel.GetBalancesAsync(string address, SolanaCluster cluster, CancellationToken)`:

1. Select whitelist entries for `cluster`.
2. `GetBalanceAsync(address)` → lamports → SOL row (`lamports / 1e9`).
3. For each distinct `ProgramId` in the selection, `GetTokenAccountsByOwnerAsync(address,
   tokenProgramId: id)`.
4. Group returned accounts by `Info.Mint`, **summing amounts** — one wallet can hold several
   accounts for the same mint, and showing only the first would under-report a balance.
5. Emit one row per whitelisted entry, zero when no account exists, ordered SOL first then
   whitelist order.

Amounts are computed as `BigInteger.Parse(TokenAmount.Amount) / 10^TokenAmount.Decimals`
using decimal arithmetic. The whitelist's `Decimals` is used only for rows with no account,
where the chain reported nothing. A mismatch between the two is not reconciled: the chain
is authoritative when present.

Returns rows even when the price step fails. Throws `SolanaRpcException` when a balance
call fails, so the page can distinguish "empty wallet" from "could not reach the network".

### Pricing

`SolanaPriceModel.GetUsdPricesAsync(IEnumerable<string> mints, CancellationToken)` returns
`IReadOnlyDictionary<string, double>`. Mints with a pinned price never reach the network.
With the seeded whitelist the request is SOL alone. Any exception, non-success status or
unparsable body yields an empty dictionary — the caller then renders amounts without USD.
The total is the sum of priced rows only.

## Part B — Page and cell

**`SolanaBalancesPage`** — `PageTemplate`, title "Balance". A header card with the total,
the address with copy and QR (`AddressView` / `AddressQrCodeView`), and a Mainnet/Devnet
badge, so a devnet balance can never be mistaken for real funds. Below it a
`RefreshView` + `BindableLayout` of `SolanaAssetView` rows: icon, symbol, amount, USD —
laid out like `AssetView` so the two balance screens read alike.

States: loading, loaded, empty-wallet (rows at zero — still a success), RPC failure
(message plus retry), and no-Solana-key (`SolanaNoAccountView`, which opens the import
popup from Part C).

**`SolanaBalanceCellView`** replaces `BalanceCellView` at `InvestorMainPage.xaml:81`, and
`InvestorMainPage.Views` (`InvestorMainPage.xaml.cs:10`) points at it. It implements
`ILocalLoadableAsyncView` only — no Substrate client is involved. Value is the USD total via
`ToUsdCurrencyString`; tapping opens `SolanaBalancesPage`. With no Solana key it shows a
dash rather than a currency-formatted zero, which would read as "you have nothing" instead
of "there is no account".

**Network changes.** `SolanaNetworkModel` gains a static `ClusterChanged` event, raised by
its setter. The page and the cell subscribe, and also reload on appearing. Without this a
user switching to Devnet in Settings returns to a main page still showing mainnet money.

## Part C — Solana-only onboarding

### Synchronous key check

`App.xaml.cs` decides the start page before any `await`, and `HasSolanaKeyAsync()` queries
the database. `KeysModel` therefore gains `HasSolanaKey()` and `GetSolanaAddress()` backed
by a new `PreferencesModel.SOLANA_PUBLIC_KEY`, written by `SaveSolanaMnemonicKeyAsync` and
`SaveSolanaMwaKeyAsync` and cleared by `ClearAsync` — exactly how `PUBLIC_KEY` backs
`HasSubstrateKey()`.

### Flow

`ImportAccountCoordinator`:

- **Create** → `SetupPasswordPage` → generate Solana mnemonics → `SaveSolanaMnemonicKeyAsync`
  → stage `Finished` → `XcavateAppShell`.
- **Import** → `ImportMethodPopupView`:
  - *Seed phrase* → `EnterSolanaMnemonicsPage` (already exists, with the live derived-address
    preview) → `SetupPasswordPage` → save → `Finished` → app shell.
  - *Connect wallet* → `SolanaMwaModel.ConnectAndSaveAsync(SelectedCluster, progress, token)`
    → `SetupPasswordPage` → `Finished` → app shell. Disabled on iOS with a plain
    explanation; MWA is Android-only by protocol, and a button that fails obscurely is worse
    than one that says why.

The password step is kept on the MWA path: the auth token is a secret in `SecureStorage`,
and `CheckAuthenticationAsync` requires a stored password to exist.

Key generation follows the password step in every branch, matching the existing order.
`CreateSolanaMnemonicsViewModel` saves the key itself before invoking its `Navigation`
callback, so the create branch generates and saves inline rather than reusing that page —
the same shape the current Polkadot create branch uses, where the phrase is not displayed
during onboarding.

Removed: `OnPasswordSetAsync`'s Sr25519 / DID / X25519 saving, `OnJsonImportedAsync`, and
the `NavigateAfterAccountCreation` tail through role selection, questionnaire, agreements,
KYC and profile registration. `ContinueAsync`'s resume table keeps its Substrate stages so
that a user interrupted mid-onboarding *before* this release can still finish; new
onboardings never enter them.

### Gates

`App.xaml.cs:118` and `RequirementsModel.CheckAccountExists()` become
`HasSolanaKey() || HasSubstrateKey()`. Existing users keep their Substrate key, so they keep
every feature; the regression lands only on accounts created after this release.

Substrate-dependent entry points are gated explicitly rather than left to fail: the KYC
navigation, `CheckRequirementsAsync` and `CheckXcavateRoleAsync` check `HasSubstrateKey()`
first and surface the existing "account required" popup. Without this, `GetSubstrateKey()`
hands out its `"Substrate key does not exist"` placeholder string, which
`GetSubstrateKey(0)` then throws on inside `Utils.GetPublicKeyFrom`.

## What this removes

For accounts created after this release, and only those:

| Lost | Because |
|---|---|
| Role selection, questionnaire, agreements | Onboarding tail removed |
| Sumsub KYC | Applicant is keyed by Substrate address and DID |
| KILT DID | Derived from the Substrate seed |
| Public profile registration and editing | `profile-api.xcavate.io` requires an ss58 address and a Substrate signature |
| Encrypted messaging | Needs the X25519 key derived from the Substrate seed |
| Owned-property list | Indexer queries take a Substrate `tokenOwner` |
| Investing | Requires the `RealEstateInvestor` role from the XcavatePaseo whitelist pallet |

New users get a browsable marketplace and a working Solana wallet. The properties live on
XcavatePaseo, a Substrate chain, so nothing here can be restored without porting that side.

Existing users keep every feature above, but the main page's Balance cell is swapped for
everyone — so until an existing user creates or imports a Solana wallet through the empty
state, that cell shows a dash instead of their Substrate USD sum. The Substrate `BalancePage`
itself is untouched and still reachable through the menu's wallet action
(`MainMenuPageViewModel.cs:118`); only the main page's route into it changes.

## Testing

NUnit 4 in `PlutoFrameworkTests`, matching the existing Solana tests.

| Test | Guards |
|---|---|
| Whitelist filtered by cluster returns only that cluster's mints | Showing mainnet balances on devnet |
| Several token accounts for one mint sum into one row | Under-reporting a split balance |
| A whitelisted mint with no token account appears at zero | The "show all whitelisted" requirement |
| Raw amount + decimals → UI amount, including 6 and 9 decimals | The ambiguous Solnet helper properties |
| Lamports → SOL | Off-by-10^9 |
| Pinned price wins and issues no request | Stablecoins must not depend on a feed |
| Empty price dictionary leaves amounts intact and USD null | Jupiter outage must not blank the page |
| Total sums only priced rows | An unpriced row silently counting as zero |
| Empty whitelist yields SOL only | The inverted empty-list semantics versus `WhitelistedTokens` |
| Jupiter response parsing, including an absent mint key | Treating "no price" as zero |

**Not unit tested**, and to be reported as untested rather than implied to work: the two RPC
calls against live clusters; the Jupiter call; the MWA import path, which needs an Android
device with a wallet installed; and the onboarding flow end to end, which needs a fresh
install.

## Explicit exclusions

- Sending or receiving tokens. The page displays balances; no transfer UI, no SPL transfer
  instructions.
- Token metadata from chain or a registry. Symbol, decimals and icon come from the whitelist.
- Token-2022 mints in the seeded list. The `ProgramId` field exists so adding one is
  configuration rather than a code change.
- Transaction history.
- Balance caching or persistence. Each visit queries; there is no Solana equivalent of
  `BalancesDatabase` here.
- Porting properties, KYC, DID, profile or messaging to Solana.
- Backfilling a Solana key for existing users. They reach one through the balances page's
  empty state, which runs the same import flow.

## Sources

- [bmresearch/Solnet](https://github.com/bmresearch/Solnet) — `IRpcClient.cs`, `Models/AccountData.cs`
- [Jupiter Price API](https://lite-api.jup.ag/price/v3) — verified live 2026-07-25
- [Solana JSON-RPC](https://solana.com/docs/rpc) — `getTokenAccountsByOwner`, `getBalance`, `getTokenSupply`
