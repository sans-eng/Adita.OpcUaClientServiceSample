using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adita.OpcUaClientServiceSample.Services
{
    /// <summary>
    /// Represents event data for <see cref="UaClient.ClientStatusChanged"/>
    /// </summary>
    public class ClientStatusEventArgs : EventArgs
    {
        #region Constructors
        /// <summary>
        /// Initialize new instance of <see cref="ClientStatusEventArgs"/> using specified 
        /// </summary>
        /// <param name="status"><see cref="ClientStatus"/> of client.</param>
        /// <param name="time">Time of event rises.</param>
        /// <param name="description">DEscription of an event.</param>
        public ClientStatusEventArgs(ClientStatus status, DateTime time = default, string description = "")
        {
            Status = status;
            Time = time == default ? DateTime.Now : time;
            Description = description;
        }
        #endregion Constructors

        #region Public properties
        /// <summary>
        /// Gets status of client on event source.
        /// </summary>
        public ClientStatus Status { get; }
        /// <summary>
        /// Gets event raised time on event source.
        /// </summary>
        public DateTime Time { get; }
        /// <summary>
        /// Gets event description on event source.
        /// </summary>
        public string Description { get; }
        #endregion Public properties

        #region Public methods
        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Status.ToString() + " " + Time.ToString("  yyyy-MM-dd HH:mm:ss  ") + " " + Description;
        }
        #endregion Public methods
    }
}
