﻿using System;
using Thycotic.Discovery.Core.Inputs;

namespace Thycotic.Messages.DE.Areas.Discovery.Request
{
    /// <summary>
    /// Scan Local Account Message
    /// </summary>
    public class ScanLocalAccountMessage : ScanMessageBase
    {
        /// <summary>
        /// Batch Id
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Computer Id
        /// </summary>
        public int ComputerId { get; set; }


        /// <summary>
        /// Scan Computer Input
        /// </summary>
        public ScanComputerInput Input { get; set; }
    }
}
