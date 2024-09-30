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


namespace InvArch.NetApi.Generated.Model.pallet_ocif_staking.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains one variant per dispatchable that can be called by an extrinsic.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> register_core
        /// </summary>
        register_core = 0,
        
        /// <summary>
        /// >> unregister_core
        /// </summary>
        unregister_core = 1,
        
        /// <summary>
        /// >> change_core_metadata
        /// </summary>
        change_core_metadata = 2,
        
        /// <summary>
        /// >> stake
        /// </summary>
        stake = 3,
        
        /// <summary>
        /// >> unstake
        /// </summary>
        unstake = 4,
        
        /// <summary>
        /// >> withdraw_unstaked
        /// </summary>
        withdraw_unstaked = 5,
        
        /// <summary>
        /// >> staker_claim_rewards
        /// </summary>
        staker_claim_rewards = 6,
        
        /// <summary>
        /// >> core_claim_rewards
        /// </summary>
        core_claim_rewards = 7,
        
        /// <summary>
        /// >> halt_unhalt_pallet
        /// </summary>
        halt_unhalt_pallet = 8,
        
        /// <summary>
        /// >> move_stake
        /// </summary>
        move_stake = 9,
    }
    
    /// <summary>
    /// >> 235 - Variant[pallet_ocif_staking.pallet.Call]
    /// Contains one variant per dispatchable that can be called by an extrinsic.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT6, InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT7, InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT8>>(Call.register_core);
				AddTypeDecoder<BaseVoid>(Call.unregister_core);
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT6, InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT7, InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT8>>(Call.change_core_metadata);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.stake);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.unstake);
				AddTypeDecoder<BaseVoid>(Call.withdraw_unstaked);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U32>(Call.staker_claim_rewards);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.core_claim_rewards);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.Bool>(Call.halt_unhalt_pallet);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.move_stake);
        }
    }
}