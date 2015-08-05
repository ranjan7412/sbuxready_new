using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseCDCList : Response
    {
        [DataMember]
        public List<CDC> cdcs { get; set; }
    }
}