using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class FailureWithReason : Failure
    {
        [DataMember]
        public ReasonChildWithParent reason { get; set; }
    }
}