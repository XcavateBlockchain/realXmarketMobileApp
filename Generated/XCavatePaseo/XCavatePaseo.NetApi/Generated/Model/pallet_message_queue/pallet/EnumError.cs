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


namespace XCavatePaseo.NetApi.Generated.Model.pallet_message_queue.pallet
{
    
    
    /// <summary>
    /// >> Error
    /// The `Error` enum of this pallet.
    /// </summary>
    public enum Error
    {
        
        /// <summary>
        /// >> NotReapable
        /// Page is not reapable because it has items remaining to be processed and is not old
        /// enough.
        /// </summary>
        NotReapable = 0,
        
        /// <summary>
        /// >> NoPage
        /// Page to be reaped does not exist.
        /// </summary>
        NoPage = 1,
        
        /// <summary>
        /// >> NoMessage
        /// The referenced message could not be found.
        /// </summary>
        NoMessage = 2,
        
        /// <summary>
        /// >> AlreadyProcessed
        /// The message was already processed and cannot be processed again.
        /// </summary>
        AlreadyProcessed = 3,
        
        /// <summary>
        /// >> Queued
        /// The message is queued for future execution.
        /// </summary>
        Queued = 4,
        
        /// <summary>
        /// >> InsufficientWeight
        /// There is temporarily not enough weight to continue servicing messages.
        /// </summary>
        InsufficientWeight = 5,
        
        /// <summary>
        /// >> TemporarilyUnprocessable
        /// This message is temporarily unprocessable.
        /// 
        /// Such errors are expected, but not guaranteed, to resolve themselves eventually through
        /// retrying.
        /// </summary>
        TemporarilyUnprocessable = 6,
        
        /// <summary>
        /// >> QueuePaused
        /// The queue is paused and no message can be executed from it.
        /// 
        /// This can change at any time and may resolve in the future by re-trying.
        /// </summary>
        QueuePaused = 7,
        
        /// <summary>
        /// >> RecursiveDisallowed
        /// Another call is in progress and needs to finish before this call can happen.
        /// </summary>
        RecursiveDisallowed = 8,
    }
    
    /// <summary>
    /// >> 499 - Variant[pallet_message_queue.pallet.Error]
    /// The `Error` enum of this pallet.
    /// </summary>
    public sealed class EnumError : BaseEnum<Error>
    {
    }
}