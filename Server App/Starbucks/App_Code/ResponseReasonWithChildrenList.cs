using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseReasonWithChildrenList : Response
    {
        [DataMember]
        public List<ReasonWithChildren> reasons { get; set; }
    }
}