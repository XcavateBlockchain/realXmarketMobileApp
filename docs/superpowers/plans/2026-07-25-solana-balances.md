# Solana Balances and Solana-First Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `SolanaBalancesPage` listing whitelisted SPL tokens plus SOL from the cluster selected in Settings, point the main page's Balance cell at it, and replace Polkadot onboarding with a Solana create/import flow.

**Architecture:** All balance logic lives in `PlutoFrameworkCore` as pure functions over plain records, with Solnet types confined to a thin RPC edge — that is what makes it unit testable, since `PlutoFrameworkTests` references Core only. The MAUI layer holds pages, view models, and anything touching `Preferences` or `SecureStorage`.

**Tech Stack:** .NET 10, MAUI, Solana.Rpc 8.7.0 (assembly `Solnet.Rpc`), CommunityToolkit.Mvvm, NUnit 4.

**Spec:** `docs/superpowers/specs/2026-07-25-solana-balances-design.md`

## Global Constraints

- Target frameworks: `net10.0-android;net10.0-ios` for the app, `net10.0` for Core and tests.
- `PlutoFrameworkTests` references **only** `PlutoFrameworkCore`. Anything needing `Preferences`, `SecureStorage` or `Application.Current` cannot be unit tested and must live in `PlutoFramework`.
- Solnet types (`Solnet.Rpc.*`, `Solnet.Wallet.*`) appear only in `SolanaRpcModel` and `SolanaBalancesModel`. They must not reach the assembler, the view models, or the pages.
- Verified mint addresses — copy exactly:
  - SOL `So11111111111111111111111111111111111111112`, 9 decimals
  - USDC Mainnet `EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v`, 6 decimals
  - USDC Devnet `4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU`, 6 decimals
  - SPL Token program `TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA`
  - Token-2022 program `TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb`
- Price endpoint: `https://lite-api.jup.ag/price/v3?ids=<comma-separated mints>`, field `usdPrice`.
- An empty Solana whitelist means "no SPL tokens", **not** "all of them" — the inverse of `PlutoConfigurationModel.WhitelistedTokens`.
- A price-feed failure must never blank the page: amounts render, USD is omitted.
- Never call `ToCurrencyString` on a USD value — it treats its input as tGBP (`ExchangeRateModel.cs:25`). Use `ToUsdCurrencyString`, added in Task 6.
- Run tests with: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo` from `realXmarketPlutoFramework/`. Takes about 3½ minutes.
- **The suite is not green before you start.** Measured on a clean checkout on 2026-07-25: `Failed: 29, Passed: 125, Skipped: 8, Total: 162`. These failures are pre-existing and are not yours to fix. Record your own baseline before Task 1:

  ```bash
  cd realXmarketPlutoFramework
  dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --logger "trx;LogFileName=baseline.trx"
  ```

  Then judge every later run by "the failure count did not rise and my new tests pass", never by "everything is green". While iterating on a single task, use `--filter <TestClassName>` to run just the class you are working on.

  Every one of the 29 calls a live service — Hydration RPC (`GetOmnipool*`, `GetDCAPositionsAsync`, `SimulateSwapCallAsync`, …), Uniquery, Sumsub (`AcceptTermsAsync_Completes`), S3 (`CreateNamespaceAndBucketAsync`), Substrate chain queries (`QueryHistoricalEventsAsync`, `GetRolesAsync_ReturnsRolesForAddressAsync`) and the version endpoint. **No existing Solana test is among them**: `SolanaAddressTests`, `SolanaBase58Tests`, `SolanaKeyTests`, `SolanaMnemonicsTests` and `SolanaTransactionFramerTests` all pass. Anything Solana-shaped that fails is yours.
- Framework changes are committed in the `realXmarketPlutoFramework` submodule (branch `Solana-support`); app changes in the parent repo (branch `solana-support`).

---

### Task 1: Token whitelist types and configuration hook

**Files:**
- Create: `PlutoFrameworkCore/Solana/SolanaTokenProgram.cs`
- Create: `PlutoFrameworkCore/Solana/SolanaNativeToken.cs`
- Create: `PlutoFrameworkCore/Solana/SolanaTokenWhitelistEntry.cs`
- Modify: `PlutoFrameworkCore/PlutoConfigurationModel.cs`
- Test: `PlutoFrameworkTests/SolanaTokenWhitelistTests.cs`

**Interfaces:**
- Consumes: `SolanaCluster` (existing, `PlutoFrameworkCore/Solana/SolanaCluster.cs`).
- Produces: `SolanaTokenProgram.Legacy`, `SolanaTokenProgram.Token2022`, `SolanaNativeToken.{Symbol,Mint,Decimals,LamportsPerSol}`, `SolanaTokenWhitelistEntry`, `SolanaTokenWhitelist.ForCluster(SolanaCluster) -> IReadOnlyList<SolanaTokenWhitelistEntry>`, `PlutoConfigurationModel.WhitelistedSolanaTokens`.

- [ ] **Step 1: Write the failing test**

Create `PlutoFrameworkTests/SolanaTokenWhitelistTests.cs`:

```csharp
using PlutoFrameworkCore;
using PlutoFrameworkCore.Solana;

namespace PlutoFrameworkTests
{
    public class SolanaTokenWhitelistTests
    {
        private const string UsdcMainnet = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
        private const string UsdcDevnet = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU";

        [SetUp]
        public void SetUp()
        {
            PlutoConfigurationModel.WhitelistedSolanaTokens =
            [
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Mainnet,
                    Mint = UsdcMainnet,
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Devnet,
                    Mint = UsdcDevnet,
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
            ];
        }

        [TearDown]
        public void TearDown() => PlutoConfigurationModel.WhitelistedSolanaTokens = [];

        /// <summary>
        /// A mint address names a different token on each cluster. Returning the whole list
        /// would show mainnet balances while the app is pointed at devnet.
        /// </summary>
        [Test]
        public void ReturnsOnlyTheSelectedClustersMints()
        {
            var mainnet = SolanaTokenWhitelist.ForCluster(SolanaCluster.Mainnet);
            var devnet = SolanaTokenWhitelist.ForCluster(SolanaCluster.Devnet);

            Assert.Multiple(() =>
            {
                Assert.That(mainnet.Select(entry => entry.Mint), Is.EqualTo(new[] { UsdcMainnet }));
                Assert.That(devnet.Select(entry => entry.Mint), Is.EqualTo(new[] { UsdcDevnet }));
            });
        }

        [Test]
        public void ReturnsEmptyForAClusterWithNoEntries()
        {
            Assert.That(SolanaTokenWhitelist.ForCluster(SolanaCluster.Testnet), Is.Empty);
        }

        /// <summary>
        /// Entries default to the legacy SPL Token program. Token-2022 accounts are returned
        /// by a different program id, so a wrong default would silently report zero.
        /// </summary>
        [Test]
        public void DefaultsToTheLegacyTokenProgram()
        {
            Assert.That(
                SolanaTokenWhitelist.ForCluster(SolanaCluster.Mainnet)[0].ProgramId,
                Is.EqualTo("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA"));
        }

        [Test]
        public void NativeSolIsNineDecimals()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaNativeToken.Decimals, Is.EqualTo(9));
                Assert.That(SolanaNativeToken.LamportsPerSol, Is.EqualTo(1_000_000_000UL));
                Assert.That(SolanaNativeToken.Mint, Is.EqualTo("So11111111111111111111111111111111111111112"));
            });
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaTokenWhitelistTests`
Expected: build failure — `SolanaTokenWhitelistEntry`, `SolanaTokenWhitelist`, `SolanaNativeToken` and `WhitelistedSolanaTokens` do not exist.

- [ ] **Step 3: Write minimal implementation**

`PlutoFrameworkCore/Solana/SolanaTokenProgram.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// SPL token program ids. Which one owns a mint decides which
    /// <c>getTokenAccountsByOwner</c> call returns its accounts, so a mint filed under the
    /// wrong program reports a zero balance rather than an error.
    /// </summary>
    public static class SolanaTokenProgram
    {
        public const string Legacy = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";

        public const string Token2022 = "TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb";
    }
}
```

`PlutoFrameworkCore/Solana/SolanaNativeToken.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// SOL. Not an SPL token and never a whitelist entry: it pays every fee, so it must be
    /// visible even when the whitelist is empty or misconfigured.
    /// </summary>
    public static class SolanaNativeToken
    {
        public const string Symbol = "SOL";

        /// <summary>
        /// The wrapped-SOL mint. SOL itself has no mint; this is the address price feeds
        /// key it by.
        /// </summary>
        public const string Mint = "So11111111111111111111111111111111111111112";

        public const int Decimals = 9;

        public const ulong LamportsPerSol = 1_000_000_000;
    }
}
```

`PlutoFrameworkCore/Solana/SolanaTokenWhitelistEntry.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// One SPL token the app displays, on one cluster. Symbol and decimals are configured
    /// rather than read from chain so a token the user holds no account for can still be
    /// listed at zero.
    /// </summary>
    public sealed record SolanaTokenWhitelistEntry
    {
        public required SolanaCluster Cluster { get; init; }

        /// <summary>Base58 mint address. Cluster-specific: the same token has a different mint on each.</summary>
        public required string Mint { get; init; }

        public required string Symbol { get; init; }

        public required int Decimals { get; init; }

        /// <summary>
        /// A fixed USD price, for stablecoins. Null means priced from the live feed. A pinned
        /// price never reaches the network, so a feed outage cannot misprice a stablecoin.
        /// </summary>
        public double? PinnedUsdPrice { get; init; }

        public string ProgramId { get; init; } = SolanaTokenProgram.Legacy;
    }

    public static class SolanaTokenWhitelist
    {
        /// <summary>
        /// The tokens configured for one cluster.
        /// </summary>
        /// <remarks>
        /// An empty result means "no SPL tokens", not "all of them" — the inverse of
        /// <see cref="PlutoConfigurationModel.WhitelistedTokens"/>, which filters a set
        /// discovered on chain. This list *is* the set.
        /// </remarks>
        public static IReadOnlyList<SolanaTokenWhitelistEntry> ForCluster(SolanaCluster cluster) =>
            PlutoConfigurationModel.WhitelistedSolanaTokens
                .Where(entry => entry.Cluster == cluster)
                .ToList();
    }
}
```

In `PlutoFrameworkCore/PlutoConfigurationModel.cs`, add inside `PlutoConfigurationModel` beside `WhitelistedTokens`:

```csharp
        /// <summary>
        /// SPL tokens shown on the Solana balances page, per cluster. Unlike
        /// <see cref="WhitelistedTokens"/>, an empty list means no SPL tokens rather than
        /// no filtering — this list is the set, not a filter over a discovered one.
        /// </summary>
        public static System.Collections.Generic.List<Solana.SolanaTokenWhitelistEntry> WhitelistedSolanaTokens { get; set; } = new System.Collections.Generic.List<Solana.SolanaTokenWhitelistEntry>();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaTokenWhitelistTests`
Expected: PASS, 4 tests.

- [ ] **Step 5: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFrameworkCore/Solana/SolanaTokenProgram.cs PlutoFrameworkCore/Solana/SolanaNativeToken.cs PlutoFrameworkCore/Solana/SolanaTokenWhitelistEntry.cs PlutoFrameworkCore/PlutoConfigurationModel.cs PlutoFrameworkTests/SolanaTokenWhitelistTests.cs
git commit -m "feat: add Solana token whitelist types and configuration hook"
```

