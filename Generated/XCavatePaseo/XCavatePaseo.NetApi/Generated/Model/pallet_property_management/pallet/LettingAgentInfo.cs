//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Substrate.NetApi.Attributes;
using Substrate.NetApi.Model.Types.Base;
using Substrate.NetApi.Model.Types.Metadata.Base;
using System.Collections.Generic;


namespace XCavatePaseo.NetApi.Generated.Model.pallet_property_management.pallet
{
    
    
    /// <summary>
    /// >> 543 - Composite[pallet_property_management.pallet.LettingAgentInfo]
    /// </summary>
    [SubstrateNodeType(TypeDefEnum.Composite)]
    public sealed class LettingAgentInfo : BaseType
    {
        
        /// <summary>
        /// >> account
        /// </summary>
        public XCavatePaseo.NetApi.Generated.Model.sp_core.crypto.AccountId32 Account { get; set; }
        /// <summary>
        /// >> region
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 Region { get; set; }
        /// <summary>
        /// >> locations
        /// </summary>
        public XCavatePaseo.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT36 Locations { get; set; }
        /// <summary>
        /// >> assigned_properties
        /// </summary>
        public XCavatePaseo.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT37 AssignedProperties { get; set; }
        /// <summary>
        /// >> deposited
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.Bool Deposited { get; set; }
        
        /// <inheritdoc/>
        public override string TypeName()
        {
            return "LettingAgentInfo";
        }
        
        /// <inheritdoc/>
        public override byte[] Encode()
        {
            var result = new List<byte>();
            result.AddRange(Account.Encode());
            result.AddRange(Region.Encode());
            result.AddRange(Locations.Encode());
            result.AddRange(AssignedProperties.Encode());
            result.AddRange(Deposited.Encode());
            return result.ToArray();
        }
        
        /// <inheritdoc/>
        public override void Decode(byte[] byteArray, ref int p)
        {
            var start = p;
            Account = new XCavatePaseo.NetApi.Generated.Model.sp_core.crypto.AccountId32();
            Account.Decode(byteArray, ref p);
            Region = new Substrate.NetApi.Model.Types.Primitive.U32();
            Region.Decode(byteArray, ref p);
            Locations = new XCavatePaseo.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT36();
            Locations.Decode(byteArray, ref p);
            AssignedProperties = new XCavatePaseo.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT37();
            AssignedProperties.Decode(byteArray, ref p);
            Deposited = new Substrate.NetApi.Model.Types.Primitive.Bool();
            Deposited.Decode(byteArray, ref p);
            var bytesLength = p - start;
            TypeSize = bytesLength;
            Bytes = new byte[bytesLength];
            global::System.Array.Copy(byteArray, start, Bytes, 0, bytesLength);
        }
    }
}