using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseFailureList : Response
    {
        [DataMember]
        public List<FailureWithReason> failures { get; set; }
    }
}