---

### Task 2: Base-unit amount conversion

**Files:**
- Create: `PlutoFrameworkCore/Solana/SolanaAmount.cs`
- Test: `PlutoFrameworkTests/SolanaAmountTests.cs`

**Interfaces:**
- Consumes: `SolanaNativeToken` (Task 1).
- Produces: `SolanaAmount.FromBaseUnits(string rawAmount, int decimals) -> decimal`, `SolanaAmount.FromLamports(ulong lamports) -> decimal`.

Solnet's `TokenBalance` exposes `Amount` (raw base units, string), `Decimals`, and helpers named `AmountDecimal` / `AmountDouble` whose scaling is undocumented. Converting explicitly here is deliberate: trusting the wrong helper renders a balance 10⁶ times too large, and that is discovered by a user, not a compiler.

- [ ] **Step 1: Write the failing test**

Create `PlutoFrameworkTests/SolanaAmountTests.cs`:

```csharp
using PlutoFrameworkCore.Solana;

namespace PlutoFrameworkTests
{
    public class SolanaAmountTests
    {
        [Test]
        public void ConvertsSixDecimalTokens()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaAmount.FromBaseUnits("40000000", 6), Is.EqualTo(40m));
                Assert.That(SolanaAmount.FromBaseUnits("1", 6), Is.EqualTo(0.000001m));
                Assert.That(SolanaAmount.FromBaseUnits("1234567", 6), Is.EqualTo(1.234567m));
            });
        }

        [Test]
        public void ConvertsNineDecimalTokens()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaAmount.FromBaseUnits("1000000000", 9), Is.EqualTo(1m));
                Assert.That(SolanaAmount.FromBaseUnits("1", 9), Is.EqualTo(0.000000001m));
            });
        }

        [Test]
        public void ZeroAndEmptyBecomeZero()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaAmount.FromBaseUnits("0", 6), Is.EqualTo(0m));
                Assert.That(SolanaAmount.FromBaseUnits("", 6), Is.EqualTo(0m));
            });
        }

        [Test]
        public void ZeroDecimalsIsIdentity()
        {
            Assert.That(SolanaAmount.FromBaseUnits("42", 0), Is.EqualTo(42m));
        }

        /// <summary>
        /// SPL amounts are u64. The largest possible value must not overflow decimal.
        /// </summary>
        [Test]
        public void HandlesMaximumUnsignedLong()
        {
            Assert.That(SolanaAmount.FromBaseUnits("18446744073709551615", 9),
                Is.EqualTo(18446744073.709551615m));
        }

        [Test]
        public void LamportsConvertToSol()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaAmount.FromLamports(1_000_000_000UL), Is.EqualTo(1m));
                Assert.That(SolanaAmount.FromLamports(0UL), Is.EqualTo(0m));
                Assert.That(SolanaAmount.FromLamports(12_345UL), Is.EqualTo(0.000012345m));
            });
        }

        [Test]
        public void RejectsNegativeDecimals()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SolanaAmount.FromBaseUnits("1", -1));
        }

        [Test]
        public void RejectsNonNumericAmounts()
        {
            Assert.Throws<FormatException>(() => SolanaAmount.FromBaseUnits("not-a-number", 6));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaAmountTests`
Expected: build failure — `SolanaAmount` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `PlutoFrameworkCore/Solana/SolanaAmount.cs`:

```csharp
using System.Globalization;
using System.Numerics;

namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// Base units to display units.
    /// </summary>
    /// <remarks>
    /// Done explicitly rather than through Solnet's <c>AmountDecimal</c> / <c>AmountDouble</c>,
    /// whose scaling the names do not settle. A wrong choice there renders a balance orders
    /// of magnitude off, which no test of ours would catch if we delegated to it.
    /// </remarks>
    public static class SolanaAmount
    {
        public static decimal FromBaseUnits(string rawAmount, int decimals)
        {
            if (decimals < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Decimals cannot be negative");
            }

            if (string.IsNullOrWhiteSpace(rawAmount))
            {
                return 0m;
            }

            // NumberStyles.None rejects signs, whitespace and separators: token amounts are
            // unsigned integers, and anything else is a malformed response, not a balance.
            if (!BigInteger.TryParse(rawAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var raw))
            {
                throw new FormatException($"'{rawAmount}' is not a base-unit token amount");
            }

            return (decimal)raw / (decimal)BigInteger.Pow(10, decimals);
        }

        public static decimal FromLamports(ulong lamports) =>
            FromBaseUnits(lamports.ToString(CultureInfo.InvariantCulture), SolanaNativeToken.Decimals);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaAmountTests`
Expected: PASS, 8 tests.

- [ ] **Step 5: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFrameworkCore/Solana/SolanaAmount.cs PlutoFrameworkTests/SolanaAmountTests.cs
git commit -m "feat: add Solana base-unit amount conversion"
```

---

### Task 3: Balance row assembly

**Files:**
- Create: `PlutoFrameworkCore/Solana/SolanaTokenBalance.cs`
- Create: `PlutoFrameworkCore/Solana/SolanaBalanceAssembler.cs`
- Test: `PlutoFrameworkTests/SolanaBalanceAssemblerTests.cs`

**Interfaces:**
- Consumes: `SolanaAmount` (Task 2), `SolanaTokenWhitelistEntry`, `SolanaNativeToken` (Task 1).
- Produces:
  - `SolanaTokenBalance` with `Symbol`, `Mint`, `Amount` (decimal), `Decimals`, `IsNative`, `UsdValue` (double?).
  - `SolanaTokenAccountAmount` with `Mint`, `RawAmount`, `Decimals`.
  - `SolanaBalanceAssembler.Assemble(ulong lamports, IReadOnlyList<SolanaTokenAccountAmount> tokenAccounts, IReadOnlyList<SolanaTokenWhitelistEntry> whitelist, IReadOnlyDictionary<string, double> usdPrices) -> IReadOnlyList<SolanaTokenBalance>`
  - `SolanaBalanceAssembler.TotalUsd(IEnumerable<SolanaTokenBalance>) -> double`

This is where every rule the page depends on lives, and it has no network, no Solnet types, and no MAUI — so all of it is testable.

- [ ] **Step 1: Write the failing test**

Create `PlutoFrameworkTests/SolanaBalanceAssemblerTests.cs`:

```csharp
using PlutoFrameworkCore.Solana;

namespace PlutoFrameworkTests
{
    public class SolanaBalanceAssemblerTests
    {
        private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
        private const string OtherMint = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU";

        private static SolanaTokenWhitelistEntry Usdc() => new()
        {
            Cluster = SolanaCluster.Mainnet,
            Mint = UsdcMint,
            Symbol = "USDC",
            Decimals = 6,
            PinnedUsdPrice = 1.00,
        };

        private static SolanaTokenAccountAmount Account(string mint, string rawAmount, int decimals) => new()
        {
            Mint = mint,
            RawAmount = rawAmount,
            Decimals = decimals,
        };

        [Test]
        public void SolIsAlwaysFirstAndAlwaysPresent()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 2_500_000_000UL,
                tokenAccounts: [],
                whitelist: [],
                usdPrices: new Dictionary<string, double>());

            Assert.Multiple(() =>
            {
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].Symbol, Is.EqualTo("SOL"));
                Assert.That(rows[0].IsNative, Is.True);
                Assert.That(rows[0].Amount, Is.EqualTo(2.5m));
            });
        }

        /// <summary>
        /// The page lists the tokens the app deals in. A token vanishing when its balance
        /// hits zero reads as a bug, and hides the account the user is about to fund.
        /// </summary>
        [Test]
        public void WhitelistedMintWithNoAccountAppearsAtZero()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 0UL,
                tokenAccounts: [],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double>());

            var usdc = rows.Single(row => row.Symbol == "USDC");

            Assert.Multiple(() =>
            {
                Assert.That(usdc.Amount, Is.EqualTo(0m));
                Assert.That(usdc.Decimals, Is.EqualTo(6));
            });
        }

        /// <summary>
        /// A wallet can hold more than one token account for the same mint. Taking the first
        /// would under-report the balance by however much sits in the others.
        /// </summary>
        [Test]
        public void SeveralAccountsForOneMintAreSummed()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 0UL,
                tokenAccounts:
                [
                    Account(UsdcMint, "40000000", 6),
                    Account(UsdcMint, "2500000", 6),
                ],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double>());

            Assert.That(rows.Single(row => row.Symbol == "USDC").Amount, Is.EqualTo(42.5m));
        }

        [Test]
        public void AccountsForUnlistedMintsAreIgnored()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 0UL,
                tokenAccounts: [Account(OtherMint, "999000000", 6)],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double>());

            Assert.Multiple(() =>
            {
                Assert.That(rows.Select(row => row.Mint), Does.Not.Contain(OtherMint));
                Assert.That(rows.Single(row => row.Symbol == "USDC").Amount, Is.EqualTo(0m));
            });
        }

        [Test]
        public void UsdValueIsAmountTimesPrice()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 2_000_000_000UL,
                tokenAccounts: [Account(UsdcMint, "40000000", 6)],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double>
                {
                    [SolanaNativeToken.Mint] = 74.0,
                    [UsdcMint] = 1.0,
                });

            Assert.Multiple(() =>
            {
                Assert.That(rows.Single(row => row.IsNative).UsdValue, Is.EqualTo(148.0).Within(0.0001));
                Assert.That(rows.Single(row => row.Symbol == "USDC").UsdValue, Is.EqualTo(40.0).Within(0.0001));
            });
        }

        /// <summary>
        /// A missing price is unknown, not zero. Rendering it as $0.00 tells the user their
        /// money is gone.
        /// </summary>
        [Test]
        public void MissingPriceLeavesUsdValueNull()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 1_000_000_000UL,
                tokenAccounts: [],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double>());

            Assert.Multiple(() =>
            {
                Assert.That(rows.Single(row => row.IsNative).UsdValue, Is.Null);
                Assert.That(rows.Single(row => row.Symbol == "USDC").UsdValue, Is.Null);
            });
        }

        [Test]
        public void TotalSumsOnlyPricedRows()
        {
            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 1_000_000_000UL,
                tokenAccounts: [Account(UsdcMint, "40000000", 6)],
                whitelist: [Usdc()],
                usdPrices: new Dictionary<string, double> { [UsdcMint] = 1.0 });

            Assert.That(SolanaBalanceAssembler.TotalUsd(rows), Is.EqualTo(40.0).Within(0.0001));
        }

        [Test]
        public void RowsFollowWhitelistOrderAfterSol()
        {
            var second = Usdc() with { Mint = OtherMint, Symbol = "TEST" };

            var rows = SolanaBalanceAssembler.Assemble(
                lamports: 0UL,
                tokenAccounts: [],
                whitelist: [Usdc(), second],
                usdPrices: new Dictionary<string, double>());

            Assert.That(rows.Select(row => row.Symbol), Is.EqualTo(new[] { "SOL", "USDC", "TEST" }));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaBalanceAssemblerTests`
Expected: build failure — `SolanaBalanceAssembler`, `SolanaTokenBalance`, `SolanaTokenAccountAmount` do not exist.

- [ ] **Step 3: Write minimal implementation**

Create `PlutoFrameworkCore/Solana/SolanaTokenBalance.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>One row on the balances page.</summary>
    public sealed record SolanaTokenBalance
    {
        public required string Symbol { get; init; }

        public required string Mint { get; init; }

        /// <summary>Display units, already scaled by decimals.</summary>
        public required decimal Amount { get; init; }

        public required int Decimals { get; init; }

        public required bool IsNative { get; init; }

        /// <summary>Null when no price is known. Not zero — those mean different things.</summary>
        public double? UsdValue { get; init; }
    }

    /// <summary>
    /// One token account's amount, free of Solnet types so the assembler stays testable.
    /// </summary>
    public sealed record SolanaTokenAccountAmount
    {
        public required string Mint { get; init; }

        /// <summary>Raw base units, as the RPC returns them.</summary>
        public required string RawAmount { get; init; }

        public required int Decimals { get; init; }
    }
}
```

Create `PlutoFrameworkCore/Solana/SolanaBalanceAssembler.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// Turns a lamport balance and a bag of token accounts into the rows the page shows.
    /// Pure: every rule the page depends on is decided here, where it can be tested without
    /// a network.
    /// </summary>
    public static class SolanaBalanceAssembler
    {
        public static IReadOnlyList<SolanaTokenBalance> Assemble(
            ulong lamports,
            IReadOnlyList<SolanaTokenAccountAmount> tokenAccounts,
            IReadOnlyList<SolanaTokenWhitelistEntry> whitelist,
            IReadOnlyDictionary<string, double> usdPrices)
        {
            var rows = new List<SolanaTokenBalance>(whitelist.Count + 1);

            var solAmount = SolanaAmount.FromLamports(lamports);

            rows.Add(new SolanaTokenBalance
            {
                Symbol = SolanaNativeToken.Symbol,
                Mint = SolanaNativeToken.Mint,
                Amount = solAmount,
                Decimals = SolanaNativeToken.Decimals,
                IsNative = true,
                UsdValue = ToUsdValue(solAmount, SolanaNativeToken.Mint, usdPrices),
            });

            // One wallet can hold several accounts for the same mint; the balance is the sum.
            var amountByMint = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var account in tokenAccounts)
            {
                var amount = SolanaAmount.FromBaseUnits(account.RawAmount, account.Decimals);

                amountByMint[account.Mint] = amountByMint.TryGetValue(account.Mint, out var running)
                    ? running + amount
                    : amount;
            }

            foreach (var entry in whitelist)
            {
                // Absent means the user has no account for this mint, which is a zero balance
                // rather than a reason to omit the row.
                var amount = amountByMint.TryGetValue(entry.Mint, out var held) ? held : 0m;

                rows.Add(new SolanaTokenBalance
                {
                    Symbol = entry.Symbol,
                    Mint = entry.Mint,
                    Amount = amount,
                    Decimals = entry.Decimals,
                    IsNative = false,
                    UsdValue = ToUsdValue(amount, entry.Mint, usdPrices),
                });
            }

            return rows;
        }

        /// <summary>
        /// Sums the rows that have a price. An unpriced row contributes nothing rather than
        /// dragging the total to zero.
        /// </summary>
        public static double TotalUsd(IEnumerable<SolanaTokenBalance> rows) =>
            rows.Sum(row => row.UsdValue ?? 0d);

        private static double? ToUsdValue(
            decimal amount, string mint, IReadOnlyDictionary<string, double> usdPrices) =>
            usdPrices.TryGetValue(mint, out var price) ? (double)amount * price : null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter SolanaBalanceAssemblerTests`
Expected: PASS, 8 tests.

- [ ] **Step 5: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFrameworkCore/Solana/SolanaTokenBalance.cs PlutoFrameworkCore/Solana/SolanaBalanceAssembler.cs PlutoFrameworkTests/SolanaBalanceAssemblerTests.cs
git commit -m "feat: assemble Solana balance rows from accounts and whitelist"
```

