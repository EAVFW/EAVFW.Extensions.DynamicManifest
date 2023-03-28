using EAVFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAVFW.Extensions.DynamicManifest.UnitTests.Models
{
    public class MyDynamicContext : DynamicManifestContext<DynamicContext, Form, Document>
    {
        protected MyDynamicContext(DynamicManifestContextFeature<DynamicContext, MyDynamicContext, Form, Document> feature,
            DbContextOptions<MyDynamicContext> options,
            IOptions<DynamicContextOptions> modelOptions,
            IMigrationManager migrationManager,
            ILogger logger) : base(options, feature, logger)
        {

        }
    }
}