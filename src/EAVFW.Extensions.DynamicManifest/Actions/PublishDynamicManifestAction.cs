using EAVFramework;
using EAVFramework.Endpoints;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.Manifest.SDK;
using ExpressionEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DynamicManifest
{
    public class PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument> : IActionImplementation
        where TStaticContext : DynamicContext
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TStaticContext,TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TStaticContext,TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields,new()
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EAVDBContext<TStaticContext> _database;
        private readonly ILogger<PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>> _logger;
        private readonly IExpressionEngine _expressionEngine;

//        static MethodInfo _propertyInfo = typeof(PublishDynamicManifestAction<TContext>).GetMethod(nameof(PublishAsync));


        public PublishDynamicManifestAction(
            
            IServiceProvider serviceProvider, EAVDBContext<TStaticContext> database, ILogger<PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>> logger, IExpressionEngine expressionEngine)
        {
            _serviceProvider = serviceProvider;
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger;
            _expressionEngine = expressionEngine;
        }

        public async ValueTask PublishAsync(Guid id, string identityid, bool enrich)
           
        {

            var (feat, context) = await _serviceProvider.GetDynamicManifestContext<TStaticContext,TDynamicContext,TDynamicManifestContextFeature,TModel, TDocument>(id, true);

            var manifest = enrich ?
                JToken.Parse((await _serviceProvider.GetRequiredService<IManifestEnricher>().LoadJsonDocumentAsync(feat.Manifest, "", _logger)).RootElement.ToString())
                : feat.Manifest;


            var latest_version = feat.Manifests.FirstOrDefault();  //await _database.Set<TDocument>().Where(d => d.Path.StartsWith($"/{feat.EntityId}/manifests/manifest."))
               // .OrderByDescending(c => c.CreatedOn).FirstOrDefaultAsync();

            if (latest_version != null)
            {
                var prior = latest_version.DeepClone();  // await latest_version.LoadJsonAsync();
                var current = manifest.DeepClone();
                prior["version"].Parent.Remove();
                current["version"].Parent.Remove();
                if (JToken.DeepEquals(prior, current))
                {
                    _logger.LogWarning("The prior version is the same as publishing, skipping");
                    return;
                }
            }

               
            using var conn = context.Context.Database.GetDbConnection();
            await conn.OpenAsync();


            try
            {
                ///Fix old
                {

                    if (latest_version == null && !string.IsNullOrEmpty(feat.SchemaName))
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE [{feat.SchemaName}].[__MigrationsHistory] SET [MigrationId] = '{feat.SchemaName}_1_0_0' WHERE[MigrationId] = '{feat.SchemaName}_Initial'";
                        var r = await cmd.ExecuteNonQueryAsync();
                    }
                }
            }catch(Exception ex)
            {
                //Swallow for none existing 
            }


            feat.AddNewManifest(manifest);
            context.ResetMigrationsContext();
            await context.MigrateAsync();

            var migrator = context.Context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sqlscript = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
             
            var version = feat.Version;

            var sqldoc = new TDocument
            {
                Name = $"manifest.{version.ToString()}.sql",
                Path = $"/{feat.EntityId}/manifests/migration.{version.ToString()}.sql",
                Container = "manifests",
                Compressed = true,
                ContentType = "application/json",
            };
            await sqldoc.SaveTextAsync(sqlscript);
            _database.Set<TDocument>().Add(sqldoc);

            var doc = new TDocument
            {
                Name = $"manifest.{version.ToString()}.g.json",
                Path = $"/{feat.EntityId}/manifests/manifest.{version.ToString()}.g.json",
                Container = "manifests",
                Compressed = true,
                ContentType = "application/json",
            };
            await doc.SaveJsonAsync(manifest);

            _database.Set<TDocument>().Add(doc);

            var record = await _database.Set<TModel>().FindAsync(feat.EntityId);
            var document = await _database.Set<TDocument>().FindAsync(record.ManifestId);
            _database.Context.Attach(record);
            _database.Context.Attach(document);
            record.Version = feat.Version.WithPatch(feat.Version.Patch + 1).ToString();
            feat.Manifest["version"] = record.Version;
            await document.SaveJsonAsync(feat.Manifest);

            await _database.SaveChangesAsync(
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub", identityid ?? "1b714972-8d0a-4feb-b166-08d93c6ae328")
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme)));




        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {

            var inputs = action.Inputs;
            var recordId = Guid.Parse(inputs["recordId"]?.ToString());
            var dynamicManifestEntityName = inputs["dynamicManifestEntityCollectionSchemaName"]?.ToString() ?? "DataModelProjects";
            var documentEntityName = inputs["documentEntityCollectionSchemaName"]?.ToString() ?? "Documents";
            var enrichParsed = bool.TryParse(inputs["enrichManifest"].ToString(), out var enrich);
         //   var method = _propertyInfo.MakeGenericMethod(_database.Context.GetEntityType(dynamicManifestEntityName), _database.Context.GetEntityType(documentEntityName));

           // var valueTask = (ValueTask)method.Invoke(this, new object[] { context,recordId });



            await PublishAsync( recordId,context.PrincipalId, enrichParsed && enrich);



            return null;

        }
    }
}