﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;


namespace Starbucks
{
    [DataContract]
    public class ResponseTrip : Response
    {
        [DataMember]
        public TripWithStops trip { get; set; }
    }
}