---

### Task 4: USD prices

**Files:**
- Create: `PlutoFrameworkCore/Solana/SolanaPriceParser.cs`
- Create: `PlutoFrameworkCore/Solana/SolanaPriceModel.cs`
- Test: `PlutoFrameworkTests/SolanaPriceTests.cs`

**Interfaces:**
- Consumes: `SolanaTokenWhitelistEntry`, `SolanaNativeToken` (Task 1).
- Produces:
  - `SolanaPriceParser.Parse(string json) -> IReadOnlyDictionary<string, double>`
  - `SolanaPriceModel.ResolvePrices(IReadOnlyList<SolanaTokenWhitelistEntry> whitelist, IReadOnlyDictionary<string, double> livePrices) -> IReadOnlyDictionary<string, double>`
  - `SolanaPriceModel.MintsNeedingLivePrice(IReadOnlyList<SolanaTokenWhitelistEntry> whitelist) -> IReadOnlyList<string>`
  - `SolanaPriceModel.GetUsdPricesAsync(IReadOnlyList<SolanaTokenWhitelistEntry> whitelist, CancellationToken token) -> Task<IReadOnlyDictionary<string, double>>`

The HTTP call is a thin shell; the two decisions worth testing — which mints need the network, and how pinned and live prices combine — are pure functions beside it.

- [ ] **Step 1: Write the failing test**

Create `PlutoFrameworkTests/SolanaPriceTests.cs`:

```csharp
using PlutoFrameworkCore.Solana;

namespace PlutoFrameworkTests
{
    public class SolanaPriceParserTests
    {
        /// <summary>The exact shape returned by lite-api.jup.ag/price/v3, captured live.</summary>
        private const string SampleResponse = """
        {"So11111111111111111111111111111111111111112":{"createdAt":"2024-06-05T08:55:25.527Z",
        "liquidity":649200458.3446863,"usdPrice":74.15443178403174,"blockId":435157282,
        "decimals":9,"priceChange24h":0.3258998223782334}}
        """;

        [Test]
        public void ReadsUsdPriceKeyedByMint()
        {
            var prices = SolanaPriceParser.Parse(SampleResponse);

            Assert.That(prices[SolanaNativeToken.Mint], Is.EqualTo(74.15443178403174).Within(0.0000001));
        }

        [Test]
        public void AbsentMintIsAbsentFromTheResult()
        {
            var prices = SolanaPriceParser.Parse("""{"SomeOtherMint":{"usdPrice":3.0}}""");

            Assert.That(prices.ContainsKey(SolanaNativeToken.Mint), Is.False);
        }

        [Test]
        public void EntryWithoutUsdPriceIsSkipped()
        {
            var prices = SolanaPriceParser.Parse("""{"MintA":{"liquidity":1.0},"MintB":{"usdPrice":2.0}}""");

            Assert.Multiple(() =>
            {
                Assert.That(prices.ContainsKey("MintA"), Is.False);
                Assert.That(prices["MintB"], Is.EqualTo(2.0));
            });
        }

        /// <summary>
        /// A malformed body is a feed problem. It must degrade to "no prices", never throw
        /// into the page's load path.
        /// </summary>
        [Test]
        public void MalformedJsonYieldsNoPrices()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SolanaPriceParser.Parse("not json"), Is.Empty);
                Assert.That(SolanaPriceParser.Parse(""), Is.Empty);
                Assert.That(SolanaPriceParser.Parse("[1,2,3]"), Is.Empty);
            });
        }
    }

    public class SolanaPriceModelTests
    {
        private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
        private const string UnpinnedMint = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU";

        private static SolanaTokenWhitelistEntry Pinned() => new()
        {
            Cluster = SolanaCluster.Mainnet,
            Mint = UsdcMint,
            Symbol = "USDC",
            Decimals = 6,
            PinnedUsdPrice = 1.00,
        };

        private static SolanaTokenWhitelistEntry Unpinned() => new()
        {
            Cluster = SolanaCluster.Mainnet,
            Mint = UnpinnedMint,
            Symbol = "TEST",
            Decimals = 6,
        };

        /// <summary>
        /// SOL always needs a live price. A pinned stablecoin never reaches the network, so
        /// a feed outage cannot reprice it.
        /// </summary>
        [Test]
        public void OnlyUnpinnedMintsAndSolNeedTheNetwork()
        {
            var mints = SolanaPriceModel.MintsNeedingLivePrice([Pinned(), Unpinned()]);

            Assert.That(mints, Is.EquivalentTo(new[] { SolanaNativeToken.Mint, UnpinnedMint }));
        }

        [Test]
        public void PinnedPriceWinsOverTheFeed()
        {
            var resolved = SolanaPriceModel.ResolvePrices(
                [Pinned()],
                new Dictionary<string, double> { [UsdcMint] = 0.87 });

            Assert.That(resolved[UsdcMint], Is.EqualTo(1.00));
        }

        [Test]
        public void LivePricesFillUnpinnedMints()
        {
            var resolved = SolanaPriceModel.ResolvePrices(
                [Pinned(), Unpinned()],
                new Dictionary<string, double>
                {
                    [UnpinnedMint] = 3.5,
                    [SolanaNativeToken.Mint] = 74.0,
                });

            Assert.Multiple(() =>
            {
                Assert.That(resolved[UsdcMint], Is.EqualTo(1.00));
                Assert.That(resolved[UnpinnedMint], Is.EqualTo(3.5));
                Assert.That(resolved[SolanaNativeToken.Mint], Is.EqualTo(74.0));
            });
        }

        /// <summary>
        /// A dead feed still leaves pinned prices usable, and leaves everything else unpriced
        /// rather than priced at zero.
        /// </summary>
        [Test]
        public void NoLivePricesStillResolvesPinnedOnes()
        {
            var resolved = SolanaPriceModel.ResolvePrices(
                [Pinned(), Unpinned()],
                new Dictionary<string, double>());

            Assert.Multiple(() =>
            {
                Assert.That(resolved[UsdcMint], Is.EqualTo(1.00));
                Assert.That(resolved.ContainsKey(UnpinnedMint), Is.False);
                Assert.That(resolved.ContainsKey(SolanaNativeToken.Mint), Is.False);
            });
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter "SolanaPriceParserTests|SolanaPriceModelTests"`
Expected: build failure — `SolanaPriceParser` and `SolanaPriceModel` do not exist.

- [ ] **Step 3: Write minimal implementation**

Create `PlutoFrameworkCore/Solana/SolanaPriceParser.cs`:

```csharp
using System.Text.Json;

namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// Reads Jupiter's price response: an object keyed by mint, each value carrying
    /// <c>usdPrice</c> among other fields.
    /// </summary>
    public static class SolanaPriceParser
    {
        public static IReadOnlyDictionary<string, double> Parse(string json)
        {
            var prices = new Dictionary<string, double>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(json))
            {
                return prices;
            }

            try
            {
                using var document = JsonDocument.Parse(json);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return prices;
                }

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (!property.Value.TryGetProperty("usdPrice", out var price) ||
                        price.ValueKind != JsonValueKind.Number)
                    {
                        continue;
                    }

                    prices[property.Name] = price.GetDouble();
                }
            }
            catch (JsonException)
            {
                // A malformed body is a feed problem. Degrade to "no prices" rather than
                // throwing into the page's load path.
                return new Dictionary<string, double>(StringComparer.Ordinal);
            }

            return prices;
        }
    }
}
```

