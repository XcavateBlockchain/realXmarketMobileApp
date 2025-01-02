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


namespace XCavatePaseo.NetApi.Generated.Model.pallet_whitelist.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> whitelist_call
        /// </summary>
        whitelist_call = 0,
        
        /// <summary>
        /// >> remove_whitelisted_call
        /// </summary>
        remove_whitelisted_call = 1,
        
        /// <summary>
        /// >> dispatch_whitelisted_call
        /// </summary>
        dispatch_whitelisted_call = 2,
        
        /// <summary>
        /// >> dispatch_whitelisted_call_with_preimage
        /// </summary>
        dispatch_whitelisted_call_with_preimage = 3,
    }
    
    /// <summary>
    /// >> 144 - Variant[pallet_whitelist.pallet.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<XCavatePaseo.NetApi.Generated.Model.primitive_types.H256>(Call.whitelist_call);
				AddTypeDecoder<XCavatePaseo.NetApi.Generated.Model.primitive_types.H256>(Call.remove_whitelisted_call);
				AddTypeDecoder<BaseTuple<XCavatePaseo.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Primitive.U32, XCavatePaseo.NetApi.Generated.Model.sp_weights.weight_v2.Weight>>(Call.dispatch_whitelisted_call);
				AddTypeDecoder<XCavatePaseo.NetApi.Generated.Model.generic_runtime_template.EnumRuntimeCall>(Call.dispatch_whitelisted_call_with_preimage);
        }
    }
}