using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class GeoPosition
    {
        [DataMember]
        public int tripID { get; set; }

        [DataMember]
        public float latitude { get; set; }

        [DataMember]
        public float longitude { get; set; }
    }
}