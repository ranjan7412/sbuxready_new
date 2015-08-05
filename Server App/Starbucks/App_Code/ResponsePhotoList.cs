using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class ResponsePhotoList : Response
    {
        [DataMember]
        public List<PhotoWithStore> photos { get; set; }
    }
}