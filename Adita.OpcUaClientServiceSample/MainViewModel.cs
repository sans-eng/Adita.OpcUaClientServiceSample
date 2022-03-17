using Adita.OpcUaClientServiceSample.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample
{
    public class MainViewModel : ObservableValidator
    {
        #region Constructors
        public MainViewModel(IUaClientService uaClientService)
        {
            _uaClientService = uaClientService;
            _monitoredItems = (MonitoredItems)uaClientService.GetSubscriptionItems();
            _monitoredItems.Value1.Notification += Value1_Notification;

            CommitCommand = new AsyncRelayCommand(CommitValue);

            Value1 = _uaClientService.Read<double>(_monitoredItems.Value1.StartNodeId.ToString());
        }
        #endregion Constructors

        #region Private fields/Services
        private readonly IUaClientService _uaClientService;
        private readonly MonitoredItems _monitoredItems;
        #endregion Private fields/Services

        #region Private fields
        private double _value1;
        #endregion Private fields

        #region Public properties
        [Range(0d, 1000d)]
        public double Value1
        {
            get { return _value1; }
            set { SetProperty(ref _value1, value, true); }
        }
        #endregion Public properties

        #region Commands
        public IAsyncRelayCommand CommitCommand { get; }
        #endregion Commands

        #region Private methods
        private async Task CommitValue()
        {
            if (GetErrors(nameof(Value1)).Any())
                return;

            await _uaClientService.WriteAsync(_monitoredItems.Value1.StartNodeId, Value1);
        }
        #endregion Private methods

        #region Notification handlers
        private void Value1_Notification(Opc.Ua.Client.MonitoredItem monitoredItem, Opc.Ua.Client.MonitoredItemNotificationEventArgs e)
        {
            if (e.NotificationValue is MonitoredItemNotification itemNotification && itemNotification.Value.WrappedValue.Value is double value)
            {
                Value1 = value;
            }
        }
        #endregion Notification handlers
    }
}
