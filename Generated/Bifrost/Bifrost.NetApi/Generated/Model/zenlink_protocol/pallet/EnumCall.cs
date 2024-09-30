//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Substrate.NetApi.Model.Types.Base;
using System.Collections.Generic;


namespace Bifrost.NetApi.Generated.Model.zenlink_protocol.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> set_fee_receiver
        /// Set the new receiver of the protocol fee.
        /// 
        /// # Arguments
        /// 
        /// - `send_to`:
        /// (1) Some(receiver): it turn on the protocol fee and the new receiver account.
        /// (2) None: it turn off the protocol fee.
        /// </summary>
        set_fee_receiver = 0,
        
        /// <summary>
        /// >> set_fee_point
        /// Set the protocol fee point.
        /// 
        /// # Arguments
        /// 
        /// - `fee_point`:
        /// 0 means that all exchange fees belong to the liquidity provider.
        /// 30 means that all exchange fees belong to the fee receiver.
        /// </summary>
        set_fee_point = 1,
        
        /// <summary>
        /// >> transfer
        /// Move some assets from one holder to another.
        /// 
        /// # Arguments
        /// 
        /// - `asset_id`: The foreign id.
        /// - `target`: The receiver of the foreign.
        /// - `amount`: The amount of the foreign to transfer.
        /// </summary>
        transfer = 2,
        
        /// <summary>
        /// >> create_pair
        /// Create pair by two assets.
        /// 
        /// The order of foreign dot effect result.
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up Pair
        /// - `asset_1`: Asset which make up Pair
        /// </summary>
        create_pair = 3,
        
        /// <summary>
        /// >> add_liquidity
        /// Provide liquidity to a pair.
        /// 
        /// The order of foreign dot effect result.
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up pair
        /// - `asset_1`: Asset which make up pair
        /// - `amount_0_desired`: Maximum amount of asset_0 added to the pair
        /// - `amount_1_desired`: Maximum amount of asset_1 added to the pair
        /// - `amount_0_min`: Minimum amount of asset_0 added to the pair
        /// - `amount_1_min`: Minimum amount of asset_1 added to the pair
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        add_liquidity = 4,
        
        /// <summary>
        /// >> remove_liquidity
        /// Extract liquidity.
        /// 
        /// The order of foreign dot effect result.
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up pair
        /// - `asset_1`: Asset which make up pair
        /// - `amount_asset_0_min`: Minimum amount of asset_0 to exact
        /// - `amount_asset_1_min`: Minimum amount of asset_1 to exact
        /// - `recipient`: Account that accepts withdrawal of assets
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        remove_liquidity = 5,
        
        /// <summary>
        /// >> swap_exact_assets_for_assets
        /// Sell amount of foreign by path.
        /// 
        /// # Arguments
        /// 
        /// - `amount_in`: Amount of the foreign will be sold
        /// - `amount_out_min`: Minimum amount of target foreign
        /// - `path`: path can convert to pairs.
        /// - `recipient`: Account that receive the target foreign
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        swap_exact_assets_for_assets = 6,
        
        /// <summary>
        /// >> swap_assets_for_exact_assets
        /// Buy amount of foreign by path.
        /// 
        /// # Arguments
        /// 
        /// - `amount_out`: Amount of the foreign will be bought
        /// - `amount_in_max`: Maximum amount of sold foreign
        /// - `path`: path can convert to pairs.
        /// - `recipient`: Account that receive the target foreign
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        swap_assets_for_exact_assets = 7,
        
        /// <summary>
        /// >> bootstrap_create
        /// Create bootstrap pair
        /// 
        /// The order of asset don't affect result.
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// - `target_supply_0`: Target amount of asset_0 total contribute
        /// - `target_supply_0`: Target amount of asset_1 total contribute
        /// - `capacity_supply_0`: The max amount of asset_0 total contribute
        /// - `capacity_supply_1`: The max amount of asset_1 total contribute
        /// - `end`: The earliest ending block.
        /// </summary>
        bootstrap_create = 8,
        
        /// <summary>
        /// >> bootstrap_contribute
        /// Contribute some asset to a bootstrap pair
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// - `amount_0_contribute`: The amount of asset_0 contribute to this bootstrap pair
        /// - `amount_1_contribute`: The amount of asset_1 contribute to this bootstrap pair
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        bootstrap_contribute = 9,
        
        /// <summary>
        /// >> bootstrap_claim
        /// Claim lp asset from a bootstrap pair
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// - `deadline`: Height of the cutoff block of this transaction
        /// </summary>
        bootstrap_claim = 10,
        
        /// <summary>
        /// >> bootstrap_end
        /// End a bootstrap pair
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// </summary>
        bootstrap_end = 11,
        
        /// <summary>
        /// >> bootstrap_update
        /// update a bootstrap pair
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// - `min_contribution_0`: The new min amount of asset_0 contribute
        /// - `min_contribution_0`: The new min amount of asset_1 contribute
        /// - `target_supply_0`: The new target amount of asset_0 total contribute
        /// - `target_supply_0`: The new target amount of asset_1 total contribute
        /// - `capacity_supply_0`: The new max amount of asset_0 total contribute
        /// - `capacity_supply_1`: The new max amount of asset_1 total contribute
        /// - `end`: The earliest ending block.
        /// </summary>
        bootstrap_update = 12,
        
        /// <summary>
        /// >> bootstrap_refund
        /// Contributor refund from disable bootstrap pair
        /// 
        /// # Arguments
        /// 
        /// - `asset_0`: Asset which make up bootstrap pair
        /// - `asset_1`: Asset which make up bootstrap pair
        /// </summary>
        bootstrap_refund = 13,
        
        /// <summary>
        /// >> bootstrap_charge_reward
        /// </summary>
        bootstrap_charge_reward = 14,
        
        /// <summary>
        /// >> bootstrap_withdraw_reward
        /// </summary>
        bootstrap_withdraw_reward = 15,
        
        /// <summary>
        /// >> set_new_fee_receiver
        /// </summary>
        set_new_fee_receiver = 16,
    }
    
    /// <summary>
    /// >> 324 - Variant[zenlink_protocol.pallet.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseOpt<Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>>(Call.set_fee_receiver);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U8>(Call.set_fee_point);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.transfer);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>>(Call.create_pair);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.add_liquidity);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.remove_liquidity);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseVec<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.swap_exact_assets_for_assets);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseVec<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.swap_assets_for_exact_assets);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Substrate.NetApi.Model.Types.Base.BaseVec<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>, Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Primitive.U128>>>>(Call.bootstrap_create);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.bootstrap_contribute);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.bootstrap_claim);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>>(Call.bootstrap_end);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Substrate.NetApi.Model.Types.Base.BaseVec<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>, Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Primitive.U128>>>>(Call.bootstrap_update);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId>>(Call.bootstrap_refund);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Substrate.NetApi.Model.Types.Primitive.U128>>>>(Call.bootstrap_charge_reward);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>>(Call.bootstrap_withdraw_reward);
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.zenlink_protocol.primitives.AssetId, Bifrost.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>>(Call.set_new_fee_receiver);
        }
    }
}