# Solana Mainnet/Devnet network switch — design

Date: 2026-09-27
Status: approved-in-auto-mode (user request: "the user should be able to switch from Solana
Devnet to Solana Mainnet and back… this option is not there, but it should be there")

## Problem

The app must let the user pick the Solana network (Devnet or Mainnet) in Settings, and that
choice must be honored everywhere: Mobile Wallet Adapter (MWA) authorization, token balances,
and token transfers. The Xcavate custom Solana programs and the Xcavate indexer exist only on
Devnet today; both must keep clean per-network separation (Devnet deployments will typically
run ahead of Mainnet) with placeholders ready for Mainnet, and the app must degrade gracefully
— not break — when the selected network has no deployment. The default network stays Devnet;
flipping the default for the production release must be a one-line change.

## Current state (as found)

Much of the plumbing already exists:

- `SolanaCluster` enum (`PlutoFrameworkCore/Solana/SolanaCluster.cs`) with MWA chain-id,
  legacy cluster-id, Solnet cluster and display-name mappings.
- `SolanaNetworkModel` (`PlutoFramework/Model/SolanaNetworkModel.cs`): the app-wide selected
  cluster, persisted in Preferences under `settingsSolanaNetwork`, with fallback-and-repair
  for stale values and a static `ClusterChanged` event.
- `SolanaNetworkSettingsView` hosted in `XcavateMobileApp/Pages/SettingsPage.xaml:19`,
  rendering one pill per entry of `SolanaNetworkOptions.Selectable`.
- `SolanaNetworkOptions` (`PlutoFrameworkCore/Solana/SolanaNetworkOptions.cs`):
  `Default = Devnet`, `Selectable = [Devnet]` — this restriction is why the switch
  effectively is not there.
- Balances (`SolanaBalancesModel`, `SolanaBalanceCellView`, `SolanaBalancesPageViewModel`,
  `SolanaTokenDetailPageViewModel`), transfers (`SolanaTransferViewModel`,
  `PlutoFrameworkSolanaAccount`), and MWA authorization (`SolanaMwaModel`,
  `ConnectMwaPopupViewModel`, `MwaSolanaAccount.RunAuthorizedAsync`, which also persists
  refreshed authorizations and chain moves via `PersistIfRefreshedAsync`) already follow
  `SelectedCluster`.
- Per-cluster registries with Mainnet placeholders: `XcavateProgramAddresses`
  (`Mainnet = null`, `Get`/`Require`), `XcavateWhitelistIndexer` (`MainnetUrl = null`,
  `GetUrl`/`IsSupported`/`GetClient` per cluster), `idls/mainnet/.keep`.
- The Xcavate marketplace/whitelist layer ignores the picker through three pinned constants:
  `WhitelistModel.WhitelistCluster = Devnet`,
  `XcavateMarketplaceIndexerModel.MarketplaceCluster = Devnet`,
  `XcavateMarketplaceCallsModel.MarketplaceCluster = Devnet`.
- The whitelisted-token list (`XcavateMobileApp/App.xaml.cs`) already carries per-cluster
  mints, including mainnet USDC.

## Decisions

### 1. The switch is app-wide and honest

Selecting a network puts the whole Solana side of the app on that network: wallet
authorization, balances, transfers — and the marketplace. On a network where the Xcavate
programs/indexer are not deployed, the marketplace shows placeholder states instead of the
other network's data. Showing Devnet listings to a Mainnet wallet (today's pinned-const
behavior, invisible while only Devnet was selectable) would invite transactions the wallet
must reject. Placeholders are the requested behavior.

### 2. Default stays Devnet; production flip is one line

`SolanaNetworkOptions.Default` remains `SolanaCluster.Devnet`. For the production release,
change that one const to `Mainnet` — the picker, persistence repair, and every consumer
follow automatically. `Selectable` lists both networks from now on.

### 3. Per-network separation lives in the two existing registries

`XcavateProgramAddresses` (program IDs per cluster, transcribed from `idls/<cluster>/*.json`)
and `XcavateWhitelistIndexer` (indexer URL + cached StrawberryShake client per cluster) are
the separation points. Devnet can move ahead — new program IDs, new indexer schema — without
touching Mainnet's entries. If the Devnet indexer schema ever diverges from what the deployed
Mainnet indexer serves, a second generated client project (e.g. `XcavateMainnetIndexer`) is
added and dispatched per cluster inside `XcavateWhitelistIndexer.GetClient`; callers go
through the models and never name the client type's cluster. `idls/mainnet/` stays the
placeholder home for the Mainnet IDLs; filling it plus the two registry entries is the entire
Mainnet deployment procedure, documented in the registries' doc comments.

