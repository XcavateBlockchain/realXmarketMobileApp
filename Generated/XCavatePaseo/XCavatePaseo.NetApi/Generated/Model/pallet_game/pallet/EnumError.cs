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


namespace XCavatePaseo.NetApi.Generated.Model.pallet_game.pallet
{
    
    
    /// <summary>
    /// >> Error
    /// The `Error` enum of this pallet.
    /// </summary>
    public enum Error
    {
        
        /// <summary>
        /// >> NotEnoughPoints
        /// A player has not enough points to play.
        /// </summary>
        NotEnoughPoints = 0,
        
        /// <summary>
        /// >> ConversionError
        /// </summary>
        ConversionError = 1,
        
        /// <summary>
        /// >> ArithmeticOverflow
        /// </summary>
        ArithmeticOverflow = 2,
        
        /// <summary>
        /// >> ArithmeticUnderflow
        /// </summary>
        ArithmeticUnderflow = 3,
        
        /// <summary>
        /// >> MultiplyError
        /// </summary>
        MultiplyError = 4,
        
        /// <summary>
        /// >> DivisionError
        /// </summary>
        DivisionError = 5,
        
        /// <summary>
        /// >> TooManyGames
        /// There are too many games active.
        /// </summary>
        TooManyGames = 6,
        
        /// <summary>
        /// >> NoThePlayer
        /// The caller has no permission.
        /// </summary>
        NoThePlayer = 7,
        
        /// <summary>
        /// >> NoActiveGame
        /// This is not an active game.
        /// </summary>
        NoActiveGame = 8,
        
        /// <summary>
        /// >> NoPermission
        /// </summary>
        NoPermission = 9,
        
        /// <summary>
        /// >> ListingDoesNotExist
        /// This listing is not listed.
        /// </summary>
        ListingDoesNotExist = 10,
        
        /// <summary>
        /// >> OfferDoesNotExist
        /// This offer does not exist.
        /// </summary>
        OfferDoesNotExist = 11,
        
        /// <summary>
        /// >> TooManyTest
        /// There are too many test properties.
        /// </summary>
        TooManyTest = 12,
        
        /// <summary>
        /// >> NoProperty
        /// No property could be found.
        /// </summary>
        NoProperty = 13,
        
        /// <summary>
        /// >> UserNotRegistered
        /// The user has not yet been registered.
        /// </summary>
        UserNotRegistered = 14,
        
        /// <summary>
        /// >> TooManyPractise
        /// The user has already made 5 practise rounds.
        /// </summary>
        TooManyPractise = 15,
        
        /// <summary>
        /// >> NoPractise
        /// The user has not yet made a practise round.
        /// </summary>
        NoPractise = 16,
        
        /// <summary>
        /// >> InvalidIndex
        /// </summary>
        InvalidIndex = 17,
        
        /// <summary>
        /// >> CollectionUnknown
        /// The color for this collection is not known.
        /// </summary>
        CollectionUnknown = 18,
        
        /// <summary>
        /// >> NoActiveRound
        /// There is no active round at the moment.
        /// </summary>
        NoActiveRound = 19,
        
        /// <summary>
        /// >> PlayerAlreadyRegistered
        /// The player is already registered.
        /// </summary>
        PlayerAlreadyRegistered = 20,
        
        /// <summary>
        /// >> AccountAlreadyAdmin
        /// The account is already an admin.
        /// </summary>
        AccountAlreadyAdmin = 21,
        
        /// <summary>
        /// >> NotAdmin
        /// This account is not an admin.
        /// </summary>
        NotAdmin = 22,
        
        /// <summary>
        /// >> TooManyAdmins
        /// There are already enough admins.
        /// </summary>
        TooManyAdmins = 23,
        
        /// <summary>
        /// >> CantRequestToken
        /// The user has to wait to request token.
        /// </summary>
        CantRequestToken = 24,
        
        /// <summary>
        /// >> NoGuess
        /// There has been no guess from the player.
        /// </summary>
        NoGuess = 25,
        
        /// <summary>
        /// >> NoPropertiesAvailable
        /// There are no properties available.
        /// </summary>
        NoPropertiesAvailable = 26,
    }
    
    /// <summary>
    /// >> 568 - Variant[pallet_game.pallet.Error]
    /// The `Error` enum of this pallet.
    /// </summary>
    public sealed class EnumError : BaseEnum<Error>
    {
    }
}