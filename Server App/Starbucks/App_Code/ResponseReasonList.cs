using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseReasonList : Response
    {
        [DataMember]
        public List<Reason> reasons { get; set; }
    }
}