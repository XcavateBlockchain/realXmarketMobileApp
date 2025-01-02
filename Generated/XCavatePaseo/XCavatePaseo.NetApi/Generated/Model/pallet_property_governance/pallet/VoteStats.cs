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


namespace XCavatePaseo.NetApi.Generated.Model.pallet_property_governance.pallet
{
    
    
    /// <summary>
    /// >> 552 - Composite[pallet_property_governance.pallet.VoteStats]
    /// </summary>
    [SubstrateNodeType(TypeDefEnum.Composite)]
    public sealed class VoteStats : BaseType
    {
        
        /// <summary>
        /// >> yes_voting_power
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 YesVotingPower { get; set; }
        /// <summary>
        /// >> no_voting_power
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 NoVotingPower { get; set; }
        
        /// <inheritdoc/>
        public override string TypeName()
        {
            return "VoteStats";
        }
        
        /// <inheritdoc/>
        public override byte[] Encode()
        {
            var result = new List<byte>();
            result.AddRange(YesVotingPower.Encode());
            result.AddRange(NoVotingPower.Encode());
            return result.ToArray();
        }
        
        /// <inheritdoc/>
        public override void Decode(byte[] byteArray, ref int p)
        {
            var start = p;
            YesVotingPower = new Substrate.NetApi.Model.Types.Primitive.U32();
            YesVotingPower.Decode(byteArray, ref p);
            NoVotingPower = new Substrate.NetApi.Model.Types.Primitive.U32();
            NoVotingPower.Decode(byteArray, ref p);
            var bytesLength = p - start;
            TypeSize = bytesLength;
            Bytes = new byte[bytesLength];
            global::System.Array.Copy(byteArray, start, Bytes, 0, bytesLength);
        }
    }
}