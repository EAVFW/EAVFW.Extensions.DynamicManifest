﻿using EAVFramework;
using EAVFW.Extensions.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EAVFW.Extensions.DynamicManifest
{
    public class DynamicManifestContext<TStaticContext,TModel, TDocument> :

       DynamicContext, IHasModelCacheKey
        where TStaticContext : DynamicContext
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {

        private readonly IExtendedFormContextFeature<TStaticContext,TModel> _feature;
      //  public string ModelCacheKey => _feature.EntityId.ToString() + _feature.SchemaName;

        public DynamicManifestContext(
            DbContextOptions<DynamicManifestContext<TStaticContext,TModel, TDocument>> options,
            IExtendedFormContextFeature<TStaticContext,TModel> feature,
            Microsoft.Extensions.Logging.ILogger<DynamicManifestContext<TStaticContext,TModel, TDocument>> logger)
            : base(options, feature.CreateOptions(), feature.CreateMigrationManager(), logger)
        {
            _feature = feature;
            ModelCacheKey = _feature.EntityId.ToString() + _feature.SchemaName +_feature.Version.ToString();
            ChangeTracker.LazyLoadingEnabled = false;
        }

        protected DynamicManifestContext(
          DbContextOptions options,
          IExtendedFormContextFeature<TStaticContext,TModel> feature,
          ILogger logger)
          : base(options, feature.CreateOptions(), feature.CreateMigrationManager(), logger)
        {
            _feature = feature;
            ModelCacheKey = _feature.EntityId.ToString() + _feature.SchemaName + _feature.Version.ToString();
            ChangeTracker.LazyLoadingEnabled = false;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            base.OnConfiguring(optionsBuilder);
        }

    }
}