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


namespace Kilt.NetApi.Generated.Storage
{
    
    
    /// <summary>
    /// >> DidStorage
    /// </summary>
    public sealed class DidStorage
    {
        
        // Substrate client for the storage calls.
        private SubstrateClientExt _client;
        
        /// <summary>
        /// >> DidStorage Constructor
        /// </summary>
        public DidStorage(SubstrateClientExt client)
        {
            this._client = client;
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("Did", "Did"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, typeof(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32), typeof(Kilt.NetApi.Generated.Model.did.did_details.DidDetails)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("Did", "ServiceEndpoints"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat,
                            Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, typeof(Substrate.NetApi.Model.Types.Base.BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Kilt.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT22>), typeof(Kilt.NetApi.Generated.Model.did.service_endpoints.DidEndpoint)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("Did", "DidEndpointsCount"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, typeof(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32), typeof(Substrate.NetApi.Model.Types.Primitive.U32)));
            _client.StorageKeyDict.Add(new System.Tuple<string, string>("Did", "DidBlacklist"), new System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>(new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                            Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, typeof(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32), typeof(Substrate.NetApi.Model.Types.Base.BaseTuple)));
        }
        
        /// <summary>
        /// >> DidParams
        ///  DIDs stored on chain.
        /// 
        ///  It maps from a DID identifier to the DID details.
        /// </summary>
        public static string DidParams(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key)
        {
            return RequestGenerator.GetStorage("Did", "Did", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, new Substrate.NetApi.Model.Types.IType[] {
                        key});
        }
        
        /// <summary>
        /// >> DidDefault
        /// Default value as hex string
        /// </summary>
        public static string DidDefault()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> Did
        ///  DIDs stored on chain.
        /// 
        ///  It maps from a DID identifier to the DID details.
        /// </summary>
        public async Task<Kilt.NetApi.Generated.Model.did.did_details.DidDetails> Did(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key, string blockhash, CancellationToken token)
        {
            string parameters = DidStorage.DidParams(key);
            var result = await _client.GetStorageAsync<Kilt.NetApi.Generated.Model.did.did_details.DidDetails>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> ServiceEndpointsParams
        ///  Service endpoints associated with DIDs.
        /// 
        ///  It maps from (DID identifier, service ID) to the service details.
        /// </summary>
        public static string ServiceEndpointsParams(Substrate.NetApi.Model.Types.Base.BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Kilt.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT22> key)
        {
            return RequestGenerator.GetStorage("Did", "ServiceEndpoints", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.Twox64Concat,
                        Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, key.Value);
        }
        
        /// <summary>
        /// >> ServiceEndpointsDefault
        /// Default value as hex string
        /// </summary>
        public static string ServiceEndpointsDefault()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> ServiceEndpoints
        ///  Service endpoints associated with DIDs.
        /// 
        ///  It maps from (DID identifier, service ID) to the service details.
        /// </summary>
        public async Task<Kilt.NetApi.Generated.Model.did.service_endpoints.DidEndpoint> ServiceEndpoints(Substrate.NetApi.Model.Types.Base.BaseTuple<Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32, Kilt.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT22> key, string blockhash, CancellationToken token)
        {
            string parameters = DidStorage.ServiceEndpointsParams(key);
            var result = await _client.GetStorageAsync<Kilt.NetApi.Generated.Model.did.service_endpoints.DidEndpoint>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> DidEndpointsCountParams
        ///  Counter of service endpoints for each DID.
        /// 
        ///  It maps from (DID identifier) to a 32-bit counter.
        /// </summary>
        public static string DidEndpointsCountParams(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key)
        {
            return RequestGenerator.GetStorage("Did", "DidEndpointsCount", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, new Substrate.NetApi.Model.Types.IType[] {
                        key});
        }
        
        /// <summary>
        /// >> DidEndpointsCountDefault
        /// Default value as hex string
        /// </summary>
        public static string DidEndpointsCountDefault()
        {
            return "0x00000000";
        }
        
        /// <summary>
        /// >> DidEndpointsCount
        ///  Counter of service endpoints for each DID.
        /// 
        ///  It maps from (DID identifier) to a 32-bit counter.
        /// </summary>
        public async Task<Substrate.NetApi.Model.Types.Primitive.U32> DidEndpointsCount(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key, string blockhash, CancellationToken token)
        {
            string parameters = DidStorage.DidEndpointsCountParams(key);
            var result = await _client.GetStorageAsync<Substrate.NetApi.Model.Types.Primitive.U32>(parameters, blockhash, token);
            return result;
        }
        
        /// <summary>
        /// >> DidBlacklistParams
        ///  The set of DIDs that have been deleted and cannot therefore be created
        ///  again for security reasons.
        /// 
        ///  It maps from a DID identifier to a unit tuple, for the sake of tracking
        ///  DID identifiers.
        /// </summary>
        public static string DidBlacklistParams(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key)
        {
            return RequestGenerator.GetStorage("Did", "DidBlacklist", Substrate.NetApi.Model.Meta.Storage.Type.Map, new Substrate.NetApi.Model.Meta.Storage.Hasher[] {
                        Substrate.NetApi.Model.Meta.Storage.Hasher.BlakeTwo128Concat}, new Substrate.NetApi.Model.Types.IType[] {
                        key});
        }
        
        /// <summary>
        /// >> DidBlacklistDefault
        /// Default value as hex string
        /// </summary>
        public static string DidBlacklistDefault()
        {
            return "0x00";
        }
        
        /// <summary>
        /// >> DidBlacklist
        ///  The set of DIDs that have been deleted and cannot therefore be created
        ///  again for security reasons.
        /// 
        ///  It maps from a DID identifier to a unit tuple, for the sake of tracking
        ///  DID identifiers.
        /// </summary>
        public async Task<Substrate.NetApi.Model.Types.Base.BaseTuple> DidBlacklist(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 key, string blockhash, CancellationToken token)
        {
            string parameters = DidStorage.DidBlacklistParams(key);
            var result = await _client.GetStorageAsync<Substrate.NetApi.Model.Types.Base.BaseTuple>(parameters, blockhash, token);
            return result;
        }
    }
    
    /// <summary>
    /// >> DidCalls
    /// </summary>
    public sealed class DidCalls
    {
        
        /// <summary>
        /// >> create
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method Create(Kilt.NetApi.Generated.Model.did.did_details.DidCreationDetails details, Kilt.NetApi.Generated.Model.did.did_details.EnumDidSignature signature)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(details.Encode());
            byteArray.AddRange(signature.Encode());
            return new Method(64, "Did", 0, "create", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> set_authentication_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method SetAuthenticationKey(Kilt.NetApi.Generated.Model.did.did_details.EnumDidVerificationKey new_key)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(new_key.Encode());
            return new Method(64, "Did", 1, "set_authentication_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> set_delegation_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method SetDelegationKey(Kilt.NetApi.Generated.Model.did.did_details.EnumDidVerificationKey new_key)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(new_key.Encode());
            return new Method(64, "Did", 2, "set_delegation_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> remove_delegation_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method RemoveDelegationKey()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(64, "Did", 3, "remove_delegation_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> set_attestation_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method SetAttestationKey(Kilt.NetApi.Generated.Model.did.did_details.EnumDidVerificationKey new_key)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(new_key.Encode());
            return new Method(64, "Did", 4, "set_attestation_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> remove_attestation_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method RemoveAttestationKey()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(64, "Did", 5, "remove_attestation_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> add_key_agreement_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method AddKeyAgreementKey(Kilt.NetApi.Generated.Model.did.did_details.EnumDidEncryptionKey new_key)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(new_key.Encode());
            return new Method(64, "Did", 6, "add_key_agreement_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> remove_key_agreement_key
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method RemoveKeyAgreementKey(Kilt.NetApi.Generated.Model.primitive_types.H256 key_id)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(key_id.Encode());
            return new Method(64, "Did", 7, "remove_key_agreement_key", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> add_service_endpoint
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method AddServiceEndpoint(Kilt.NetApi.Generated.Model.did.service_endpoints.DidEndpoint service_endpoint)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(service_endpoint.Encode());
            return new Method(64, "Did", 8, "add_service_endpoint", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> remove_service_endpoint
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method RemoveServiceEndpoint(Kilt.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT22 service_id)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(service_id.Encode());
            return new Method(64, "Did", 9, "remove_service_endpoint", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> delete
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method Delete(Substrate.NetApi.Model.Types.Primitive.U32 endpoints_to_remove)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(endpoints_to_remove.Encode());
            return new Method(64, "Did", 10, "delete", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> reclaim_deposit
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method ReclaimDeposit(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 did_subject, Substrate.NetApi.Model.Types.Primitive.U32 endpoints_to_remove)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(did_subject.Encode());
            byteArray.AddRange(endpoints_to_remove.Encode());
            return new Method(64, "Did", 11, "reclaim_deposit", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> submit_did_call
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method SubmitDidCall(Kilt.NetApi.Generated.Model.did.did_details.DidAuthorizedCallOperation did_call, Kilt.NetApi.Generated.Model.did.did_details.EnumDidSignature signature)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(did_call.Encode());
            byteArray.AddRange(signature.Encode());
            return new Method(64, "Did", 12, "submit_did_call", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> change_deposit_owner
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method ChangeDepositOwner()
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            return new Method(64, "Did", 13, "change_deposit_owner", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> update_deposit
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method UpdateDeposit(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 did)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(did.Encode());
            return new Method(64, "Did", 14, "update_deposit", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> dispatch_as
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method DispatchAs(Kilt.NetApi.Generated.Model.sp_core.crypto.AccountId32 did_identifier, Kilt.NetApi.Generated.Model.spiritnet_runtime.EnumRuntimeCall call)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(did_identifier.Encode());
            byteArray.AddRange(call.Encode());
            return new Method(64, "Did", 15, "dispatch_as", byteArray.ToArray());
        }
        
        /// <summary>
        /// >> create_from_account
        /// Contains a variant per dispatchable extrinsic that this pallet has.
        /// </summary>
        public static Method CreateFromAccount(Kilt.NetApi.Generated.Model.did.did_details.EnumDidVerificationKey authentication_key)
        {
            System.Collections.Generic.List<byte> byteArray = new List<byte>();
            byteArray.AddRange(authentication_key.Encode());
            return new Method(64, "Did", 16, "create_from_account", byteArray.ToArray());
        }
    }
    
    /// <summary>
    /// >> DidConstants
    /// </summary>
    public sealed class DidConstants
    {
        
        /// <summary>
        /// >> BaseDeposit
        ///  The amount of balance that will be taken for each DID as a deposit
        ///  to incentivise fair use of the on chain storage. The deposits
        ///  increase by the amount of used keys and service endpoints. The
        ///  deposit can be reclaimed when the DID is deleted.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U128 BaseDeposit()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U128();
            result.Create("0x00008D49FD1A07000000000000000000");
            return result;
        }
        
        /// <summary>
        /// >> ServiceEndpointDeposit
        ///  The amount of balance that will be taken for each service endpoint
        ///  as a deposit to incentivise fair use of the on chain storage. The
        ///  deposit can be reclaimed when the service endpoint is removed or the
        ///  DID deleted.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U128 ServiceEndpointDeposit()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U128();
            result.Create("0x00F024EEBDED00000000000000000000");
            return result;
        }
        
        /// <summary>
        /// >> KeyDeposit
        ///  The amount of balance that will be taken for each added key as a
        ///  deposit to incentivise fair use of the on chain storage.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U128 KeyDeposit()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U128();
            result.Create("0x00DC2074970100000000000000000000");
            return result;
        }
        
        /// <summary>
        /// >> Fee
        ///  The amount of balance that will be taken for each DID as a fee to
        ///  incentivise fair use of the on chain storage. The fee will not get
        ///  refunded when the DID is deleted.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U128 Fee()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U128();
            result.Create("0x00203D88792D00000000000000000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxPublicKeysPerDid
        ///  Maximum number of total public keys which can be stored per DID key
        ///  identifier. This includes the ones currently used for
        ///  authentication, key agreement, attestation, and delegation.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxPublicKeysPerDid()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x14000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxNewKeyAgreementKeys
        ///  Maximum number of key agreement keys that can be added in a creation
        ///  operation.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxNewKeyAgreementKeys()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x0A000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxTotalKeyAgreementKeys
        ///  Maximum number of total key agreement keys that can be stored for a
        ///  DID subject.
        /// 
        ///  Should be greater than `MaxNewKeyAgreementKeys`.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxTotalKeyAgreementKeys()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x13000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxBlocksTxValidity
        ///  The maximum number of blocks a DID-authorized operation is
        ///  considered valid after its creation.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U64 MaxBlocksTxValidity()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U64();
            result.Create("0x2C01000000000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxNumberOfServicesPerDid
        ///  The maximum number of services that can be stored under a DID.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxNumberOfServicesPerDid()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x19000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxServiceIdLength
        ///  The maximum length of a service ID.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxServiceIdLength()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x32000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxServiceTypeLength
        ///  The maximum length of a service type description.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxServiceTypeLength()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x32000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxNumberOfTypesPerService
        ///  The maximum number of a types description for a service endpoint.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxNumberOfTypesPerService()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x01000000");
            return result;
        }
        
        /// <summary>
        /// >> MaxServiceUrlLength
        ///  The maximum length of a service URL.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxServiceUrlLength()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0xD0070000");
            return result;
        }
        
        /// <summary>
        /// >> MaxNumberOfUrlsPerService
        ///  The maximum number of a URLs for a service endpoint.
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 MaxNumberOfUrlsPerService()
        {
            var result = new Substrate.NetApi.Model.Types.Primitive.U32();
            result.Create("0x02000000");
            return result;
        }
    }
    
    /// <summary>
    /// >> DidErrors
    /// </summary>
    public enum DidErrors
    {
        
        /// <summary>
        /// >> InvalidSignatureFormat
        /// The DID operation signature is not in the format the verification
        /// key expects.
        /// </summary>
        InvalidSignatureFormat,
        
        /// <summary>
        /// >> InvalidSignature
        /// The DID operation signature is invalid for the payload and the
        /// verification key provided.
        /// </summary>
        InvalidSignature,
        
        /// <summary>
        /// >> AlreadyExists
        /// The DID with the given identifier is already present on chain.
        /// </summary>
        AlreadyExists,
        
        /// <summary>
        /// >> NotFound
        /// No DID with the given identifier is present on chain.
        /// </summary>
        NotFound,
        
        /// <summary>
        /// >> VerificationKeyNotFound
        /// One or more verification keys referenced are not stored in the set
        /// of verification keys.
        /// </summary>
        VerificationKeyNotFound,
        
        /// <summary>
        /// >> InvalidNonce
        /// The DID operation nonce is not equal to the current DID nonce + 1.
        /// </summary>
        InvalidNonce,
        
        /// <summary>
        /// >> UnsupportedDidAuthorizationCall
        /// The called extrinsic does not support DID authorisation.
        /// </summary>
        UnsupportedDidAuthorizationCall,
        
        /// <summary>
        /// >> InvalidDidAuthorizationCall
        /// The call had parameters that conflicted with each other
        /// or were invalid.
        /// </summary>
        InvalidDidAuthorizationCall,
        
        /// <summary>
        /// >> MaxNewKeyAgreementKeysLimitExceeded
        /// A number of new key agreement keys greater than the maximum allowed
        /// has been provided.
        /// </summary>
        MaxNewKeyAgreementKeysLimitExceeded,
        
        /// <summary>
        /// >> MaxPublicKeysExceeded
        /// The maximum number of public keys for this DID key identifier has
        /// been reached.
        /// </summary>
        MaxPublicKeysExceeded,
        
        /// <summary>
        /// >> MaxKeyAgreementKeysExceeded
        /// The maximum number of key agreements has been reached for the DID
        /// subject.
        /// </summary>
        MaxKeyAgreementKeysExceeded,
        
        /// <summary>
        /// >> BadDidOrigin
        /// The DID call was submitted by the wrong account
        /// </summary>
        BadDidOrigin,
        
        /// <summary>
        /// >> TransactionExpired
        /// The block number provided in a DID-authorized operation is invalid.
        /// </summary>
        TransactionExpired,
        
        /// <summary>
        /// >> AlreadyDeleted
        /// The DID has already been previously deleted.
        /// </summary>
        AlreadyDeleted,
        
        /// <summary>
        /// >> NotOwnerOfDeposit
        /// Only the owner of the deposit can reclaim its reserved balance.
        /// </summary>
        NotOwnerOfDeposit,
        
        /// <summary>
        /// >> UnableToPayFees
        /// The origin is unable to reserve the deposit and pay the fee.
        /// </summary>
        UnableToPayFees,
        
        /// <summary>
        /// >> MaxNumberOfServicesExceeded
        /// The maximum number of service endpoints for a DID has been exceeded.
        /// </summary>
        MaxNumberOfServicesExceeded,
        
        /// <summary>
        /// >> MaxServiceIdLengthExceeded
        /// The service endpoint ID exceeded the maximum allowed length.
        /// </summary>
        MaxServiceIdLengthExceeded,
        
        /// <summary>
        /// >> MaxServiceTypeLengthExceeded
        /// One of the service endpoint types exceeded the maximum allowed
        /// length.
        /// </summary>
        MaxServiceTypeLengthExceeded,
        
        /// <summary>
        /// >> MaxNumberOfTypesPerServiceExceeded
        /// The maximum number of types for a service endpoint has been
        /// exceeded.
        /// </summary>
        MaxNumberOfTypesPerServiceExceeded,
        
        /// <summary>
        /// >> MaxServiceUrlLengthExceeded
        /// One of the service endpoint URLs exceeded the maximum allowed
        /// length.
        /// </summary>
        MaxServiceUrlLengthExceeded,
        
        /// <summary>
        /// >> MaxNumberOfUrlsPerServiceExceeded
        /// The maximum number of URLs for a service endpoint has been exceeded.
        /// </summary>
        MaxNumberOfUrlsPerServiceExceeded,
        
        /// <summary>
        /// >> ServiceAlreadyExists
        /// A service with the provided ID is already present for the given DID.
        /// </summary>
        ServiceAlreadyExists,
        
        /// <summary>
        /// >> ServiceNotFound
        /// A service with the provided ID is not present under the given DID.
        /// </summary>
        ServiceNotFound,
        
        /// <summary>
        /// >> InvalidServiceEncoding
        /// One of the service endpoint details contains non-ASCII characters.
        /// </summary>
        InvalidServiceEncoding,
        
        /// <summary>
        /// >> MaxStoredEndpointsCountExceeded
        /// The number of service endpoints stored under the DID is larger than
        /// the number of endpoints to delete.
        /// </summary>
        MaxStoredEndpointsCountExceeded,
        
        /// <summary>
        /// >> Internal
        /// An error that is not supposed to take place, yet it happened.
        /// </summary>
        Internal,
    }
}
