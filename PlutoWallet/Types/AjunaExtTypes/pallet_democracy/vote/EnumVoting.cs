//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Ajuna.NetApi.Model.Types.Base;
using System.Collections.Generic;


namespace PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.vote
{
    
    
    public enum Voting
    {
        
        Direct = 0,
        
        Delegating = 1,
    }
    
    /// <summary>
    /// >> 535 - Variant[pallet_democracy.vote.Voting]
    /// </summary>
    public sealed class EnumVoting : BaseEnumExt<Voting, BaseTuple<PlutoWallet.NetApiExt.Generated.Model.sp_core.bounded.bounded_vec.BoundedVecT13, PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.types.Delegations, PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.vote.PriorLock>, BaseTuple<Ajuna.NetApi.Model.Types.Primitive.U128, PlutoWallet.NetApiExt.Generated.Model.sp_core.crypto.AccountId32, PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.conviction.EnumConviction, PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.types.Delegations, PlutoWallet.NetApiExt.Generated.Model.pallet_democracy.vote.PriorLock>>
    {
    }
}