### 4. Layering: Core takes the cluster as a parameter

`PlutoFrameworkCore` targets plain `net10.0` and cannot see the Preferences-backed
`SolanaNetworkModel` (MAUI layer). So the Core marketplace/whitelist models stop reading
pinned constants and take `SolanaCluster` parameters; the MAUI-layer callers pass
`SolanaNetworkModel.SelectedCluster` and gate on availability.

New tiny facade in `PlutoFrameworkCore/Xcavate/XcavateDeploymentModel.cs`:

```csharp
public static class XcavateDeploymentModel
{
    // Programs AND indexer both deployed — the marketplace feature set works end to end.
    public static bool IsDeployed(SolanaCluster cluster);

    // Shared wording so every guard (toast, empty view, menu) says the same thing.
    public static string NotDeployedMessage(SolanaCluster cluster);
}
```

### 5. Graceful degradation matrix (selected cluster = Mainnet, today)

| Feature | Behavior on Mainnet today |
|---|---|
| MWA connect / sign-and-send | Works (cluster sent in authorize; stored-token reauthorization already moves chains) |
| SOL + whitelisted SPL balances | Works (mainnet USDC already whitelisted) |
| Transfers | Works |
| Jupiter USD prices | Unpriced mints simply show no USD row (existing behavior) |
| Marketplace feed / owned properties | Empty list + "not available on Mainnet" placeholder; no indexer query |
| Property detail actions (buy/reserve/claim/withdraw/…) | Not reachable (feed empty); guarded anyway — `SubmitAsync` fails fast with the shared message |
| Roles (whitelist program) | Treated as none; no indexer query |
| Reserved-balance netting (balances page, balance cell, token detail, transfer view) | No reserved values; no indexer query |
| Rent-collector signature service | Unreachable (only used by marketplace transactions) |

## Changes

### Core (`PlutoFrameworkCore`)

1. `Solana/SolanaNetworkOptions.cs` — `Selectable = [Devnet, Mainnet]`; doc comments updated
   (`Default` comment becomes the production-flip instruction).
2. New `Xcavate/XcavateDeploymentModel.cs` — `IsDeployed` + `NotDeployedMessage` as above.
3. `Xcavate/XcavateMarketplaceIndexerModel.cs` — remove the `MarketplaceCluster` const; add a
   `SolanaCluster cluster` first parameter to `GetMarketplaceListedPropertiesAsync`,
   `GetListingFullInfoAsync`, `GetInvestorPropertiesAsync`; use it for the indexer client.
4. `Xcavate/XcavateMarketplaceCallsModel.cs` — remove the `MarketplaceCluster` const alias;
   add a `SolanaCluster cluster` parameter to the public transaction builders
   (`ReserveSharesAsync`, `BuyPropertySharesAsync`, `ClaimSharesAsync`, `CreateSpvAsync`,
   `WithdrawExpiredAsync`, `WithdrawCancelledAsync`, `WithdrawLegalProcessExpiredAsync`,
   `CancelReservationAsync`) and thread it through the private helpers
   (`XcavateProgramAddresses.Require(cluster)`, indexer client, RPC account probes, token
   whitelist lookups). Pure helpers (`ComputeMaxTotalCost`, `PickPaymentMint`,
   `ScaleToMintDecimals`) are unchanged.
5. `Xcavate/XcavateReserveBalanceModel.cs` — `GetReservedValuesAsync` and
   `GetTotalAssetValuesAsync` take `SolanaCluster cluster` and return an empty dictionary
   when `!XcavateDeploymentModel.IsDeployed(cluster)`, centralizing the guard for all four
   balance/transfer call sites.
6. `Xcavate/WhitelistModel.cs` — remove the `WhitelistCluster` const and the parameterless
   overloads; every caller states the cluster explicitly. The "nowhere to ask must not look
   like no roles" throw stays for direct misuse; UI callers gate on `IsDeployed` instead.

### Framework (`PlutoFramework`)

7. `Components/XcavateProperty/XcavateMarketplaceTransactionModel.cs` — `SubmitAsync`
   resolves `SolanaNetworkModel.SelectedCluster`, fails fast with `NotDeployedMessage` when
   undeployed, and passes the resolved cluster into the instruction-builder closure
   (closure signature gains a `SolanaCluster` parameter) so the cluster is resolved once.
