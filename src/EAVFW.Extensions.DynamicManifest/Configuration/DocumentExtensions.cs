﻿using EAVFW.Extensions.Documents;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DynamicManifest
{
    public static class DocumentExtensions
    {
        public static async Task<JToken> LoadJsonAsync(this IDocumentEntity record)
        {
            var stream = new GZipStream(new MemoryStream(record.Data), CompressionMode.Decompress);
            var target = new MemoryStream();
            await stream.CopyToAsync(target);

            var a = System.Text.Encoding.UTF8.GetString(target.ToArray());

            return JToken.Parse(a);
        }
        public static async Task SaveJsonAsync(this IDocumentEntity record, JToken manifest)
        {
            var a = System.Text.Encoding.UTF8.GetBytes(manifest.ToString());

            using (var data = new MemoryStream())
            {
                using (var stream = new GZipStream(data, CompressionMode.Compress))
                {
                    await stream.WriteAsync(a, 0, a.Length);
                   
                }


                record.Data = data.ToArray();

            }
            record.Compressed = true;
        }
    }
}