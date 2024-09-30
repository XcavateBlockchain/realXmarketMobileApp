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


namespace Bajun.NetApi.Generated.Model.frame_support.traits.preimages
{
    
    
    /// <summary>
    /// >> Bounded
    /// </summary>
    public enum Bounded
    {
        
        /// <summary>
        /// >> Legacy
        /// </summary>
        Legacy = 0,
        
        /// <summary>
        /// >> Inline
        /// </summary>
        Inline = 1,
        
        /// <summary>
        /// >> Lookup
        /// </summary>
        Lookup = 2,
    }
    
    /// <summary>
    /// >> 403 - Variant[frame_support.traits.preimages.Bounded]
    /// </summary>
    public sealed class EnumBounded : BaseEnumRust<Bounded>
    {
        
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public EnumBounded()
        {
				AddTypeDecoder<Bajun.NetApi.Generated.Model.primitive_types.H256>(Bounded.Legacy);
				AddTypeDecoder<Bajun.NetApi.Generated.Model.bounded_collections.bounded_vec.BoundedVecT20>(Bounded.Inline);
				AddTypeDecoder<BaseTuple<Bajun.NetApi.Generated.Model.primitive_types.H256, Substrate.NetApi.Model.Types.Primitive.U32>>(Bounded.Lookup);
        }
    }
}