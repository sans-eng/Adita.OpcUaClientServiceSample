using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Services
{
    public class UaClientService : IUaClientService
    {
        #region Constructors
        public UaClientService(string clientName, string serverUrl, bool useSecurity, int reconnectPeriod, IUserIdentity? userIdentity = null)
        {
            ClientName = clientName;
            ServerUrl = serverUrl;
            UseSecurity = useSecurity;
            UserIdentity = userIdentity;
            ReconnectPeriod = reconnectPeriod;
        }
        public UaClientService()
        {

        }
        #endregion Constructors

        #region Private fields
        private Session? _session;
        private SessionReconnectHandler? _reconnectHandler;
        private IMonitoredItemsContainer _monitoredItems;
        #endregion Private fields

        #region Public properties
        public string ClientName { get; set; } = Process.GetCurrentProcess().ProcessName + " - " + Process.GetCurrentProcess().Id + " - " + Environment.UserName;
        public string ServerUrl { get; set; } = string.Empty;
        public bool UseSecurity { get; set; }
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public IUserIdentity? UserIdentity { get; set; }
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public int ReconnectPeriod { get; set; }

        public bool IsConnected { get; private set; }
        #endregion Public properties

        #region Events
        public event EventHandler? Reconnecting;
        public event EventHandler? Reconnected;
        public event EventHandler? Connected;
        public event EventHandler<ClientStatusEventArgs>? ClientStatusChanged;
        #endregion Events

        #region Public methods
        public async Task<bool> ConnectAsync()
        {
            ApplicationConfiguration configuration = CreateConfiguration();
            if (configuration == null)
            {
                return false;
            }

            _session = await CreateSession(configuration);

            return _session != null;
        }
        public bool Disconnect()
        {
            StatusCode status = default;

            // stop any reconnect operation.
            _reconnectHandler?.Dispose();
            _reconnectHandler = null;

            // disconnect any existing session.
            if (_session != null)
            {
                status = _session.Close(10000);
                _session = null;
            }

            // update the client status
            IsConnected = false;

            ClientStatusChanged?.Invoke(this, new ClientStatusEventArgs(ClientStatus.Disconnected));

            return StatusCode.IsGood(status);
        }
        public IMonitoredItemsContainer GetSubscriptionItems()
        {
            return _monitoredItems;
        }
        public IList<Subscription> GetSubscriptionList()
        {
            if (IsConnected && _session != null)
                return _session.Subscriptions.ToList();
            else
                throw new InvalidOperationException("Client is not connected.");
        }
        public bool Subscribe(IMonitoredItemsContainer monitoredItemsContainer)
        {
            if (IsConnected && _session != null)
            {
                _monitoredItems = monitoredItemsContainer;
                Subscription subcription = new(_session.DefaultSubscription);
                subcription.PublishingEnabled = true;
                subcription.PublishingInterval = 0;
                subcription.KeepAliveCount = uint.MaxValue;
                subcription.LifetimeCount = uint.MaxValue;
                subcription.MaxNotificationsPerPublish = uint.MaxValue;
                subcription.Priority = 100;
                subcription.DisplayName = "Main";

                List<MonitoredItem>? items = GetMonitoredItems(monitoredItemsContainer);

                if (items?.Count > 0)
                {
                    subcription.AddItems(items);
                    _session.AddSubscription(subcription);
                    subcription.Create();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool Subscribe(IMonitoredItemsContainer monitoredItemsContainer, int maxItemsPerSubscription = 500)
        {
            if (IsConnected && _session != null && maxItemsPerSubscription > 0)
            {
                _monitoredItems = monitoredItemsContainer;

                List<MonitoredItem>? items = GetMonitoredItems(monitoredItemsContainer);

                if (items?.Count > 0)
                {
                    var splittedItems = new List<List<MonitoredItem>>();
                    for (int i = 0; i < items.Count; i += maxItemsPerSubscription)
                        splittedItems.Add(items.GetRange(i, Math.Min(maxItemsPerSubscription, items.Count - i)));

                    int subsCounter = 0;

                    foreach (var item in splittedItems)
                    {
                        if (item?.Count > 0)
                        {
                            Subscription subcription = new(_session.DefaultSubscription);
                            subcription.PublishingEnabled = true;
                            subcription.PublishingInterval = 0;
                            subcription.KeepAliveCount = uint.MaxValue;
                            subcription.LifetimeCount = uint.MaxValue;
                            subcription.MaxNotificationsPerPublish = uint.MaxValue;
                            subcription.Priority = 100;
                            subcription.DisplayName = $"{nameof(Subscription)} {subsCounter}";
                            subcription.AddItems(item);

                            if (_session.AddSubscription(subcription))
                            {
                                subcription.Create();
                            }
                            else
                            {
                                return false;
                            }
                        }
                        subsCounter++;
                    }

                    return true;
                }
            }
            return false;
        }
        public bool Subscribe(Subscription subscription)
        {
            if (IsConnected && _session?.AddSubscription(subscription) == true)
            {
                subscription.Create();
                return true;
            }

            return false;
        }
        public T Read<T>(string tag)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException($"'{nameof(tag)}' cannot be null or whitespace.", nameof(tag));
            }

            DataValue dataValue = Read(new NodeId(tag));
            return (T)dataValue.Value;
        }
        public DataValue Read(NodeId nodeId)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            if (nodeId is null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            ReadValueIdCollection nodesToRead = new()
            {
                new ReadValueId()
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                }
            };

            // read the current value
            DataValueCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            try
            {
                _session.Read(
              null,
              0,
              TimestampsToReturn.Neither,
              nodesToRead,
              out results,
              out diagnosticInfos);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on read value(s) to server: " + ex.Message);
            }

            ClientBase.ValidateResponse(results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            return results[0];
        }
        public Task<T> ReadAsync<T>(string tag)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException($"'{nameof(tag)}' cannot be null or whitespace.", nameof(tag));
            }

            ReadValueIdCollection nodesToRead = new()
            {
                new ReadValueId()
                {
                    NodeId = new NodeId(tag),
                    AttributeId = Attributes.Value
                }
            };

            // Wrap the ReadAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            var taskCompletionSource = new TaskCompletionSource<T>();
            _session?.BeginRead(
            requestHeader: null,
            maxAge: 0,
            timestampsToReturn: TimestampsToReturn.Neither,
            nodesToRead: nodesToRead,
            callback: ar =>
            {
                var response = _session.EndRead(
                  result: ar,
                  results: out DataValueCollection results,
                  diagnosticInfos: out DiagnosticInfoCollection diag);

                try
                {
                    CheckReturnValue(response.ServiceResult);
                    CheckReturnValue(results[0].StatusCode);
                    var val = results[0];
                    taskCompletionSource.TrySetResult((T)val.Value);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            },
            asyncState: null);

            return taskCompletionSource.Task;
        }
        public Task<DataValue> ReadAsync(NodeId nodeId)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            if (nodeId is null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            ReadValueIdCollection nodesToRead = new()
            {
                new ReadValueId()
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                }
            };

            // Wrap the ReadAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            var taskCompletionSource = new TaskCompletionSource<DataValue>();
            _session?.BeginRead(
            requestHeader: null,
            maxAge: 0,
            timestampsToReturn: TimestampsToReturn.Neither,
            nodesToRead: nodesToRead,
            callback: ar =>
            {
                var response = _session.EndRead(
                  result: ar,
                  results: out DataValueCollection results,
                  diagnosticInfos: out DiagnosticInfoCollection diag);

                try
                {
                    CheckReturnValue(response.ServiceResult);
                    CheckReturnValue(results[0].StatusCode);
                    var val = results[0];
                    taskCompletionSource.TrySetResult(val);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            },
            asyncState: null);

            return taskCompletionSource.Task;
        }
        public bool Write<T>(string tag, T value)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            WriteValue valueToWrite = new()
            {
                NodeId = new NodeId(tag),
                AttributeId = Attributes.Value
            };
            valueToWrite.Value.Value = value;
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

            WriteValueCollection valuesToWrite = new WriteValueCollection
            {
                valueToWrite
            };

            StatusCodeCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            try
            {
                _session.Write(null, valuesToWrite, out results, out diagnosticInfos);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on writing value(s) to server: " + ex.Message);
            }

            ClientBase.ValidateResponse(results, valuesToWrite);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

            if (StatusCode.IsBad(results[0]))
            {
                throw new ServiceResultException(results[0]);
            }

            return !StatusCode.IsBad(results[0]);
        }
        public bool Write<T>(NodeId nodeId, T value)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            WriteValue valueToWrite = new WriteValue()
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };
            valueToWrite.Value.Value = value;
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

            WriteValueCollection valuesToWrite = new WriteValueCollection
            {
                valueToWrite
            };

            StatusCodeCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            try
            {
                _session.Write(null, valuesToWrite, out results, out diagnosticInfos);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on writing value(s) to server: " + ex.Message);
            }

            ClientBase.ValidateResponse(results, valuesToWrite);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

            if (StatusCode.IsBad(results[0]))
            {
                throw new ServiceResultException(results[0]);
            }

            return !StatusCode.IsBad(results[0]);
        }
        public Task<bool> WriteAsync<T>(string tag, T value)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();

            WriteValue valueToWrite = new()
            {
                NodeId = new NodeId(tag),
                AttributeId = Attributes.Value,
            };
            valueToWrite.Value.Value = value;
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
            WriteValueCollection valuesToWrite = new WriteValueCollection
            {
                valueToWrite
            };

            try
            {
                _session.BeginWrite(requestHeader: null, nodesToWrite: valuesToWrite,
                callback: ar =>
                {
                    var response = _session.EndWrite(
                      result: ar,
                      results: out StatusCodeCollection results,
                      diagnosticInfos: out DiagnosticInfoCollection diag);

                    ClientBase.ValidateResponse(results, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diag, valuesToWrite);
                    taskCompletionSource.SetResult(StatusCode.IsBad(results[0]));

                }, asyncState: null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
                throw new Exception("Error on write async value(s) to server: " + ex.Message);
            }

            return taskCompletionSource.Task;
        }
        public Task<bool> WriteAsync<T>(NodeId nodeId, T value)
        {
            if (_session == null)
            {
                throw new InvalidOperationException($"Current {nameof(UaClientService)} is not connected to server.");
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();

            WriteValue valueToWrite = new()
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
            };
            valueToWrite.Value.Value = value;
            valueToWrite.Value.StatusCode = StatusCodes.Good;
            valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
            valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
            WriteValueCollection valuesToWrite = new WriteValueCollection
            {
                valueToWrite
            };

            try
            {
                _session.BeginWrite(requestHeader: null, nodesToWrite: valuesToWrite,
                callback: ar =>
                {
                    var response = _session.EndWrite(
                      result: ar,
                      results: out StatusCodeCollection results,
                      diagnosticInfos: out DiagnosticInfoCollection diag);

                    ClientBase.ValidateResponse(results, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diag, valuesToWrite);
                    taskCompletionSource.SetResult(StatusCode.IsBad(results[0]));

                }, asyncState: null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
                throw new Exception("Error on write async value(s) to server: " + ex.Message);
            }

            return taskCompletionSource.Task;
        }
        #endregion Public methods

        #region Private methods
        private ApplicationConfiguration CreateConfiguration()
        {

            var certificateValidator = new CertificateValidator();
            certificateValidator.CertificateValidation += (_, eventArgs) =>
            {
                if (ServiceResult.IsGood(eventArgs.Error))
                    eventArgs.Accept = true;
                else if (eventArgs.Error.StatusCode.Code == StatusCodes.BadCertificateUntrusted)
                    eventArgs.Accept = true;
                else
                    throw new Exception(string.Format("Failed to validate certificate with error code {0}: {1}", eventArgs.Error.Code, eventArgs.Error.AdditionalInfo));
            };

            SecurityConfiguration securityConfigurationcv = new()
            {
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024,
            };
            certificateValidator.Update(securityConfigurationcv);

            // Build the application configuration
            ApplicationInstance application = new()
            {
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = ClientName,
                ApplicationConfiguration = new ApplicationConfiguration
                {
                    ApplicationName = ClientName,
                    ApplicationType = ApplicationType.Client,
                    CertificateValidator = certificateValidator,
                    ServerConfiguration = new ServerConfiguration
                    {
                        MaxSubscriptionCount = 100000,
                        MaxMessageQueueSize = 1000000,
                        MaxNotificationQueueSize = 1000000,
                        MaxPublishRequestCount = 10000000,
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        MinimumCertificateKeySize = 1024,
                    },

                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 6000000,
                        MaxStringLength = int.MaxValue,
                        MaxByteStringLength = int.MaxValue,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 419430400,
                        MaxBufferSize = 65535,
                        ChannelLifetime = -1,
                        SecurityTokenLifetime = -1
                    },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = -1,
                        MinSubscriptionLifetime = -1,
                    },
                    DisableHiResClock = true
                }
            };

            return application.ApplicationConfiguration;
        }
        private async Task<Session> CreateSession(ApplicationConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // disconnect from existing session.
            Disconnect();

            // select the best endpoint.
            EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(ServerUrl, UseSecurity);

            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(configuration);
            ConfiguredEndpoint endpoint = new(null, endpointDescription, endpointConfiguration);

            var session = await Session.Create(
                configuration,
                endpoint,
                false,
                false,
                string.IsNullOrWhiteSpace(ClientName) ? configuration.ApplicationName : ClientName,
                60000,
                UserIdentity,
                Array.Empty<string>()).ConfigureAwait(false);

            // set up keep alive callback.
            session.KeepAlive += Session_KeepAlive;

            // update the client status
            IsConnected = true;

            // raise an event.
            Connected?.Invoke(this, EventArgs.Empty);

            // return the new session.
            return session;
        }
        private void CheckReturnValue(StatusCode status)
        {
            if (!StatusCode.IsGood(status))
                throw new Exception(string.Format("Invalid response from the server. (Response Status: {0})", status));
        }
        private List<MonitoredItem>? GetMonitoredItems(object? tagItems)
        {
            if (tagItems == null) return null;
            List<MonitoredItem> result = new();

            foreach (var obj in tagItems.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {

                if (obj.GetValue(tagItems) is MonitoredItem item)
                {
                    result.Add(item);
                }
                else if (obj.GetValue(tagItems) is IEnumerable<object> collection)
                {
                    foreach (var collectionItem in collection)
                    {
                        var items = GetMonitoredItems(collectionItem);
                        if (items != null)
                        {
                            foreach (var monitoredItem in items)
                            {
                                result.Add(monitoredItem);
                            }
                        }
                    }
                }
                else
                {
                    var items = GetMonitoredItems(obj.GetValue(tagItems));
                    if (items != null)
                    {
                        foreach (var monitoredItem in items)
                        {
                            result.Add(monitoredItem);
                        }
                    }
                }
            }

            return result;
        }
        #endregion Private methods

        #region Event handlers
        private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            try
            {
                if (!ReferenceEquals(session, _session)) return;

                if (ServiceResult.IsBad(e.Status))
                {
                    IsConnected = false;

                    ClientStatusChanged?.Invoke(this, new ClientStatusEventArgs(ClientStatus.Reconnecting));

                    if (_reconnectHandler == null)
                    {
                        Reconnecting?.Invoke(this, e);

                        _reconnectHandler = new SessionReconnectHandler();
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
                        _reconnectHandler.BeginReconnect(_session, ReconnectPeriod * 1000, callback: Server_ReconnectComplete);
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                // ignore callbacks from discarded objects.
                if (!ReferenceEquals(sender, _reconnectHandler)) return;

                _session = _reconnectHandler.Session;
                _reconnectHandler.Dispose();
                _reconnectHandler = null;

                // raise any additional notifications.
                Reconnected?.Invoke(this, e);
                ClientStatusChanged?.Invoke(this, new ClientStatusEventArgs(ClientStatus.Connected));
                IsConnected = true;
            }
            catch (Exception exception)
            {
                throw new Exception("Error reconnecting client: " + exception.Message);
            }
        }
        #endregion Event handlers
    }
}
