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


namespace Kilt.NetApi.Generated.Model.pallet_dip_provider.pallet
{
    
    
    /// <summary>
    /// >> Event
    /// The `Event` enum of this pallet
    /// </summary>
    public enum Event
    {
        
        /// <summary>
        /// >> VersionedIdentityCommitted
        /// A new commitment has been stored.
        /// </summary>
        VersionedIdentityCommitted = 0,
        
        /// <summary>
        /// >> VersionedIdentityDeleted
        /// A commitment has been deleted.
        /// </summary>
        VersionedIdentityDeleted = 1,
    }
    
    /// <summary>
    /// >> 165 - Variant[pallet_dip_provider.pallet.Event]
    /// The `Event` enum of this pallet
    /// </summary>
    public sealed class EnumEvent : BaseEnumRust<Event>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumEvent()
        {
				AddTypeDecoder<BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Kilt.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Primitive.U16>>(Event.VersionedIdentityCommitted);
				AddTypeDecoder<BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Substrate.NetApi.Model.Types.Primitive.U16>>(Event.VersionedIdentityDeleted);
        }
    }
}
