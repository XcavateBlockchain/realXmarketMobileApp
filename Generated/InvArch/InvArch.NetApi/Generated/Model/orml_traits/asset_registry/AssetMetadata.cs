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


namespace InvArch.NetApi.Generated.Model.orml_traits.asset_registry
{
    
    
    /// <summary>
    /// >> 49 - Composite[orml_traits.asset_registry.AssetMetadata]
    /// </summary>
    [SubstrateNodeType(TypeDefEnum.Composite)]
    public sealed class AssetMetadata : BaseType
    {
        
        /// <summary>
        /// >> decimals
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 Decimals { get; set; }
        /// <summary>
        /// >> name
        /// </summary>
        public InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2 Name { get; set; }
        /// <summary>
        /// >> symbol
        /// </summary>
        public InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2 Symbol { get; set; }
        /// <summary>
        /// >> existential_deposit
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U128 ExistentialDeposit { get; set; }
        /// <summary>
        /// >> location
        /// </summary>
        public Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.xcm.EnumVersionedLocation> Location { get; set; }
        /// <summary>
        /// >> additional
        /// </summary>
        public InvArch.NetApi.Generated.Model.invarch_runtime.assets.CustomMetadata Additional { get; set; }
        
        /// <inheritdoc/>
        public override string TypeName()
        {
            return "AssetMetadata";
        }
        
        /// <inheritdoc/>
        public override byte[] Encode()
        {
            var result = new List<byte>();
            result.AddRange(Decimals.Encode());
            result.AddRange(Name.Encode());
            result.AddRange(Symbol.Encode());
            result.AddRange(ExistentialDeposit.Encode());
            result.AddRange(Location.Encode());
            result.AddRange(Additional.Encode());
            return result.ToArray();
        }
        
        /// <inheritdoc/>
        public override void Decode(byte[] byteArray, ref int p)
        {
            var start = p;
            Decimals = new Substrate.NetApi.Model.Types.Primitive.U32();
            Decimals.Decode(byteArray, ref p);
            Name = new InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2();
            Name.Decode(byteArray, ref p);
            Symbol = new InvArch.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT2();
            Symbol.Decode(byteArray, ref p);
            ExistentialDeposit = new Substrate.NetApi.Model.Types.Primitive.U128();
            ExistentialDeposit.Decode(byteArray, ref p);
            Location = new Substrate.NetApi.Model.Types.Base.BaseOpt<InvArch.NetApi.Generated.Model.xcm.EnumVersionedLocation>();
            Location.Decode(byteArray, ref p);
            Additional = new InvArch.NetApi.Generated.Model.invarch_runtime.assets.CustomMetadata();
            Additional.Decode(byteArray, ref p);
            var bytesLength = p - start;
            TypeSize = bytesLength;
            Bytes = new byte[bytesLength];
            global::System.Array.Copy(byteArray, start, Bytes, 0, bytesLength);
        }
    }
}