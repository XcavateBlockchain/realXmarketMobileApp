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


namespace Bajun.NetApi.Generated.Model.pallet_ajuna_awesome_avatars.types.season
{
    
    
    /// <summary>
    /// >> 162 - Composite[pallet_ajuna_awesome_avatars.types.season.SeasonMeta]
    /// </summary>
    [SubstrateNodeType(TypeDefEnum.Composite)]
    public sealed class SeasonMeta : BaseType
    {
        
        /// <summary>
        /// >> name
        /// </summary>
        public Bajun.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT7 Name { get; set; }
        /// <summary>
        /// >> description
        /// </summary>
        public Bajun.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT8 Description { get; set; }
        
        /// <inheritdoc/>
        public override string TypeName()
        {
            return "SeasonMeta";
        }
        
        /// <inheritdoc/>
        public override byte[] Encode()
        {
            var result = new List<byte>();
            result.AddRange(Name.Encode());
            result.AddRange(Description.Encode());
            return result.ToArray();
        }
        
        /// <inheritdoc/>
        public override void Decode(byte[] byteArray, ref int p)
        {
            var start = p;
            Name = new Bajun.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT7();
            Name.Decode(byteArray, ref p);
            Description = new Bajun.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT8();
            Description.Decode(byteArray, ref p);
            var bytesLength = p - start;
            TypeSize = bytesLength;
            Bytes = new byte[bytesLength];
            global::System.Array.Copy(byteArray, start, Bytes, 0, bytesLength);
        }
    }
}