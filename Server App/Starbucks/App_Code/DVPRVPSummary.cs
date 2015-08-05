using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Starbucks
{
    public class DVPRVPSummary
    {
        public string dvpName { get; set; }
        public string rvpName { get; set; }
        
        public double percentageStoresReady { get; set; }
        public double changeFromLastPeriod { get; set; }
        
        public int dairyBackhaulUnits { get; set; }
        public double dairyBackhaulCOGS { get; set; }

        public int deliveries { get; set; }
        public int deliveriesWithIssues { get; set; }
        public int totalReadinessIssues { get; set; }

        public int totalSecurityFacilityIssues { get; set; }
        public int totalCapacityIssues { get; set; }

	public int leftoutUnits { get; set; }
        public double leftoutCOGS { get; set; }
    }
}