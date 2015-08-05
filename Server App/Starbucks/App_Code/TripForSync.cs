using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class TripForSync : Trip
    {
        [DataMember]
        public List<StopWithStoreAndFailure> stops { get; set; }
    }
}