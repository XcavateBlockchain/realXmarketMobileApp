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


namespace Hydration.NetApi.Generated.Model.pallet_uniques.pallet
{
    
    
    /// <summary>
    /// >> Call
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public enum Call
    {
        
        /// <summary>
        /// >> create
        /// Issue a new collection of non-fungible items from a public origin.
        /// 
        /// This new collection has no items initially and its owner is the origin.
        /// 
        /// The origin must conform to the configured `CreateOrigin` and have sufficient funds free.
        /// 
        /// `ItemDeposit` funds of sender are reserved.
        /// 
        /// Parameters:
        /// - `collection`: The identifier of the new collection. This must not be currently in use.
        /// - `admin`: The admin of this collection. The admin is the initial address of each
        /// member of the collection's admin team.
        /// 
        /// Emits `Created` event when successful.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        create = 0,
        
        /// <summary>
        /// >> force_create
        /// Issue a new collection of non-fungible items from a privileged origin.
        /// 
        /// This new collection has no items initially.
        /// 
        /// The origin must conform to `ForceOrigin`.
        /// 
        /// Unlike `create`, no funds are reserved.
        /// 
        /// - `collection`: The identifier of the new item. This must not be currently in use.
        /// - `owner`: The owner of this collection of items. The owner has full superuser
        ///   permissions
        /// over this item, but may later change and configure the permissions using
        /// `transfer_ownership` and `set_team`.
        /// 
        /// Emits `ForceCreated` event when successful.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        force_create = 1,
        
        /// <summary>
        /// >> destroy
        /// Destroy a collection of fungible items.
        /// 
        /// The origin must conform to `ForceOrigin` or must be `Signed` and the sender must be the
        /// owner of the `collection`.
        /// 
        /// - `collection`: The identifier of the collection to be destroyed.
        /// - `witness`: Information on the items minted in the collection. This must be
        /// correct.
        /// 
        /// Emits `Destroyed` event when successful.
        /// 
        /// Weight: `O(n + m)` where:
        /// - `n = witness.items`
        /// - `m = witness.item_metadatas`
        /// - `a = witness.attributes`
        /// </summary>
        destroy = 2,
        
        /// <summary>
        /// >> mint
        /// Mint an item of a particular collection.
        /// 
        /// The origin must be Signed and the sender must be the Issuer of the `collection`.
        /// 
        /// - `collection`: The collection of the item to be minted.
        /// - `item`: The item value of the item to be minted.
        /// - `beneficiary`: The initial owner of the minted item.
        /// 
        /// Emits `Issued` event when successful.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        mint = 3,
        
        /// <summary>
        /// >> burn
        /// Destroy a single item.
        /// 
        /// Origin must be Signed and the signing account must be either:
        /// - the Admin of the `collection`;
        /// - the Owner of the `item`;
        /// 
        /// - `collection`: The collection of the item to be burned.
        /// - `item`: The item of the item to be burned.
        /// - `check_owner`: If `Some` then the operation will fail with `WrongOwner` unless the
        ///   item is owned by this value.
        /// 
        /// Emits `Burned` with the actual amount burned.
        /// 
        /// Weight: `O(1)`
        /// Modes: `check_owner.is_some()`.
        /// </summary>
        burn = 4,
        
        /// <summary>
        /// >> transfer
        /// Move an item from the sender account to another.
        /// 
        /// This resets the approved account of the item.
        /// 
        /// Origin must be Signed and the signing account must be either:
        /// - the Admin of the `collection`;
        /// - the Owner of the `item`;
        /// - the approved delegate for the `item` (in this case, the approval is reset).
        /// 
        /// Arguments:
        /// - `collection`: The collection of the item to be transferred.
        /// - `item`: The item of the item to be transferred.
        /// - `dest`: The account to receive ownership of the item.
        /// 
        /// Emits `Transferred`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        transfer = 5,
        
        /// <summary>
        /// >> redeposit
        /// Reevaluate the deposits on some items.
        /// 
        /// Origin must be Signed and the sender should be the Owner of the `collection`.
        /// 
        /// - `collection`: The collection to be frozen.
        /// - `items`: The items of the collection whose deposits will be reevaluated.
        /// 
        /// NOTE: This exists as a best-effort function. Any items which are unknown or
        /// in the case that the owner account does not have reservable funds to pay for a
        /// deposit increase are ignored. Generally the owner isn't going to call this on items
        /// whose existing deposit is less than the refreshed deposit as it would only cost them,
        /// so it's of little consequence.
        /// 
        /// It will still return an error in the case that the collection is unknown of the signer
        /// is not permitted to call it.
        /// 
        /// Weight: `O(items.len())`
        /// </summary>
        redeposit = 6,
        
        /// <summary>
        /// >> freeze
        /// Disallow further unprivileged transfer of an item.
        /// 
        /// Origin must be Signed and the sender should be the Freezer of the `collection`.
        /// 
        /// - `collection`: The collection of the item to be frozen.
        /// - `item`: The item of the item to be frozen.
        /// 
        /// Emits `Frozen`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        freeze = 7,
        
        /// <summary>
        /// >> thaw
        /// Re-allow unprivileged transfer of an item.
        /// 
        /// Origin must be Signed and the sender should be the Freezer of the `collection`.
        /// 
        /// - `collection`: The collection of the item to be thawed.
        /// - `item`: The item of the item to be thawed.
        /// 
        /// Emits `Thawed`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        thaw = 8,
        
        /// <summary>
        /// >> freeze_collection
        /// Disallow further unprivileged transfers for a whole collection.
        /// 
        /// Origin must be Signed and the sender should be the Freezer of the `collection`.
        /// 
        /// - `collection`: The collection to be frozen.
        /// 
        /// Emits `CollectionFrozen`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        freeze_collection = 9,
        
        /// <summary>
        /// >> thaw_collection
        /// Re-allow unprivileged transfers for a whole collection.
        /// 
        /// Origin must be Signed and the sender should be the Admin of the `collection`.
        /// 
        /// - `collection`: The collection to be thawed.
        /// 
        /// Emits `CollectionThawed`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        thaw_collection = 10,
        
        /// <summary>
        /// >> transfer_ownership
        /// Change the Owner of a collection.
        /// 
        /// Origin must be Signed and the sender should be the Owner of the `collection`.
        /// 
        /// - `collection`: The collection whose owner should be changed.
        /// - `owner`: The new Owner of this collection. They must have called
        ///   `set_accept_ownership` with `collection` in order for this operation to succeed.
        /// 
        /// Emits `OwnerChanged`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        transfer_ownership = 11,
        
        /// <summary>
        /// >> set_team
        /// Change the Issuer, Admin and Freezer of a collection.
        /// 
        /// Origin must be Signed and the sender should be the Owner of the `collection`.
        /// 
        /// - `collection`: The collection whose team should be changed.
        /// - `issuer`: The new Issuer of this collection.
        /// - `admin`: The new Admin of this collection.
        /// - `freezer`: The new Freezer of this collection.
        /// 
        /// Emits `TeamChanged`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        set_team = 12,
        
        /// <summary>
        /// >> approve_transfer
        /// Approve an item to be transferred by a delegated third-party account.
        /// 
        /// The origin must conform to `ForceOrigin` or must be `Signed` and the sender must be
        /// either the owner of the `item` or the admin of the collection.
        /// 
        /// - `collection`: The collection of the item to be approved for delegated transfer.
        /// - `item`: The item of the item to be approved for delegated transfer.
        /// - `delegate`: The account to delegate permission to transfer the item.
        /// 
        /// Important NOTE: The `approved` account gets reset after each transfer.
        /// 
        /// Emits `ApprovedTransfer` on success.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        approve_transfer = 13,
        
        /// <summary>
        /// >> cancel_approval
        /// Cancel the prior approval for the transfer of an item by a delegate.
        /// 
        /// Origin must be either:
        /// - the `Force` origin;
        /// - `Signed` with the signer being the Admin of the `collection`;
        /// - `Signed` with the signer being the Owner of the `item`;
        /// 
        /// Arguments:
        /// - `collection`: The collection of the item of whose approval will be cancelled.
        /// - `item`: The item of the item of whose approval will be cancelled.
        /// - `maybe_check_delegate`: If `Some` will ensure that the given account is the one to
        ///   which permission of transfer is delegated.
        /// 
        /// Emits `ApprovalCancelled` on success.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        cancel_approval = 14,
        
        /// <summary>
        /// >> force_item_status
        /// Alter the attributes of a given item.
        /// 
        /// Origin must be `ForceOrigin`.
        /// 
        /// - `collection`: The identifier of the item.
        /// - `owner`: The new Owner of this item.
        /// - `issuer`: The new Issuer of this item.
        /// - `admin`: The new Admin of this item.
        /// - `freezer`: The new Freezer of this item.
        /// - `free_holding`: Whether a deposit is taken for holding an item of this collection.
        /// - `is_frozen`: Whether this collection is frozen except for permissioned/admin
        /// instructions.
        /// 
        /// Emits `ItemStatusChanged` with the identity of the item.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        force_item_status = 15,
        
        /// <summary>
        /// >> set_attribute
        /// Set an attribute for a collection or item.
        /// 
        /// Origin must be either `ForceOrigin` or Signed and the sender should be the Owner of the
        /// `collection`.
        /// 
        /// If the origin is Signed, then funds of signer are reserved according to the formula:
        /// `MetadataDepositBase + DepositPerByte * (key.len + value.len)` taking into
        /// account any already reserved funds.
        /// 
        /// - `collection`: The identifier of the collection whose item's metadata to set.
        /// - `maybe_item`: The identifier of the item whose metadata to set.
        /// - `key`: The key of the attribute.
        /// - `value`: The value to which to set the attribute.
        /// 
        /// Emits `AttributeSet`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        set_attribute = 16,
        
        /// <summary>
        /// >> clear_attribute
        /// Clear an attribute for a collection or item.
        /// 
        /// Origin must be either `ForceOrigin` or Signed and the sender should be the Owner of the
        /// `collection`.
        /// 
        /// Any deposit is freed for the collection's owner.
        /// 
        /// - `collection`: The identifier of the collection whose item's metadata to clear.
        /// - `maybe_item`: The identifier of the item whose metadata to clear.
        /// - `key`: The key of the attribute.
        /// 
        /// Emits `AttributeCleared`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        clear_attribute = 17,
        
        /// <summary>
        /// >> set_metadata
        /// Set the metadata for an item.
        /// 
        /// Origin must be either `ForceOrigin` or Signed and the sender should be the Owner of the
        /// `collection`.
        /// 
        /// If the origin is Signed, then funds of signer are reserved according to the formula:
        /// `MetadataDepositBase + DepositPerByte * data.len` taking into
        /// account any already reserved funds.
        /// 
        /// - `collection`: The identifier of the collection whose item's metadata to set.
        /// - `item`: The identifier of the item whose metadata to set.
        /// - `data`: The general information of this item. Limited in length by `StringLimit`.
        /// - `is_frozen`: Whether the metadata should be frozen against further changes.
        /// 
        /// Emits `MetadataSet`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        set_metadata = 18,
        
        /// <summary>
        /// >> clear_metadata
        /// Clear the metadata for an item.
        /// 
        /// Origin must be either `ForceOrigin` or Signed and the sender should be the Owner of the
        /// `item`.
        /// 
        /// Any deposit is freed for the collection's owner.
        /// 
        /// - `collection`: The identifier of the collection whose item's metadata to clear.
        /// - `item`: The identifier of the item whose metadata to clear.
        /// 
        /// Emits `MetadataCleared`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        clear_metadata = 19,
        
        /// <summary>
        /// >> set_collection_metadata
        /// Set the metadata for a collection.
        /// 
        /// Origin must be either `ForceOrigin` or `Signed` and the sender should be the Owner of
        /// the `collection`.
        /// 
        /// If the origin is `Signed`, then funds of signer are reserved according to the formula:
        /// `MetadataDepositBase + DepositPerByte * data.len` taking into
        /// account any already reserved funds.
        /// 
        /// - `collection`: The identifier of the item whose metadata to update.
        /// - `data`: The general information of this item. Limited in length by `StringLimit`.
        /// - `is_frozen`: Whether the metadata should be frozen against further changes.
        /// 
        /// Emits `CollectionMetadataSet`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        set_collection_metadata = 20,
        
        /// <summary>
        /// >> clear_collection_metadata
        /// Clear the metadata for a collection.
        /// 
        /// Origin must be either `ForceOrigin` or `Signed` and the sender should be the Owner of
        /// the `collection`.
        /// 
        /// Any deposit is freed for the collection's owner.
        /// 
        /// - `collection`: The identifier of the collection whose metadata to clear.
        /// 
        /// Emits `CollectionMetadataCleared`.
        /// 
        /// Weight: `O(1)`
        /// </summary>
        clear_collection_metadata = 21,
        
        /// <summary>
        /// >> set_accept_ownership
        /// Set (or reset) the acceptance of ownership for a particular account.
        /// 
        /// Origin must be `Signed` and if `maybe_collection` is `Some`, then the signer must have a
        /// provider reference.
        /// 
        /// - `maybe_collection`: The identifier of the collection whose ownership the signer is
        ///   willing to accept, or if `None`, an indication that the signer is willing to accept no
        ///   ownership transferal.
        /// 
        /// Emits `OwnershipAcceptanceChanged`.
        /// </summary>
        set_accept_ownership = 22,
        
        /// <summary>
        /// >> set_collection_max_supply
        /// Set the maximum amount of items a collection could have.
        /// 
        /// Origin must be either `ForceOrigin` or `Signed` and the sender should be the Owner of
        /// the `collection`.
        /// 
        /// Note: This function can only succeed once per collection.
        /// 
        /// - `collection`: The identifier of the collection to change.
        /// - `max_supply`: The maximum amount of items a collection could have.
        /// 
        /// Emits `CollectionMaxSupplySet` event when successful.
        /// </summary>
        set_collection_max_supply = 23,
        
        /// <summary>
        /// >> set_price
        /// Set (or reset) the price for an item.
        /// 
        /// Origin must be Signed and must be the owner of the asset `item`.
        /// 
        /// - `collection`: The collection of the item.
        /// - `item`: The item to set the price for.
        /// - `price`: The price for the item. Pass `None`, to reset the price.
        /// - `buyer`: Restricts the buy operation to a specific account.
        /// 
        /// Emits `ItemPriceSet` on success if the price is not `None`.
        /// Emits `ItemPriceRemoved` on success if the price is `None`.
        /// </summary>
        set_price = 24,
        
        /// <summary>
        /// >> buy_item
        /// Allows to buy an item if it's up for sale.
        /// 
        /// Origin must be Signed and must not be the owner of the `item`.
        /// 
        /// - `collection`: The collection of the item.
        /// - `item`: The item the sender wants to buy.
        /// - `bid_price`: The price the sender is willing to pay.
        /// 
        /// Emits `ItemBought` on success.
        /// </summary>
        buy_item = 25,
    }
    
