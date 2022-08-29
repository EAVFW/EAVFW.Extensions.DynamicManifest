using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Validation;
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



        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> _dynamicManifestContextOptionFactory;

        public DynamicManifestContextFeature(ILoggerFactory loggerFactory, IMemoryCache memoryCache,
            IDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument> dynamicManifestContextOptionFactory)
        {
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
            _dynamicManifestContextOptionFactory = dynamicManifestContextOptionFactory;
        }
        public virtual void OnDataLoaded(TModel data)
        {

        }
        public virtual async Task LoadAsync(DynamicContext database, Guid entityid, bool loadAllVersions = false)
        {
            var record = await database.Set<TModel>().FindAsync(entityid);
            var document = record.Manifest ?? await database.Set<TDocument>().FindAsync(record.ManifestId);

            var stream = new GZipStream(new MemoryStream(document.Data), CompressionMode.Decompress);
            var target = new MemoryStream();
            await stream.CopyToAsync(target);

            var a = System.Text.Encoding.UTF8.GetString(target.ToArray());

            EntityId = entityid;
            Manifest = JToken.Parse(a);
            SchemaName = record.Schema;
            Version = SemVersion.Parse(Manifest.SelectToken("$.version")?.ToString(), SemVersionStyles.Strict);// SemVersion.Parse(record.Version, SemVersionStyles.Strict);

            //  Manifest["version"] = Version.ToString();

            if (loadAllVersions)
            {
                var latest_versions = await database.Set<TDocument>().Where(d => d.Path.StartsWith($"/{entityid}/manifests/manifest."))
                   .OrderByDescending(c => c.CreatedOn).ToArrayAsync();

                Manifests = await Task.WhenAll(latest_versions.
                    OrderByDescending(k => SemVersion.Parse(k.Name.Substring(9, k.Name.Length - 9 - 7), SemVersionStyles.Strict))
                    .Select(c => c.LoadJsonAsync()));

                Manifests = new[]
                      { Manifest
                }.Concat(Manifests).ToArray();
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