Create `PlutoFrameworkCore/Solana/SolanaPriceModel.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// USD prices for the tokens on the balances page. Stablecoins carry a pinned price and
    /// never reach the network; everything else, SOL included, comes from Jupiter.
    /// </summary>
    public static class SolanaPriceModel
    {
        private const string PRICE_ENDPOINT = "https://lite-api.jup.ag/price/v3?ids=";

        /// <summary>
        /// Reused: a fresh HttpClient per call leaks sockets, matching how
        /// <see cref="SolanaRpcModel"/> reuses its RPC clients.
        /// </summary>
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

        public static IReadOnlyList<string> MintsNeedingLivePrice(
            IReadOnlyList<SolanaTokenWhitelistEntry> whitelist)
        {
            var mints = new List<string> { SolanaNativeToken.Mint };

            mints.AddRange(whitelist
                .Where(entry => entry.PinnedUsdPrice is null)
                .Select(entry => entry.Mint));

            return mints;
        }

        public static IReadOnlyDictionary<string, double> ResolvePrices(
            IReadOnlyList<SolanaTokenWhitelistEntry> whitelist,
            IReadOnlyDictionary<string, double> livePrices)
        {
            var resolved = new Dictionary<string, double>(livePrices, StringComparer.Ordinal);

            // Applied last so a pinned price overrides whatever the feed said. A depegged
            // quote for a token we treat as a dollar is noise, not news.
            foreach (var entry in whitelist)
            {
                if (entry.PinnedUsdPrice is double pinned)
                {
                    resolved[entry.Mint] = pinned;
                }
            }

            return resolved;
        }

        public static async Task<IReadOnlyDictionary<string, double>> GetUsdPricesAsync(
            IReadOnlyList<SolanaTokenWhitelistEntry> whitelist,
            CancellationToken token)
        {
            var livePrices = new Dictionary<string, double>(StringComparer.Ordinal);

            var mints = MintsNeedingLivePrice(whitelist);

            try
            {
                var body = await Client.GetStringAsync(PRICE_ENDPOINT + string.Join(',', mints), token);

                foreach (var (mint, price) in SolanaPriceParser.Parse(body))
                {
                    livePrices[mint] = price;
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                // Prices are decoration; balances are the feature. A feed outage must leave
                // the page showing amounts rather than nothing.
                Console.WriteLine($"Solana price fetch failed: {ex.Message}");
            }

            return ResolvePrices(whitelist, livePrices);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo --filter "SolanaPriceParserTests|SolanaPriceModelTests"`
Expected: PASS, 8 tests.

- [ ] **Step 5: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFrameworkCore/Solana/SolanaPriceParser.cs PlutoFrameworkCore/Solana/SolanaPriceModel.cs PlutoFrameworkTests/SolanaPriceTests.cs
git commit -m "feat: add Solana USD pricing with pinned stablecoins"
```

---

### Task 5: RPC queries and balance orchestration

**Files:**
- Modify: `PlutoFrameworkCore/Solana/SolanaRpcModel.cs`
- Create: `PlutoFrameworkCore/Solana/SolanaBalancesModel.cs`
- Test: none — this is the Solnet edge. Its logic lives in Tasks 3 and 4.

**Interfaces:**
- Consumes: `SolanaBalanceAssembler` (Task 3), `SolanaPriceModel` (Task 4), `SolanaTokenWhitelist` (Task 1), existing `SolanaRpcModel.GetClient` and `Unwrap`.
- Produces: `SolanaBalancesModel.GetBalancesAsync(string address, SolanaCluster cluster, CancellationToken token) -> Task<IReadOnlyList<SolanaTokenBalance>>`.

- [ ] **Step 1: Add the RPC wrappers**

In `PlutoFrameworkCore/Solana/SolanaRpcModel.cs`, add `using Solnet.Rpc.Models;` to the existing usings and these two methods after `SendTransactionAsync`:

```csharp
        /// <summary>
        /// The account's SOL balance, in lamports.
        /// </summary>
        public static async Task<ulong> GetLamportBalanceAsync(
            SolanaCluster cluster, string address, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var result = await GetClient(cluster).GetBalanceAsync(address);

            return Unwrap(result, $"fetch the SOL balance on {cluster.GetName()}").Value;
        }

        /// <summary>
        /// Every token account the address owns under one token program.
        /// </summary>
        /// <remarks>
        /// Filtered by program rather than by mint: the RPC accepts exactly one filter, and
        /// one call per whitelisted mint would multiply round trips against a rate-limited
        /// public endpoint.
        /// </remarks>
        public static async Task<List<TokenAccount>> GetTokenAccountsAsync(
            SolanaCluster cluster, string address, string programId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var result = await GetClient(cluster).GetTokenAccountsByOwnerAsync(
                address, tokenMintPubKey: null, tokenProgramId: programId);

            return Unwrap(result, $"fetch token accounts on {cluster.GetName()}").Value ?? [];
        }
```

- [ ] **Step 2: Write the orchestrator**

Create `PlutoFrameworkCore/Solana/SolanaBalancesModel.cs`:

```csharp
namespace PlutoFrameworkCore.Solana
{
    /// <summary>
    /// The balances page's single entry point: address plus cluster in, display rows out.
    /// </summary>
    /// <remarks>
    /// Deliberately thin. Everything decidable without a network lives in
    /// <see cref="SolanaBalanceAssembler"/> and <see cref="SolanaPriceModel"/>, where it is
    /// tested; this method only fetches and maps.
    /// </remarks>
    public static class SolanaBalancesModel
    {
        public static async Task<IReadOnlyList<SolanaTokenBalance>> GetBalancesAsync(
            string address, SolanaCluster cluster, CancellationToken token)
        {
            var whitelist = SolanaTokenWhitelist.ForCluster(cluster);

            var lamports = await SolanaRpcModel.GetLamportBalanceAsync(cluster, address, token);

            var accounts = new List<SolanaTokenAccountAmount>();

            // One call per distinct program: legacy SPL accounts and Token-2022 accounts are
            // returned by different program ids, never together.
            foreach (var programId in whitelist
                .Select(entry => entry.ProgramId)
                .Distinct(StringComparer.Ordinal))
            {
                var tokenAccounts = await SolanaRpcModel.GetTokenAccountsAsync(
                    cluster, address, programId, token);

                foreach (var tokenAccount in tokenAccounts)
                {
                    var info = tokenAccount.Account?.Data?.Parsed?.Info;

                    if (info?.TokenAmount is null || string.IsNullOrEmpty(info.Mint))
                    {
                        continue;
                    }

                    accounts.Add(new SolanaTokenAccountAmount
                    {
                        Mint = info.Mint,
                        RawAmount = info.TokenAmount.Amount,
                        Decimals = info.TokenAmount.Decimals,
                    });
                }
            }

            var prices = await SolanaPriceModel.GetUsdPricesAsync(whitelist, token);

            return SolanaBalanceAssembler.Assemble(lamports, accounts, whitelist, prices);
        }
    }
}
```

- [ ] **Step 3: Verify it compiles and existing tests still pass**

Run: `dotnet build PlutoFrameworkCore/PlutoFrameworkCore.csproj --nologo`
Expected: build succeeded, 0 errors.

Run: `dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo`
Expected: the failure count matches your baseline (29 on a clean checkout) and every Solana test passes.

If `Data.Parsed.Info` does not resolve, check the actual property names against
`~/.nuget/packages/solana.rpc/8.7.0/lib/net8.0/Solnet.Rpc.dll` — the chain is
`TokenAccount.Account` (`TokenAccountInfo`) → `.Data` (`TokenAccountData`) → `.Parsed`
(`ParsedTokenAccountData`) → `.Info` (`TokenAccountInfoDetails`) → `.TokenAmount`
(`TokenBalance`), verified against `bmresearch/Solnet@master`.

- [ ] **Step 4: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFrameworkCore/Solana/SolanaRpcModel.cs PlutoFrameworkCore/Solana/SolanaBalancesModel.cs
git commit -m "feat: query Solana SOL and SPL token balances over RPC"
```

---

### Task 6: USD currency formatting and the SOL icon

**Files:**
- Modify: `PlutoFramework/Model/Currency/ExchangeRateModel.cs`
- Modify: `PlutoFrameworkCore/Constants/Assets.cs`
- Create: `PlutoFramework/Resources/Images/Assets/solana.svg`

**Interfaces:**
- Produces: `ExchangeRateModel.ToUsdCurrencyString(this double usdValue, string? location = null, string? currencyFormat = null) -> string`; `Assets.GetAssetIcon("SOL") -> "solana.png"`.

`ToCurrencyString` converts with `GetExchangeRate("tGBP", currency)` regardless of what it is handed, so a USD total passed through it is mislabelled rather than converted. This adds the sibling that uses the `USDT` pair the existing table already defines.

- [ ] **Step 1: Add the USD formatter**

In `PlutoFramework/Model/Currency/ExchangeRateModel.cs`, add after the existing `ToCurrencyString(decimal, …)`:

```csharp
        /// <summary>
        /// Formats a genuine USD value in the user's currency.
        /// </summary>
        /// <remarks>
        /// <see cref="ToCurrencyString(double, string?, string?)"/> converts from tGBP no
        /// matter what it is given, so it cannot be used here: a USD total passed through it
        /// comes out mislabelled rather than converted.
        /// </remarks>
        public static string ToUsdCurrencyString(
            this double usdValue,
            string? location = null,
            string? currencyFormat = null
        )
        {
            currencyFormat ??= (string)Application.Current.Resources["CurrencyFormat"];

            location ??= AppConfigurationModel.Location;
            var currency = GetCurrencyInLocation(location);

            return $"{currency}{String.Format(currencyFormat, (decimal)GetExchangeRate("USDT", currency) * (decimal)usdValue)}";
        }
```

- [ ] **Step 2: Register the SOL icon**

In `PlutoFrameworkCore/Constants/Assets.cs`, add to the `AssetIcons` dictionary:

```csharp
            { "sol", "solana.png" },
```

Create `PlutoFramework/Resources/Images/Assets/solana.svg` (MAUI compiles `MauiImage` SVGs to PNG at build, which is why the mapping above names `.png` — the same convention as `hydration.svg` → `"hydration.png"`):

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
  <defs>
    <linearGradient id="sol" x1="10" y1="118" x2="118" y2="10" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#9945FF" />
      <stop offset="0.5" stop-color="#14F195" />
      <stop offset="1" stop-color="#00D18C" />
    </linearGradient>
  </defs>
  <circle cx="64" cy="64" r="64" fill="#131313" />
  <g fill="url(#sol)">
    <path d="M36.4 82.6c0.6-0.6 1.4-0.9 2.2-0.9h58.1c1.4 0 2.1 1.7 1.1 2.7L86.3 96.1c-0.6 0.6-1.4 0.9-2.2 0.9H26c-1.4 0-2.1-1.7-1.1-2.7z" />
    <path d="M36.4 31.9c0.6-0.6 1.4-0.9 2.2-0.9h58.1c1.4 0 2.1 1.7 1.1 2.7L86.3 45.4c-0.6 0.6-1.4 0.9-2.2 0.9H26c-1.4 0-2.1-1.7-1.1-2.7z" />
    <path d="M86.3 57.1c-0.6-0.6-1.4-0.9-2.2-0.9H26c-1.4 0-2.1 1.7-1.1 2.7l11.5 11.7c0.6 0.6 1.4 0.9 2.2 0.9h58.1c1.4 0 2.1-1.7 1.1-2.7z" />
  </g>
