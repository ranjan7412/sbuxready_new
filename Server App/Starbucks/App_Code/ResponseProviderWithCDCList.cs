using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponseProviderWithCDCList : Response
    {
        [DataMember]
        public List<ProviderWithCDC> providers { get; set; }
    }
}