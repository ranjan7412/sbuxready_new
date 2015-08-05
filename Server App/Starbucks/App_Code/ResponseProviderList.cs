﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseProviderList : Response
    {
        [DataMember]
        public List<Provider> providers { get; set; }
    }
}