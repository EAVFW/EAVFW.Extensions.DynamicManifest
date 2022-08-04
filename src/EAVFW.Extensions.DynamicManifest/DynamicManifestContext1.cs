using DotNetDevOps.Extensions.EAVFramework;
using EAVFW.Extensions.Documents;
using Microsoft.EntityFrameworkCore;

namespace EAVFW.Extensions.DynamicManifest
{
    public class DynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument> : DynamicContext, IHasModelCacheKey
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<DynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument>, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {

        private readonly TDynamicManifestContextFeature _feature;
        public string ModelCacheKey => _feature.EntityId.ToString() + _feature.SchemaName;

        public DynamicManifestContext(
            DbContextOptions<DynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument>> options,
            TDynamicManifestContextFeature feature,
            Microsoft.Extensions.Logging.ILogger<DynamicManifestContext<TDynamicManifestContextFeature, TModel, TDocument>> logger)
            : base(options, feature.CreateOptions(), feature.CreateMigrationManager(), logger)
        {
            _feature = feature;

            ChangeTracker.LazyLoadingEnabled = false;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            base.OnConfiguring(optionsBuilder);
        }

    }
}