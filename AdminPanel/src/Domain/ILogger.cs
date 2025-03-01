using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message);
    }
}
