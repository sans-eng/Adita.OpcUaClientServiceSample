using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Services
{
    public class MonitoredItems : IMonitoredItemsContainer
    {
        #region Constructors
        public MonitoredItems()
        {
            Value1 = new MonitoredItem
            {
                AttributeId = Opc.Ua.Attributes.Value,
                StartNodeId = "ns=2;s=Simulator.GrindingMachine.Application.GVL_Gcon.SetAxis1.PosAbsolute",
                DisplayName = "PosAbsolute",
                SamplingInterval = 100
            };
        }
        #endregion Constructors

        #region Public properties
        public MonitoredItem Value1 { get; }
        #endregion Public properties
    }
}
