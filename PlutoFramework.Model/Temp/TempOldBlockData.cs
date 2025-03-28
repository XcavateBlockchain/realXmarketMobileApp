﻿using Newtonsoft.Json;

namespace PlutoFramework.Model.Temp
{
    public class TempOldBlockData
    {
        //
        // Summary:
        //     Block
        public TempOldBlock Block { get; set; }

        //
        // Summary:
        //     Justification
        public object Justification { get; set; }

        //
        // Summary:
        //     Block Data Constructor
        //
        // Parameters:
        //   block:
        //
        //   justification:
        public TempOldBlockData(TempOldBlock block, object justification)
        {
            Block = block;
            Justification = justification;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
