using EAVFramework;
using EAVFW.Extensions.SecurityModel;
using EAVFW.Extensions.Documents;
using EAVFramework.Validation;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    public class MyDefaultDynamicManifestContextOptionFactory<TDynamicContext, TModel, TDocument>
       : IDynamicManifestContextOptionFactory<DynamicContext, TDynamicContext, TModel, TDocument>
       where TDynamicContext : DynamicManifestContext<DynamicContext, TModel, TDocument>
       where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
       where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {
        public virtual DynamicContextOptions CreateOptions(IExtendedFormContextFeature<DynamicContext, TModel> feature)
        {
            {
                return new DynamicContextOptions
                {
                    Manifests = feature.Manifests,
                    Schema = feature.SchemaName,
                    EnableDynamicMigrations = true,
                    Namespace = $"EAVFW.Extensions.DynamicManifest.{feature.SchemaName?.Replace("-", "_")}.Model",

                    //UseOnlyExpliciteExternalDTOClases = true,
                    DTOAssembly = typeof(TModel).Assembly,
                    DTOBaseClasses = new[]
                    {
                        typeof(BaseOwnerEntity<>), typeof(BaseIdEntity<>), 
                        typeof(BaseTargetMigrationEntity),
                        typeof(BaseSourceMigrationEntity)
                    },
                    DisabledPlugins = new[] { typeof(RequiredPlugin) }
                };
            }
        }

    }
}