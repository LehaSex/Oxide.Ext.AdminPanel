using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public interface IFileSystem
    {
        Task CreateDirectoryAsync(string path);
        Task<bool> DirectoryExistsAsync(string path);
        Task<byte[]> ReadFileAsync(string path);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        Task<bool> FileExistsAsync(string path);
    }
}
