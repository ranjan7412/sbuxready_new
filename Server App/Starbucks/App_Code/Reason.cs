using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class Reason
    {
        [DataMember]
        public int reasonCode { get; set; }

        [DataMember]
        public string reasonName { get; set; }
    }
}