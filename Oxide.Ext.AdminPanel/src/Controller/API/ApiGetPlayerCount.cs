using System;
using System.Net;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class ApiGetPlayerCount : ApiController
    {
        public ApiGetPlayerCount(Controller controller)
            : base(controller)
        {
        }

        public async Task GetPlayerCount(HttpListenerContext context)
        {
            int playerCount = GetPlayerCountFromServer();
            int maxPlayers = GetMaxPlayers();
            await SendResponse(context.Response, true, "OK", new { PlayerCount = playerCount, MaxPlayers = maxPlayers });
        }

        private int GetPlayerCountFromServer()
        {
            // Логика получения количества игроков
            return BasePlayer.activePlayerList.Count;
        }        
        private int GetMaxPlayers()
        {
            // Логика получения количества игроков
            return ConVar.Server.maxplayers;
        }
    }

}