    /// <summary>
    /// >> 177 - Variant[pallet_uniques.pallet.Call]
    /// Contains a variant per dispatchable extrinsic that this pallet has.
    /// </summary>
    public sealed class EnumCall : BaseEnumRust<Call>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumCall()
        {
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.create);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Substrate.NetApi.Model.Types.Primitive.Bool>>(Call.force_create);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.pallet_uniques.types.DestroyWitness>>(Call.destroy);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.mint);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseOpt<Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>>(Call.burn);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.transfer);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseVec<Substrate.NetApi.Model.Types.Primitive.U128>>>(Call.redeposit);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128>>(Call.freeze);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128>>(Call.thaw);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U128>(Call.freeze_collection);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U128>(Call.thaw_collection);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.transfer_ownership);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.set_team);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>(Call.approve_transfer);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseOpt<Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>>(Call.cancel_approval);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32, Substrate.NetApi.Model.Types.Primitive.Bool, Substrate.NetApi.Model.Types.Primitive.Bool>>(Call.force_item_status);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U128>, Hydration.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT3, Hydration.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT4>>(Call.set_attribute);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U128>, Hydration.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT3>>(Call.clear_attribute);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2, Substrate.NetApi.Model.Types.Primitive.Bool>>(Call.set_metadata);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128>>(Call.clear_metadata);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Hydration.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2, Substrate.NetApi.Model.Types.Primitive.Bool>>(Call.set_collection_metadata);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Primitive.U128>(Call.clear_collection_metadata);
				AddTypeDecoder<Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U128>>(Call.set_accept_ownership);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U32>>(Call.set_collection_max_supply);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Base.BaseOpt<Substrate.NetApi.Model.Types.Primitive.U128>, Substrate.NetApi.Model.Types.Base.BaseOpt<Hydration.NetApi.Generated.Model.sp_core.crypto.AccountId32>>>(Call.set_price);
				AddTypeDecoder<BaseTuple<Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128, Substrate.NetApi.Model.Types.Primitive.U128>>(Call.buy_item);
        }
    }
}
