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


namespace XCavatePaseo.NetApi.Generated.Model.generic_runtime_template
{
    
    
    /// <summary>
    /// >> RuntimeHoldReason
    /// </summary>
    public enum RuntimeHoldReason
    {
        
        /// <summary>
        /// >> Preimage
        /// </summary>
        Preimage = 8,
        
        /// <summary>
        /// >> NftFractionalization
        /// </summary>
        NftFractionalization = 41,
    }
    
    /// <summary>
    /// >> 387 - Variant[generic_runtime_template.RuntimeHoldReason]
    /// </summary>
    public sealed class EnumRuntimeHoldReason : BaseEnumRust<RuntimeHoldReason>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumRuntimeHoldReason()
        {
				AddTypeDecoder<XCavatePaseo.NetApi.Generated.Model.pallet_preimage.pallet.EnumHoldReason>(RuntimeHoldReason.Preimage);
				AddTypeDecoder<XCavatePaseo.NetApi.Generated.Model.pallet_nft_fractionalization.pallet.EnumHoldReason>(RuntimeHoldReason.NftFractionalization);
        }
    }
}