# Solana Token Detail Page — Design

**Date:** 2026-07-26
**Status:** Approved, ready for implementation planning
**Builds on:** [Solana balances and Solana-first onboarding](2026-07-25-solana-balances-design.md)

## Goal

1. A `SolanaTokenDetailPage` reached by tapping any row on `SolanaBalancesPage`.
2. For a token whose price actually moves — SOL today — a price chart with 1d / 3w / 6m
   intervals, a live price, and a 24-hour change.
3. For a stablecoin — USDC today, tGBP when it arrives — **no chart at all**. A flat line
   teaches nothing, and drawing one implies a volatility the token does not have.

Every token, chartable or not, shows its holdings, its on-chain facts, and a Receive action.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Chart eligibility | New `ShowPriceChart` flag on `SolanaTokenWhitelistEntry`, default `false` | Explicit. Reusing `PinnedUsdPrice is null` would silently grant tGBP a chart if it ships priced from a live GBP/USD feed instead of pinned — a real possibility for a GBP-pegged token. |
| Flag delivery | Denormalized onto `SolanaTokenBalance` by the assembler | The row already denormalizes `Symbol` and `Decimals` from the whitelist. The detail page then needs no whitelist lookup and no cluster argument. |
| SOL | Always chartable | Not a whitelist entry, so no flag can turn it off. It is the only token on the page whose price moves. |
| Price history source | Jupiter `datapi.jup.ag/v2/charts` | Chosen by the product owner. No API key, and the app already depends on Jupiter for spot prices, so this adds a path to an existing vendor rather than a new one. |
| Undocumented endpoint | Accepted, with best-effort failure handling | Not in Jupiter's published docs and may change without notice. A chart failure degrades to a message; nothing else on the page depends on it. |
| Chart rendering | Microcharts `LineChart` over closes | Matches the existing Substrate `AssetDetailPage` exactly. Microcharts has no candlestick type, so OHLC would mean hand-drawn SkiaSharp. |
| Chart steps | 24, matching `AssetDetailPage.CHART_STEPS` | Jupiter's `candles=24` returns exactly the most recent 24 for every interval — verified. |
| Stablecoin page | Holdings + facts + Receive, no peg wording, no price row | Chosen by the product owner. |
| Stablecoin network cost | Zero calls | Everything shown already arrived on the row. |
| Receive | Sets `AddressQrCodeViewModel` directly | `ReceiveAndTransferModel.Receive()` early-returns when there is no Substrate key — precisely the Solana-only user this page serves. |
| Transfer | Omitted | The framework has no Solana transaction-building or sending. A button that cannot work is worse than an absent one. |
| Cluster change while open | Pop back to balances | Mints are cluster-specific. USDC's devnet mint does not exist on mainnet, so re-rendering would make the page's own "Network" row false. |
| Page location | `PlutoFramework/Components/Solana/` | Alongside every other Solana component. |

## Research findings

### Jupiter has no documented price-history endpoint

`developers.jup.ag/docs/llms.txt` lists the full documented surface. Under **Price** there is
exactly one route:

```
GET /price/v3?ids={mints}
```

No charts, candles, OHLCV, or history endpoint appears anywhere in the documentation.

### The undocumented charts endpoint works and needs no key

Verified live on 2026-07-26:

```
GET https://datapi.jup.ag/v2/charts/{mint}
      ?interval=1_HOUR&from={unixMs}&to={unixMs}&candles=24&type=price
```

```json
{"candles":[
  {"time":1784977200,"open":73.91344226642872,"high":73.97769893283103,
   "low":73.77231204963448,"close":73.85630878051603,"volume":4344541.16387761},
  …
]}
```

Confirmed by probing:

- `interval` accepts `1_MINUTE`, `15_MINUTE`, `1_HOUR`, `4_HOUR`, `1_DAY`, `1_WEEK`,
  `1_MONTH`. Only `1_HOUR`, `1_DAY` and `1_WEEK` are used here.
- `from` / `to` are **Unix milliseconds**. Seconds are rejected — the route returns
  `{"candles":[]}` rather than an error, which is why the parser must treat an empty array
  as "no data" and never as "price is zero".
- `candles=24` caps the response at 24 and returns the **most recent** 24 within the range.
  Checked against a 400-day window at `1_DAY`: 24 entries, last one the current day's open.
- `time` is Unix **seconds**, not milliseconds — the opposite unit from the request
  parameters. Mixing them up shifts every point to 1970 or to the year 58000.
- No `X-API-KEY` header required.

### `price/v3` already carries the 24-hour change

Verified live on 2026-07-26 against the endpoint the balances page already calls:

```json
{"So11111111111111111111111111111111111111112":{
  "usdPrice":75.06634976056819,"blockId":435318986,"decimals":9,
  "priceChange24h":1.6657517647060245,"liquidity":655459748.2044258,
  "createdAt":"2024-06-05T08:55:25.527Z"}}
```

`priceChange24h` is a percentage: `1.6657…` means +1.67%. `SolanaPriceParser.Parse` reads
only `usdPrice` and discards the rest, so surfacing the change needs a second parser entry
point, not a change to the existing one.

### `ReceiveAndTransferModel` cannot serve this page

`PlutoFramework/Model/ReceiveAndTransferModel.cs:14` opens with:

```csharp
if (!KeysModel.HasSubstrateKey())
{
    var noAccountPopupViewModel = DependencyService.Get<NoAccountPopupViewModel>();
    noAccountPopupViewModel.IsVisible = true;
    return;
}
```

New accounts are Solana-only. Routing Receive through this would show a "no account" popup
to a user who is looking at their own balance. `SolanaBalancesPage` already sidesteps it by
binding `AddressView` to `KeysModel.GetSolanaAddress()` with a `solana:` URI; this page does
the same.

### Icons already exist

`PlutoFrameworkCore/Constants/Assets.cs` maps `sol → solana.png`, `usdc → usdc.png` and
`tgbp → tgbp.png`, with `unknown.png` as the fallback. No new assets are needed.

### `AssetDetailViewModel` is not reusable

It binds `AssetInfo` (Substrate `AssetPallet` and `EndpointEnum`), loads prices through
`SubstrateClientModel.GetOrAddSubstrateClientAsync(EndpointEnum.Hydration, …)` and the
HydraDX `Sdk`, and wires its buttons to `ReceiveAndTransferModel`. Extracting a shared base
would drag Substrate concerns into the Solana path and modify working code for no user-visible
gain. Its **chart XAML and chart-building logic are copied**, deliberately: the two pages
should look identical without being coupled.

## Architecture

```
PlutoFrameworkCore/Solana/          (no MAUI, unit-tested)
  SolanaTokenWhitelistEntry.cs      + ShowPriceChart
  SolanaTokenBalance.cs             + ShowPriceChart
  SolanaBalanceAssembler.cs         propagates the flag
  SolanaPricePoint.cs               new — one plotted point
  SolanaChartInterval.cs            new — Interval → Jupiter string + lookback
  SolanaPriceHistoryParser.cs       new — pure JSON → points
  SolanaPriceHistoryModel.cs        new — fetch + parse
  SolanaPriceParser.cs              + ParseQuotes
  SolanaPriceModel.cs               + GetSpotQuoteAsync
  SolanaSpotQuote.cs                new — price + 24h change

PlutoFramework/Components/Solana/   (MAUI)
  SolanaAssetView.xaml(.cs)         + tap to navigate
  SolanaTokenDetailPage.xaml(.cs)   new
  SolanaTokenDetailPageViewModel.cs new

XcavateMobileApp/App.xaml.cs        unchanged
```

`App.xaml.cs` needs no edit: both USDC entries keep `ShowPriceChart` at its default `false`.

## Part A — The chart flag

```csharp
// SolanaTokenWhitelistEntry
/// <summary>
/// Stablecoins opt out: a flat line teaches nothing. Off by default — a token earns a
/// chart deliberately, so a new entry cannot acquire one by omission.
/// </summary>
public bool ShowPriceChart { get; init; }
```

```csharp
// SolanaTokenBalance
public required bool ShowPriceChart { get; init; }
```

`required`, not defaulted. `SolanaTokenBalance` is constructed in exactly two places, both in
`SolanaBalanceAssembler` (the SOL row and the whitelist loop) — verified by grep — so the
compiler catches both, and a silently-defaulted flag is how SOL would lose its chart.

`SolanaBalanceAssembler.Assemble` sets `ShowPriceChart = true` on the SOL row it hardcodes,
and `entry.ShowPriceChart` on each whitelist row.

## Part B — Price history

### `SolanaPricePoint`

```csharp
public sealed record SolanaPricePoint
{
    public required DateTimeOffset Time { get; init; }
    public required double UsdPrice { get; init; }
}
```

Jupiter returns full OHLCV. A line chart plots closes, so open/high/low/volume are dropped
at the parser rather than carried unused through three layers.

### `SolanaChartInterval`

Pure, no I/O. Maps the existing `Interval` enum — declared in `PlutoFrameworkCore/BlockModel.cs`
under namespace `PlutoFramework.Model`, not `PlutoFrameworkCore.Solana`, so the new file needs
a `using`:

| `Interval` | Jupiter `interval` | Lookback | Button |
|---|---|---|---|
| `Hourly` | `1_HOUR` | 24 hours | 1d |
| `Daily` | `1_DAY` | 24 days | 3w |
| `Weekly` | `1_WEEK` | 24 weeks | 6m |

The lookback is `steps × the interval's own length`, with no padding. Jupiter caps at
`candles` and returns the most recent, so an exact window is sufficient and a padded one
would silently discard the oldest points it fetched.

Exposes the Unix-millisecond `from` / `to` pair, so the seconds-vs-milliseconds trap is
resolved in one tested place instead of at the call site.

### `SolanaPriceHistoryParser`

Pure `string → IReadOnlyList<SolanaPricePoint>`, defensive in the same style as
`SolanaPriceParser`:

- Not an object, or no `candles` array → empty list.
- An element missing `time` or `close`, or holding a non-number in either → skipped, the
  rest kept.
- Unparseable JSON → empty list, no throw.
- `time` is read as Unix **seconds** and converted with
  `DateTimeOffset.FromUnixTimeSeconds`.

Empty is always "no data", never "the price is zero".

### `SolanaPriceHistoryModel`

```csharp
public static Task<IReadOnlyList<SolanaPricePoint>> GetPriceHistoryAsync(
    string mint, Interval interval, int steps, CancellationToken token);
```

Builds the URL from `SolanaChartInterval`, GETs, parses. Uses a `static readonly HttpClient`
with a 10-second timeout, matching `SolanaPriceModel` — a client per call leaks sockets.

Returns an **empty list** on any non-cancellation failure and logs, mirroring
`SolanaPriceModel`'s existing behaviour. `OperationCanceledException` propagates, so the
view model's staleness guard still works.

### `SolanaSpotQuote` and `GetSpotQuoteAsync`

```csharp
public sealed record SolanaSpotQuote
{
    public required double UsdPrice { get; init; }

    /// <summary>Percent, e.g. 1.67 for +1.67%. Null when Jupiter omitted it.</summary>
    public double? Change24h { get; init; }
}
```

`SolanaPriceParser.ParseQuotes(json)` returns `IReadOnlyDictionary<string, SolanaSpotQuote>`.
The existing `Parse` is left exactly as it is — the balances page depends on it, and it has
no need for the change field.

`SolanaPriceModel.GetSpotQuoteAsync(mint, token)` calls `price/v3` for the single mint and
returns `null` on failure or when the mint is absent from the response.

The unit price cannot be derived from the row instead: the row carries `UsdValue`
(amount × price), which is `0` at a zero balance and yields no price at all.

## Part C — The page

`SolanaTokenDetailPage : PageTemplate`, `Title="{Binding Symbol}"`, constructed with the
`SolanaTokenBalance` the user tapped.

### Layout

```
┌─────────────────────────┐   ┌─────────────────────────┐
│            $76.02  ░░░  │   │        ( USDC )         │
│        ╱╲    ╱╲╱        │   │         USDC            │
│   ╱╲╱    ╲╱             │   │      40.00 USDC         │
│ ╱  $73.53               │   │        $40.00           │
│ 08:00 14:00 20:00 02:00 │   ├─────────────────────────┤
│    [1d]  3w   6m        │   │ Network      Devnet     │
├─────────────────────────┤   │ Mint      4zMM…ncDU  ⧉  │
│ ( SOL )   SOL           │   │ Decimals          6     │
│ 0.5241 SOL      $39.34  │   ├─────────────────────────┤
│ Price   $75.07  +1.67%  │   │      [ Receive ]        │
├─────────────────────────┤   └─────────────────────────┘
│ Network      Devnet     │
│ Mint      So11… 112  ⧉  │      ShowPriceChart = false
│ Decimals          9     │
├─────────────────────────┤
│      [ Receive ]        │
└─────────────────────────┘

   ShowPriceChart = true
```

- **Chart block** — `IsVisible="{Binding ChartIsVisible}"`. `microcharts:ChartView` at 300pt
  inside an `AbsoluteLayout`, min/max labels positioned proportionally, four time labels, and
  a row of three `ChoiceTextButton`s. Copied from `AssetDetailPage.xaml`.
- **Holdings card** — icon from `Assets.GetAssetIcon(Symbol)`, symbol, `{amount} {symbol}`,
  USD value. Amount formatting reuses `SolanaAssetView`'s rule: rounded to
  `min(decimals, 6)` and formatted `0.######`, so a whole balance reads `40 USDC`.
- **Price row** — chartable tokens only. Live price and the 24h change, coloured green when
  positive and red when negative, signed and to two decimals.