8. `Components/XcavateProperty/XcavateIndexedPropertyMarketplaceViewModel.cs` — pass the
   selected cluster to the feed query; when undeployed, skip querying and surface the
   placeholder (empty-view caption); subscribe to `SolanaNetworkModel.ClusterChanged` to
   reload/swap the placeholder (the VM is a session-wide singleton, matching the documented
   static-event convention).
9. `Components/XcavateProperty/PropertyDetailViewModel.cs`, `XCavatePropertyModel.cs`,
   `BuyPropertyTokensViewModel.cs` — pass the selected cluster into listing refetches, role
   reads, payment-token balance reads, reserve-value reads, and the `SubmitAsync` closures.
10. `Components/Solana/SolanaTokenDetailPageViewModel.cs` — guard the investor-positions
    query with `IsDeployed`; none when undeployed.
11. `Components/Solana/SolanaBalancesPageViewModel.cs`,
    `Components/Solana/SolanaBalanceCellView.xaml.cs`,
    `Components/Solana/Transfer/SolanaTransferViewModel.cs` — pass the selected cluster to
    the reserve-balance reads (the Core guard returns empty on undeployed clusters).
12. `Components/Menu/MainMenuPageViewModel.cs` — roles read for the selected cluster when
    deployed, empty otherwise. Refresh rides on `LoadProfileAsync` (which runs on every
    navigation back to the menu, including returning from Settings) rather than a
    `ClusterChanged` subscription: this view model is page-scoped, and the static event is
    reserved for session-wide singletons.
13. `Components/Solana/SolanaNetworkSettingsView.xaml` — explainer text notes that property
    investing (Xcavate programs) is available on Devnet only for now.

### App (`XcavateMobileApp`)

14. `Pages/InvestorMainPageViewModel.cs` — pass the selected cluster to
    `GetInvestorPropertiesAsync`, skip querying when undeployed (empty owned list +
    placeholder caption), and reload on `ClusterChanged`.

### Tests (`PlutoFrameworkTests`)

15. `SolanaKeyTests.cs` — `SelectableOffersOnlyDevnetUntilMainnetProgramsDeploy` becomes a
    test that both networks are offered; the Default-is-Devnet and round-trip tests stay.
16. `WhitelistModelTests.cs`, `XcavateMarketplaceIndexerModelTests.cs` — pass
    `SolanaCluster.Devnet` explicitly (these are live-indexer integration tests).
17. New `XcavateDeploymentModelTests` — availability matrix: Devnet deployed, Mainnet and
    Testnet not; `XcavateProgramAddresses.Require(Mainnet)` throws `NotSupportedException`
    (this pins the placeholder contract and is the test to update when Mainnet deploys).

### Docs/comments

18. Update the doc comments that describe the pinned-const behavior
    (`XcavateWhitelistIndexer.MainnetUrl`, `WhitelistModel`, marketplace models) and the
    Mainnet fill-in procedure in `XcavateProgramAddresses`.

## Non-goals

- Deploying Mainnet programs or a Mainnet indexer; this change ships placeholders only.
- Changing Substrate/XcavatePaseo endpoints or the legacy SubQuery feed.
- Backend environments for the rent-collector signature service (profile-api).
- The dapp WebView bridge (`SolanaWalletStandardBridge`) — already advertises and parses
  per-request chains.

## Error handling

- Marketplace action on an undeployed cluster: status toast registers, then fails
  immediately with `NotDeployedMessage` — before any unlock prompt or wallet trip.
- Indexer/role reads on an undeployed cluster: never attempted (callers gate on
  `IsDeployed`); `GetClient`/`Require` keep throwing `NotSupportedException` as the
  last-line guard so a missed gate fails loudly, not silently empty.
- Stored preference naming a network no longer selectable: existing repair logic in
  `SolanaNetworkModel` resets to `Default`.

## Testing

- `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests` — updated + new tests above.
  The whitelist/indexer tests are live-network integration tests and require connectivity
  to the devnet indexer.
- Build: `dotnet build XcavateMobileApp.sln` (Android target available on this machine;
  full MAUI build verification per repo tooling).
- Manual matrix: Devnet (unchanged behavior end to end), switch to Mainnet (balances/MWA/
  transfers work, marketplace placeholders), switch back (marketplace works again).
