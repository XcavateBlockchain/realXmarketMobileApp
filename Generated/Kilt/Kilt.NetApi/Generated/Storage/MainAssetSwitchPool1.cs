//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Substrate.NetApi;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Meta;
using Substrate.NetApi.Model.Types;
using Substrate.NetApi.Model.Types.Base;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Kilt.NetApi.Generated.Storage
{
    
    
    /// <summary>
    /// >> AssetSwitchPool1Storage
    /// </summary>
    public sealed class AssetSwitchPool1Storage
    {
        
        // Substrate client for the storage calls.
        private SubstrateClientExt _client;
        
        /// <summary>
        /// >> AssetSwitchPool1Storage Constructor
        /// </summary>
        public AssetSwitchPool1Storage(SubstrateClientExt client)
        {
            this._client = client;
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("AssetSwitchPool1", "SwitchPair"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(null, null, typeof(Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.SwitchPairInfo)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("AssetSwitchPool1", "PendingSwitchConfirmations"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat}, typeof(Substrate.NetApi.Model.Types.Primitive.U64), typeof(Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.UnconfirmedSwitchInfo)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("AssetSwitchPool1", "CounterForPendingSwitchConfirmations"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(null, null, typeof(Substrate.NetApi.Model.Types.Primitive.U32)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("AssetSwitchPool1", "NextQueryId"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(null, null, typeof(Substrate.NetApi.Model.Types.Primitive.U64)));
        }
        
        /// <summary>
        /// >> SwitchPairParams
        ///  Stores the switch pair.
        /// </summary>
        public static string SwitchPairParams()
        {
            return RequestGenerator.GetStorage("AssetSwitchPool1", "SwitchPair", Substrate.NetApi.Model.Meta.Storage.Type.Plain);
        }
        
        /// <summary>
        /// >> SwitchPairDefault
        /// Default value as hex string
        /// </summary>
        public static string SwitchPairDefault()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> SwitchPair
        ///  Stores the switch pair.
        /// </summary>
        public async Task<Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.SwitchPairInfo> SwitchPair(string blockhash, CancellationToken token)
        {
            string parameters = AssetSwitchPool1Storage.SwitchPairParams();
            var result = await _client.GetStorageAsync<Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.SwitchPairInfo>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> PendingSwitchConfirmationsParams
        ///  Stores the switches that have been applied locally but not yet on the
        ///  remote. Used to rollback failed ones.
        /// </summary>
        public static string PendingSwitchConfirmationsParams(Substrate.NetApi.Model.Types.Primitive.U64 key)
        {
            return RequestGenerator.GetStorage("AssetSwitchPool1", "PendingSwitchConfirmations", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat}, new Substrate.NetApi.Model.Types.IType[] {
                        key});
        }
        
        /// <summary>
        /// >> PendingSwitchConfirmationsDefault
        /// Default value as hex string
        /// </summary>
        public static string PendingSwitchConfirmationsDefault()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> PendingSwitchConfirmations
        ///  Stores the switches that have been applied locally but not yet on the
        ///  remote. Used to rollback failed ones.
        /// </summary>
        public async Task<Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.UnconfirmedSwitchInfo> PendingSwitchConfirmations(Substrate.NetApi.Model.Types.Primitive.U64 key, string blockhash, CancellationToken token)
        {
            string parameters = AssetSwitchPool1Storage.PendingSwitchConfirmationsParams(key);
            var result = await _client.GetStorageAsync<Kilt.NetApi.Generated.Model.pallet_asset_switch.@switch.UnconfirmedSwitchInfo>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> CounterForPendingSwitchConfirmationsParams
        /// Counter for the related counted storage map
        /// </summary>
        public static string CounterForPendingSwitchConfirmationsParams()
        {
            return RequestGenerator.GetStorage("AssetSwitchPool1", "CounterForPendingSwitchConfirmations", Substrate.NetApi.Model.Meta.Storage.Type.Plain);
        }
        
        /// <summary>
        /// >> CounterForPendingSwitchConfirmationsDefault
        /// Default value as hex string
        /// </summary>
        public static string CounterForPendingSwitchConfirmationsDefault()
        {
            return "0x00000000";
        }
        
        /// <summary>
        /// >> CounterForPendingSwitchConfirmations
        /// Counter for the related counted storage map
        /// </summary>
        public async Task<Substrate.NetApi.Model.Types.Primitive.U32> CounterForPendingSwitchConfirmations(string blockhash, CancellationToken token)
        {
            string parameters = AssetSwitchPool1Storage.CounterForPendingSwitchConfirmationsParams();
            var result = await _client.GetStorageAsync<Substrate.NetApi.Model.Types.Primitive.U32>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> NextQueryIdParams
        ///  Stores the next query ID to use for queries.
        /// </summary>
        public static string NextQueryIdParams()
        {
            return RequestGenerator.GetStorage("AssetSwitchPool1", "NextQueryId", Substrate.NetApi.Model.Meta.Storage.Type.Plain);
        }
        
        /// <summary>
        /// >> NextQueryIdDefault
        /// Default value as hex string
        /// </summary>
        public static string NextQueryIdDefault()
        {
            return "0x0000000000000000";
        }
        
        /// <summary>
        /// >> NextQueryId
        ///  Stores the next query ID to use for queries.
        /// </summary>
        public async Task<Substrate.NetApi.Model.Types.Primitive.U64> NextQueryId(string blockhash, CancellationToken token)
        {
            string parameters = AssetSwitchPool1Storage.NextQueryIdParams();
            var result = await _client.GetStorageAsync<Substrate.NetApi.Model.Types.Primitive.U64>(parameters, blockhash, token);
            return result;
        }
    }
    
    /// <summary>
    /// >> AssetSwitchPool1Calls
    /// </summary>
    public sealed class AssetSwitchPool1Calls
    {
        
        /// <summary>
        /// >> set_switch_pair
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method SetSwitchPair(Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_total_supply, Kilt.NetApi.Generated.Model.xcm.EnumVersionedAssetId remote_asset_id, Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_circulating_supply, Kilt.NetApi.Generated.Model.xcm.EnumVersionedLocation remote_reserve_location, Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_ed, Kilt.NetApi.Generated.Model.xcm.EnumVersionedAsset remote_xcm_fee)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(remote_asset_total_supply.Encode());
            byteArray.AddRange(remote_asset_id.Encode());
            byteArray.AddRange(remote_asset_circulating_supply.Encode());
            byteArray.AddRange(remote_reserve_location.Encode());
            byteArray.AddRange(remote_asset_ed.Encode());
            byteArray.AddRange(remote_xcm_fee.Encode());
            return new Method(48, "AssetSwitchPool1", 0, "set_switch_pair", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> force_set_switch_pair
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method ForceSetSwitchPair(Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_total_supply, Kilt.NetApi.Generated.Model.xcm.EnumVersionedAssetId remote_asset_id, Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_circulating_supply, Kilt.NetApi.Generated.Model.xcm.EnumVersionedLocation remote_reserve_location, Substrate.NetApi.Model.Types.Primitive.U128 remote_asset_ed, Kilt.NetApi.Generated.Model.xcm.EnumVersionedAsset remote_xcm_fee)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(remote_asset_total_supply.Encode());
            byteArray.AddRange(remote_asset_id.Encode());
            byteArray.AddRange(remote_asset_circulating_supply.Encode());
            byteArray.AddRange(remote_reserve_location.Encode());
            byteArray.AddRange(remote_asset_ed.Encode());
            byteArray.AddRange(remote_xcm_fee.Encode());
            return new Method(48, "AssetSwitchPool1", 1, "force_set_switch_pair", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> force_unset_switch_pair
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method ForceUnsetSwitchPair()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(48, "AssetSwitchPool1", 2, "force_unset_switch_pair", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> pause_switch_pair
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method PauseSwitchPair()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(48, "AssetSwitchPool1", 3, "pause_switch_pair", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> resume_switch_pair
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method ResumeSwitchPair()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(48, "AssetSwitchPool1", 4, "resume_switch_pair", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> update_remote_xcm_fee
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method UpdateRemoteXcmFee(Kilt.NetApi.Generated.Model.xcm.EnumVersionedAsset @new)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(@new.Encode());
            return new Method(48, "AssetSwitchPool1", 5, "update_remote_xcm_fee", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> switch
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method Switch(Substrate.NetApi.Model.Types.Primitive.U128 local_asset_amount, Kilt.NetApi.Generated.Model.xcm.EnumVersionedLocation beneficiary)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(local_asset_amount.Encode());
            byteArray.AddRange(beneficiary.Encode());
            return new Method(48, "AssetSwitchPool1", 6, "switch", byteArray.ToArray());
        }
    }
    
    /// <summary>
    /// >> AssetSwitchPool1Constants
    /// </summary>
    public sealed class AssetSwitchPool1Constants
    {
    }
    
    /// <summary>
    /// >> AssetSwitchPool1Errors
    /// </summary>
    public enum AssetSwitchPool1Errors
    {
        
        /// <summary>
        /// >> InvalidInput
        /// Provided switch pair info is not valid.
        /// </summary>
        InvalidInput,
        
        /// <summary>
        /// >> Hook
        /// The runtime-injected logic returned an error with a specific code.
        /// </summary>
        Hook,
        
        /// <summary>
        /// >> Liquidity
        /// There are not enough remote assets to cover the specified amount of
        /// local tokens to switch.
        /// </summary>
        Liquidity,
        
        /// <summary>
        /// >> LocalPoolBalance
        /// Failure in transferring the local tokens from the user's balance to
        /// the switch pair pool account.
        /// </summary>
        LocalPoolBalance,
        
        /// <summary>
        /// >> PoolInitialLiquidityRequirement
        /// The calculated switch pair pool account does not have enough local
        /// tokens to cover the specified `circulating_supply`.
        /// </summary>
        PoolInitialLiquidityRequirement,
        
        /// <summary>
        /// >> SwitchPairAlreadyExisting
        /// A switch pair has already been set.
        /// </summary>
        SwitchPairAlreadyExisting,
        
        /// <summary>
        /// >> SwitchPairNotEnabled
        /// The switch pair did not enable switches.
        /// </summary>
        SwitchPairNotEnabled,
        
        /// <summary>
        /// >> SwitchPairNotFound
        /// No switch pair found.
        /// </summary>
        SwitchPairNotFound,
        
        /// <summary>
        /// >> UserSwitchBalance
        /// The user does not have enough local tokens to cover the requested
        /// switch.
        /// </summary>
        UserSwitchBalance,
        
        /// <summary>
        /// >> UserXcmBalance
        /// The user does not have enough assets to pay for the remote XCM fees.
        /// </summary>
        UserXcmBalance,
        
        /// <summary>
        /// >> Xcm
        /// Something regarding XCM went wrong.
        /// </summary>
        Xcm,
        
        /// <summary>
        /// >> Internal
        /// Internal error.
        /// </summary>
        Internal,
        
        /// <summary>
        /// >> AmountTooLow
        /// Attempt to switch less than the local ED tokens.
        /// </summary>
        AmountTooLow,
        
        /// <summary>
        /// >> PendingSwitches
        /// Some switches have not yet been processed.
        /// </summary>
        PendingSwitches,
    }
}
