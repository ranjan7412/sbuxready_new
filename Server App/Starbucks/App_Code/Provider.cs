using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Provider
    {
        [DataMember]
        public int providerID { get; set; }

        [DataMember]
        public string providerName { get; set; }
    }
}