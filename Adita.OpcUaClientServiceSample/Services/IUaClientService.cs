using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Services
{
    public interface IUaClientService
    {
        #region Properties
        string ClientName { get; set; }
        string ServerUrl { get; set; }
        bool UseSecurity { get; set; }
        IUserIdentity UserIdentity { get; set; }
        int ReconnectPeriod { get; set; }
        bool IsConnected { get; }
        #endregion Properties

        #region Events
        event EventHandler Reconnecting;
        event EventHandler Reconnected;
        event EventHandler Connected;
        event EventHandler<ClientStatusEventArgs> ClientStatusChanged;
        #endregion Events

        #region Methods
        Task<bool> ConnectAsync();
        bool Disconnect();
        bool Write<T>(string tag, T value);
        Task<bool> WriteAsync<T>(string tag, T value);
        bool Write<T>(NodeId nodeId, T value);
        Task<bool> WriteAsync<T>(NodeId nodeId, T value);
        T Read<T>(string tag);
        Task<T> ReadAsync<T>(string tag);
        DataValue Read(NodeId nodeId);
        Task<DataValue> ReadAsync(NodeId nodeId);
        bool Subscribe(IMonitoredItemsContainer monitoredItemsContainer);
        bool Subscribe(IMonitoredItemsContainer monitoredItemsContainer, int maxItemsPerSubscription);
        bool Subscribe(Subscription subscription);
        IMonitoredItemsContainer GetSubscriptionItems();
        IList<Subscription> GetSubscriptionList();
        #endregion Methods
    }
}
