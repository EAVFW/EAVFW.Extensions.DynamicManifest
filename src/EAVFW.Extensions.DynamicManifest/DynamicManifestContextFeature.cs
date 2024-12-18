using EAVFramework;
using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Shared.V2;
using EAVFramework.Validation;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DynamicManifest
{
    public interface IDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument>
        where TStaticContext : DynamicContext
        where TDynamicContext : DynamicManifestContext<TStaticContext, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {
        public DynamicContextOptions CreateOptions(IExtendedFormContextFeature<TStaticContext, TModel> feature);
    }

    public class DefaultDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument>
        : IDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument>
        where TStaticContext : DynamicContext
        where TDynamicContext : DynamicManifestContext<TStaticContext, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {
        public virtual DynamicContextOptions CreateOptions(IExtendedFormContextFeature<TStaticContext, TModel> feature)
        {
            {
                return new DynamicContextOptions
                {
                    Manifests = feature.Manifests,
                    Schema = feature.SchemaName,
                    EnableDynamicMigrations = true,
                    Namespace = $"EAVFW.Extensions.DynamicManifest.{feature.SchemaName?.Replace("-", "_")}.Model",
                  //  UseOnlyExpliciteExternalDTOClases = true,
                    DTOAssembly = typeof(TModel).Assembly,
                    DTOBaseClasses = new[] { typeof(BaseOwnerEntity<>), typeof(BaseIdEntity<>) },
                    DisabledPlugins = new[] { typeof(RequiredPlugin) },
                    DTOBaseInterfaces = new[] { typeof(IAuditFields), typeof(IHasAdminEmail) }

                };
            }
        }
    }

    public interface IExtendedFormContextFeature<TStaticContext, TModel>
        where TModel : DynamicEntity
        where TStaticContext : DynamicContext
    {
        IOptions<DynamicContextOptions> CreateOptions();
        IMigrationManager CreateMigrationManager();
        Guid EntityId { get; }
        SemVersion Version { get;}
        string SchemaName { get; }
        string ConnectionString { get; }
        Task LoadAsync(EAVDBContext<TStaticContext> database, Guid entityid, bool loadAllVersions = false);
        JToken[] Manifests { get; }
    }
    public static class AuditFieldsExtensions
    {
        static ulong BigEndianToUInt64(byte[] bigEndianBinary)
        {
            return ((ulong)bigEndianBinary[0] << 56) |
                   ((ulong)bigEndianBinary[1] << 48) |
                   ((ulong)bigEndianBinary[2] << 40) |
                   ((ulong)bigEndianBinary[3] << 32) |
                   ((ulong)bigEndianBinary[4] << 24) |
                   ((ulong)bigEndianBinary[5] << 16) |
                   ((ulong)bigEndianBinary[6] << 8) |
                           bigEndianBinary[7];
        }

        public static ulong GetVersion(this IAuditFields auditFields)
        {

            return BigEndianToUInt64(auditFields.RowVersion);
        }
    }

 
    public class DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument> : IFormContextFeature<TDynamicContext>, IExtendedFormContextFeature<TStaticContext, TModel>
        where TStaticContext : DynamicContext
        where TDynamicContext : DynamicManifestContext<TStaticContext, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields

    {
        public Guid EntityId { get; protected set; }
        public JToken Manifest { get; protected set; }
        public JToken[] Manifests { get; protected set; } = Array.Empty<JToken>();
        public string SchemaName { get; protected set; }
        public SemVersion Version { get; protected set; }
        public SemVersion LatestPublishedVersion { get; protected set; }
        public string ConnectionString { get; protected set; }
        public ulong DocumentVersion { get; private set; }

        private readonly ILogger<DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IDynamicCodeServiceFactory _dynamicCodeServiceFactory;
        private readonly IDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument> _dynamicManifestContextOptionFactory;

        public DynamicManifestContextFeature(ILoggerFactory loggerFactory, IMemoryCache memoryCache, IDynamicCodeServiceFactory dynamicCodeServiceFactory,
            IDynamicManifestContextOptionFactory<TStaticContext, TDynamicContext, TModel, TDocument> dynamicManifestContextOptionFactory)
        {
            _logger = loggerFactory.CreateLogger<DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument>>();
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
            _dynamicCodeServiceFactory = dynamicCodeServiceFactory;
            _dynamicManifestContextOptionFactory = dynamicManifestContextOptionFactory;
        }
        public virtual Task OnDataLoadedAsync(EAVDBContext<TStaticContext> database, Guid entityid,TModel data)
        {
            return Task.CompletedTask;
        }
        public virtual async Task LoadAsync(EAVDBContext<TStaticContext> database, Guid entityid, bool loadAllVersions = false)
        {



            _logger.LogInformation("Loading {TModel} {entityid} from database", typeof(TModel).Name, entityid);
            var record = await database.Set<TModel>().FindAsync(entityid);

            _logger.LogInformation("Loading {TDocument} {record.ManifestId} from database", typeof(TDocument).Name, entityid);
            var document = record.Manifest ?? await database.Set<TDocument>().FindAsync(record.ManifestId);

            //var document = await database.Set<TDocument>()
            //    .Where(x => x.Container == "manifests" && x.Path.StartsWith($"/{entityid}/manifests/manifest."))
            //    .OrderByDescending(c => c.CreatedOn)                
            //    .FirstOrDefaultAsync();

            var documentVersion = document.GetVersion();
            if (DocumentVersion == documentVersion)
            {
                return;
            }
            DocumentVersion = documentVersion;



            var stream = document.Compressed ?? false ?
              new GZipStream(new MemoryStream(document.Data), CompressionMode.Decompress) as Stream : new MemoryStream(document.Data);


            var target = new MemoryStream();


            await stream.CopyToAsync(target);

            var a = System.Text.Encoding.UTF8.GetString(target.ToArray());


            Manifest = JToken.Parse(a);
            SchemaName = record.Schema; 
            EntityId = entityid;
            Version = SemVersion.Parse(Manifest.SelectToken("$.version")?.ToString(), SemVersionStyles.Strict);// SemVersion.Parse(record.Version, SemVersionStyles.Strict);
            _logger.LogInformation("Loaded {SchemaName} {ManifestHashCode} at {version}|{parsedVersion}", SchemaName, a.GetHashCode(), Manifest.SelectToken("$.version")?.ToString(),Version);

            if (loadAllVersions)
            {
                try
                {
                    var latest_versions = await database.Set<TDocument>().Where(d => d.Path.StartsWith($"/{entityid}/manifests/") && d.Path.EndsWith(".g.json") )
                       .OrderByDescending(c => c.CreatedOn).ToArrayAsync();

                    var manifests = new List<JToken>();
                    foreach (var m in latest_versions.
                        OrderByDescending(k => SemVersion.Parse(k.Name.Substring(9, k.Name.Length - 9 - 7), SemVersionStyles.Strict), SemVersion.SortOrderComparer))
                    {
                        manifests.Add(await m.LoadJsonAsync());
                    }
                    Manifests = manifests.ToArray();

                    if (!Manifests.Any())
                    {
                        Manifests = new[]
                        {
                           
                           JToken.FromObject(new {entities = new { }, version = "0.0.0" }),
                         
                        };
                    }
                    LatestPublishedVersion = SemVersion.Parse(Manifests[0].SelectToken("$.version")?.ToString(), SemVersionStyles.Strict);
                    //Manifests = new[]
                    //      { Manifest
                    //    }.Concat(Manifests).ToArray();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to load manifests", ex);
                }
            }
            else
            {

                var latest = await _memoryCache.GetOrCreateAsync($"{entityid}{document.GetVersion()}", async cachekey =>
                {
                    cachekey.SetSize(1);

                    return await database.Set<TDocument>()
                    .Where(x => x.Container == "manifests" && x.Path.StartsWith($"/{entityid}/manifests/manifest."))
                    .OrderByDescending(c => c.CreatedOn)
                    .FirstOrDefaultAsync();
                });

                Manifests = new[] { await latest.LoadJsonAsync() };
                LatestPublishedVersion= SemVersion.Parse(Manifests[0].SelectToken("$.version")?.ToString(), SemVersionStyles.Strict);
            }

            await OnDataLoadedAsync(database, entityid, record);

        }
        public IOptions<DynamicContextOptions> CreateOptions()
        {
            return Options.Create(_dynamicManifestContextOptionFactory.CreateOptions(this));
        }


        private static ConcurrentDictionary<string, IMigrationManager> _managers = new ConcurrentDictionary<string, IMigrationManager>();
        public IMigrationManager CreateMigrationManager()
        {
            //  return new MigrationManager(_loggerFactory.CreateLogger<MigrationManager>());
            return _managers.GetOrAdd(EntityId.ToString() + SchemaName, (entry) =>
            {

                var o = _dynamicManifestContextOptionFactory.CreateOptions(this);
                //entry.Size = 1;
                // entry.(TimeSpan.FromHours(1));
                return new MigrationManager(_loggerFactory.CreateLogger<MigrationManager>(),
                    Options.Create(new MigrationManagerOptions
                    {
                        Namespace = o.Namespace,
                        SkipValidateSchemaNameForRemoteTypes = false,
                        RequiredSupport = o.RequiredSupport,// false,
                        Schema = SchemaName,
                        DTOBaseInterfaces = o.DTOBaseInterfaces,// new[] { typeof(IAuditFields), typeof(IHasAdminEmail) },
                        DTOAssembly = o.DTOAssembly ?? typeof(TModel).Assembly,
                        DTOBaseClasses = o.DTOBaseClasses ?? new[] { typeof(BaseOwnerEntity<>), typeof(BaseIdEntity<>) }
                    }), _dynamicCodeServiceFactory); 
            });
        }

        public ValueTask<JToken> GetManifestAsync()
        {
            return new ValueTask<JToken>(Manifest);
        }

        internal void AddNewManifest(JToken manifest)
        {
            Manifests = new[] { manifest }.Concat(Manifests).ToArray();
        }
    }
}