</svg>
```

- [ ] **Step 3: Verify the framework builds**

Run: `dotnet build PlutoFramework/PlutoFramework.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Model/Currency/ExchangeRateModel.cs PlutoFrameworkCore/Constants/Assets.cs PlutoFramework/Resources/Images/Assets/solana.svg
git commit -m "feat: add USD currency formatting and SOL icon"
```

---

### Task 7: Synchronous Solana key presence

**Files:**
- Modify: `PlutoFramework/Model/PreferencesModel.cs`
- Modify: `PlutoFramework/Model/KeysModel.cs`
- Modify: `PlutoFramework/Model/GenericLockedKeyExtensions.cs`

**Interfaces:**
- Produces: `PreferencesModel.SOLANA_PUBLIC_KEY`, `KeysModel.HasSolanaKey() -> bool`, `KeysModel.GetSolanaAddress() -> string?`.

`App.xaml.cs` picks the start page before any `await`, and the existing `HasSolanaKeyAsync()` queries the database. This mirrors exactly how `PreferencesModel.PUBLIC_KEY` backs `HasSubstrateKey()`.

- [ ] **Step 1: Add the preference key**

In `PlutoFramework/Model/PreferencesModel.cs`, add:

```csharp
        public const string SOLANA_PUBLIC_KEY = "solanaPublicKey";
```

- [ ] **Step 2: Write the preference on save**

In `PlutoFramework/Model/KeysModel.cs`, inside `SaveSolanaMnemonicKeyAsync`, after `string address = SolanaMnemonicsModel.GetAddressFromMnemonics(mnemonics);`:

```csharp
            // Mirrors PUBLIC_KEY for Substrate: app start decides its shell before it can
            // await, so key presence has to be readable synchronously.
            Preferences.Set(PreferencesModel.SOLANA_PUBLIC_KEY, address);
```

Inside `SaveSolanaMwaKeyAsync`, before the `SaveKeyAsync` call:

```csharp
            Preferences.Set(PreferencesModel.SOLANA_PUBLIC_KEY, key.Address);
```

- [ ] **Step 3: Add the readers**

In `PlutoFramework/Model/KeysModel.cs`, beside `HasSubstrateKey` / `GetSubstrateKey`:

```csharp
        public static bool HasSolanaKey()
        {
            return Preferences.ContainsKey(PreferencesModel.SOLANA_PUBLIC_KEY);
        }

        /// <summary>
        /// The stored Solana address, or null when no Solana key is configured. Returns null
        /// rather than a placeholder string: <see cref="GetSubstrateKey()"/>'s placeholder
        /// habit is what makes <c>GetSubstrateKey(0)</c> throw further down the call chain.
        /// </summary>
        public static string? GetSolanaAddress()
        {
            return Preferences.ContainsKey(PreferencesModel.SOLANA_PUBLIC_KEY)
                ? Preferences.Get(PreferencesModel.SOLANA_PUBLIC_KEY, string.Empty)
                : null;
        }
```

- [ ] **Step 4: Clear it on delete**

In `PlutoFramework/Model/KeysModel.cs`, in `ClearAsync`, after the existing `Preferences.Remove(PreferencesModel.PUBLIC_KEY);`:

```csharp
            Preferences.Remove(PreferencesModel.SOLANA_PUBLIC_KEY);
```

In `PlutoFramework/Model/GenericLockedKeyExtensions.cs`, inside `RemoveAsync`, before the `return`:

```csharp
            // Both Solana detail pages delete through here, so this is the one place that
            // has to stay in step with the preference the save methods write.
            if (key.Type == KeyTypeEnum.SolanaMnemonic || key.Type == KeyTypeEnum.SolanaMwa)
            {
                Preferences.Remove(PreferencesModel.SOLANA_PUBLIC_KEY);
            }
```

Add `using PlutoFrameworkCore.Keys;` to that file if `KeyTypeEnum` does not already resolve.

- [ ] **Step 5: Verify the framework builds**

Run: `dotnet build PlutoFramework/PlutoFramework.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Model/PreferencesModel.cs PlutoFramework/Model/KeysModel.cs PlutoFramework/Model/GenericLockedKeyExtensions.cs
git commit -m "feat: expose Solana key presence synchronously"
```

---

### Task 8: Network change notification

**Files:**
- Modify: `PlutoFramework/Model/SolanaNetworkModel.cs`

**Interfaces:**
- Produces: `SolanaNetworkModel.ClusterChanged` (`event EventHandler<SolanaCluster>?`).

Without this, switching to Devnet in Settings and returning to the main page leaves it showing mainnet money.

- [ ] **Step 1: Raise an event from the setter**

Replace the `SelectedCluster` property in `PlutoFramework/Model/SolanaNetworkModel.cs` with:

```csharp
        /// <summary>
        /// Raised after the selected network changes. Balance views hold figures that are
        /// only meaningful for one cluster, so they must re-query rather than keep showing
        /// numbers from the other one.
        /// </summary>
        public static event EventHandler<SolanaCluster>? ClusterChanged;

        public static SolanaCluster SelectedCluster
        {
            get => SolanaClusterExtensions.FromChainId(
                Preferences.Get(PreferencesModel.SETTINGS_SOLANA_NETWORK, SolanaNetworkOptions.Default.ToChainId()));

            set
            {
                if (value == SelectedCluster)
                {
                    return;
                }

                Preferences.Set(PreferencesModel.SETTINGS_SOLANA_NETWORK, value.ToChainId());

                ClusterChanged?.Invoke(null, value);
            }
        }
```

- [ ] **Step 2: Verify the framework builds**

Run: `dotnet build PlutoFramework/PlutoFramework.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Model/SolanaNetworkModel.cs
git commit -m "feat: notify listeners when the Solana network changes"
```

---

### Task 9: Balances page

**Files:**
- Create: `PlutoFramework/Components/Solana/SolanaAssetView.xaml`
- Create: `PlutoFramework/Components/Solana/SolanaAssetView.xaml.cs`
- Create: `PlutoFramework/Components/Solana/SolanaBalancesPageViewModel.cs`
- Create: `PlutoFramework/Components/Solana/SolanaBalancesPage.xaml`
- Create: `PlutoFramework/Components/Solana/SolanaBalancesPage.xaml.cs`

**Interfaces:**
- Consumes: `SolanaBalancesModel.GetBalancesAsync` (Task 5), `SolanaBalanceAssembler.TotalUsd` (Task 3), `KeysModel.GetSolanaAddress` (Task 7), `SolanaNetworkModel.ClusterChanged` (Task 8), `ToUsdCurrencyString` (Task 6).
- Produces: `SolanaBalancesPage` (parameterless constructor), `SolanaBalancesPageViewModel.LoadAsync(CancellationToken) -> Task`.

- [ ] **Step 1: Write the row view**

Create `PlutoFramework/Components/Solana/SolanaAssetView.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:card="clr-namespace:PlutoFramework.Components.Card"
             x:Class="PlutoFramework.Components.Solana.SolanaAssetView">
    <card:Card CardPadding="0, 0, 0, 0">
        <card:Card.View>
            <Grid ColumnDefinitions="70,*,130"
                  HeightRequest="70">

                <Image x:Name="assetIcon"
                       Grid.Column="0"
                       Aspect="AspectFit"
                       HeightRequest="40"
                       WidthRequest="40"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />

                <Label x:Name="symbolLabel"
                       Grid.Column="1"
                       FontAttributes="Bold"
                       FontSize="16"
                       LineBreakMode="TailTruncation"
                       VerticalTextAlignment="Center" />

                <VerticalStackLayout Grid.Column="2"
                                     Spacing="2"
                                     Margin="0, 0, 15, 0"
                                     VerticalOptions="Center">
                    <Label x:Name="amountLabel"
                           FontAttributes="Bold"
                           FontSize="15"
                           HorizontalTextAlignment="End" />
                    <Label x:Name="usdLabel"
                           FontAttributes="Bold"
                           FontSize="13"
                           TextColor="#888888"
                           HorizontalTextAlignment="End" />
                </VerticalStackLayout>
            </Grid>
        </card:Card.View>
    </card:Card>
</ContentView>
```

Create `PlutoFramework/Components/Solana/SolanaAssetView.xaml.cs`:

```csharp
using PlutoFramework.Model.Currency;
using PlutoFrameworkCore.Constants;
using PlutoFrameworkCore.Solana;

namespace PlutoFramework.Components.Solana;

public partial class SolanaAssetView : ContentView
{
    public static readonly BindableProperty BalanceProperty = BindableProperty.Create(
        nameof(Balance), typeof(SolanaTokenBalance), typeof(SolanaAssetView),
        defaultBindingMode: BindingMode.OneWay,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            var control = (SolanaAssetView)bindable;

            if (newValue is not SolanaTokenBalance balance)
            {
                return;
            }

            control.assetIcon.Source = Assets.GetAssetIcon(balance.Symbol);
            control.symbolLabel.Text = balance.Symbol;
            control.amountLabel.Text = $"{FormatAmount(balance)} {balance.Symbol}";

            // An unknown price shows nothing at all. "$0.00" would read as "your money is
            // gone" rather than "we could not reach the price feed".
            control.usdLabel.Text = balance.UsdValue is double usd ? usd.ToUsdCurrencyString() : string.Empty;
        });

    public SolanaAssetView()
    {
        InitializeComponent();
    }

    public SolanaTokenBalance Balance
    {
        get => (SolanaTokenBalance)GetValue(BalanceProperty);
        set => SetValue(BalanceProperty, value);
    }

    /// <summary>
    /// Trailing zeros are trimmed so a whole balance reads "40 USDC" rather than
    /// "40.000000 USDC", but a dust balance keeps enough places to stay visible.
    /// </summary>
    private static string FormatAmount(SolanaTokenBalance balance)
    {
        var rounded = Math.Round(balance.Amount, Math.Min(balance.Decimals, 6));

        return rounded.ToString("0.######");
    }
}
```

- [ ] **Step 2: Write the view model**

Create `PlutoFramework/Components/Solana/SolanaBalancesPageViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;
using PlutoFramework.Model.Currency;
using PlutoFrameworkCore.Solana;
using System.Collections.ObjectModel;

namespace PlutoFramework.Components.Solana
{
    public partial class SolanaBalancesPageViewModel : ObservableObject
    {
        public ObservableCollection<SolanaTokenBalance> Balances { get; } = [];

