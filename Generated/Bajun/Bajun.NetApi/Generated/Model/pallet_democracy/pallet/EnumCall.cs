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


namespace Bajun.NetApi.Generated.Model.pallet_democracy.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> propose
        /// Propose a sensitive action to be taken.
        /// 
        /// The dispatch origin of this call must be _Signed_ and the sender must
        /// have funds to cover the deposit.
        /// 
        /// - `proposal_hash`: The hash of the proposal preimage.
        /// - `value`: The amount of deposit (must be at least `MinimumDeposit`).
        /// 
        /// Emits `Proposed`.
        /// </summary>
        propose = 0,
        
        /// <summary>
        /// >> second
        /// Signals agreement with a particular proposal.
        /// 
        /// The dispatch origin of this call must be _Signed_ and the sender
        /// must have funds to cover the deposit, equal to the original deposit.
        /// 
        /// - `proposal`: The index of the proposal to second.
        /// </summary>
        second = 1,
        
        /// <summary>
        /// >> vote
        /// Vote in a referendum. If `vote.is_aye()`, the vote is to enact the proposal;
        /// otherwise it is a vote to keep the status quo.
        /// 
        /// The dispatch origin of this call must be _Signed_.
        /// 
        /// - `ref_index`: The index of the referendum to vote for.
        /// - `vote`: The vote configuration.
        /// </summary>
        vote = 2,
        
        /// <summary>
        /// >> emergency_cancel
        /// Schedule an emergency cancellation of a referendum. Cannot happen twice to the same
        /// referendum.
        /// 
        /// The dispatch origin of this call must be `CancellationOrigin`.
        /// 
        /// -`ref_index`: The index of the referendum to cancel.
        /// 
        /// Weight: `O(1)`.
        /// </summary>
        emergency_cancel = 3,
        
        /// <summary>
        /// >> external_propose
        /// Schedule a referendum to be tabled once it is legal to schedule an external
        /// referendum.
        /// 
        /// The dispatch origin of this call must be `ExternalOrigin`.
        /// 
        /// - `proposal_hash`: The preimage hash of the proposal.
        /// </summary>
        external_propose = 4,
        
        /// <summary>
        /// >> external_propose_majority
        /// Schedule a majority-carries referendum to be tabled next once it is legal to schedule
        /// an external referendum.
        /// 
        /// The dispatch of this call must be `ExternalMajorityOrigin`.
        /// 
        /// - `proposal_hash`: The preimage hash of the proposal.
        /// 
        /// Unlike `external_propose`, blacklisting has no effect on this and it may replace a
        /// pre-scheduled `external_propose` call.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        external_propose_majority = 5,
        
        /// <summary>
        /// >> external_propose_default
        /// Schedule a negative-turnout-bias referendum to be tabled next once it is legal to
        /// schedule an external referendum.
        /// 
        /// The dispatch of this call must be `ExternalDefaultOrigin`.
        /// 
        /// - `proposal_hash`: The preimage hash of the proposal.
        /// 
        /// Unlike `external_propose`, blacklisting has no effect on this and it may replace a
        /// pre-scheduled `external_propose` call.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        external_propose_default = 6,
        
        /// <summary>
        /// >> fast_track
        /// Schedule the currently externally-proposed majority-carries referendum to be tabled
        /// immediately. If there is no externally-proposed referendum currently, or if there is one
        /// but it is not a majority-carries referendum then it fails.
        /// 
        /// The dispatch of this call must be `FastTrackOrigin`.
        /// 
        /// - `proposal_hash`: The hash of the current external proposal.
        /// - `voting_period`: The period that is allowed for voting on this proposal. Increased to
        /// 	Must be always greater than zero.
        /// 	For `FastTrackOrigin` must be equal or greater than `FastTrackVotingPeriod`.
        /// - `delay`: The number of block after voting has ended in approval and this should be
        ///   enacted. This doesn't have a minimum amount.
        /// 
        /// Emits `Started`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        fast_track = 7,
        
        /// <summary>
        /// >> veto_external
        /// Veto and blacklist the external proposal hash.
        /// 
        /// The dispatch origin of this call must be `VetoOrigin`.
        /// 
        /// - `proposal_hash`: The preimage hash of the proposal to veto and blacklist.
        /// 
        /// Emits `Vetoed`.
        /// 
        /// Weight: `O(V + log(V))` where V is number of `existing vetoers`
        /// </summary>
        veto_external = 8,
        
        /// <summary>
        /// >> cancel_referendum
        /// Remove a referendum.
        /// 
        /// The dispatch origin of this call must be _Root_.
        /// 
        /// - `ref_index`: The index of the referendum to cancel.
        /// 
        /// # Weight: `O(1)`.
        /// </summary>
        cancel_referendum = 9,
        
        /// <summary>
        /// >> delegate
        /// Delegate the voting power (with some given conviction) of the sending account.
        /// 
        /// The balance delegated is locked for as long as it's delegated, and thereafter for the
        /// time appropriate for the conviction's lock period.
        /// 
        /// The dispatch origin of this call must be _Signed_, and the signing account must either:
        ///   - be delegating already; or
        ///   - have no voting activity (if there is, then it will need to be removed/consolidated
        ///     through `reap_vote` or `unvote`).
        /// 
        /// - `to`: The account whose voting the `target` account's voting power will follow.
        /// - `conviction`: The conviction that will be attached to the delegated votes. When the
        ///   account is undelegated, the funds will be locked for the corresponding period.
        /// - `balance`: The amount of the account's balance to be used in delegating. This must not
        ///   be more than the account's current balance.
        /// 
        /// Emits `Delegated`.
        /// 
        /// Weight: `O(R)` where R is the number of referendums the voter delegating to has
        ///   voted on. Weight is charged as if maximum votes.
        /// </summary>
        @delegate = 10,
        
        /// <summary>
        /// >> undelegate
        /// Undelegate the voting power of the sending account.
        /// 
        /// Tokens may be unlocked following once an amount of time consistent with the lock period
        /// of the conviction with which the delegation was issued.
        /// 
        /// The dispatch origin of this call must be _Signed_ and the signing account must be
        /// currently delegating.
        /// 
        /// Emits `Undelegated`.
        /// 
        /// Weight: `O(R)` where R is the number of referendums the voter delegating to has
        ///   voted on. Weight is charged as if maximum votes.
        /// </summary>
        undelegate = 11,
        
        /// <summary>
        /// >> clear_public_proposals
        /// Clears all public proposals.
        /// 
        /// The dispatch origin of this call must be _Root_.
        /// 
        /// Weight: `O(1)`.
        /// </summary>
        clear_public_proposals = 12,
        
        /// <summary>
        /// >> unlock
        /// Unlock tokens that have an expired lock.
        /// 
        /// The dispatch origin of this call must be _Signed_.
        /// 
        /// - `target`: The account to remove the lock on.
        /// 
        /// Weight: `O(R)` with R number of vote of target.
        /// </summary>
        unlock = 13,
        
        /// <summary>
        /// >> remove_vote
        /// Remove a vote for a referendum.
        /// 
        /// If:
        /// - the referendum was cancelled, or
        /// - the referendum is ongoing, or
        /// - the referendum has ended such that
        ///   - the vote of the account was in opposition to the result; or
        ///   - there was no conviction to the account's vote; or
        ///   - the account made a split vote
        /// ...then the vote is removed cleanly and a following call to `unlock` may result in more
        /// funds being available.
        /// 
        /// If, however, the referendum has ended and:
        /// - it finished corresponding to the vote of the account, and
        /// - the account made a standard vote with conviction, and
        /// - the lock period of the conviction is not over
        /// ...then the lock will be aggregated into the overall account's lock, which may involve
        /// *overlocking* (where the two locks are combined into a single lock that is the maximum
        /// of both the amount locked and the time is it locked for).
        /// 
        /// The dispatch origin of this call must be _Signed_, and the signer must have a vote
        /// registered for referendum `index`.
        /// 
        /// - `index`: The index of referendum of the vote to be removed.
        /// 
        /// Weight: `O(R + log R)` where R is the number of referenda that `target` has voted on.
        ///   Weight is calculated for the maximum number of vote.
        /// </summary>
        remove_vote = 14,
        
        /// <summary>
        /// >> remove_other_vote
        /// Remove a vote for a referendum.
        /// 
        /// If the `target` is equal to the signer, then this function is exactly equivalent to
        /// `remove_vote`. If not equal to the signer, then the vote must have expired,
        /// either because the referendum was cancelled, because the voter lost the referendum or
        /// because the conviction period is over.
        /// 
        /// The dispatch origin of this call must be _Signed_.
        /// 
        /// - `target`: The account of the vote to be removed; this account must have voted for
        ///   referendum `index`.
        /// - `index`: The index of referendum of the vote to be removed.
        /// 
        /// Weight: `O(R + log R)` where R is the number of referenda that `target` has voted on.
        ///   Weight is calculated for the maximum number of vote.
        /// </summary>
        remove_other_vote = 15,
        
        /// <summary>
        /// >> blacklist
        /// Permanently place a proposal into the blacklist. This prevents it from ever being
        /// proposed again.
        /// 
        /// If called on a queued public or external proposal, then this will result in it being
        /// removed. If the `ref_index` supplied is an active referendum with the proposal hash,
        /// then it will be cancelled.
        /// 
        /// The dispatch origin of this call must be `BlacklistOrigin`.
        /// 
        /// - `proposal_hash`: The proposal hash to blacklist permanently.
        /// - `ref_index`: An ongoing referendum whose hash is `proposal_hash`, which will be
        /// cancelled.
        /// 
        /// Weight: `O(p)` (though as this is an high-privilege dispatch, we assume it has a
        ///   reasonable value).
        /// </summary>
        blacklist = 16,
        
        /// <summary>
        /// >> cancel_proposal
        /// Remove a proposal.
        /// 
        /// The dispatch origin of this call must be `CancelProposalOrigin`.
        /// 
        /// - `prop_index`: The index of the proposal to cancel.
        /// 
        /// Weight: `O(p)` where `p = PublicProps::<T>::decode_len()`
        /// </summary>
        cancel_proposal = 17,
        
        /// <summary>
        /// >> set_metadata
        /// Set or clear a metadata of a proposal or a referendum.
        /// 
        /// Parameters:
        /// - `origin`: Must correspond to the `MetadataOwner`.
        ///     - `ExternalOrigin` for an external proposal with the `SuperMajorityApprove`
        ///       threshold.
        ///     - `ExternalDefaultOrigin` for an external proposal with the `SuperMajorityAgainst`
        ///       threshold.
        ///     - `ExternalMajorityOrigin` for an external proposal with the `SimpleMajority`
        ///       threshold.
        ///     - `Signed` by a creator for a public proposal.
        ///     - `Signed` to clear a metadata for a finished referendum.
        ///     - `Root` to set a metadata for an ongoing referendum.
        /// - `owner`: an identifier of a metadata owner.
        /// - `maybe_hash`: The hash of an on-chain stored preimage. `None` to clear a metadata.
        /// </summary>
        set_metadata = 18,
    }
    
    /// <summary>
    /// >> 402 - Variant[pallet_democracy.pallet.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.frame_support.traits.preimages.EnumBounded, Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.propose);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>(Call.second);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>, Bajun.NetApi.Generated.Model.pallet_democracy.vote.EnumAccountVote>>(Call.vote);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U32>(Call.emergency_cancel);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.frame_support.traits.preimages.EnumBounded>(Call.external_propose);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.frame_support.traits.preimages.EnumBounded>(Call.external_propose_majority);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.frame_support.traits.preimages.EnumBounded>(Call.external_propose_default);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Primitive.U32, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.fast_track);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.primitive_types.H256>(Call.veto_external);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>(Call.cancel_referendum);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Bajun.NetApi.Generated.Model.pallet_democracy.conviction.EnumConviction, Substrate.NetApi.Model.Types.Primitive.U128>>(Call.@delegate);
				AddTypeDecoder<BaseVoid>(Call.undelegate);
				AddTypeDecoder<BaseVoid>(Call.clear_public_proposals);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress>(Call.unlock);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U32>(Call.remove_vote);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.sp_runtime.multiaddress.EnumMultiAddress, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.remove_other_vote);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U32>>>(Call.blacklist);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseCom<Substrate.NetApi.Model.Types.Primitive.U32>>(Call.cancel_proposal);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.pallet_democracy.types.EnumMetadataOwner, Substrate.NetApi.Model.Types.Base.BaseOpt<Bajun.NetApi.Generated.Model.primitive_types.H256>>>(Call.set_metadata);
        }
    }
}