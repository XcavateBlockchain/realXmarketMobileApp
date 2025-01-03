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


namespace XCavatePaseo.NetApi.Generated.Model.pallet_game.types
{
    
    
    /// <summary>
    /// >> 560 - Composite[pallet_game.types.User]
    /// </summary>
    [SubstrateNodeType(TypeDefEnum.Composite)]
    public sealed class User : BaseType
    {
        
        /// <summary>
        /// >> points
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 Points { get; set; }
        /// <summary>
        /// >> wins
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 Wins { get; set; }
        /// <summary>
        /// >> losses
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 Losses { get; set; }
        /// <summary>
        /// >> practise_rounds
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U8 PractiseRounds { get; set; }
        /// <summary>
        /// >> last_played_round
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 LastPlayedRound { get; set; }
        /// <summary>
        /// >> next_token_request
        /// </summary>
        public Substrate.NetApi.Model.Types.Primitive.U32 NextTokenRequest { get; set; }
        /// <summary>
        /// >> nfts
        /// </summary>
        public XCavatePaseo.NetApi.Generated.Model.pallet_game.types.CollectedColors Nfts { get; set; }
        
        /// <inheritdoc/>
        public override string TypeName()
        {
            return "User";
        }
        
        /// <inheritdoc/>
        public override byte[] Encode()
        {
            var result = new List<byte>();
            result.AddRange(Points.Encode());
            result.AddRange(Wins.Encode());
            result.AddRange(Losses.Encode());
            result.AddRange(PractiseRounds.Encode());
            result.AddRange(LastPlayedRound.Encode());
            result.AddRange(NextTokenRequest.Encode());
            result.AddRange(Nfts.Encode());
            return result.ToArray();
        }
        
        /// <inheritdoc/>
        public override void Decode(byte[] byteArray, ref int p)
        {
            var start = p;
            Points = new Substrate.NetApi.Model.Types.Primitive.U32();
            Points.Decode(byteArray, ref p);
            Wins = new Substrate.NetApi.Model.Types.Primitive.U32();
            Wins.Decode(byteArray, ref p);
            Losses = new Substrate.NetApi.Model.Types.Primitive.U32();
            Losses.Decode(byteArray, ref p);
            PractiseRounds = new Substrate.NetApi.Model.Types.Primitive.U8();
            PractiseRounds.Decode(byteArray, ref p);
            LastPlayedRound = new Substrate.NetApi.Model.Types.Primitive.U32();
            LastPlayedRound.Decode(byteArray, ref p);
            NextTokenRequest = new Substrate.NetApi.Model.Types.Primitive.U32();
            NextTokenRequest.Decode(byteArray, ref p);
            Nfts = new XCavatePaseo.NetApi.Generated.Model.pallet_game.types.CollectedColors();
            Nfts.Decode(byteArray, ref p);
            var bytesLength = p - start;
            TypeSize = bytesLength;
            Bytes = new byte[bytesLength];
            global::System.Array.Copy(byteArray, start, Bytes, 0, bytesLength);
        }
    }
}