        [ObservableProperty]
        private bool isRefreshing = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAccount))]
        [NotifyPropertyChangedFor(nameof(NoAccount))]
        [NotifyPropertyChangedFor(nameof(QrAddress))]
        private string address = string.Empty;

        [ObservableProperty]
        private string totalText = "-";

        [ObservableProperty]
        private string networkName = SolanaNetworkModel.SelectedCluster.GetName();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ErrorIsVisible))]
        private string errorMessage = string.Empty;

        public bool HasAccount => !string.IsNullOrEmpty(Address);

        public bool NoAccount => !HasAccount;

        public bool ErrorIsVisible => !string.IsNullOrEmpty(ErrorMessage);

        public string QrAddress => $"solana:{Address}";

        public SolanaBalancesPageViewModel()
        {
            SolanaNetworkModel.ClusterChanged += OnClusterChanged;
        }

        /// <summary>
        /// Called by the page when it disappears. Without it the static event keeps every
        /// view model this page ever created alive, each re-querying on a network change.
        /// </summary>
        public void Unsubscribe() => SolanaNetworkModel.ClusterChanged -= OnClusterChanged;

        private void OnClusterChanged(object? sender, SolanaCluster cluster)
        {
            NetworkName = cluster.GetName();

            MainThread.BeginInvokeOnMainThread(async () => await LoadAsync(CancellationToken.None));
        }

        [RelayCommand]
        public Task RefreshAsync() => LoadAsync(CancellationToken.None);

        public async Task LoadAsync(CancellationToken token)
        {
            Address = KeysModel.GetSolanaAddress() ?? string.Empty;
            NetworkName = SolanaNetworkModel.SelectedCluster.GetName();

            if (!HasAccount)
            {
                Balances.Clear();
                TotalText = "-";
                return;
            }

            IsRefreshing = true;
            ErrorMessage = string.Empty;

            try
            {
                var rows = await SolanaBalancesModel.GetBalancesAsync(
                    Address, SolanaNetworkModel.SelectedCluster, token);

                Balances.Clear();

                foreach (var row in rows)
                {
                    Balances.Add(row);
                }

                TotalText = SolanaBalanceAssembler.TotalUsd(rows).ToUsdCurrencyString();
            }
            catch (OperationCanceledException)
            {
                // The page went away mid-query.
            }
            catch (SolanaRpcException ex)
            {
                // Distinguished from an empty wallet on purpose: showing zeros here would
                // claim a balance we never actually read.
                ErrorMessage = ex.Message;
                TotalText = "-";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Could not load balances: {ex.Message}";
                TotalText = "-";
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}
```

- [ ] **Step 3: Write the page**

Create `PlutoFramework/Components/Solana/SolanaBalancesPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<template:PageTemplate xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                       xmlns:template="clr-namespace:PlutoFramework.Templates.PageTemplate"
                       xmlns:address="clr-namespace:PlutoFramework.Components.AddressView"
                       xmlns:card="clr-namespace:PlutoFramework.Components.Card"
                       xmlns:solana="clr-namespace:PlutoFramework.Components.Solana"
                       x:Class="PlutoFramework.Components.Solana.SolanaBalancesPage"
                       Title="Balance">
    <RefreshView AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
                 AbsoluteLayout.LayoutFlags="All"
                 Command="{Binding RefreshCommand}"
                 IsRefreshing="{Binding IsRefreshing}">
        <ScrollView>
            <VerticalStackLayout Padding="20, 10, 20, 100"
                                 Spacing="15">

                <card:Card IsVisible="{Binding HasAccount}">
                    <card:Card.View>
                        <VerticalStackLayout Padding="15, 12, 15, 12"
                                             Spacing="4">
                            <Label Text="Total"
                                   FontSize="13"
                                   TextColor="#A6A6A6" />
                            <Label Text="{Binding TotalText}"
                                   FontAttributes="Bold"
                                   FontSize="26" />
                            <Label Text="{Binding NetworkName, StringFormat='Solana {0}'}"
                                   FontSize="12"
                                   TextColor="#A6A6A6" />
                        </VerticalStackLayout>
                    </card:Card.View>
                </card:Card>

                <address:AddressView Title="Solana address"
                                     IsVisible="{Binding HasAccount}"
                                     Address="{Binding Address}"
                                     QrAddress="{Binding QrAddress}" />

                <Label Text="{Binding ErrorMessage}"
                       IsVisible="{Binding ErrorIsVisible}"
                       TextColor="#C33"
                       FontSize="13"
                       HorizontalTextAlignment="Center" />

                <solana:SolanaNoAccountView IsVisible="{Binding NoAccount}" />

                <VerticalStackLayout BindableLayout.ItemsSource="{Binding Balances}"
                                     IsVisible="{Binding HasAccount}"
                                     Spacing="15">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate>
                            <solana:SolanaAssetView Balance="{Binding .}" />
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </VerticalStackLayout>
            </VerticalStackLayout>
        </ScrollView>
    </RefreshView>

    <template:PageTemplate.PopupContent>
        <address:AddressQrCodeView />
        <solana:ImportMethodPopupView />
    </template:PageTemplate.PopupContent>
</template:PageTemplate>
```

Create `PlutoFramework/Components/Solana/SolanaBalancesPage.xaml.cs`:

```csharp
using PlutoFramework.Templates.PageTemplate;

namespace PlutoFramework.Components.Solana;

public partial class SolanaBalancesPage : PageTemplate
{
    private readonly SolanaBalancesPageViewModel viewModel = new();

    public SolanaBalancesPage()
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Reloaded on every appearance, not just construction: the user may have changed
        // network, or created an account, while this page sat on the stack.
        _ = viewModel.LoadAsync(CancellationToken.None);
    }

    protected override void OnDisappearing()
    {
        viewModel.Unsubscribe();

        base.OnDisappearing();
    }
}
```

`SolanaNoAccountView` and `ImportMethodPopupView` are created in Task 10. Implement Task 10 before building this page, or the XAML will not compile.

- [ ] **Step 4: Commit (after Task 10 builds)**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Components/Solana/SolanaAssetView.xaml PlutoFramework/Components/Solana/SolanaAssetView.xaml.cs PlutoFramework/Components/Solana/SolanaBalancesPage.xaml PlutoFramework/Components/Solana/SolanaBalancesPage.xaml.cs PlutoFramework/Components/Solana/SolanaBalancesPageViewModel.cs
git commit -m "feat: add Solana balances page"
```

---

### Task 10: Import-method popup and empty state

**Files:**
- Create: `PlutoFramework/Components/Solana/ImportMethodPopupViewModel.cs`
- Create: `PlutoFramework/Components/Solana/ImportMethodPopupView.xaml`
- Create: `PlutoFramework/Components/Solana/ImportMethodPopupView.xaml.cs`
- Create: `PlutoFramework/Components/Solana/SolanaNoAccountView.xaml`
- Create: `PlutoFramework/Components/Solana/SolanaNoAccountView.xaml.cs`
- Modify: `PlutoFramework/MauiAppBuilderExtensions.cs`

**Interfaces:**
- Consumes: `SolanaMwaModel.IsSupported` (existing, `PlutoFramework/Model/SolanaMwaModel.cs`).
- Produces: `ImportMethodPopupViewModel` with `IsVisible`, `SeedPhraseChosen`/`MwaChosen` (`Func<Task>`), commands `ChooseSeedPhraseCommand` and `ChooseMwaCommand`; `ImportMethodPopupView`; `SolanaNoAccountView`.

The popup is a framework component because both onboarding (Task 12) and the balances page empty state open it. It only reports the choice — the caller decides what the choice leads to, because onboarding continues into a password step while the balances page does not.

- [ ] **Step 1: Write the view model**

Create `PlutoFramework/Components/Solana/ImportMethodPopupViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;

namespace PlutoFramework.Components.Solana;

/// <summary>
/// Asks how an existing Solana account should be brought in: the user's own seed phrase, or
/// a wallet app over Mobile Wallet Adapter.
/// </summary>
/// <remarks>
/// Reports the choice and nothing more. Onboarding continues into a password step afterwards
/// and the balances page does not, so the destination belongs to the caller.
/// </remarks>
public partial class ImportMethodPopupViewModel : ObservableObject, IPopup, ISetToDefault
{
    [ObservableProperty]
    private bool isVisible = false;

    public Func<Task> SeedPhraseChosen { get; set; } = () => Task.CompletedTask;

    public Func<Task> MwaChosen { get; set; } = () => Task.CompletedTask;

    /// <summary>
    /// Mobile Wallet Adapter is specified for Android only, so on iOS the option explains
    /// itself instead of failing when tapped.
    /// </summary>
    public bool MwaIsSupported => SolanaMwaModel.IsSupported;

    public bool MwaIsUnsupported => !MwaIsSupported;

    public void SetToDefault()
    {
        IsVisible = false;
        SeedPhraseChosen = () => Task.CompletedTask;
        MwaChosen = () => Task.CompletedTask;
    }

    [RelayCommand]
    public async Task ChooseSeedPhraseAsync()
    {
        IsVisible = false;

        await SeedPhraseChosen.Invoke();
    }

    [RelayCommand]
    public async Task ChooseMwaAsync()
    {
        if (!MwaIsSupported)
        {
            return;
        }

        IsVisible = false;

        await MwaChosen.Invoke();
    }
}
```

- [ ] **Step 2: Write the popup view**

Create `PlutoFramework/Components/Solana/ImportMethodPopupView.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:card="clr-namespace:PlutoFramework.Components.Card"
             xmlns:buttons="clr-namespace:PlutoFramework.Components.Buttons"
             x:Class="PlutoFramework.Components.Solana.ImportMethodPopupView"
             IsVisible="{Binding IsVisible}"
             AbsoluteLayout.LayoutBounds="0.5, 0.5, 1, 1"
             AbsoluteLayout.LayoutFlags="All">

    <card:BottomPopupCard Title="Import your Solana account"
                          IsShown="{Binding IsVisible}">
        <card:BottomPopupCard.View>
            <VerticalStackLayout Spacing="15"
                                 Padding="10, 10, 10, 20">

                <Label Text="Connect the wallet app that already holds your account, or enter its seed phrase."
                       FontSize="13"
                       TextColor="#A6A6A6" />

                <buttons:ElevatedButton Text="Connect wallet app"
                                        ButtonState="Enabled"
                                        IsVisible="{Binding MwaIsSupported}"
                                        Command="{Binding ChooseMwaCommand}" />

                <Label Text="Connecting a wallet app is only available on Android."
                       FontSize="12"
                       TextColor="#A6A6A6"
                       IsVisible="{Binding MwaIsUnsupported}"
                       HorizontalTextAlignment="Center" />

                <buttons:ElevatedButton Text="Enter seed phrase"
                                        ButtonState="Enabled"
                                        Command="{Binding ChooseSeedPhraseCommand}" />
            </VerticalStackLayout>
        </card:BottomPopupCard.View>
    </card:BottomPopupCard>
</ContentView>
```

Create `PlutoFramework/Components/Solana/ImportMethodPopupView.xaml.cs`:

```csharp
namespace PlutoFramework.Components.Solana;

public partial class ImportMethodPopupView : ContentView
{
    public ImportMethodPopupView()
    {
        InitializeComponent();

        BindingContext = DependencyService.Get<ImportMethodPopupViewModel>();
    }
}
```

- [ ] **Step 3: Write the empty state**

Create `PlutoFramework/Components/Solana/SolanaNoAccountView.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:card="clr-namespace:PlutoFramework.Components.Card"
             xmlns:buttons="clr-namespace:PlutoFramework.Components.Buttons"
             x:Class="PlutoFramework.Components.Solana.SolanaNoAccountView"
             x:Name="noAccountView">
    <card:Card>
        <card:Card.View>
            <VerticalStackLayout Padding="15, 20, 15, 20"
                                 Spacing="15">
                <Label Text="No Solana account yet"
                       FontAttributes="Bold"
                       FontSize="16"
                       HorizontalTextAlignment="Center" />

                <Label Text="Create a new Solana account, or import one you already have."
                       FontSize="13"
                       TextColor="#A6A6A6"
                       HorizontalTextAlignment="Center" />

                <buttons:ElevatedButton Text="Create account"
                                        ButtonState="Enabled"
                                        Command="{Binding Source={x:Reference noAccountView}, Path=CreateCommand}" />

                <buttons:ElevatedButton Text="Import account"
                                        ButtonState="Enabled"
                                        Command="{Binding Source={x:Reference noAccountView}, Path=ImportCommand}" />
            </VerticalStackLayout>
        </card:Card.View>
    </card:Card>
</ContentView>
```

