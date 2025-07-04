//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Substrate.NetApi;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Meta;
using Substrate.NetApi.Model.Types;
using Substrate.NetApi.Model.Types.Base;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Polkadot.NetApi.Generated.Storage
{
    
    
    /// <summary>
    /// >> ParaInclusionStorage
    /// </summary>
    public sealed class ParaInclusionStorage
    {
        
        // Substrate client for the storage calls.
        private SubstrateClientExt _client;
        
        /// <summary>
        /// >> ParaInclusionStorage Constructor
        /// </summary>
        public ParaInclusionStorage(SubstrateClientExt client)
        {
            this._client = client;
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("ParaInclusion", "V1"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat}, typeof(Polkadot.NetApi.Generated.Model.polkadot_parachain_primitives.primitives.Id), typeof(Substrate.NetApi.Model.Types.Base.BaseVec<Polkadot.NetApi.Generated.Model.polkadot_runtime_parachains.inclusion.CandidatePendingAvailability>)));
        }
        
        /// <summary>
        /// >> V1Params
        ///  Candidates pending availability by `ParaId`. They form a chain starting from the latest
        ///  included head of the para.
        ///  Use a different prefix post-migration to v1, since the v0 `PendingAvailability` storage
        ///  would otherwise have the exact same prefix which could cause undefined behaviour when doing
        ///  the migration.
        /// </summary>
        public static string V1Params(Polkadot.NetApi.Generated.Model.polkadot_parachain_primitives.primitives.Id key)
        {
            return RequestGenerator.GetStorage("ParaInclusion", "V1", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat}, new Substrate.NetApi.Model.Types.IType[] {
                        key});
        }
        
        /// <summary>
        /// >> V1Default
        /// Default value as hex string
        /// </summary>
        public static string V1Default()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> V1
        ///  Candidates pending availability by `ParaId`. They form a chain starting from the latest
        ///  included head of the para.
        ///  Use a different prefix post-migration to v1, since the v0 `PendingAvailability` storage
        ///  would otherwise have the exact same prefix which could cause undefined behaviour when doing
        ///  the migration.
        /// </summary>
        public async Task<Substrate.NetApi.Model.Types.Base.BaseVec<Polkadot.NetApi.Generated.Model.polkadot_runtime_parachains.inclusion.CandidatePendingAvailability>> V1(Polkadot.NetApi.Generated.Model.polkadot_parachain_primitives.primitives.Id key, string blockhash, CancellationToken token)
        {
            string parameters = ParaInclusionStorage.V1Params(key);
            var result = await _client.GetStorageAsync<Substrate.NetApi.Model.Types.Base.BaseVec<Polkadot.NetApi.Generated.Model.polkadot_runtime_parachains.inclusion.CandidatePendingAvailability>>(parameters, blockhash, token);
            return result;
        }
    }
    
    /// <summary>
    /// >> ParaInclusionCalls
    /// </summary>
    public sealed class ParaInclusionCalls
    {
    }
    
    /// <summary>
    /// >> ParaInclusionConstants
    /// </summary>
    public sealed class ParaInclusionConstants
    {
    }
    
    /// <summary>
    /// >> ParaInclusionErrors
    /// </summary>
    public enum ParaInclusionErrors
    {
        
        /// <summary>
        /// >> ValidatorIndexOutOfBounds
        /// Validator index out of bounds.
        /// </summary>
        ValidatorIndexOutOfBounds,
        
        /// <summary>
        /// >> UnscheduledCandidate
        /// Candidate submitted but para not scheduled.
        /// </summary>
        UnscheduledCandidate,
        
        /// <summary>
        /// >> HeadDataTooLarge
        /// Head data exceeds the configured maximum.
        /// </summary>
        HeadDataTooLarge,
        
        /// <summary>
        /// >> PrematureCodeUpgrade
        /// Code upgrade prematurely.
        /// </summary>
        PrematureCodeUpgrade,
        
        /// <summary>
        /// >> NewCodeTooLarge
        /// Output code is too large
        /// </summary>
        NewCodeTooLarge,
        
        /// <summary>
        /// >> DisallowedRelayParent
        /// The candidate's relay-parent was not allowed. Either it was
        /// not recent enough or it didn't advance based on the last parachain block.
        /// </summary>
        DisallowedRelayParent,
        
        /// <summary>
        /// >> InvalidAssignment
        /// Failed to compute group index for the core: either it's out of bounds
        /// or the relay parent doesn't belong to the current session.
        /// </summary>
        InvalidAssignment,
        
        /// <summary>
        /// >> InvalidGroupIndex
        /// Invalid group index in core assignment.
        /// </summary>
        InvalidGroupIndex,
        
        /// <summary>
        /// >> InsufficientBacking
        /// Insufficient (non-majority) backing.
        /// </summary>
        InsufficientBacking,
        
        /// <summary>
        /// >> InvalidBacking
        /// Invalid (bad signature, unknown validator, etc.) backing.
        /// </summary>
        InvalidBacking,
        
        /// <summary>
        /// >> ValidationDataHashMismatch
        /// The validation data hash does not match expected.
        /// </summary>
        ValidationDataHashMismatch,
        
        /// <summary>
        /// >> IncorrectDownwardMessageHandling
        /// The downward message queue is not processed correctly.
        /// </summary>
        IncorrectDownwardMessageHandling,
        
        /// <summary>
        /// >> InvalidUpwardMessages
        /// At least one upward message sent does not pass the acceptance criteria.
        /// </summary>
        InvalidUpwardMessages,
        
        /// <summary>
        /// >> HrmpWatermarkMishandling
        /// The candidate didn't follow the rules of HRMP watermark advancement.
        /// </summary>
        HrmpWatermarkMishandling,
        
        /// <summary>
        /// >> InvalidOutboundHrmp
        /// The HRMP messages sent by the candidate is not valid.
        /// </summary>
        InvalidOutboundHrmp,
        
        /// <summary>
        /// >> InvalidValidationCodeHash
        /// The validation code hash of the candidate is not valid.
        /// </summary>
        InvalidValidationCodeHash,
        
        /// <summary>
        /// >> ParaHeadMismatch
        /// The `para_head` hash in the candidate descriptor doesn't match the hash of the actual
        /// para head in the commitments.
        /// </summary>
        ParaHeadMismatch,
    }
}
