using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Starbucks
{
    public class RDDMSummary
    {
        public string rdName { get; set; }
        public string dmName { get; set; }

        public double percentageStoresReady { get; set; }
        public double changeFromLastPeriod { get; set; }

        public int dairyBackhaulUnits { get; set; }
        public double dairyBackhaulCOGS { get; set; }

        public int deliveries { get; set; }
        public int deliveriesWithIssues { get; set; }
        public int totalReadinessIssues { get; set; }

        public int totalSecurityFacilityIssues { get; set; }
        public int totalCapacityIssues { get; set; }

	//public int totalSecurityFacilityIssues { get; set; }
        //public int totalCapacityIssues { get; set; }
        public int totalProductivityIssues { get; set; }
    }
}