Create `PlutoFramework/Components/Solana/SolanaNoAccountView.xaml.cs`:

```csharp
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;

namespace PlutoFramework.Components.Solana;

/// <summary>
/// Shown where a Solana account is required but none exists. Routes into the same create and
/// import flows onboarding uses, so an existing Substrate-only user reaches a Solana account
/// without reinstalling.
/// </summary>
public partial class SolanaNoAccountView : ContentView
{
    public SolanaNoAccountView()
    {
        InitializeComponent();

        CreateCommand = new AsyncRelayCommand(CreateAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
    }

    public IAsyncRelayCommand CreateCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    private static Task CreateAsync() =>
        NavigationModel.PushAsync(new CreateSolanaMnemonicsPage(new CreateSolanaMnemonicsViewModel
        {
            Navigation = () => NavigationModel.PopAsync(),
        }));

    private static Task ImportAsync()
    {
        var popup = DependencyService.Get<ImportMethodPopupViewModel>();

        popup.SeedPhraseChosen = () => NavigationModel.PushAsync(new EnterSolanaMnemonicsPage(
            new EnterSolanaMnemonicsViewModel
            {
                Navigation = async (mnemonics) =>
                {
                    await KeysModel.SaveSolanaMnemonicKeyAsync(mnemonics);

                    await NavigationModel.PopAsync();
                },
            }));

        popup.MwaChosen = () => NavigationModel.PushAsync(new ConnectMwaPage(new ConnectMwaPageViewModel
        {
            Navigation = () => NavigationModel.PopAsync(),
        }));

        popup.IsVisible = true;

        return Task.CompletedTask;
    }
}
```

All three pages already exist and take their view model as the sole constructor argument —
`CreateSolanaMnemonicsPage(CreateSolanaMnemonicsViewModel)`,
`EnterSolanaMnemonicsPage(EnterSolanaMnemonicsViewModel)`,
`ConnectMwaPage(ConnectMwaPageViewModel)` — each with a `Navigation` callback on the view
model. `CreateSolanaMnemonicsViewModel` saves the key itself before invoking `Navigation`;
`EnterSolanaMnemonicsViewModel.Navigation` takes the phrase and leaves saving to the caller.

- [ ] **Step 4: Register the popup view model**

In `PlutoFramework/MauiAppBuilderExtensions.cs`, beside the other `DependencyService.Register<…>()` calls (near `ImportWarningPopupViewModel` at line 220):

```csharp
            DependencyService.Register<ImportMethodPopupViewModel>();
```

- [ ] **Step 5: Verify the framework builds**

Run: `dotnet build PlutoFramework/PlutoFramework.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors. This is the first build that compiles the Task 9 XAML too.

- [ ] **Step 6: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Components/Solana/ PlutoFramework/MauiAppBuilderExtensions.cs
git commit -m "feat: add Solana import-method popup and empty state"
```

---

### Task 11: Main page balance cell

**Files:**
- Create: `PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml`
- Create: `PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml.cs`
- Modify: `XcavateMobileApp/Pages/InvestorMainPage.xaml:81-82`
- Modify: `XcavateMobileApp/Pages/InvestorMainPage.xaml.cs:10`

**Interfaces:**
- Consumes: `SolanaBalancesModel`, `SolanaBalanceAssembler.TotalUsd`, `KeysModel.GetSolanaAddress`, `SolanaNetworkModel.ClusterChanged`, `SolanaBalancesPage`, `ToUsdCurrencyString`.
- Produces: `SolanaBalanceCellView` implementing `ILocalLoadableAsyncView`.

- [ ] **Step 1: Write the cell**

Create `PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:xcavate="clr-namespace:PlutoFramework.Components.Xcavate"
             x:Class="PlutoFramework.Components.Solana.SolanaBalanceCellView"
             HeightRequest="80">
    <xcavate:XcavateCell Title="Balance"
                         x:Name="cell"
                         Value="-"
                         RollingTicker="True" />
</ContentView>
```

Create `PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml.cs`:

```csharp
using CommunityToolkit.Mvvm.Input;
using PlutoFramework.Model;
using PlutoFramework.Model.Currency;
using PlutoFrameworkCore.Solana;

namespace PlutoFramework.Components.Solana;

/// <summary>
/// The main page's Balance cell, showing the Solana total and opening the balances page.
/// </summary>
/// <remarks>
/// Implements <see cref="ILocalLoadableAsyncView"/> only. Unlike the Substrate cell it
/// replaces, nothing here needs a connected Substrate client.
/// </remarks>
public partial class SolanaBalanceCellView : ContentView, ILocalLoadableAsyncView
{
    public SolanaBalanceCellView()
    {
        InitializeComponent();

        cell.Command = new AsyncRelayCommand(OpenBalancesPageAsync);

        SolanaNetworkModel.ClusterChanged += OnClusterChanged;
    }

    private void OnClusterChanged(object? sender, SolanaCluster cluster) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync(CancellationToken.None));

    private static Task OpenBalancesPageAsync() => NavigationModel.PushAsync(new SolanaBalancesPage());

    public async Task LoadAsync(CancellationToken token)
    {
        var address = KeysModel.GetSolanaAddress();

        if (string.IsNullOrEmpty(address))
        {
            // A dash, not a formatted zero: "you have no account" and "you have no money"
            // are different statements.
            cell.Value = "-";
            return;
        }

        try
        {
            var rows = await SolanaBalancesModel.GetBalancesAsync(
                address, SolanaNetworkModel.SelectedCluster, token);

            cell.Value = SolanaBalanceAssembler.TotalUsd(rows).ToUsdCurrencyString();
        }
        catch (OperationCanceledException)
        {
            // The page went away mid-query.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Solana balance cell failed to load: {ex.Message}");

            cell.Value = "-";
        }
    }
}
```

- [ ] **Step 2: Swap the cell on the main page**

In `XcavateMobileApp/Pages/InvestorMainPage.xaml`, add to the root element's namespace declarations:

```xml
             xmlns:solanacomponents="clr-namespace:PlutoFramework.Components.Solana;assembly=PlutoFramework"
```

Replace lines 81-82:

```xml
                        <xcavatecells:BalanceCellView Grid.Column="1"
                                                      x:Name="balanceCellView"/>
```

with:

```xml
                        <solanacomponents:SolanaBalanceCellView Grid.Column="1"
                                                                x:Name="balanceCellView"/>
```

The `x:Name` is kept, so `InvestorMainPage.xaml.cs:10` (`public IList<IView> Views => [balanceCellView];`) needs no change. Verify that line still compiles — `SolanaBalanceCellView` is a `ContentView` and therefore an `IView`.

- [ ] **Step 3: Make the cell load on page load and on pull-to-refresh**

`MainPageLayoutUpdater.ViewLocalLoadAsync` is what calls `LoadAsync` on the page's `Views`, and today it is only reached through `MainPageLayoutUpdater.ReloadAsync` on the Substrate client warm-up path. Left alone, the balance cell would refresh as a side effect of Substrate connection work and would not refresh at all when the user pulls to refresh.

In `XcavateMobileApp/Pages/InvestorMainPage.xaml.cs`, in `OnLoaded`, after `await viewModel.RefreshAsync(cancellationToken);`:

```csharp
            // Views on this page are loaded by MainPageLayoutUpdater, which is otherwise only
            // reached through the Substrate warm-up. The Solana balance must not depend on that.
            await MainPageLayoutUpdater.ViewLocalLoadAsync(cancellationToken);
```

In `XcavateMobileApp/Pages/InvestorMainPageViewModel.cs`, in `RefreshAsync(CancellationToken externalToken)`, after `await LoadOwnedPropertiesForSelectedEndpointAsync(token).ConfigureAwait(false);`:

```csharp
            await MainPageLayoutUpdater.ViewLocalLoadAsync(token).ConfigureAwait(false);
```

Add `using PlutoFramework;` to both files if it is not already present — `MainPageLayoutUpdater` sits in the `PlutoFramework` root namespace.

- [ ] **Step 4: Verify the app builds**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android --nologo` from the repository root.
Expected: build succeeded, 0 errors.

- [ ] **Step 5: Commit**

Framework and app are separate repositories, so this is two commits:

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml PlutoFramework/Components/Solana/SolanaBalanceCellView.xaml.cs
git commit -m "feat: add Solana balance cell for the main page"
cd ..
git add XcavateMobileApp/Pages/InvestorMainPage.xaml XcavateMobileApp/Pages/InvestorMainPage.xaml.cs XcavateMobileApp/Pages/InvestorMainPageViewModel.cs
git commit -m "feat: show the Solana balance on the investor main page"
```

---

### Task 12: Solana-only onboarding

**Files:**
- Modify: `XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs`

**Interfaces:**
- Consumes: `ImportMethodPopupViewModel` (Task 10), `KeysModel.SaveSolanaMnemonicKeyAsync`, `SolanaMnemonicsModel.GenerateMnemonics`, `EnterSolanaMnemonicsPage`, `ConnectMwaPage`, `OnboardingModel.SetOnboardingStage`.
- Produces: `ImportAccountCoordinator.StartAsync(ImportAccountFlowMode)` saving a Solana key and finishing onboarding.

**Ordering is load-bearing.** `SaveSolanaMnemonicKeyAsync` and `SolanaMwaModel.ConnectAndSaveAsync` both read the stored password (`SecureStorage.Default.GetAsync(PreferencesModel.PASSWORD)`) and pass it to `SaveKeyAsync` as non-null. The password step must therefore complete before any key is saved — the create branch generates after the password page, and the MWA branch connects after it.

`OnboardingStage.Finished` is set only in `ModifyUserProfilePageViewModel.cs:147` today. Since this flow no longer reaches profile registration, the coordinator must set it, or `App.xaml.cs:116` sends the user back into onboarding on every launch.

- [ ] **Step 1: Replace the flow**

In `XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs`, replace `StartAsync`, `OnMnemonicsEnteredAsync`, `OnJsonImportedAsync`, `OnPasswordSetAsync` and `ContinueSetupPasswordAsync` with:

