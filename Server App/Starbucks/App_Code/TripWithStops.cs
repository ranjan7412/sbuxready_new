using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class TripWithStops : Trip
    {
        [DataMember]
        public List<StopWithStore> stops { get; set; }
    }
}