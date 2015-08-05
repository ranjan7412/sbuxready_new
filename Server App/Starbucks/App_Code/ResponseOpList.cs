using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseOpList : Response
    {
        [DataMember]
        public List<Op> ops { get; set; }

        [DataMember]
        public int numberOfRecords { get; set; }
    }
}