- **Facts card** — Network (`SolanaNetworkModel.SelectedCluster.GetName()`), Mint
  (truncated, tap copies the full value via `CopyAddress.CopyToClipboardAsync`), Decimals.
- **Bottom bar** — `PageBottomBarView` holding one full-width `ElevatedButton` reading
  "Receive", in `PageTemplate.PopupContent` alongside `AddressQrCodeView`.

### View model

Follows `SolanaBalancesPageViewModel`'s conventions exactly, for the reasons documented
there:

- `loadCts` cancel-and-replace at the top of every load, so a superseded interval switch
  cannot write its points after a newer one.
- `Unsubscribe()` called from `OnDisappearing`, unhooking
  `SolanaNetworkModel.ClusterChanged` and cancelling any in-flight load.
- `ThrowIfCancellationRequested()` after each await, before writing to observable state.

`ChangeChartIntervalCommand` sets `ChartInterval` and triggers a reload, as in
`AssetDetailViewModel`. A repeat tap on the already-selected interval is a no-op.

### Load

**`ShowPriceChart == false`:** no network calls, no spinner. The page renders entirely from
the row it was constructed with.

**`ShowPriceChart == true`:** `GetPriceHistoryAsync` and `GetSpotQuoteAsync` run concurrently
via `Task.WhenAll`. Two independent best-effort calls; neither should wait on the other.

### Failures

| Failure | Result |
|---|---|
| History empty or fewer than 2 points | "Price history unavailable" replaces the chart. Interval buttons stay, so the user can retry by switching. |
| Spot quote null | Price shows `-`, no change badge. |
| Either | Holdings and facts unaffected — they came from the row. |

The chart is **not** replaced with the flat placeholder series `AssetDetailViewModel` falls
back to. A flat line at an arbitrary value reads as a real, stable price; that is the exact
misreading this feature exists to prevent.

### Cluster change

`OnClusterChanged` pops back to the balances page rather than reloading. The mint on screen
may not exist on the new cluster, and the page's own "Network" row would otherwise assert a
cluster the mint does not belong to.

## Part D — Navigation in

`SolanaAssetView` gains a `TapGestureRecognizer` on its `Card` and an `OnClicked` handler
mirroring `AssetView.OnClicked`:

```csharp
private async void OnClicked(object sender, TappedEventArgs e)
{
    if (Balance is null)
    {
        return;
    }

    await Navigation.PushAsync(new SolanaTokenDetailPage(Balance));
}
```

The null guard is not ceremony: `BindableProperty` defaults to `null`, and a row whose
binding has not yet resolved is tappable.

## Testing

New unit tests in `PlutoFrameworkTests`:

**`SolanaPriceHistoryParserTests`**
- Well-formed response → points in order, `time` interpreted as Unix seconds.
- `{"candles":[]}` → empty list.
- Malformed JSON → empty list, no throw.
- Element missing `close` → skipped, siblings kept.
- Non-numeric `close` or `time` → skipped, siblings kept.
- Root is an array, not an object → empty list.

**`SolanaChartIntervalTests`**
- Each `Interval` maps to the right Jupiter string.
- `from`/`to` are Unix **milliseconds** and span exactly `steps × interval length`.

**`SolanaPriceParserTests`** (extended)
- `ParseQuotes` reads `usdPrice` and `priceChange24h`.
- A missing `priceChange24h` yields `Change24h == null`, not `0` — no data and no movement
  are different claims.
- The existing `Parse` behaviour is unchanged.

**`SolanaBalanceAssemblerTests`** (extended)
- The SOL row always has `ShowPriceChart == true`.
- A whitelist entry with the flag off produces a row with it off.
- A whitelist entry with the flag on produces a row with it on.

No test hits Jupiter. The parsers are pure and the fixtures are the responses captured in
"Research findings".

## Explicit exclusions

- **Transfer / send.** No Solana transaction building or signing for transfers exists in the
  framework.
- **Transaction history.** `SolanaRpcModel` exposes balances, token accounts, blockhash and
  send; nothing for signatures.
- **Charts for SPL tokens.** No whitelisted SPL token is chartable today. The flag exists so
  the first one that is needs a config change, not a code change.
- **Candlestick rendering.** Microcharts has no candlestick type; OHLC would mean hand-drawn
  SkiaSharp and a look that diverges from the Substrate page.
- **Caching price history.** Each page open and interval switch refetches. One call to a free
  endpoint, on an explicit user action.

## Sources

- Jupiter documented API surface — https://developers.jup.ag/docs/llms.txt
- Jupiter Price API — https://developers.jup.ag/docs/price
- `datapi.jup.ag/v2/charts` behaviour — probed directly, 2026-07-26 (undocumented)
