using Blish_HUD;
using Blish_HUD.Modules.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Teh.BHUD.Blacklist_Buddy_Module.Models;

namespace Teh.BHUD.Blacklist_Buddy_Module.Utils
{
    internal class FileUtil
    {
        /// <summary>
        /// Loads and serializes an object of T type from the given path
        /// </summary>
        public static async Task<T> LoadFromJson<T>(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
        }

        /// <summary>
        /// Serializes and saves the given object to JSON at the given path
        /// </summary>
        public static async Task SaveToJson<T>(string path, T obj)
        {
            using (FileStream stream = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };

                await JsonSerializer.SerializeAsync(stream, obj, obj.GetType(), options);
            }
        }
    }
}
