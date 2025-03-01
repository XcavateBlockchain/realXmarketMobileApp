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


namespace Unique.NetApi.Generated.Model.pallet_identity.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Identity pallet declaration.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> add_registrar
        /// See [`Pallet::add_registrar`].
        /// </summary>
        add_registrar = 0,
        
        /// <summary>
        /// >> set_identity
        /// See [`Pallet::set_identity`].
        /// </summary>
        set_identity = 1,
        
        /// <summary>
        /// >> set_subs
        /// See [`Pallet::set_subs`].
        /// </summary>
        set_subs = 2,
        
        /// <summary>
        /// >> clear_identity
        /// See [`Pallet::clear_identity`].
        /// </summary>
        clear_identity = 3,
        
        /// <summary>
        /// >> request_judgement
        /// See [`Pallet::request_judgement`].
        /// </summary>
        request_judgement = 4,
        
        /// <summary>
        /// >> cancel_request
        /// See [`Pallet::cancel_request`].
        /// </summary>
        cancel_request = 5,
        
        /// <summary>
        /// >> set_fee
        /// See [`Pallet::set_fee`].
        /// </summary>
        set_fee = 6,
        
        /// <summary>
        /// >> set_account_id
        /// See [`Pallet::set_account_id`].
        /// </summary>
        set_account_id = 7,
        
        /// <summary>
        /// >> set_fields
        /// See [`Pallet::set_fields`].
        /// </summary>
        set_fields = 8,
        
        /// <summary>
        /// >> provide_judgement
        /// See [`Pallet::provide_judgement`].
        /// </summary>
        provide_judgement = 9,
        
        /// <summary>
        /// >> kill_identity
        /// See [`Pallet::kill_identity`].
        /// </summary>
        kill_identity = 10,
        
        /// <summary>
        /// >> add_sub
        /// See [`Pallet::add_sub`].
        /// </summary>
        add_sub = 11,
        
        /// <summary>
        /// >> rename_sub
        /// See [`Pallet::rename_sub`].
        /// </summary>
        rename_sub = 12,
        
        /// <summary>
        /// >> remove_sub
        /// See [`Pallet::remove_sub`].
        /// </summary>
        remove_sub = 13,
        
        /// <summary>
        /// >> quit_sub
        /// See [`Pallet::quit_sub`].
        /// </summary>
        quit_sub = 14,
        
        /// <summary>
        /// >> force_insert_identities
        /// See [`Pallet::force_insert_identities`].
        /// </summary>
        force_insert_identities = 15,
        
        /// <summary>
        /// >> force_remove_identities
        /// See [`Pallet::force_remove_identities`].
        /// </summary>
        force_remove_identities = 16,
        
        /// <summary>
        /// >> force_set_subs
        /// See [`Pallet::force_set_subs`].
        /// </summary>
        force_set_subs = 17,
    }
    
    /// <summary>
    /// >> 139 - Variant[pallet_identity.pallet.Call]
    /// Identity pallet declaration.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>(Call.add_registrar);
				AddTypeDecoder<Unique.NetApi.Generated.Model.pallet_identity.types.IdentityInfo>(Call.set_identity);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Unique.NetApi.Generated.Model.sp_core.crypto.AccountId32, Unique.NetApi.Generated.Model.pallet_identity.types.EnumData>>>(Call.set_subs);
				AddTypeDecoder<BaseVoid>(Call.clear_identity);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.request_judgement);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U32>(Call.cancel_request);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.set_fee);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>>(Call.set_account_id);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Unique.NetApi.Generated.Model.pallet_identity.types.BitFlags>>(Call.set_fields);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Unique.NetApi.Generated.Model.pallet_identity.types.EnumJudgement, Unique.NetApi.Generated.Model.primitive_types.H256>>(Call.provide_judgement);
				AddTypeDecoder<Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>(Call.kill_identity);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Unique.NetApi.Generated.Model.pallet_identity.types.EnumData>>(Call.add_sub);
				AddTypeDecoder<BaseTuple<Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Unique.NetApi.Generated.Model.pallet_identity.types.EnumData>>(Call.rename_sub);
				AddTypeDecoder<Unique.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>(Call.remove_sub);
				AddTypeDecoder<BaseVoid>(Call.quit_sub);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Unique.NetApi.Generated.Model.sp_core.crypto.AccountId32, Unique.NetApi.Generated.Model.pallet_identity.types.Registration>>>(Call.force_insert_identities);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseVec<Unique.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.force_remove_identities);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Base.BaseTuple<Unique.NetApi.Generated.Model.sp_core.crypto.AccountId32, Substrate.NetApi.Model.Types.Base.BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Unique.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT4>>>>(Call.force_set_subs);
        }
    }
}
