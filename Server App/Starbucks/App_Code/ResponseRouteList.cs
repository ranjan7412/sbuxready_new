using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseRouteList : Response
    {
        [DataMember]
        public List<Route> routes;

        [DataMember]
        public int numberOfRecords { get; set; }
    }
}