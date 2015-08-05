using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseTripList : Response
    {
        [DataMember]
        public List<Trip> trips { get; set; }
    }
}