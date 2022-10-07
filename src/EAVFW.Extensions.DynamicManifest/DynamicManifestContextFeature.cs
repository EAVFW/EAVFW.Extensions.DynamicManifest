using EAVFramework;
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
    public interface IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument>    
        where TDynamicContext : DynamicManifestContext<TModel,TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {
        public DynamicContextOptions CreateOptions(IExtendedFormContextFeature<TModel> feature);
    }

    public class DefaultDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> 
        : IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> 
        where TDynamicContext : DynamicManifestContext<TModel,TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {
        public virtual DynamicContextOptions CreateOptions(IExtendedFormContextFeature<TModel> feature)
        {
            {
                return new DynamicContextOptions
                {
                    Manifests = feature.Manifests,
                    PublisherPrefix = feature.SchemaName,
                    EnableDynamicMigrations = true,
                    Namespace = $"EAVFW.Extensions.DynamicManifest.{feature.SchemaName?.Replace("-", "_")}.Model",
                    UseOnlyExpliciteExternalDTOClases = true,
                    DTOAssembly = typeof(TModel).Assembly,
                    DTOBaseClasses = new[] {typeof(BaseOwnerEntity<>), typeof(BaseIdEntity<>)},
                    DisabledPlugins = new[] {typeof(RequiredPlugin)}
                };
            }
        }
    } 

    public interface IExtendedFormContextFeature<TModel> 
        where TModel : DynamicEntity      
    {
        IOptions<DynamicContextOptions> CreateOptions();
        IMigrationManager CreateMigrationManager();
        Guid EntityId { get;  }
        string SchemaName { get; }
        string ConnectionString { get; }
        Task LoadAsync(DynamicContext database, Guid entityid, bool loadAllVersions = false);
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
    public class DynamicManifestContextFeature<TDynamicContext, TModel, TDocument> : IFormContextFeature<TDynamicContext>, IExtendedFormContextFeature<TModel>
        where TDynamicContext : DynamicManifestContext<TModel,TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields

    {
        public Guid EntityId { get; protected set; }
        public JToken Manifest { get; protected set; }
        public JToken[] Manifests { get; protected set; } = Array.Empty<JToken>();
        public string SchemaName { get; protected set; }
        public SemVersion Version { get; protected set; }
        public string ConnectionString { get; protected set; }
        public ulong DocumentVersion { get; private set; }

        private readonly ILogger<DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>> logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> _dynamicManifestContextOptionFactory;

        public DynamicManifestContextFeature(ILogger<DynamicManifestContextFeature<TDynamicContext, TModel, TDocument>> logger, ILoggerFactory loggerFactory, IMemoryCache memoryCache,
            IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> dynamicManifestContextOptionFactory)
        {
            this.logger = logger;
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
            _dynamicManifestContextOptionFactory = dynamicManifestContextOptionFactory;
        }
        public virtual void OnDataLoaded(TModel data)
        {

        }
        public virtual async Task LoadAsync(DynamicContext database, Guid entityid, bool loadAllVersions = false)
        {
          
          
            
                logger.LogInformation("Loading {TModel} {entityid} from database", typeof(TModel).Name, entityid);
                var record = await database.Set<TModel>().FindAsync(entityid);

                logger.LogInformation("Loading {TDocument} {record.ManifestId} from database", typeof(TDocument).Name, entityid);
                var document = record.Manifest ?? await database.Set<TDocument>().FindAsync(record.ManifestId);


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

                logger.LogInformation("Loaded {SchemaName} {ManifestHashCode} at {version}", SchemaName, a.GetHashCode(), Version);
            
           

            EntityId = entityid; 
            Version = SemVersion.Parse(Manifest.SelectToken("$.version")?.ToString(), SemVersionStyles.Strict);// SemVersion.Parse(record.Version, SemVersionStyles.Strict);
             
            if (loadAllVersions)
            {
                try
                {
                    var latest_versions = await database.Set<TDocument>().Where(d => d.Path.StartsWith($"/{entityid}/manifests/manifest."))
                       .OrderByDescending(c => c.CreatedOn).ToArrayAsync();

                    var manifests = new List<JToken>();
                    foreach (var m in latest_versions.
                        OrderByDescending(k => SemVersion.Parse(k.Name.Substring(9, k.Name.Length - 9 - 7), SemVersionStyles.Strict)))
                    {
                        manifests.Add(await m.LoadJsonAsync());
                    }
                    Manifests = manifests.ToArray();

                    //Manifests = await Task.WhenAll(latest_versions.
                    //    OrderByDescending(k => SemVersion.Parse(k.Name.Substring(9, k.Name.Length - 9 - 7), SemVersionStyles.Strict))
                    //    .Select(c => c.LoadJsonAsync()));

                    Manifests = new[]
                          { Manifest
                }.Concat(Manifests).ToArray();
                }catch(Exception ex)
                {
                    throw new Exception("Failed to load manifests", ex);
                }
            }
            else
            {
                Manifests = new[] { Manifest };
            }

            OnDataLoaded(record);

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

                //entry.Size = 1;
                // entry.(TimeSpan.FromHours(1));
                return new MigrationManager(_loggerFactory.CreateLogger<MigrationManager>(),
                    Options.Create(new MigrationManagerOptions
                    {
                        SkipValidateSchemaNameForRemoteTypes = false,
                        RequiredSupport = false

                    }));
            });
        }

        public ValueTask<JToken> GetManifestAsync()
        {
            return new ValueTask<JToken>(Manifest);
        }
    }
}