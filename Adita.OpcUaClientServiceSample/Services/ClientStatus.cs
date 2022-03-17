using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Services
{
    public enum ClientStatus
    {
        Disconnected = 0,
        Connected = 1,
        Reconnecting = 2,
        ComError = 3
    }
}
