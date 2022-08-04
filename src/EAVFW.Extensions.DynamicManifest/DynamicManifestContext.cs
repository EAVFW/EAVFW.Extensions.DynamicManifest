using DotNetDevOps.Extensions.EAVFramework;
using EAVFW.Extensions.Documents;
using Microsoft.EntityFrameworkCore;

namespace EAVFW.Extensions.DynamicManifest
{
    public class DynamicManifestContext<TModel, TDocument> :
       DynamicContext, IHasModelCacheKey
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {

        private readonly DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument> _feature;
        public string ModelCacheKey => _feature.EntityId.ToString() + _feature.SchemaName;

        public DynamicManifestContext(
            DbContextOptions<DynamicManifestContext<TModel, TDocument>> options,
            DynamicManifestContextFeature<DynamicManifestContext<TModel, TDocument>, TModel, TDocument> feature,
            Microsoft.Extensions.Logging.ILogger<DynamicManifestContext<TModel, TDocument>> logger)
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