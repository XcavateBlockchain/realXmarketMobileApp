# Solana Mainnet/Devnet Network Switch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Commits:** this environment does not run `git commit` unless the user asked for it. Each task still ends at a verified green state (tests/build), and the changes are left uncommitted for the user to review.

**Goal:** Let the user switch between Solana Devnet and Mainnet in Settings, with the whole app (MWA, balances, transfers, marketplace, roles) honoring the selection and degrading to placeholders where the Xcavate programs/indexer are not deployed.

**Architecture:** The persisted app-wide selection (`SolanaNetworkModel.SelectedCluster`, MAUI layer) stays the single source of truth. The Core marketplace/whitelist models lose their pinned devnet constants and take `SolanaCluster` parameters; a new Core facade `XcavateDeploymentModel.IsDeployed(cluster)` answers "do the Xcavate programs + indexer exist on this cluster" so every UI gate is consistent. Per-cluster separation stays in the two existing registries (`XcavateProgramAddresses`, `XcavateWhitelistIndexer`), whose Mainnet placeholders remain until deployment.

**Tech Stack:** .NET 10 / C# 13, .NET MAUI (Android build available locally), CommunityToolkit.Mvvm, Solnet 8.7.0, StrawberryShake (XcavateDevnetIndexer client), NUnit (PlutoFrameworkTests).

**Spec:** `docs/superpowers/specs/2026-09-27-solana-mainnet-devnet-switch-design.md`

## Global Constraints

- Core (`realXmarketPlutoFramework/PlutoFrameworkCore/`, plain `net10.0`) must not reference `SolanaNetworkModel` (MAUI layer); the cluster enters Core only as a parameter.
- `SolanaNetworkOptions.Default` stays `SolanaCluster.Devnet`; the production flip is a one-line change, documented in its doc comment.
- Follow the repo's existing conventions: static model classes, verbose doc comments that explain *why*, `ConfigureAwait(false)` in Core, NUnit `[Test]`/`Assert.That`.
- No new NuGet dependencies.
- Static-event subscriptions (`SolanaNetworkModel.ClusterChanged`) only on session-wide singleton view models, matching the documented convention at `XcavateIndexedPropertyMarketplaceViewModel`'s constructor.
- Test verification: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`. Some tests are live-network integration tests against the devnet indexer — they need connectivity; a failure that reads as "devnet was reset" is not a regression.
- App verification: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android` (maui-android workload is installed; iOS cannot build on this machine).

---

### Task 1: `XcavateDeploymentModel` availability facade (Core)

**Files:**
- Create: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateDeploymentModel.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/XcavateDeploymentModelTests.cs`

**Interfaces:**
- Consumes: `XcavateProgramAddresses.Get(SolanaCluster)` → `XcavateProgramSet?`; `XcavateWhitelistIndexer.IsSupported(SolanaCluster)` → `bool`; `SolanaNetworkOptions.Selectable` → `SolanaCluster[]`; `SolanaClusterExtensions.GetName()`.
- Produces (used by Tasks 2-5):
  - `XcavateDeploymentModel.IsDeployed(SolanaCluster cluster)` → `bool`
  - `XcavateDeploymentModel.NotDeployedMessage(SolanaCluster cluster)` → `string`
  - Namespace `PlutoFramework.Model.Xcavate` (same as the sibling Core/Xcavate files).

- [ ] **Step 1: Write the failing tests**

Create `realXmarketPlutoFramework/PlutoFrameworkTests/XcavateDeploymentModelTests.cs`:

```csharp
using PlutoFramework.Model.Xcavate;
using PlutoFrameworkCore.Solana;

namespace PlutoFrameworkTests
{
    /// <summary>
    /// Pins the deployment reality behind XcavateDeploymentModel: devnet has the Xcavate
    /// programs and indexer, mainnet and testnet do not. The mainnet assertions are the
    /// placeholder contract - they are the tests to update when the mainnet deployment
    /// lands (idls/mainnet/, XcavateProgramAddresses.Mainnet, XcavateWhitelistIndexer.MainnetUrl).
    /// </summary>
    internal class XcavateDeploymentModelTests
    {
        [Test]
        public void IsDeployed_Devnet_IsTrue()
        {
            Assert.That(XcavateDeploymentModel.IsDeployed(SolanaCluster.Devnet), Is.True);
        }

        [Test]
        public void IsDeployed_Mainnet_IsFalseUntilTheProgramsDeploy()
        {
            Assert.That(XcavateDeploymentModel.IsDeployed(SolanaCluster.Mainnet), Is.False);
        }

        [Test]
        public void IsDeployed_Testnet_IsFalse()
        {
            // Nothing of Xcavate's runs on testnet, by design.
            Assert.That(XcavateDeploymentModel.IsDeployed(SolanaCluster.Testnet), Is.False);
        }

        [Test]
        public void NotDeployedMessage_NamesTheClusterAndPointsAtADeployedOne()
        {
            var message = XcavateDeploymentModel.NotDeployedMessage(SolanaCluster.Mainnet);

            Assert.That(message, Does.Contain("Mainnet"));
            // While devnet is the only deployment, it is the network the message sends
            // the user to.
            Assert.That(message, Does.Contain("Devnet"));
        }

