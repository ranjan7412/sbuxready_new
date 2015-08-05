using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class PhotoWithStore : Photo
    {
        [DataMember]
        public int storeID { get; set; }

        [DataMember]
        public DateTime dateUpdated { get; set; }

        [DataMember]
        public int dateUpdatedEpoch { get; set; }

        [DataMember]
        public string dateUpdatedString { get; set; }
    }
}