```csharp
    private Task ContinueSetupPasswordAsync()
    {
        return _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = CreateSolanaAccountAsync,
        });
    }

    public async Task StartAsync(ImportAccountFlowMode flowMode)
    {
        _flowMode = flowMode;

        OnboardingModel.SetOnboardingStage(OnboardingStage.SetupPassword);

        var nextNavigation = flowMode switch
        {
            ImportAccountFlowMode.Create => _navigationService.NavigateToAsync(new SetupPasswordPage
            {
                Navigation = CreateSolanaAccountAsync,
            }),
            ImportAccountFlowMode.Import => ShowImportMethodPopupAsync(),
            _ => throw new Exception("Unsupported flow mode"),
        };

        await nextNavigation;
    }

    /// <summary>
    /// Asks how the account arrives. Both answers end at the password page, because saving
    /// any key — a phrase or an MWA auth token — needs the stored password to encrypt it.
    /// </summary>
    private Task ShowImportMethodPopupAsync()
    {
        var popup = DependencyService.Get<ImportMethodPopupViewModel>();

        popup.SeedPhraseChosen = () => _navigationService.NavigateToAsync(new EnterSolanaMnemonicsPage(
            new EnterSolanaMnemonicsViewModel
            {
                Navigation = OnSolanaMnemonicsEnteredAsync,
            }));

        popup.MwaChosen = () => _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = () => _navigationService.NavigateToAsync(new ConnectMwaPage(
                new ConnectMwaPageViewModel
                {
                    Navigation = FinishOnboardingAsync,
                })),
        });

        popup.IsVisible = true;

        return Task.CompletedTask;
    }

    private Task OnSolanaMnemonicsEnteredAsync(string mnemonics)
    {
        return _navigationService.NavigateToAsync(new SetupPasswordPage
        {
            Navigation = async () =>
            {
                await KeysModel.SaveSolanaMnemonicKeyAsync(mnemonics);

                await FinishOnboardingAsync();
            },
        });
    }

    private static async Task CreateSolanaAccountAsync()
    {
        // Generated after the password step, never before: SaveSolanaMnemonicKeyAsync reads
        // the stored password to encrypt the phrase.
        var mnemonics = SolanaMnemonicsModel.GenerateMnemonics();

        await KeysModel.SaveSolanaMnemonicKeyAsync(mnemonics);

        await FinishOnboardingAsync();
    }

    /// <summary>
    /// Ends onboarding. This flow no longer reaches profile registration, which is where
    /// <see cref="OnboardingStage.Finished"/> used to be set, so setting it here is what
    /// stops App.xaml.cs routing the user back into onboarding on every launch.
    /// </summary>
    private static Task FinishOnboardingAsync()
    {
        OnboardingModel.SetOnboardingStage(OnboardingStage.Finished);

        return NavigateToAppShellAsync();
    }
```

Add these usings at the top of the file:

```csharp
using PlutoFramework.Components.Solana;
```

Remove now-unused usings the compiler flags (`PlutoFramework.Components.Mnemonics` and `PlutoFrameworkCore.Keys` may become unused; `PlutoFramework.Model.SQLite` will if `OnJsonImportedAsync` is gone).

Leave `ContinueAsync`'s resume table untouched. Its Substrate stages exist for users interrupted mid-onboarding before this release; new onboardings never enter them.

- [ ] **Step 2: Put the popup in the onboarding page's visual tree**

Setting `IsVisible` on the shared view model does nothing unless a bound `ImportMethodPopupView` is on the page currently displayed. Onboarding starts from `WelcomePage`, which already hosts `OnboardingInProgressPopup`.

In `XcavateMobileApp/Pages/WelcomePage.xaml`, add to the root element's namespaces:

```xml
             xmlns:solana="clr-namespace:PlutoFramework.Components.Solana;assembly=PlutoFramework"
```

and add beside the existing popup, inside the outer `AbsoluteLayout`:

```xml
        <solana:ImportMethodPopupView />
```

- [ ] **Step 3: Verify the app builds**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add XcavateMobileApp/Components/Account/ImportAccountCoordinator.cs XcavateMobileApp/Pages/WelcomePage.xaml
git commit -m "feat: onboard with a Solana account instead of a Polkadot one"
```

---

### Task 13: App wiring — routing, whitelist, account generation

**Files:**
- Modify: `XcavateMobileApp/App.xaml.cs:78-98` (whitelist), `:116-120` (routing), `:161-174` (account generation)

**Interfaces:**
- Consumes: `PlutoConfigurationModel.WhitelistedSolanaTokens` (Task 1), `KeysModel.HasSolanaKey` (Task 7), `KeysModel.GenerateNewSolanaAccountAsync` (existing).

- [ ] **Step 1: Configure the Solana whitelist**

In `XcavateMobileApp/App.xaml.cs`, after the existing `PlutoConfigurationModel.WhitelistedTokens = [ … ];` block:

```csharp
            // Mint addresses are cluster-specific; the same token has a different one on each.
            // Both verified live on 2026-07-25.
            PlutoConfigurationModel.WhitelistedSolanaTokens = [
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Mainnet,
                    Mint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
                new SolanaTokenWhitelistEntry
                {
                    Cluster = SolanaCluster.Devnet,
                    Mint = "4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU",
                    Symbol = "USDC",
                    Decimals = 6,
                    PinnedUsdPrice = 1.00,
                },
            ];
```

Add `using PlutoFrameworkCore.Solana;` to the file's usings.

- [ ] **Step 2: Route on either key**

Replace lines 116-120:

```csharp
            MainPage = OnboardingModel.IsOnboardingCompleted() switch
            {
                true when KeysModel.HasSubstrateKey() => new XcavateAppShell(),
                _ => new OnboardingShell(),
            };
```

with:

```csharp
            // Either key counts. New accounts are Solana-only; users onboarded before that
            // change still hold a Substrate key and must not be pushed back into onboarding.
            MainPage = OnboardingModel.IsOnboardingCompleted() switch
            {
                true when KeysModel.HasSolanaKey() || KeysModel.HasSubstrateKey() => new XcavateAppShell(),
                _ => new OnboardingShell(),
            };
```

- [ ] **Step 3: Generate a Solana account**

Replace the body of `GenerateNewAccountAsync` (lines 161-174):

```csharp
        public static async Task GenerateNewAccountAsync()
        {
            await KeysModel.ClearAsync();

            await KeysModel.GenerateNewSolanaAccountAsync();
        }
```

- [ ] **Step 4: Verify the app builds**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add XcavateMobileApp/App.xaml.cs
git commit -m "feat: wire Solana whitelist, routing and account generation"
```

---

### Task 14: Gate the Substrate-only features

**Files:**
- Modify: `realXmarketPlutoFramework/PlutoFramework/Model/RequirementsModel.cs:105-119` and `:24-77`, `:79-103`
- Modify: `XcavateMobileApp/App.xaml.cs:71`
- Modify: `XcavateMobileApp/Pages/SettingsViewModel.cs:12`

**Interfaces:**
- Consumes: `KeysModel.HasSolanaKey` (Task 7).

`GetSubstrateKey()` returns the string `"Substrate key does not exist"` when no key is present, and `GetSubstrateKey(0)` throws on it inside `Utils.GetPublicKeyFrom`. Every path that would reach it needs an explicit guard, or a Solana-only user meets a crash instead of a popup.

- [ ] **Step 1: Accept either key for "account exists"**

In `RequirementsModel.CheckAccountExists()`, replace the condition:

```csharp
            if (!KeysModel.HasSubstrateKey() || !onboardingCompleted)
```

with:

```csharp
            if ((!KeysModel.HasSolanaKey() && !KeysModel.HasSubstrateKey()) || !onboardingCompleted)
```

- [ ] **Step 2: Guard the Substrate-only checks**

At the top of `RequirementsModel.CheckRequirementsAsync(CancellationToken token)`, immediately after the `fullPageLoadingViewModel` line:

```csharp
            // KYC, DID and the profile are all keyed to a Substrate address. A Solana-only
            // account has none, so this reports "account required" rather than sending the
            // GetSubstrateKey() placeholder string to Sumsub.
            if (!KeysModel.HasSubstrateKey())
            {
                DependencyService.Get<NoAccountPopupViewModel>().IsVisible = true;

                return false;
            }
```

At the top of `RequirementsModel.CheckXcavateRoleAsync(XcavateRole role, CancellationToken token)`, before `var address = KeysModel.GetSubstrateKey();`:

```csharp
            // Roles live in the XcavatePaseo whitelist pallet, keyed by Substrate address.
            if (!KeysModel.HasSubstrateKey())
            {
                DependencyService.Get<NotWhitelistedPopupViewModel>().IsVisible = true;

                return false;
            }
```

- [ ] **Step 3: Guard the KYC navigation**

In `XcavateMobileApp/App.xaml.cs`, replace line 71:

```csharp
            NavigationModel.NavigateToKYCUserPage = () => Shell.Current.Navigation.PushAsync(new SumsubUserPage(KeysModel.GetSubstrateKey()));
```

with:

```csharp
            // Sumsub applicants are keyed by Substrate address. Without one there is nothing
            // to look up, and GetSubstrateKey() would hand over its placeholder string.
            NavigationModel.NavigateToKYCUserPage = () =>
            {
                if (!KeysModel.HasSubstrateKey())
                {
                    DependencyService.Get<NoAccountPopupViewModel>().IsVisible = true;

                    return Task.CompletedTask;
                }

                return Shell.Current.Navigation.PushAsync(new SumsubUserPage(KeysModel.GetSubstrateKey()));
            };
```

Add `using PlutoFramework.Components.Account;` if `NoAccountPopupViewModel` does not already resolve.

- [ ] **Step 4: Settings reflects either key**

In `XcavateMobileApp/Pages/SettingsViewModel.cs`, replace line 12:

```csharp
            hasAccount = KeysModel.HasSubstrateKey();
```

with:

```csharp
            hasAccount = KeysModel.HasSolanaKey() || KeysModel.HasSubstrateKey();
```

- [ ] **Step 5: Verify everything builds and all tests pass**

Run: `dotnet build XcavateMobileApp/XcavateMobileApp.csproj -f net10.0-android --nologo`
Expected: build succeeded, 0 errors.

Run: `cd realXmarketPlutoFramework && dotnet test PlutoFrameworkTests/PlutoFrameworkTests.csproj --nologo`
Expected: the 28 tests added by Tasks 1-4 all pass, and the failure count is unchanged from your baseline (29 on a clean checkout).

- [ ] **Step 6: Commit**

```bash
cd realXmarketPlutoFramework
git add PlutoFramework/Model/RequirementsModel.cs
git commit -m "fix: gate Substrate-only checks behind a Substrate key"
cd ..
git add XcavateMobileApp/App.xaml.cs XcavateMobileApp/Pages/SettingsViewModel.cs
git commit -m "fix: gate KYC navigation and settings on the available key type"
```

---

## Manual verification

None of the following is covered by the unit tests, and each must be reported as tested or untested rather than assumed:

1. **Balances against a live cluster.** Fund a devnet account (`solana airdrop 1 <address> --url devnet`), open the balances page on Devnet, confirm SOL shows the airdropped amount and USDC shows 0.00.
2. **Network switch.** Change the network in Settings and return to the main page; the cell and the page must re-query rather than keep the other cluster's figures.
3. **Price failure.** Put the device in airplane mode after a successful load, pull to refresh, and confirm the page shows an RPC error rather than zeros.
4. **Onboarding, create.** Fresh install → Create Account → password → app shell, with a Solana address on the balances page.
5. **Onboarding, seed import.** Fresh install → Import Account → Enter seed phrase → confirm the derived address preview matches the source wallet → password → app shell.
6. **Onboarding, MWA.** Android device with Phantom, Solflare or Backpack installed → Import Account → Connect wallet app → password → approve in the wallet → app shell.
7. **iOS import.** The MWA option is hidden and its explanation is shown instead.
8. **Existing user upgrade.** Install the previous build, onboard fully, upgrade: the app must open to the app shell with properties intact, and the Balance cell must show a dash until a Solana account is created through the empty state.
