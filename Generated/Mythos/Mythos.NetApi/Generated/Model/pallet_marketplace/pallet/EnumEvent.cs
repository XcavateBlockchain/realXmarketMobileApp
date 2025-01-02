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


namespace Mythos.NetApi.Generated.Model.pallet_marketplace.pallet
{
    
    
    /// <summary>
    /// >> Event
    /// The `Event` enum of this pallet
    /// </summary>
    public enum Event
    {
        
        /// <summary>
        /// >> AuthorityUpdated
        /// The pallet's authority was updated.
        /// </summary>
        AuthorityUpdated = 0,
        
        /// <summary>
        /// >> FeeSignerAddressUpdate
        /// The fee signer account was updated.
        /// </summary>
        FeeSignerAddressUpdate = 1,
        
        /// <summary>
        /// >> PayoutAddressUpdated
        /// The payout address account was updated.
        /// </summary>
        PayoutAddressUpdated = 2,
        
        /// <summary>
        /// >> OrderCreated
        /// An Ask/Bid order was created.
        /// </summary>
        OrderCreated = 3,
        
        /// <summary>
        /// >> OrderExecuted
        /// A trade of Ask and Bid was executed.
        /// </summary>
        OrderExecuted = 4,
        
        /// <summary>
        /// >> OrderCanceled
        /// The order was canceled by the order creator or the pallet's authority.
        /// </summary>
        OrderCanceled = 5,
    }
    
    /// <summary>
    /// >> 58 - Variant[pallet_marketplace.pallet.Event]
    /// The `Event` enum of this pallet
    /// </summary>
    public sealed class EnumEvent : BaseEnumRust<Event>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumEvent()
        {
				AddTypeDecoder<Mythos.NetApi.Generated.Model.account.AccountId20>(Event.AuthorityUpdated);
				AddTypeDecoder<Mythos.NetApi.Generated.Model.account.AccountId20>(Event.FeeSignerAddressUpdate);
				AddTypeDecoder<Mythos.NetApi.Generated.Model.account.AccountId20>(Event.PayoutAddressUpdated);
				AddTypeDecoder<BaseTuple<Mythos.NetApi.Generated.Model.account.AccountId20, Mythos.NetApi.Generated.Model.pallet_marketplace.types.EnumOrderType, Mythos.NetApi.Generated.Model.runtime_common.IncrementableU256, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U64, Substrate.NetApi.Model.Types.Primitive.U128>>(Event.OrderCreated);
				AddTypeDecoder<BaseTuple<Mythos.NetApi.Generated.Model.runtime_common.IncrementableU256, Substrate.NetApi.Model.Types.Primitive.U128, Mythos.NetApi.Generated.Model.account.AccountId20, Mythos.NetApi.Generated.Model.account.AccountId20, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128>>(Event.OrderExecuted);
				AddTypeDecoder<BaseTuple<Mythos.NetApi.Generated.Model.runtime_common.IncrementableU256, Substrate.NetApi.Model.Types.Primitive.U128, Mythos.NetApi.Generated.Model.account.AccountId20>>(Event.OrderCanceled);
        }
    }
}