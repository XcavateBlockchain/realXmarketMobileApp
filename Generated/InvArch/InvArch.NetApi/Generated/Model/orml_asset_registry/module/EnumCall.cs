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


namespace InvArch.NetApi.Generated.Model.orml_asset_registry.module
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> register_asset
        /// </summary>
        register_asset = 0,
        
        /// <summary>
        /// >> update_asset
        /// </summary>
        update_asset = 1,
    }
    
    /// <summary>
    /// >> 200 - Variant[orml_asset_registry.module.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.orml_traits.asset_registry.AssetMetadata, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.register_asset);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U32>, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2>, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2>, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.xcm.EnumVersionedLocation>>, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.invarch_runtime.assets.CustomMetadata>>>(Call.update_asset);
        }
    }
}
