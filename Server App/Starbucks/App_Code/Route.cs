using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [Serializable]
    [DataContract]
    public class Route
    {
        [DataMember]
        public int routeID { get; set; }

        [DataMember]
        public string routeName { get; set; }

        [DataMember]
        public CDC cdc { get; set; }

        [DataMember]
        public string cdcName { get; set; }

        [DataMember]
        public int routeStatus { get; set; }

        [DataMember]
        public List<Store> stores { get; set; }
    }
}