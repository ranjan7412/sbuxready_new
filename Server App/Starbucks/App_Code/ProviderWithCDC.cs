using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ProviderWithCDC : Provider
    {
        [DataMember]
        public List<CDC> cdcs { get; set; }
    }
}