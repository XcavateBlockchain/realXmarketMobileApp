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


namespace Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation
{
    
    
    /// <summary>
    /// >> ParametersKey
    /// </summary>
    public enum ParametersKey
    {
        
        /// <summary>
        /// >> MinInflation
        /// </summary>
        MinInflation = 0,
        
        /// <summary>
        /// >> MaxInflation
        /// </summary>
        MaxInflation = 1,
        
        /// <summary>
        /// >> IdealStake
        /// </summary>
        IdealStake = 2,
        
        /// <summary>
        /// >> Falloff
        /// </summary>
        Falloff = 3,
        
        /// <summary>
        /// >> UseAuctionSlots
        /// </summary>
        UseAuctionSlots = 4,
    }
    
    /// <summary>
    /// >> 456 - Variant[polkadot_runtime.dynamic_params.inflation.ParametersKey]
    /// </summary>
    public sealed class EnumParametersKey : BaseEnumRust<ParametersKey>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumParametersKey()
        {
				AddTypeDecoder<Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation.MinInflation>(ParametersKey.MinInflation);
				AddTypeDecoder<Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation.MaxInflation>(ParametersKey.MaxInflation);
				AddTypeDecoder<Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation.IdealStake>(ParametersKey.IdealStake);
				AddTypeDecoder<Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation.Falloff>(ParametersKey.Falloff);
				AddTypeDecoder<Polkadot.NetApi.Generated.Model.polkadot_runtime.dynamic_params.inflation.UseAuctionSlots>(ParametersKey.UseAuctionSlots);
        }
    }
}
