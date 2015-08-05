using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseStoreList : Response
    {
        [DataMember]
        public List<Store> stores { get; set; }

        [DataMember]
        public int numberOfRecords { get; set; }
    }
}