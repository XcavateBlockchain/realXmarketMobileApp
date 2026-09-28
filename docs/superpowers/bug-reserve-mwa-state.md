# Bug: reserve property token tx via Phantom (MWA) stuck Pending forever

## User constraints
- KEEP sign-transaction flow (sign_and_send_transactions), NOT sign_messages.
- User demanded real Phantom testing: "Download the Phantom wallet and try it!!"

## Rig (all working)
- redroid 13 arm64 Docker container `redroid13`; adb via `export PATH=/home/rosta/Android/platform-tools:$PATH; adb connect localhost:5555`; root via `docker exec -u 0 redroid13 sh`. Data dir /home/rosta/redroid-data (PRISTINE one; never install re-signed Phantom — pairip native anti-tamper SIGSEGV).
- Phantom v26.30.2 (app.phantom) LICENSE WALL BYPASSED and ONBOARDED:
  - Fake `com.android.vending` licensing service installed as SYSTEM app at /system/app/vending/vending.apk (source /tmp/vending/, smali-built, throwaway keystore /tmp/phantom_patch/throwaway.keystore pass android). REQUIRED `android:forceQueryable="true"` in its manifest because redroid AOSP has config_forceSystemPackagesQueryable=false (that's why "service not found" persisted even as system app).
  - Conscrypt RSA verify patched: /system/lib64/libcrypto.so, EVP_DigestVerifyFinal at 0xedeb4 → mov w0,#1;ret, plus embedded FIPS self-hash at file offset 0x31210 replaced with calculated value. Patched file /tmp/phantom_patch/libcrypto_patched2.so; original /tmp/phantom_patch/libcrypto.so. Revert: docker cp original back + restart.
  - LicenseClient contract: transact(2) parcel has NO interface token (empty) → enforceInterface only warns, don't throw; reply LICENSED via listener transact(1): token ILlicenseV2ResultListener, int 0, int 1, Bundle{"LICENSE_DATA"=JWS RS256, payload {"packageName":"app.phantom"}, sig 256 zero bytes b64url}.
- Phantom wallet: seed `attract afford paddle march order wasp unable syrup bounce champion pair month` (also /tmp/mwaharness_seed.txt), Solana addr m/44'/501'/0'/0' = `CkipwVKaN7v4AmoCvp2MVgDj35xHcPBXJt1J27th6dkE` (/tmp/mwaharness_addr.txt). Testnet Mode ON + Solana Devnet selected (Settings→Developer Settings). Username mwatesterone.
- Devnet SOL airdrop: all public faucets 429/captcha (api.devnet.solana.com, rpcpool, drpc, ankr, triangle, faucet.solana.com has captcha; WebBridge browser ext not connected). Background retry loop running (task bash-lw50d9og, /tmp/airdrop_loop.sh, every 2 min, 2h cap). NOTE: wallet SOL NOT needed for experiments (fee payer = rent collector).

## EXPERIMENT A — BUG REPRODUCED on real Phantom (2026-09-28)
Harness app /tmp/mwaharness (com.xcavate.mwaharness, references PlutoFrameworkCore, mirrors SubmitTwoSignerAsync EXACTLY; MainActivity crc64dbe6d32848599905; buttons A/B/C; logs tag MWATEST; EmbedAssembliesIntoApk=true for Debug).
Flow verified working end-to-end: MWA authorize (identity MwaHarness/xcavate.io) → blockhash via SolanaRpcModel → reserve_shares ix listing 5, amount 1, maxTotalCost 10_000_000_000, payer=rent collector 2nLLLhSQmJHWmj4zcfcmZPVmDmxzqJqYGKDS5shPLB8z, mint 71G3dc4B9p9QBosLx3XhWY3ULRPAxjopngsin66M9HUb (Tokenkeg, 9 dec) → profile API POST signed OK (200) → sign_and_send → Phantom approve (dialogs: Connect; Confirm transaction; swipe up; "Confirm (unsafe)"; checkbox "I understand..."; "Yes, confirm (unsafe)").
RESULT: Phantom returned TXID 5bV4ZfqM5hVFEx84eHfpFV6dKywGLA2UzgRURCccBWrYpAdFTgNNGjjp6pU8cWzgPrQuLKpknYr3FrCqbZc5uTAF (= RC signature = fee-payer sig slot). getSignatureStatuses with searchTransactionHistory polled 2.5+ min / ~250 slots: **value [null] every time — tx NEVER landed**. Exactly the user's bug.

## API BLOCKER CONFIRMED (experiment B attempt)
Profile API rent-collector-signature REJECTS messages containing ComputeBudget instructions: HTTP 400 "Instruction does not target the marketplace program". So embedding CB ixs ALSO needs user's backend to whitelist ComputeBudgetProgram.

## EXPERIMENTS B/C — NO PHANTOM REWRITE (2026-09-28)
Button B (sign_transactions, no CB ixs, real API RC sig): Phantom returned the message BYTE-IDENTICAL (MESSAGE_UNCHANGED=true). No priority-fee injection, no message alteration. The earlier CB-rewrite theory is DEAD. (B's manual RPC submit passed sigverify; failed only on blockhash expired during slow manual tapping.)
Button C (2 ComputeBudget ixs prepended, RC slot zero-filled, no API): same — byte-identical. CB presence is irrelevant to Phantom's behaviour.

## EXPERIMENT D — DOUBLE-SIGNED BYTES ARE LANDABLE (2026-09-28)
Same reserve tx (listing 5, RC sig from real API applied, investor slot signed LOCALLY with harness seed) submitted via raw RPC:
- preflight ON → rejected "custom program error: 0xbc4" (3012 — test wallet's payment ATA empty; expected, the test investor is broke).
- skipPreflight → ACCEPTED, FINALIZED on-chain: slot 505163445, txid 5YPkRqQDNfNqHJyJe8wfAFXLos7SUvfbCSHCXPRP4ShZbQfsPzWkqxkfHmgDHgEVoMHgWmwCQsB6PnLU7dYyvm3w, err Custom 3012 recorded on-chain (execution failed, but the transaction itself landed and finalized — the pipeline works).
→ The exact bytes Phantom was given are valid and landable. Phantom just never broadcast them.

## ROOT CAUSE (PROVEN)
Phantom's `sign_and_send_transactions` silently returns the fee-payer signature WITHOUT broadcasting when its own preflight/simulation fails (in the repro: empty payment ATA → custom error 3012; for real users plausibly blockhash expiry during slow approval, or any simulation failure). No MWA error -4 NOT_SUBMITTED is returned. The app gets a txid that never exists on-chain → tracker waits → Pending forever. NOT a message rewrite (B/C), NOT invalid bytes (D), NOT blockhash expiry in A (fresh blockhash, 14s approval).

## FIX (implemented 2026-09-28)
Keep transaction signing (user constraint); remove Phantom's unreliable send path from the two-signer flow:
- MwaClient: new public SignTransactionsAsync (MWA `sign_transactions`: payloads → signed_payloads); MwaModels: MwaSignTransactionsRequest/Response. Class doc updated (sign_transactions now deliberately implemented despite MWA 2.0 deprecation).
- MwaSolanaAccount.SignWireTransactionAsync: implemented via SignTransactionsAsync through RunAuthorizedAsync (was NotSupportedException).
- XcavateMarketplaceTransactionModel.SubmitTwoSignerAsync: MWA branch deleted — both account types now SignWireTransactionAsync + SolanaRpcModel.SendTransactionAsync. Real submission errors surface; blockhash-expiry retry (IsExpiredError matches any "blockhash" mention, incl. RPC "Blockhash not found") now covers app-side submits. SignAndSendWireTransactionAsync kept for the one-signer SendAsync path (wallet IS rent collector) which works.
- Tests: PlutoFrameworkTests/MwaSignTransactionsWireTests.cs pins the payloads/signed_payloads wire shape.
Injected-wallet FeaturesFor deliberately unchanged: dapp-facing solana:signTransaction still not advertised for MWA keys (out of scope; dapps fall back to signAndSendTransaction).

## FIX VALIDATED on real Phantom 26.30.2 (2026-09-28)
Harness button A rewired to the fixed flow (new public MwaClient.SignTransactionsAsync + SolanaRpcModel.SendTransactionAsync), rebuilt, reinstalled, run twice on devnet:
- Run 1 (slow ~49s approval, deliberate dump-between-steps): Phantom signed (592 bytes) → app RPC submit → HONEST rejection "Transaction simulation failed: Blockhash not found". Contrast with old flow: Phantom returned txid for a tx it never broadcast. This wording matches SolanaBlockhashExpiry.IsExpiredError → the app's existing attempt-2 retry (fresh blockhash + fresh RC sig) covers it. Unit-pinned in SolanaBlockhashExpiryTests.
- Run 2 (fast ~6s approval): Phantom signed → app RPC submit → HONEST rejection "Error processing Instruction 0: custom program error: 0xbc4" (3012 = test investor's payment ATA empty — the real reason, surfaced immediately instead of Pending forever).
Verification: PlutoFrameworkTests 506 passed / 8 failed (8 = pre-existing external-service failures, baseline unchanged; includes 3 new MwaSignTransactionsWireTests). XcavateMobileApp net10.0-android build 0 errors.
Note: injected-wallet FeaturesFor intentionally NOT extended — dapp-facing solana:signTransaction stays unadvertised for MWA keys (dapps fall back to signAndSendTransaction). One-signer path (wallet IS rent collector) still uses sign_and_send and is unchanged.

## Key file refs
- Flow: realXmarketPlutoFramework/PlutoFramework/Components/XcavateProperty/XcavateMarketplaceTransactionModel.cs (SubmitTwoSignerAsync lines 178-243)
- API client: .../Model/Xcavate/Profile/RentCollectorSignatureClient.cs (endpoint https://profile-api.xcavate.io/api/marketplace/rent-collector-signature, X-SS58-Address header)
- MWA client: PlutoFrameworkCore/Solana/Mwa/MwaClient.cs (no sign_transactions by design)
- Indexer: https://indexer-devnet.xcavate.io/graphql. Config: rentCollector 2nLLLh..., paymentMints [71G3...(9dec), 8umv...(6dec)], both mintAuthority 7bGxnDFi3zKLAbgeXtCANcf8MGSYob1EAmoWZY77qjp2 (not held). Listing 5 LISTED, sharePrice 5750000000.
- Final fix location when proven: SubmitTwoSignerAsync + tests in PlutoFrameworkTests; rebuild XcavateMobileApp -f net10.0-android; baseline tests 8 external-svc fails/503 pass.

## Wire log
/home/rosta/.kimi-code/sessions/wd_realxmarketmobileapp_c1200cec204c/session_51309a15-9c4f-4065-bd43-f1349699617d/agents/main/wire.jsonl
