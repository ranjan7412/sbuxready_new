using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseFailure : Response
    {
        [DataMember]
        public Failure failure { get; set; }
    }
}