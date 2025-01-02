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


namespace Unique.NetApi.Generated.Model.pallet_xcm.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> send
        /// See [`Pallet::send`].
        /// </summary>
        send = 0,
        
        /// <summary>
        /// >> teleport_assets
        /// See [`Pallet::teleport_assets`].
        /// </summary>
        teleport_assets = 1,
        
        /// <summary>
        /// >> reserve_transfer_assets
        /// See [`Pallet::reserve_transfer_assets`].
        /// </summary>
        reserve_transfer_assets = 2,
        
        /// <summary>
        /// >> execute
        /// See [`Pallet::execute`].
        /// </summary>
        execute = 3,
        
        /// <summary>
        /// >> force_xcm_version
        /// See [`Pallet::force_xcm_version`].
        /// </summary>
        force_xcm_version = 4,
        
        /// <summary>
        /// >> force_default_xcm_version
        /// See [`Pallet::force_default_xcm_version`].
        /// </summary>
        force_default_xcm_version = 5,
        
        /// <summary>
        /// >> force_subscribe_version_notify
        /// See [`Pallet::force_subscribe_version_notify`].
        /// </summary>
        force_subscribe_version_notify = 6,
        
        /// <summary>
        /// >> force_unsubscribe_version_notify
        /// See [`Pallet::force_unsubscribe_version_notify`].
        /// </summary>
        force_unsubscribe_version_notify = 7,
        
        /// <summary>
        /// >> limited_reserve_transfer_assets
        /// See [`Pallet::limited_reserve_transfer_assets`].
        /// </summary>
        limited_reserve_transfer_assets = 8,
        
        /// <summary>
        /// >> limited_teleport_assets
        /// See [`Pallet::limited_teleport_assets`].
        /// </summary>
        limited_teleport_assets = 9,
        
        /// <summary>
        /// >> force_suspension
        /// See [`Pallet::force_suspension`].
        /// </summary>
        force_suspension = 10,
    }
    
    /// <summary>
    /// >> 215 - Variant[pallet_xcm.pallet.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedXcm>>(Call.send);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiAssets, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.teleport_assets);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiAssets, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.reserve_transfer_assets);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedXcm, Unique.NetApi.Generated.Model.sp_weights.weight_v2.Weight>>(Call.execute);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.staging_xcm.v3.multilocation.MultiLocation, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.force_xcm_version);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U32>>(Call.force_default_xcm_version);
				AddTypeDecoder<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation>(Call.force_subscribe_version_notify);
				AddTypeDecoder<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation>(Call.force_unsubscribe_version_notify);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiAssets, Substrate.NetApi.Model.Types.Primitive.U32, Unique.NetApi.Generated.Model.xcm.v3.EnumWeightLimit>>(Call.limited_reserve_transfer_assets);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiLocation, Unique.NetApi.Generated.Model.xcm.EnumVersionedMultiAssets, Substrate.NetApi.Model.Types.Primitive.U32, Unique.NetApi.Generated.Model.xcm.v3.EnumWeightLimit>>(Call.limited_teleport_assets);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.Bool>(Call.force_suspension);
        }
    }
}