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


namespace Kilt.NetApi.Generated.Model.ctype.pallet
{
    
    
    /// <summary>
    /// >> Event
    /// The `Event` enum of this pallet
    /// </summary>
    public enum Event
    {
        
        /// <summary>
        /// >> CTypeCreated
        /// A new CType has been created.
        /// \[creator identifier, CType hash\]
        /// </summary>
        CTypeCreated = 0,
        
        /// <summary>
        /// >> CTypeUpdated
        /// Information about a CType has been updated.
        /// \[CType hash\]
        /// </summary>
        CTypeUpdated = 1,
    }
    
    /// <summary>
    /// >> 111 - Variant[ctype.pallet.Event]
    /// The `Event` enum of this pallet
    /// </summary>
    public sealed class EnumEvent : BaseEnumRust<Event>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumEvent()
        {
				AddTypeDecoder<BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Kilt.NetApi.Generated.Model.primitive_types.H256>>(Event.CTypeCreated);
				AddTypeDecoder<Kilt.NetApi.Generated.Model.primitive_types.H256>(Event.CTypeUpdated);
        }
    }
}
