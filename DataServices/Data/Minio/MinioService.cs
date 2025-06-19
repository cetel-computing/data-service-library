using System.Text;
using Minio;
using DataServices.Config;

namespace DataServices.Data
{
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _client;
        private MinioConfigOptions _settings;

        public MinioService(MinioConfigOptions settings)
        {
            // WithSSL() enables SSL support in MinIO client
            if (settings.EnableHTTPS)
            {
                _client = new MinioClient()
                                        .WithEndpoint(settings.MinioUri, settings.Port)
                                        .WithCredentials(settings.AccessKey, settings.SecretKey)
                                        .WithSSL()
                                        .Build();
            }
            else
            {
                _client = new MinioClient()
                                        .WithEndpoint(settings.MinioUri, settings.Port)
                                        .WithCredentials(settings.AccessKey, settings.SecretKey)
                                        .Build();
            }

            _settings = settings;
        }

        public async Task<byte[]> GetObject(string id)
        {
            return await GetObject(IdToPath(id), _settings.Bucket);
        }

        private async Task<byte[]> GetObject(string path, string bucket)
        {
            var fileInfo = Array.Empty<byte>();

            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var args = new Minio.DataModel.Args.StatObjectArgs()
                                .WithBucket(bucket)
                                .WithObject(path);

                var found = await _client.StatObjectAsync(args);

                if (found != null)
                {
                    var getArgs = new Minio.DataModel.Args.GetObjectArgs()
                                .WithBucket(bucket)
                                .WithObject(path)
                                .WithCallbackStream(stream =>
                                {
                                    using (var newStream = new MemoryStream())
                                    {
                                        stream.CopyTo(newStream);
                                        fileInfo = newStream.ToArray();
                                    }
                                });

                    var stat = await _client.GetObjectAsync(getArgs).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                //Log.Information(e, "[Bucket]  Exception: {E}", e);
            }

            // fileInfo either has the matching file information or is null
            return fileInfo;
        }

        /// <summary>
        /// Convert UUID to min.io format 
        /// </summary>
        /// <param name="id">id in fomrmat i.e. YYYYMMDDHHmm-GUI i.e. 202205111416-0033B4D2-D135-11EC-A5C3-5A52971E80B0</param>
        /// <param name="filename"></param>
        /// <returns>path in format YYYY/MM/DD/HH/MM/GUID </returns>
        private static string IdToPath(string id, string filename = null)
        {
            // <date-time-stamp>-<dash-separated-8-4-4-4-12-format-guid>
            // i.e. YYYYMMDDHHmm-GUID
            // i.e. 202205111416-0033B4D2-D135-11EC-A5C3-5A52971E80B0
            // for min.io lookup the format must be:
            // YYYY/MM/DD/HH/MM/GUID

            var builder = new StringBuilder();
            builder.Append(id.AsSpan(0, 4)).Append('/') //year
                .Append(id.AsSpan(4, 2)).Append('/') //month
                .Append(id.AsSpan(6, 2)).Append('/') //day
                .Append(id.AsSpan(8, 2)).Append('/') //hour
                .Append(id.AsSpan(10, 2)).Append('/') //minute
                .Append(id.AsSpan(13)); //guid

            if (filename != null)
            {
                builder.Append('/').Append(filename);
            }

            return builder.ToString();
        }
    }
}
