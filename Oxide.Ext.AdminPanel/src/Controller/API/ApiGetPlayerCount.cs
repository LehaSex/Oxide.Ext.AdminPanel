using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class ApiGetPlayerCount : ApiController, IWebSocketDataProvider
    {
        [WebSocketExpose]
        private int _playerCount;
        private int _maxPlayers;
        public string DataKey => "players";

        public ApiGetPlayerCount(Controller controller)
            : base(controller)
        {
        }

        public async Task GetPlayerCount(HttpListenerContext context)
        {
            GetAll();
            await SendResponse(context.Response, true, "OK", new { PlayerCount = _playerCount, MaxPlayers = _maxPlayers });
        }

        private int GetPlayerCountFromServer()
        {
            return BasePlayer.activePlayerList.Count;
        }        
        private int GetMaxPlayers()
        {
            return ConVar.Server.maxplayers;
        }

        private void GetAll()
        {
            _playerCount = GetPlayerCountFromServer();
            _maxPlayers = GetMaxPlayers();
        }

        public Dictionary<string, object> GetWebSocketData()
        {
            GetAll();
            return WebSocketExposeHelper.GetExposedValues(this);
        }
    }

}
