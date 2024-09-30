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


namespace Bifrost.NetApi.Generated.Model.pallet_conviction_voting.pallet
{
    
    
    /// <summary>
    /// >> Event
    /// The `Event` enum of this pallet
    /// </summary>
    public enum Event
    {
        
        /// <summary>
        /// >> Delegated
        /// An account has delegated their vote to another account. \[who, target\]
        /// </summary>
        Delegated = 0,
        
        /// <summary>
        /// >> Undelegated
        /// An \[account\] has cancelled a previous delegation operation.
        /// </summary>
        Undelegated = 1,
    }
    
    /// <summary>
    /// >> 57 - Variant[pallet_conviction_voting.pallet.Event]
    /// The `Event` enum of this pallet
    /// </summary>
    public sealed class EnumEvent : BaseEnumRust<Event>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumEvent()
        {
				AddTypeDecoder<BaseTuple<Bifrost.NetApi.Generated.Model.sp_core.crypto.AccountId32, Bifrost.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Event.Delegated);
				AddTypeDecoder<Bifrost.NetApi.Generated.Model.sp_core.crypto.AccountId32>(Event.Undelegated);
        }
    }
}