        [Test]
        public void Require_Mainnet_ThrowsNotSupportedInsteadOfNullReference()
        {
            Assert.That(
                () => XcavateProgramAddresses.Require(SolanaCluster.Mainnet),
                Throws.InstanceOf<NotSupportedException>());
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter "FullyQualifiedName~XcavateDeploymentModelTests"`
Expected: FAIL to compile — `XcavateDeploymentModel` does not exist.

- [ ] **Step 3: Implement the facade**

Create `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateDeploymentModel.cs`:

```csharp
using PlutoFrameworkCore.Solana;

namespace PlutoFramework.Model.Xcavate
{
    /// <summary>
    /// Whether the Xcavate feature set that lives on the custom Solana programs - the
    /// property marketplace, roles, reservations - is deployed on a given cluster at all.
    /// <para>
    /// This is the single gate every UI decision goes through, so "the marketplace is not
    /// on this network" looks the same everywhere. The answer is computed from the two
    /// per-cluster registries: the programs (<see cref="XcavateProgramAddresses"/>) and the
    /// indexer (<see cref="XcavateWhitelistIndexer"/>). Both keep their own per-cluster
    /// values, so a devnet deployment that runs ahead of mainnet never disturbs mainnet's
    /// entries - filling in the two Mainnet placeholders is all it takes for
    /// <see cref="IsDeployed"/> to light mainnet up.
    /// </para>
    /// </summary>
    public static class XcavateDeploymentModel
    {
        /// <summary>
        /// True when the programs AND the indexer are both deployed on
        /// <paramref name="cluster"/> - the marketplace works end to end only then.
        /// </summary>
        public static bool IsDeployed(SolanaCluster cluster) =>
            XcavateProgramAddresses.Get(cluster) is not null && XcavateWhitelistIndexer.IsSupported(cluster);

        /// <summary>
        /// The shared wording for every "not on this network" surface (empty feeds, toasts,
        /// gated actions). Points at a network where the marketplace does work, when one is
        /// selectable, so the message is an instruction rather than a dead end.
        /// </summary>
        public static string NotDeployedMessage(SolanaCluster cluster)
        {
            var message = $"The Xcavate marketplace is not available on Solana {cluster.GetName()} yet.";

            SolanaCluster? fallback = SolanaNetworkOptions.Selectable
                .Cast<SolanaCluster?>()
                .FirstOrDefault(candidate => candidate != cluster && IsDeployed(candidate.Value));

            return fallback is SolanaCluster deployed
                ? $"{message} Switch to {deployed.GetName()} in Settings to use it."
                : message;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter "FullyQualifiedName~XcavateDeploymentModelTests"`
Expected: 5 PASS.

- [ ] **Step 5: Checkpoint (no commit per environment policy — leave changes staged for review)**

---

### Task 2: Thread `SolanaCluster` through the Core marketplace/whitelist models

The four Core models drop their pinned devnet constants and take the cluster as a
parameter. Behavior is unchanged: the test callers pass `SolanaCluster.Devnet` explicitly,
and the MAUI-layer callers (fixed in Task 3) still see `SelectedCluster == Devnet` because
`Selectable` is not flipped until Task 5.

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/WhitelistModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateMarketplaceIndexerModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateMarketplaceCallsModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateReserveBalanceModel.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/WhitelistModelTests.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/XcavateMarketplaceIndexerModelTests.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/XcavateMarketplaceProgramTests.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/XcavateReserveBalanceModelTests.cs` (check for existing calls there; add the guard test below)

**Interfaces:**
- Consumes: `XcavateDeploymentModel.IsDeployed` (Task 1).
- Produces (Task 3's callers rely on these exact signatures):
  - `WhitelistModel.GetRolesAsync(string address, SolanaCluster cluster, CancellationToken token)` (already exists; parameterless overload removed)
  - `WhitelistModel.GetRolesCachedAsync(string address, SolanaCluster cluster, CancellationToken token)` (already exists; parameterless overload removed)
  - `WhitelistModel.HasRoleAsync(string address, XcavateRole role, SolanaCluster cluster, CancellationToken token)` (already exists; parameterless overload removed)
  - `XcavateMarketplaceIndexerModel.GetMarketplaceListedPropertiesAsync(SolanaCluster cluster, int first, int offset, CancellationToken token = default)`
  - `XcavateMarketplaceIndexerModel.GetListingFullInfoAsync(SolanaCluster cluster, long listingId, string? investor, CancellationToken token = default)`
  - `XcavateMarketplaceIndexerModel.GetInvestorPropertiesAsync(SolanaCluster cluster, string investor, bool? owned, bool? reserved, string? name, string? townCity, string? propertyType, int first, int offset, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.ReserveSharesAsync(SolanaCluster cluster, string investor, long listingId, uint amount, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.BuyPropertySharesAsync(SolanaCluster cluster, string investor, long listingId, uint amount, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.ClaimSharesAsync(SolanaCluster cluster, string investor, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.CreateSpvAsync(SolanaCluster cluster, string confirmer, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.CancelReservationAsync(SolanaCluster cluster, string investor, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.WithdrawExpiredAsync(SolanaCluster cluster, string investor, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.WithdrawCancelledAsync(SolanaCluster cluster, string investor, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.WithdrawLegalProcessExpiredAsync(SolanaCluster cluster, string investor, long listingId, CancellationToken token = default)`
  - `XcavateMarketplaceCallsModel.PickPaymentMint(SolanaCluster cluster, string acceptedPaymentMintsJson)`
  - `XcavateReserveBalanceModel.GetReservedValuesAsync(SolanaCluster cluster, string address, CancellationToken token)`
  - `XcavateReserveBalanceModel.GetTotalAssetValuesAsync(SolanaCluster cluster, string address, CancellationToken token)`
  - The `WhitelistCluster` and both `MarketplaceCluster` constants are gone; any remaining reference is a bug.

- [ ] **Step 1: Update the tests first (they pin the new signatures)**

`WhitelistModelTests.cs` — add `using PlutoFrameworkCore.Solana;`, then:
- Line 23: `WhitelistModel.GetRolesAsync(address, CancellationToken.None)` → `WhitelistModel.GetRolesAsync(address, SolanaCluster.Devnet, CancellationToken.None)`
- Lines 39-41: `GetRolesAsync("11111111111111111111111111111111", CancellationToken.None)` → `GetRolesAsync("11111111111111111111111111111111", SolanaCluster.Devnet, CancellationToken.None)`
- Line 54: `WhitelistModel.HasRoleAsync(address, role, CancellationToken.None)` → `WhitelistModel.HasRoleAsync(address, role, SolanaCluster.Devnet, CancellationToken.None)`

`XcavateMarketplaceIndexerModelTests.cs` — add `using PlutoFrameworkCore.Solana;`, then:
- Lines 16-19: `GetMarketplaceListedPropertiesAsync(first: 20, offset: 0, CancellationToken.None)` → `GetMarketplaceListedPropertiesAsync(SolanaCluster.Devnet, first: 20, offset: 0, CancellationToken.None)`
- Lines 74-77: `GetListingFullInfoAsync(long.MaxValue, investor: null, CancellationToken.None)` → `GetListingFullInfoAsync(SolanaCluster.Devnet, long.MaxValue, investor: null, CancellationToken.None)`

`XcavateMarketplaceProgramTests.cs` — the two `XcavateMarketplaceCallsModel.PickPaymentMint(accepted)` calls (around lines 266 and 281) become `XcavateMarketplaceCallsModel.PickPaymentMint(SolanaCluster.Devnet, accepted)`; add `using PlutoFrameworkCore.Solana;` if absent.

In `XcavateReserveBalanceModelTests.cs` (create it if it does not exist, matching the file's existing NUnit style if it does), add:

```csharp
[Test]
public async Task GetReservedValuesAsync_UndeployedCluster_ReturnsEmptyWithoutQuerying()
{
    // Mainnet has no indexer to ask, so the answer comes back empty immediately -
    // querying would throw NotSupportedException from XcavateWhitelistIndexer.GetClient.
    var reserved = await XcavateReserveBalanceModel.GetReservedValuesAsync(
        SolanaCluster.Mainnet, "11111111111111111111111111111111", CancellationToken.None);

    Assert.That(reserved, Is.Empty);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
Expected: FAIL to compile — the new cluster-first calls do not exist yet (`GetMarketplaceListedPropertiesAsync(SolanaCluster, ...)`, `GetListingFullInfoAsync(SolanaCluster, ...)`, `PickPaymentMint(SolanaCluster, ...)`, `GetReservedValuesAsync(SolanaCluster, ...)`). The `WhitelistModel` cluster overloads already exist, so those updated tests compile; the others drive the red state.

- [ ] **Step 3: `WhitelistModel` — drop the pinned cluster**

In `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/WhitelistModel.cs`:

Delete the `WhitelistCluster` constant including its doc comment (lines 35-45), and delete the three parameterless overloads:

```csharp
// DELETE:
public static Task<HashSet<XcavateRole>> GetRolesCachedAsync(string address, CancellationToken token)
    => GetRolesCachedAsync(address, WhitelistCluster, token);
// DELETE:
public static Task<HashSet<XcavateRole>> GetRolesAsync(string address, CancellationToken token)
    => GetRolesAsync(address, WhitelistCluster, token);
// DELETE:
public static Task<bool> HasRoleAsync(string address, XcavateRole role, CancellationToken token)
    => HasRoleAsync(address, role, WhitelistCluster, token);
```

Replace the deleted constant's rationale in the class doc comment (lines 26-32 region):

```csharp
/// <summary>
/// Reads Xcavate roles from the whitelist Solana program, through the Xcavate indexer.
/// <para>
/// Addresses here are Solana addresses (base58), not Substrate ones. The whitelist moved
/// off the XcavatePaseo pallet, so a wallet's Substrate key says nothing about its roles.
/// </para>
/// <para>
/// The cluster is the caller's - the app's selected Solana network. On a cluster with no
/// deployment the caller gates on <see cref="XcavateDeploymentModel.IsDeployed"/> and
/// treats the wallet as role-less rather than querying an indexer that is not there.
/// </para>
/// </summary>
```

- [ ] **Step 4: `XcavateMarketplaceIndexerModel` — cluster parameter on the three readers**

In `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateMarketplaceIndexerModel.cs`:

Delete the `MarketplaceCluster` constant and its doc comment (lines 31-36). Then:

```csharp
// line ~51 - signature and client line (56):
public static async Task<IReadOnlyList<XcavateSolanaListingNft>> GetMarketplaceListedPropertiesAsync(
    SolanaCluster cluster,
    int first,
    int offset,
    CancellationToken token = default)
{
    var client = XcavateWhitelistIndexer.GetClient(cluster);
    ...
}

// line ~85 - signature and client line (90):
public static async Task<XcavateSolanaListingNft?> GetListingFullInfoAsync(
    SolanaCluster cluster,
    long listingId,
    string? investor,
    CancellationToken token = default)
{
    var client = XcavateWhitelistIndexer.GetClient(cluster);
    ...
}

// line ~192 - signature and client line (203):
public static async Task<IReadOnlyList<XcavateSolanaInvestorProperty>> GetInvestorPropertiesAsync(
    SolanaCluster cluster,
    string investor,
    bool? owned,
    bool? reserved,
    string? name,
    string? townCity,
    string? propertyType,
    int first,
    int offset,
    CancellationToken token = default)
{
    var client = XcavateWhitelistIndexer.GetClient(cluster);
    ...
}
```

In the class doc comment (lines 13-28), append a paragraph so the cluster semantics are stated where a reader looks first:

```csharp
/// <para>
/// The cluster is the caller's - the app's selected Solana network. Callers gate on
/// <see cref="XcavateDeploymentModel.IsDeployed"/> first: on a cluster with no deployment
/// these readers throw <see cref="NotSupportedException"/> from the indexer client, and a
/// marketplace that is not there must look like an empty placeholder, not an error.
/// </para>
```

- [ ] **Step 5: `XcavateMarketplaceCallsModel` — cluster parameter on every builder**

In `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateMarketplaceCallsModel.cs`:

Delete the `MarketplaceCluster` const alias and its doc comment (lines 35-40). Then apply these edits (every current `MarketplaceCluster` use becomes the `cluster` parameter):

```csharp
// lines ~53-58 and ~66-71:
public static Task<List<TransactionInstruction>> ReserveSharesAsync(
    SolanaCluster cluster,
    string investor,
    long listingId,
    uint amount,
    CancellationToken token = default) =>
    PurchaseAsync(XcavateMarketplaceProgram.ReserveShares, cluster, investor, listingId, amount, token, sponsorFrontsRent: true);

public static Task<List<TransactionInstruction>> BuyPropertySharesAsync(
    SolanaCluster cluster,
    string investor,
    long listingId,
    uint amount,
    CancellationToken token = default) =>
    PurchaseAsync(XcavateMarketplaceProgram.BuyPropertyShares, cluster, investor, listingId, amount, token, sponsorFrontsRent: true);

// lines ~73-114 - PurchaseAsync gains the parameter; every pinned use inside becomes it:
private static async Task<List<TransactionInstruction>> PurchaseAsync(
    Func<XcavateProgramSet, PublicKey, PublicKey, ulong, uint, ulong, PublicKey, PublicKey, PublicKey, TransactionInstruction> build,
    SolanaCluster cluster,
    string investor,
    long listingId,
    uint amount,
    CancellationToken token,
    bool sponsorFrontsRent)
{
    var programs = XcavateProgramAddresses.Require(cluster);
    var client = XcavateWhitelistIndexer.GetClient(cluster);

    var listing = await GetListingAsync(client, listingId, token).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"Listing {listingId} does not exist or is closed.");

    var config = await GetConfigAsync(cluster, client, token).ConfigureAwait(false);

    var payment = await ResolvePaymentAsync(cluster, client, investor, listingId, config, token).ConfigureAwait(false);
    // ...remainder unchanged...
}

// lines ~131-165 - ResolvePaymentAsync: signature gains cluster first; inside,
// ResolveMintAsync(cluster, recordedMint, token) / ResolveMintAsync(cluster, mint, token) and
// SolanaRpcModel.GetAccountInfoAsync(cluster, paymentAccount.Key, token) and DescribeMint(cluster, mint).
private static async Task<PaymentRoute> ResolvePaymentAsync(
    SolanaCluster cluster,
    IXcavateDevnetIndexerClient client,
    string investor,
    long listingId,
    IMarketplaceConfigInfo_MarketplaceConfig config,
    CancellationToken token)

// lines ~172-197:
public static async Task<List<TransactionInstruction>> ClaimSharesAsync(
    SolanaCluster cluster,
    string investor,
    long listingId,
    CancellationToken token = default)
{
    var programs = XcavateProgramAddresses.Require(cluster);
    var client = XcavateWhitelistIndexer.GetClient(cluster);
    // ...ResolveMintAsync(cluster, paymentMint, token)... remainder unchanged
}

// lines ~203-214:
public static Task<List<TransactionInstruction>> CreateSpvAsync(
    SolanaCluster cluster,
    string confirmer,
    long listingId,
    CancellationToken token = default)
{
    var programs = XcavateProgramAddresses.Require(cluster);
    // ...remainder unchanged...
}

// lines ~220-238: CancelReservationAsync gains `SolanaCluster cluster` first;
// Require(cluster), GetClient(cluster).

// lines ~241-253 - the three withdraw entry points:
public static Task<List<TransactionInstruction>> WithdrawExpiredAsync(
    SolanaCluster cluster, string investor, long listingId, CancellationToken token = default) =>
    WithdrawAsync(XcavateMarketplaceProgram.WithdrawExpired, cluster, investor, listingId, token);

public static Task<List<TransactionInstruction>> WithdrawCancelledAsync(
    SolanaCluster cluster, string investor, long listingId, CancellationToken token = default) =>
    WithdrawAsync(XcavateMarketplaceProgram.WithdrawCancelled, cluster, investor, listingId, token);

public static Task<List<TransactionInstruction>> WithdrawLegalProcessExpiredAsync(
    SolanaCluster cluster, string investor, long listingId, CancellationToken token = default) =>
    WithdrawAsync(XcavateMarketplaceProgram.WithdrawLegalProcessExpired, cluster, investor, listingId, token);

// lines ~255-309 - WithdrawAsync: signature becomes
// (Func<...> build, SolanaCluster cluster, string investor, long listingId, CancellationToken token);
// Require(cluster), GetClient(cluster), ResolveMintAsync(cluster, ...),
// GetConfigAsync(cluster, client, token), and both SolanaRpcModel.GetAccountInfoAsync(cluster, ...).

// lines ~342-357 - PickPaymentMint gains the cluster:
public static PublicKey PickPaymentMint(SolanaCluster cluster, string acceptedPaymentMintsJson)
{
    // ...
    var known = SolanaTokenWhitelist.ForCluster(cluster);
    // ...remainder unchanged...
}

// lines ~365-387 - ResolveMintAsync gains the cluster:
private static async Task<(PublicKey TokenProgram, int Decimals)> ResolveMintAsync(
    SolanaCluster cluster,
    PublicKey mint,
    CancellationToken token)
{
    var entry = SolanaTokenWhitelist.ForCluster(cluster)
        .FirstOrDefault(entry => entry.Mint == mint.Key);
    // ...
    var accountInfo = await SolanaRpcModel.GetAccountInfoAsync(cluster, mint.Key, token).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"Payment mint {mint.Key} does not exist on {cluster.GetName()}.");
    // ...remainder unchanged...
}

// lines ~420-422 - DescribeMint gains the cluster:
private static string DescribeMint(SolanaCluster cluster, PublicKey mint) =>
    SolanaTokenWhitelist.ForCluster(cluster)
        .FirstOrDefault(entry => entry.Mint == mint.Key)?.Symbol ?? mint.Key;

// lines ~454-466 - GetConfigAsync gains the cluster:
private static async Task<IMarketplaceConfigInfo_MarketplaceConfig> GetConfigAsync(
    SolanaCluster cluster,
    IXcavateDevnetIndexerClient client,
    CancellationToken token)
{
    // ...
    return result.Data?.MarketplaceConfig
        ?? throw new InvalidOperationException(
            $"The marketplace is not initialized on {cluster.GetName()}.");
}
```

Update the class-level remarks (lines 20-32 stay, but the doc at lines 12-19 mentions nothing cluster-related; the deleted const's comment is replaced by this paragraph on the class):

```csharp
/// <summary>
/// Builds the Solana marketplace transactions behind the property pages - the
/// replacement for the Substrate-era <c>MarketplaceCalls</c> factories. Pure
/// instruction encoding lives in <see cref="XcavateMarketplaceProgram"/>; this layer
/// resolves what the encoding needs from live state: the listing's current price and
/// fees, the investor's recorded position, the config's rent collector and accepted
/// payment mints.
/// <para>
/// Every method takes the cluster explicitly - the app's selected Solana network - so a
/// transaction is always built against the deployment its listing came from. Callers
/// gate on <see cref="XcavateDeploymentModel.IsDeployed"/> first; where that is false,
/// <see cref="XcavateProgramAddresses.Require"/> throws the real reason.
/// </para>
/// </summary>
```

- [ ] **Step 6: `XcavateReserveBalanceModel` — cluster parameter with the centralized guard**

In `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateReserveBalanceModel.cs`, replace `GetReservedValuesAsync` (lines 137-151) and `GetTotalAssetValuesAsync` (lines 152-167):

```csharp
/// <summary>
/// The wallet-wide value <paramref name="address"/> has bound through reservations,
/// grouped by payment token symbol, in display units. Empty on a cluster with no
/// marketplace deployment: there is no indexer to ask, and "no marketplace" must net
/// the same as "nothing reserved" in every balance view.
/// </summary>
public static async Task<IReadOnlyDictionary<string, decimal>> GetReservedValuesAsync(
    SolanaCluster cluster, string address, CancellationToken token)
{
    if (!XcavateDeploymentModel.IsDeployed(cluster))
    {
        return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    var positions = await XcavateMarketplaceIndexerModel
        .GetInvestorPropertiesAsync(cluster, address, null, null, null, null, null, InvestorPropertiesPageSize, 0, token)
        .ConfigureAwait(false);

    return ComputeReservedValues(positions);
}

/// <summary>
/// The wallet-wide value of every property the investor has bought or reserved
/// shares in, grouped by payment token symbol, in display units. Same query, page cap
/// and undeployed-cluster guard as <see cref="GetReservedValuesAsync"/>: no filters, so
/// both reserved and purchased positions are in the total.
/// </summary>
public static async Task<IReadOnlyDictionary<string, decimal>> GetTotalAssetValuesAsync(
    SolanaCluster cluster, string address, CancellationToken token)
{
    if (!XcavateDeploymentModel.IsDeployed(cluster))
    {
        return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    var positions = await XcavateMarketplaceIndexerModel
        .GetInvestorPropertiesAsync(cluster, address, null, null, null, null, null, InvestorPropertiesPageSize, 0, token)
        .ConfigureAwait(false);

    return ComputeTotalAssetValues(positions);
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
Expected: compile clean, all PASS (the live-indexer tests need network access to the devnet indexer; the new guard test passes offline).

- [ ] **Step 8: Checkpoint (no commit per environment policy)**

---

### Task 3: Thread the selected cluster through the MAUI-layer callers

Pure compile repair plus the one behavioral keystone: `SubmitAsync` resolves and guards the
selected cluster. `SolanaNetworkModel.SelectedCluster` is still always Devnet at this point
(`Selectable` flips in Task 5), so runtime behavior is unchanged.

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateMarketplaceTransactionModel.cs:41-75`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/PropertyDetailViewModel.cs` (5 sites: ~374, ~491, ~517, ~543, ~604-612, ~711)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/BuyPropertyTokensViewModel.cs` (~214-217, ~296-299, ~355-359)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XCavatePropertyModel.cs` (~192-203, ~294)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateIndexedPropertyMarketplaceViewModel.cs` (~169)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Menu/MainMenuPageViewModel.cs` (~88)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Model/RequirementsModel.cs` (~84)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaTokenDetailPageViewModel.cs` (~322)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaBalancesPageViewModel.cs` (~233)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml.cs` (~150)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/Transfer/SolanaTransferViewModel.cs` (~577)
- Modify: `XcavateMobileApp/Pages/InvestorMainPageViewModel.cs` (~265-267 comment, ~381)

**Interfaces:**
- Consumes: every Task 2 signature; `SolanaNetworkModel.SelectedCluster` → `SolanaCluster` (`PlutoFramework.Model`); `XcavateDeploymentModel.IsDeployed` / `NotDeployedMessage` (Task 1).
- Produces: `XcavateMarketplaceTransactionModel.SubmitAsync(string description, Func<string, SolanaCluster, CancellationToken, Task<List<TransactionInstruction>>> buildInstructionsAsync)` — the closure gains the resolved cluster so it is resolved exactly once.

- [ ] **Step 1: `XcavateMarketplaceTransactionModel.SubmitAsync`**

In `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateMarketplaceTransactionModel.cs`:

```csharp
// The class doc (lines 41-45 region): replace "sent on the marketplace's own cluster"
// wording with "sent on the selected cluster - the one the listing came from".

// Replace lines 50-53 (the "Deliberately the marketplace's cluster" comment and the
// pinned const read) with:
            // The marketplace follows the selected network: the listing being acted on came
            // from this cluster's deployment, and so must the transaction. Where nothing is
            // deployed there is nothing to build against - fail before any wallet trip.
            var cluster = SolanaNetworkModel.SelectedCluster;

            var stack = DependencyService.Get<SolanaTransactionStatusStackViewModel>();

            // Registered before anything slow, so the user sees the action acknowledged
            // the moment they tap rather than after an unlock prompt and a round trip.
            var info = stack.Register(description, cluster);

            if (!XcavateDeploymentModel.IsDeployed(cluster))
            {
                info.Status = SolanaTransactionStatus.Error;
                info.ErrorMessage = XcavateDeploymentModel.NotDeployedMessage(cluster);

                return;
            }

// The closure type gains the cluster, and the invocation (line ~75) passes it:
        public static async Task SubmitAsync(
            string description,
            Func<string, SolanaCluster, CancellationToken, Task<List<TransactionInstruction>>> buildInstructionsAsync)
// ...
                var instructions = await buildInstructionsAsync(address, cluster, CancellationToken.None);
```

`SolanaNetworkModel` (`PlutoFramework.Model`) and `XcavateDeploymentModel` (`PlutoFramework.Model.Xcavate`) — the file already has both `using PlutoFramework.Model;` and `using PlutoFramework.Model.Xcavate;`. Leave the rest of the method unchanged; it already uses the local `cluster` for the send.

- [ ] **Step 2: `PropertyDetailViewModel` — five closures + one refetch**

In `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/PropertyDetailViewModel.cs` (add `using PlutoFrameworkCore.Solana;` if absent):

```csharp
// line ~374:
var freshListing = await XcavateMarketplaceIndexerModel.GetListingFullInfoAsync(
        SolanaNetworkModel.SelectedCluster, solanaListing.ListingId, solanaAddress, token)
    .ConfigureAwait(false);

// line ~491:
            await XcavateMarketplaceTransactionModel.SubmitAsync(
                "Create SPV",
                (confirmer, cluster, ct) => XcavateMarketplaceCallsModel.CreateSpvAsync(cluster, confirmer, ListingId, ct));

// line ~517:
            await XcavateMarketplaceTransactionModel.SubmitAsync(
                "Claim property tokens",
                (investor, cluster, ct) => XcavateMarketplaceCallsModel.ClaimSharesAsync(cluster, investor, ListingId, ct));

// line ~543:
            await XcavateMarketplaceTransactionModel.SubmitAsync(
                "Refund property tokens",
                (investor, cluster, ct) => XcavateMarketplaceCallsModel.WithdrawExpiredAsync(cluster, investor, ListingId, ct));

// lines ~604-612 - BuildClaimPhaseRefundAsync gains the cluster:
        private Task<List<Solnet.Rpc.Models.TransactionInstruction>> BuildClaimPhaseRefundAsync(
            string investor, SolanaCluster cluster, CancellationToken ct)
        {
            var isTornDown = (NftWrapper?.NftBase as XcavateSolanaListingNft)?.IsTornDown == true;

            return isTornDown
                ? XcavateMarketplaceCallsModel.WithdrawCancelledAsync(cluster, investor, ListingId, ct)
                : XcavateMarketplaceCallsModel.WithdrawLegalProcessExpiredAsync(cluster, investor, ListingId, ct);
        }

// line ~711:
            await XcavateMarketplaceTransactionModel.SubmitAsync(
                "Cancel reservation",
                (investor, cluster, ct) => XcavateMarketplaceCallsModel.CancelReservationAsync(cluster, investor, ListingId, ct));
```

- [ ] **Step 3: `BuyPropertyTokensViewModel`**

In `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/BuyPropertyTokensViewModel.cs`:

```csharp
// lines ~214-217 (LoadBalanceAsync) and ~296-299 (ContinueAsync), both pairs:
                var balanceTask = XcavateReserveBalanceModel.GetPaymentTokenBalanceAsync(
                    SolanaNetworkModel.SelectedCluster, address, PaymentTokenSymbol, CancellationToken.None);

                var reservedTask = XcavateReserveBalanceModel.GetReservedValuesAsync(
                    SolanaNetworkModel.SelectedCluster, address, CancellationToken.None);

// lines ~355-359:
            await XcavateMarketplaceTransactionModel.SubmitAsync(
                description,
                (investor, cluster, ct) => directBuyIsOpen
                    ? XcavateMarketplaceCallsModel.BuyPropertySharesAsync(cluster, investor, listingId, parsedTokens, ct)
                    : XcavateMarketplaceCallsModel.ReserveSharesAsync(cluster, investor, listingId, parsedTokens, ct));
```

- [ ] **Step 4: `XCavatePropertyModel`**

In `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XCavatePropertyModel.cs`:

```csharp
// lines ~192-194 comment: "Solana-sourced items refresh from the Xcavate devnet indexer"
// becomes "Solana-sourced items refresh from the selected cluster's Xcavate indexer".

// lines ~199-203:
                        var freshListing = await XcavateMarketplaceIndexerModel.GetListingFullInfoAsync(
                                SolanaNetworkModel.SelectedCluster,
                                solanaListing.ListingId,
                                solanaAddress,
                                token)
                            .ConfigureAwait(false);

// line ~294:
                        roles = await WhitelistModel.GetRolesCachedAsync(
                            rolesAddress, SolanaNetworkModel.SelectedCluster, token).ConfigureAwait(false);
```

- [ ] **Step 5: `XcavateIndexedPropertyMarketplaceViewModel` feed query**

In `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateIndexedPropertyMarketplaceViewModel.cs`, lines ~169-173:

```csharp
                    var results = await XcavateMarketplaceIndexerModel.GetMarketplaceListedPropertiesAsync(
                            SolanaNetworkModel.SelectedCluster,
                            first: (int)LIMIT,
                            offset: offset,
                            token)
                        .ConfigureAwait(false);
```

- [ ] **Step 6: `MainMenuPageViewModel`, `RequirementsModel` role reads**

`realXmarketPlutoFramework/PlutoFramework/Components/Menu/MainMenuPageViewModel.cs`, line ~88:

```csharp
            Roles = [.. await WhitelistModel.GetRolesCachedAsync(
                address, SolanaNetworkModel.SelectedCluster, CancellationToken.None)];
```

`realXmarketPlutoFramework/PlutoFramework/Model/RequirementsModel.cs`, line ~84:

```csharp
            if (!await WhitelistModel.HasRoleAsync(address, role, SolanaNetworkModel.SelectedCluster, token))
```

- [ ] **Step 7: Solana balance/transfer/detail readers**

```csharp
// SolanaBalancesPageViewModel.cs line ~233:
                var reservedValues = await XcavateReserveBalanceModel.GetReservedValuesAsync(
                    SolanaNetworkModel.SelectedCluster, address, token);

// SolanaBalanceCellView.xaml.cs line ~150:
            var reservedValues = await XcavateReserveBalanceModel.GetReservedValuesAsync(
                SolanaNetworkModel.SelectedCluster, address, token);

// SolanaTransferViewModel.cs line ~577:
                var reserved = XcavateReserveBalanceModel.ValueFor(
                    await XcavateReserveBalanceModel.GetReservedValuesAsync(
                        SolanaNetworkModel.SelectedCluster, address, token),
                    XcavateReserveBalanceModel.TgBpSymbol);

// SolanaTokenDetailPageViewModel.cs lines ~322-324:
                var positions = await XcavateMarketplaceIndexerModel.GetInvestorPropertiesAsync(
                    SolanaNetworkModel.SelectedCluster, address, null, null, null, null, null,
                    XcavateReserveBalanceModel.InvestorPropertiesPageSize, 0, token);
```

- [ ] **Step 8: `InvestorMainPageViewModel`**

In `XcavateMobileApp/Pages/InvestorMainPageViewModel.cs`:

```csharp
// lines ~265-267 comment becomes:
        // The investor's positions live in the Xcavate Solana marketplace on the selected
        // network, read through that cluster's Xcavate indexer - the wallet that signs
        // there is the Solana key, not the Substrate one.

// lines ~381-391 - the query gains the cluster first:
            var page = await XcavateMarketplaceIndexerModel.GetInvestorPropertiesAsync(
                    SolanaNetworkModel.SelectedCluster,
                    ownerAddress,
                    owned: OwnedActive ? true : null,
                    reserved: BoughtActive ? true : null,
                    name: string.IsNullOrEmpty(includesPropertyName) ? null : includesPropertyName,
                    townCity: string.IsNullOrEmpty(includesTownCity) ? null : includesTownCity,
                    propertyType: string.IsNullOrEmpty(includesPropertyType) ? null : includesPropertyType,
                    first: PageSize,
                    offset: offset,
                    token: token)
                .ConfigureAwait(false);
```

- [ ] **Step 9: Build the app to verify everything compiles**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: 0 errors. Any `CS1501`/`CS7036` about a missing `cluster` argument means a call site was missed — grep for it (`GetReservedValuesAsync(|GetInvestorPropertiesAsync(|GetListingFullInfoAsync(|GetMarketplaceListedPropertiesAsync(|GetRolesAsync(|GetRolesCachedAsync(|HasRoleAsync(|PickPaymentMint(`) and fix it the same way.

- [ ] **Step 10: Checkpoint (no commit per environment policy)**

---

### Task 4: Placeholders and reactive reload on cluster change

Now the behavioral layer: guards that keep undeployed clusters from being queried,
placeholder copy, and reloads when the selection changes. Still unreachable by users until
Task 5 flips `Selectable`.

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateIndexedPropertyMarketplaceViewModel.cs`
- Modify: `XcavateMobileApp/Pages/InvestorMainPageViewModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Menu/MainMenuPageViewModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Model/RequirementsModel.cs`
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XCavatePropertyModel.cs` (~292-295)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaTokenDetailPageViewModel.cs` (~318-324)
- Modify: `realXmarketPlutoFramework/PlutoFramework/Components/Solana/SolanaNetworkSettingsView.xaml` (~63)
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Xcavate/XcavateWhitelistIndexer.cs` (~26-31 comment)

**Interfaces:**
- Consumes: `XcavateDeploymentModel.IsDeployed` / `NotDeployedMessage`; `SolanaNetworkModel.ClusterChanged` (`EventHandler<SolanaCluster>`); `XcavateIndexedPropertyMarketplaceViewModel.RefreshInBackgroundAsync()`; `InvestorMainPageViewModel.RefreshAsync(CancellationToken)` (both already exist).
- Produces: no new interfaces.

- [ ] **Step 1: Marketplace feed — guard, placeholder caption, reload on switch**

In `XcavateIndexedPropertyMarketplaceViewModel.cs` (add `using PlutoFrameworkCore.Solana;`):

```csharp
// NoItemsMessage (lines ~67-73) becomes:
        /// <summary>
        /// The empty-state caption. A cluster with no marketplace deployment explains itself
        /// instead of pretending the search found nothing; otherwise the filter-specific
        /// wording is only used when a filter is actually applied.
        /// </summary>
        public string NoItemsMessage =>
            !XcavateDeploymentModel.IsDeployed(SolanaNetworkModel.SelectedCluster)
                ? XcavateDeploymentModel.NotDeployedMessage(SolanaNetworkModel.SelectedCluster)
                : HasActiveFilter
                    ? "No properties were found for this filter. Try to search for something different."
                    : "No properties were found";

// In LoadMoreAsync, immediately after the `if (!hasMore || !clientLoaded) return;` guard
// (line ~140-143), before the semaphore:
            // The marketplace exists only where the Xcavate programs are deployed; anywhere
            // else the feed is the placeholder caption, not an error.
            if (!XcavateDeploymentModel.IsDeployed(SolanaNetworkModel.SelectedCluster))
            {
                hasMore = false;
                return;
            }

// In the constructor, after the TransactionConfirmed subscription (~line 119):
            // Switching the Solana network swaps which deployment the feed reads, so the list
            // is re-read just like after a confirmed transaction. Same singleton lifetime,
            // same no-unsubscribe convention as the subscription above.
            SolanaNetworkModel.ClusterChanged += OnSolanaClusterChanged;

// New handler next to OnMarketplaceTransactionConfirmed (~line 126):
        /// <summary>
        /// Runs on the main thread - the event already is - and fire-and-forget: the
        /// re-fetch is pure async I/O, so nothing blocks the UI while it runs.
        /// </summary>
        private void OnSolanaClusterChanged(object? sender, SolanaCluster cluster) =>
            MainThread.BeginInvokeOnMainThread(() => _ = RefreshInBackgroundAsync());
```

(`RefreshInternalAsync` already clears the list, re-runs `InitialLoadAsync`, and re-raises
`NoItemsMessage`, so the placeholder swap needs nothing more.)

- [ ] **Step 2: Investor main page — guard + reload on switch**

In `XcavateMobileApp/Pages/InvestorMainPageViewModel.cs` (add `using PlutoFrameworkCore.Solana;`):

```csharp
// In LoadMoreOwnedPropertiesAsync, after token.ThrowIfCancellationRequested() (~line 360),
// before loadMoreSemaphore.WaitAsync:
            // Positions come from the selected cluster's marketplace deployment; where there
            // is none the owned list stays empty rather than erroring.
            if (!XcavateDeploymentModel.IsDeployed(SolanaNetworkModel.SelectedCluster))
            {
                hasMore = false;
                return;
            }

// In the constructor, after the TransactionConfirmed subscription (~line 70):
        // The owned list is read from the selected network's marketplace deployment, so a
        // network switch re-reads it. Same singleton lifetime and no-unsubscribe convention
        // as the subscription above.
        SolanaNetworkModel.ClusterChanged += OnSolanaClusterChanged;

// New handler next to OnMarketplaceTransactionConfirmed (~line 78):
    private void OnSolanaClusterChanged(object? sender, SolanaCluster cluster) =>
        MainThread.BeginInvokeOnMainThread(() => _ = RefreshAsync(CancellationToken.None));
```

- [ ] **Step 3: Main menu roles follow the network**

In `MainMenuPageViewModel.cs`, restructure the roles load out of `LoadAsync` (lines ~74-89):

```csharp
        private async Task LoadAsync()
        {
            User = await XcavateUserDatabase.GetUserInformationAsync();

            await LoadRolesAsync();
        }

        /// <summary>
        /// Roles come from the Xcavate whitelist Solana program on the selected network, so
        /// they follow the Solana key rather than the main one. A Substrate-only user simply
        /// has none, and neither does a wallet on a network the programs are not deployed on;
        /// the badge layout renders nothing for an empty list. Best effort: a failed query
        /// keeps whatever roles were shown before.
        /// </summary>
        private async Task LoadRolesAsync()
        {
            var address = KeysModel.GetSolanaAddress();

            if (address is null)
            {
                return;
            }

            var cluster = SolanaNetworkModel.SelectedCluster;

            if (!XcavateDeploymentModel.IsDeployed(cluster))
            {
                Roles = [];

                return;
            }

            try
            {
                Roles = [.. await WhitelistModel.GetRolesCachedAsync(address, cluster, CancellationToken.None)];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load the wallet's Xcavate roles: {ex}");
            }
        }
```

And at the start of `LoadProfileAsync` (line ~96), so coming back from Settings picks up a
network change without any event subscription (this VM is page-scoped, not a singleton):

```csharp
        public async Task LoadProfileAsync()
        {
            // Settings is pushed over this page, so coming back re-reads the roles for the
            // network the user may just have switched to.
            _ = LoadRolesAsync();

            var address = MainKeyModel.GetAddress();
            // ...rest unchanged...
```

- [ ] **Step 4: `RequirementsModel.CheckXcavateRoleAsync` — say "not on this network" instead of "not whitelisted"**

In `realXmarketPlutoFramework/PlutoFramework/Model/RequirementsModel.cs`, between the
null-address block (ends ~line 78) and the "Querying roles" line (~82):

```csharp
            // On a network with no Xcavate programs there are no roles to hold: say so,
            // rather than querying an indexer that is not there and reporting
            // "not whitelisted".
            var cluster = SolanaNetworkModel.SelectedCluster;

            if (!XcavateDeploymentModel.IsDeployed(cluster))
            {
                var toast = Toast.Make(XcavateDeploymentModel.NotDeployedMessage(cluster));
                await toast.Show(token);

                return false;
            }
```

(`Toast` needs `using CommunityToolkit.Maui.Alerts;` — add if absent. `XcavateDeploymentModel` shares the file's existing `PlutoFramework.Model.Xcavate` using via `WhitelistModel`.)

- [ ] **Step 5: `XCavatePropertyModel` role read guard**

Lines ~290-295 become:

```csharp
                    var rolesAddress = KeysModel.GetSolanaAddress();

                    // On a cluster with no deployment the wallet holds no roles there;
                    // answering empty without querying keeps the gated actions off.
                    if (rolesAddress is not null && XcavateDeploymentModel.IsDeployed(SolanaNetworkModel.SelectedCluster))
                    {
                        roles = await WhitelistModel.GetRolesCachedAsync(
                            rolesAddress, SolanaNetworkModel.SelectedCluster, token).ConfigureAwait(false);
                    }
```

- [ ] **Step 6: `SolanaTokenDetailPageViewModel` reserved-section guard**

Lines ~318-324 become:

```csharp
            try
            {
                var cluster = SolanaNetworkModel.SelectedCluster;

                // One indexer query feeds both the section's total and its per-property
                // list - a second round trip would only risk the two disagreeing. On a
                // cluster with no marketplace deployment there are no positions to find
                // and no indexer to ask, so the section answers empty without querying.
                IReadOnlyList<XcavateSolanaInvestorProperty> positions = XcavateDeploymentModel.IsDeployed(cluster)
                    ? await XcavateMarketplaceIndexerModel.GetInvestorPropertiesAsync(
                        cluster, address, null, null, null, null, null,
                        XcavateReserveBalanceModel.InvestorPropertiesPageSize, 0, token)
                    : [];
```

(`XcavateSolanaInvestorProperty` lives in `PlutoFrameworkCore.Xcavate` — add the `using` if the file lacks it.)

- [ ] **Step 7: Settings explainer + indexer comment**

`SolanaNetworkSettingsView.xaml` line ~63:

```xml
                <Label Text="Used for every Solana address and transaction in the app. Property investing runs on the Xcavate programs, which are deployed on Devnet only for now. A connected wallet stays authorized only on the network it was connected on."
```

`XcavateWhitelistIndexer.cs` lines ~26-31 — the `MainnetUrl` doc comment no longer points at the deleted const:

```csharp
        /// <summary>
        /// Placeholder. Xcavate's programs are not deployed to Solana mainnet yet, so there is
        /// no mainnet indexer to point at. Filling this in - together with
        /// <see cref="XcavateProgramAddresses.Mainnet"/> - is all it takes for
        /// <see cref="XcavateDeploymentModel.IsDeployed"/> to light the marketplace up on
        /// mainnet; nothing else has to change.
        /// </summary>
```

- [ ] **Step 8: Build + test**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android` then `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
Expected: build 0 errors; tests PASS.

- [ ] **Step 9: Checkpoint (no commit per environment policy)**

---

### Task 5: Offer Mainnet in Settings (activation)

The one-line user-facing change, deliberately last: every degradation path from Tasks 1-4
is in place before a user can reach Mainnet.

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFrameworkCore/Solana/SolanaNetworkOptions.cs`
- Test: `realXmarketPlutoFramework/PlutoFrameworkTests/SolanaKeyTests.cs` (~61-98, `SolanaNetworkOptionsTests`)

**Interfaces:**
- Consumes: everything above.
- Produces: `SolanaNetworkOptions.Selectable` = `[SolanaCluster.Devnet, SolanaCluster.Mainnet]`.

- [ ] **Step 1: Update the pinning test to the new contract (red)**

In `SolanaKeyTests.cs`, replace `SelectableOffersOnlyDevnetUntilMainnetProgramsDeploy` (lines ~71-79):

```csharp
        [Test]
        public void SelectableOffersDevnetAndMainnet()
        {
            // The wallet layer (MWA authorization, balances, transfers) works on both
            // networks. Where the Xcavate programs are not deployed, the marketplace
            // degrades to a placeholder (XcavateDeploymentModel.IsDeployed) instead of
            // being a reason to keep the network locked away.
            Assert.That(SolanaNetworkOptions.Selectable,
                Is.EqualTo(new[] { SolanaCluster.Devnet, SolanaCluster.Mainnet }));
        }
```

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter "FullyQualifiedName~SolanaNetworkOptionsTests"`
Expected: `SelectableOffersDevnetAndMainnet` FAILS (Selectable is still devnet-only); the other three pass. Note that `EverySelectableClusterSurvivesAPreferencesRoundTrip` and `SelectableContainsTheDefault` keep passing before AND after the flip — they are written against `Selectable`, not a literal.

- [ ] **Step 2: Flip `Selectable` (green)**

In `SolanaNetworkOptions.cs`, replace both members' doc comments and the array:

```csharp
        /// <summary>
        /// Devnet. The Xcavate Solana programs are only deployed there today, so a user
        /// who never opens Settings lands on the network where everything works.
        /// For the production release - once idls/mainnet/ is filled in and
        /// XcavateProgramAddresses.Mainnet and XcavateWhitelistIndexer.MainnetUrl point at
        /// the mainnet deployment - flip this to <see cref="SolanaCluster.Mainnet"/>. The
        /// picker, the stored-preference repair and every consumer follow automatically.
        /// </summary>
        public const SolanaCluster Default = SolanaCluster.Devnet;

        /// <summary>
        /// Both networks the wallet layer works on. Where the Xcavate programs are not
        /// deployed the marketplace degrades to a placeholder
        /// (XcavateDeploymentModel.IsDeployed) rather than blocking the network. Testnet is
        /// deliberately absent: it exists to stage validator releases, not as a place this
        /// app's programs are deployed.
        /// </summary>
        public static readonly SolanaCluster[] Selectable =
            [SolanaCluster.Devnet, SolanaCluster.Mainnet];
```

- [ ] **Step 3: Run the tests to verify they pass**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj --filter "FullyQualifiedName~SolanaNetworkOptionsTests"`
Expected: 4 PASS.

- [ ] **Step 4: Checkpoint (no commit per environment policy)**

---

### Task 6: Verification sweep

**Files:** none (read-only checks).

- [ ] **Step 1: Full test run**

Run: `dotnet test realXmarketPlutoFramework/PlutoFrameworkTests/PlutoFrameworkTests.csproj`
Expected: all PASS. Live-indexer tests (`WhitelistModelTests`, `XcavateMarketplaceIndexerModelTests`) require network access to `https://indexer-devnet.xcavate.io/graphql`; if they fail with HTTP/deserialization errors, re-check connectivity before suspecting the change.

- [ ] **Step 2: Full Android build**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android`
Expected: 0 errors, 0 new warnings in the touched files.

- [ ] **Step 3: Stale-reference sweep**

Run: `grep -rn "WhitelistCluster\|MarketplaceCluster" realXmarketPlutoFramework/ XcavateMobileApp/ --include="*.cs"`
Expected: no hits (the constants are gone; the only legitimate remaining mention is this plan and historical spec docs under `docs/`).

Run: `grep -rn "indexer-devnet\|devnet indexer" realXmarketPlutoFramework/PlutoFramework XcavateMobileApp --include="*.cs" -i`
Expected: only hits whose wording is cluster-aware now (e.g. `XcavateWhitelistIndexer.DevnetUrl`); fix any comment that still says the app reads "the devnet indexer" unconditionally.

- [ ] **Step 4: Manual matrix (on an Android emulator/device with a wallet app)**

- Devnet (default): connect MWA, see devnet balances, marketplace feed lists properties, reserve flow works — unchanged from before this feature.
- Settings → Solana network → Mainnet: balances re-query (mainnet USDC shows), marketplace feed and owned properties show the "not available on Mainnet yet" placeholder, role-gated actions report the same via toast, transfers of SOL/USDC work.
- Settings → back to Devnet: marketplace and roles return without an app restart.

- [ ] **Step 5: Spec cross-check**

Re-read `docs/superpowers/specs/2026-09-27-solana-mainnet-devnet-switch-design.md` section "Changes" and confirm each numbered item landed; if implementation drifted (different guard placement, renamed member), update the spec to match the code. One drift is known up front: the spec says the main-menu roles "refresh on `ClusterChanged`", but `MainMenuPageViewModel` is page-scoped (not a session-wide singleton), so Task 4 step 3 refreshes the roles from `LoadProfileAsync` — which runs on every navigation back to the menu, including returning from Settings — instead of subscribing to the static event. Update the spec's item 12 wording to match.
