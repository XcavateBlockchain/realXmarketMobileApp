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


namespace Bajun.NetApi.Generated.Model.pallet_ajuna_tournament.config
{
    
    
    /// <summary>
    /// >> RewardClaimState
    /// </summary>
    public enum RewardClaimState
    {
        
        /// <summary>
        /// >> Unclaimed
        /// </summary>
        Unclaimed = 0,
        
        /// <summary>
        /// >> Claimed
        /// </summary>
        Claimed = 1,
    }
    
    /// <summary>
    /// >> 652 - Variant[pallet_ajuna_tournament.config.RewardClaimState]
    /// </summary>
    public sealed class EnumRewardClaimState : BaseEnumRust<RewardClaimState>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumRewardClaimState()
        {
				AddTypeDecoder<BaseVoid>(RewardClaimState.Unclaimed);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.sp_core.crypto.AccountId32>(RewardClaimState.Claimed);
        }
    }
}