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


namespace InvArch.NetApi.Generated.Model.orml_currencies.module
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> transfer
        /// Transfer some balance to another account under `currency_id`.
        /// 
        /// The dispatch origin for this call must be `Signed` by the
        /// transactor.
        /// </summary>
        transfer = 0,
        
        /// <summary>
        /// >> transfer_native_currency
        /// Transfer some native currency to another account.
        /// 
        /// The dispatch origin for this call must be `Signed` by the
        /// transactor.
        /// </summary>
        transfer_native_currency = 1,
        
        /// <summary>
        /// >> update_balance
        /// update amount of account `who` under `currency_id`.
        /// 
        /// The dispatch origin of this call must be _Root_.
        /// </summary>
        update_balance = 2,
    }
    
    /// <summary>
    /// >> 205 - Variant[orml_currencies.module.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.transfer);
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.transfer_native_currency);
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Primitive.I128>>(Call.update_balance);
        }
    }
}