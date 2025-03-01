using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Ext.AdminPanel
{
    public class FileSystem : IFileSystem
    {
        public async Task CreateDirectoryAsync(string path)
        {
            await Task.Run(() => Directory.CreateDirectory(path));
        }

        public async Task<bool> DirectoryExistsAsync(string path)
        {
            return await Task.Run(() => Directory.Exists(path));
        }

        public async Task<byte[]> ReadFileAsync(string path)
        {
            return await Task.Run(() => File.ReadAllBytes(path));
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public async Task<bool> FileExistsAsync(string path)
        { 
            return await Task.Run(() => File.Exists(path));
        }
    }
}
