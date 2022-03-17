using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Settings
{
    public class UaOptions
    {
        public string ServerUrl { get; set; } = string.Empty;
        public int ReconnectPeriod { get; set; }
        public bool UseSecurity { get; set; }
    }
}
