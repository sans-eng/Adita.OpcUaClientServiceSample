# Adita.OpcUaClientServiceSample
Opc Ua Client Service Sample using .Net 6 WPF with DependencyInjection.

this sample also include :
* TextBox numeric input behavior
* TextBox update source & invoke command on enter key behavior
* Validations including error template sample

### *Adita* is our new namespace root, *Sans* will be depreceated.

Highly recommended to use handler only for one *MonitoredItem.Notification* if need high speed notification for multiple MonitoredItems, that's the purpose of abstraction of *IMonitoredItemsContainer*.

*"Tested on KepServerEx 6.4"*
