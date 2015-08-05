using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ReasonWithChildren : Reason
    {
        [DataMember]
        public List<ReasonChild> children { get; set; }
    }
}