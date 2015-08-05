using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ReasonChildWithParent : ReasonChild
    {
        [DataMember]
        public Reason parentReason { get; set; }
    }
}