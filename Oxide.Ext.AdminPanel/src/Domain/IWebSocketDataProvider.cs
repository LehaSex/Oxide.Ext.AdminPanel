using System.Collections.Generic;

namespace Oxide.Ext.AdminPanel
{
    /// <summary>
    /// Defines the contract for the data provider intended for WebSocket.
    /// </summary>
    public interface IWebSocketDataProvider
    {
        /// <summary>
        /// A unique key identifying this data type (for example, "performance", "players").
        /// It will be used as a top-level key in JSON.
        /// </summary>
        string DataKey { get; }

        /// <summary>
        /// Gets up-to-date data to send via WebSocket.
        /// The values should be simple types that can be easily serialized in JSON.
        /// </summary>
        /// <returns>A dictionary with field names and their values.</returns>
        Dictionary<string, object> GetWebSocketData();
    }
}
