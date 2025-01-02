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


namespace InvArch.NetApi.Generated.Model.pallet_dao_staking.pallet
{
    
    
    /// <summary>
    /// >> Error
    /// The `Error` enum of this pallet.
    /// </summary>
    public enum Error
    {
        
        /// <summary>
        /// >> StakingNothing
        /// Staking nothing.
        /// </summary>
        StakingNothing = 0,
        
        /// <summary>
        /// >> InsufficientBalance
        /// Attempted to stake less than the minimum amount.
        /// </summary>
        InsufficientBalance = 1,
        
        /// <summary>
        /// >> MaxStakersReached
        /// Maximum number of stakers reached.
        /// </summary>
        MaxStakersReached = 2,
        
        /// <summary>
        /// >> DaoNotFound
        /// DAO not found.
        /// </summary>
        DaoNotFound = 3,
        
        /// <summary>
        /// >> NoStakeAvailable
        /// No stake available for withdrawal.
        /// </summary>
        NoStakeAvailable = 4,
        
        /// <summary>
        /// >> UnclaimedRewardsAvailable
        /// Unclaimed rewards available.
        /// </summary>
        UnclaimedRewardsAvailable = 5,
        
        /// <summary>
        /// >> UnstakingNothing
        /// Unstaking nothing.
        /// </summary>
        UnstakingNothing = 6,
        
        /// <summary>
        /// >> NothingToWithdraw
        /// Nothing available for withdrawal.
        /// </summary>
        NothingToWithdraw = 7,
        
        /// <summary>
        /// >> DaoAlreadyRegistered
        /// DAO already registered.
        /// </summary>
        DaoAlreadyRegistered = 8,
        
        /// <summary>
        /// >> UnknownEraReward
        /// Unknown rewards for era.
        /// </summary>
        UnknownEraReward = 9,
        
        /// <summary>
        /// >> UnexpectedStakeInfoEra
        /// Unexpected stake info for era.
        /// </summary>
        UnexpectedStakeInfoEra = 10,
        
        /// <summary>
        /// >> TooManyUnlockingChunks
        /// Too many unlocking chunks.
        /// </summary>
        TooManyUnlockingChunks = 11,
        
        /// <summary>
        /// >> RewardAlreadyClaimed
        /// Reward already claimed.
        /// </summary>
        RewardAlreadyClaimed = 12,
        
        /// <summary>
        /// >> IncorrectEra
        /// Incorrect era.
        /// </summary>
        IncorrectEra = 13,
        
        /// <summary>
        /// >> TooManyEraStakeValues
        /// Too many era stake values.
        /// </summary>
        TooManyEraStakeValues = 14,
        
        /// <summary>
        /// >> NotAStaker
        /// Not a staker.
        /// </summary>
        NotAStaker = 15,
        
        /// <summary>
        /// >> NoPermission
        /// No permission.
        /// </summary>
        NoPermission = 16,
        
        /// <summary>
        /// >> MaxNameExceeded
        /// Name exceeds maximum length.
        /// </summary>
        MaxNameExceeded = 17,
        
        /// <summary>
        /// >> MaxDescriptionExceeded
        /// Description exceeds maximum length.
        /// </summary>
        MaxDescriptionExceeded = 18,
        
        /// <summary>
        /// >> MaxImageExceeded
        /// Image URL exceeds maximum length.
        /// </summary>
        MaxImageExceeded = 19,
        
        /// <summary>
        /// >> NotRegistered
        /// DAO not registered.
        /// </summary>
        NotRegistered = 20,
        
        /// <summary>
        /// >> Halted
        /// Halted.
        /// </summary>
        Halted = 21,
        
        /// <summary>
        /// >> NoHaltChange
        /// No halt change.
        /// </summary>
        NoHaltChange = 22,
        
        /// <summary>
        /// >> MoveStakeToSameDao
        /// Attempted to move stake to the same dao.
        /// </summary>
        MoveStakeToSameDao = 23,
    }
    
    /// <summary>
    /// >> 511 - Variant[pallet_dao_staking.pallet.Error]
    /// The `Error` enum of this pallet.
    /// </summary>
    public sealed class EnumError : BaseEnum<Error>
    {
    }
}