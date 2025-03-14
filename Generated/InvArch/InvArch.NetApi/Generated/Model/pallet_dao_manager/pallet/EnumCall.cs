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


namespace InvArch.NetApi.Generated.Model.pallet_dao_manager.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Dispatch functions
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> create_dao
        /// Create a new DAO
        /// - `metadata`: Arbitrary byte vec to be attached to the dao info
        /// - `minimum_support`: Minimum amount of positive votes out of total token supply required to approve a proposal
        /// - `required_approval`: Minimum amount of positive votes out of current positive + negative votes required to approve a proposal
        /// - `creation_fee_asset`: Token to be used to pay the dao creation fee
        /// </summary>
        create_dao = 0,
        
        /// <summary>
        /// >> token_mint
        /// Mint the dao's voting token to a target (called by a dao origin)
        /// - `amount`: Balance amount
        /// - `target`: Account receiving the minted tokens
        /// </summary>
        token_mint = 1,
        
        /// <summary>
        /// >> token_burn
        /// Burn the dao's voting token from a target (called by a dao origin)
        /// - `amount`: Balance amount
        /// - `target`: Account having tokens burned
        /// </summary>
        token_burn = 2,
        
        /// <summary>
        /// >> operate_multisig
        /// Create a new multisig proposal, auto-executing if caller passes execution threshold requirements
        /// Fees are calculated using the length of the metadata and the call
        /// The proposed call's weight is used internally to charge the multisig instead of the user proposing the call
        /// - `dao_id`: Id of the dao to propose the call in
        /// - `metadata`: Arbitrary byte vec to be attached to the proposal
        /// - `fee_asset`: Token to be used by the multisig to pay for call fees
        /// - `call`: The actual call to be proposed
        /// </summary>
        operate_multisig = 3,
        
        /// <summary>
        /// >> vote_multisig
        /// Vote on an existing multisig proposal, auto-executing if caller puts vote tally past execution threshold requirements
        /// - `dao_id`: Id of the dao where the proposal is
        /// - `call_hash`: Hash of the call identifying the proposal
        /// - `aye`: Wheter or not to vote positively
        /// </summary>
        vote_multisig = 4,
        
        /// <summary>
        /// >> withdraw_vote_multisig
        /// Remove caller's vote from an existing multisig proposal
        /// - `dao_id`: Id of the dao where the proposal is
        /// - `call_hash`: Hash of the call identifying the proposal
        /// </summary>
        withdraw_vote_multisig = 5,
        
        /// <summary>
        /// >> cancel_multisig_proposal
        /// Cancel an existing multisig proposal (called by a dao origin)
        /// - `call_hash`: Hash of the call identifying the proposal
        /// </summary>
        cancel_multisig_proposal = 6,
        
        /// <summary>
        /// >> set_parameters
        /// Change dao parameters incl. voting thresholds and token freeze state (called by a dao origin)
        /// - `metadata`: Arbitrary byte vec to be attached to the dao info
        /// - `minimum_support`: Minimum amount of positive votes out of total token supply required to approve a proposal
        /// - `required_approval`: Minimum amount of positive votes out of current positive + negative votes required to approve a proposal
        /// - `frozen_tokens`: Wheter or not the dao's voting token should be transferable by the holders
        /// </summary>
        set_parameters = 9,
    }
    
    /// <summary>
    /// >> 309 - Variant[pallet_dao_manager.pallet.Call]
    /// Dispatch functions
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT12, InvArch.NetApi.Generated.Model.sp_arithmetic.per_things.Perbill, InvArch.NetApi.Generated.Model.sp_arithmetic.per_things.Perbill, InvArch.NetApi.Generated.Model.pallet_dao_manager.fee_handling.EnumFeeAsset>>(Call.create_dao);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, InvArch.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.token_mint);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, InvArch.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.token_burn);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT12>, InvArch.NetApi.Generated.Model.pallet_dao_manager.fee_handling.EnumFeeAsset, InvArch.NetApi.Generated.Model.invarch_runtime.EnumRuntimeCall>>(Call.operate_multisig);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, InvArch.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Primitive.Bool>>(Call.vote_multisig);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U32, InvArch.NetApi.Generated.Model.primitive_types.H256>>(Call.withdraw_vote_multisig);
				AddTypeDecoder<InvArch.NetApi.Generated.Model.primitive_types.H256>(Call.cancel_multisig_proposal);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT12>, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.sp_arithmetic.per_things.Perbill>, Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.sp_arithmetic.per_things.Perbill>, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.Bool>>>(Call.set_parameters);
        }
    }
}
