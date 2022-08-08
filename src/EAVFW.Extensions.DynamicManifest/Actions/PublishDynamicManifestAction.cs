using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using EAVFW.Extensions.Documents;
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
    public class PublishDynamicManifestAction<TStaticContext> : IActionImplementation
      where TStaticContext : DynamicContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EAVDBContext<TStaticContext> _database;
        private readonly ILogger<PublishDynamicManifestAction<TStaticContext>> _logger;
        private readonly IExpressionEngine _expressionEngine;

        static MethodInfo _propertyInfo = typeof(PublishDynamicManifestAction<TStaticContext>).GetMethod(nameof(PublishAsync));

        public PublishDynamicManifestAction(IServiceProvider serviceProvider, EAVDBContext<TStaticContext> database, ILogger<PublishDynamicManifestAction<TStaticContext>> logger, IExpressionEngine expressionEngine)
        {
            _serviceProvider = serviceProvider;
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger;
            _expressionEngine = expressionEngine;
        }

        public async ValueTask PublishAsync<TModel, TDocument>(IRunContext runContext,Guid id)
            where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
            where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
        {

            var (feat, context) = await _serviceProvider.GetDynamicManifestContext<TModel, TDocument>(id, true);


            var latest_version = await _database.Set<TDocument>().Where(d => d.Path.StartsWith($"/{feat.EntityId}/manifests/manifest."))
                .OrderByDescending(c => c.CreatedOn).FirstOrDefaultAsync();

            if (latest_version != null)
            {
                var prior = await latest_version.LoadJsonAsync();
                var current = feat.Manifest.DeepClone();
                prior["version"].Parent.Remove();
                current["version"].Parent.Remove();
                if (JToken.DeepEquals(prior, current))
                {
                    _logger.LogWarning("The prior version is the same as publishing, skipping");
                    return;
                }
            }


            var migrator = context.Context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sqlscript = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

            using var conn = context.Context.Database.GetDbConnection();
            await conn.OpenAsync();

            foreach (var sql in sqlscript.Split("GO"))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                //  await context.Context.Database.ExecuteSqlRawAsync(sql);


                var r = await cmd.ExecuteNonQueryAsync();
            }

            // await context.MigrateAsync();

            var manifest = feat.Manifest;
            var version = feat.Version;

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
                                   new Claim("sub", runContext.PrincipalId ?? "1b714972-8d0a-4feb-b166-08d93c6ae328")
                                }, DotNetDevOps.Extensions.EAVFramework.Constants.DefaultCookieAuthenticationScheme)));




        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {

            var inputs = action.Inputs;
            var recordId = Guid.Parse(inputs["recordId"]?.ToString());
            var dynamicManifestEntityName = inputs["dynamicManifestEntityCollectionSchemaName"]?.ToString() ?? "DataModelProjects";
            var documentEntityName = inputs["documentEntityCollectionSchemaName"]?.ToString() ?? "Documents";

            var method = _propertyInfo.MakeGenericMethod(_database.Context.GetEntityType(dynamicManifestEntityName), _database.Context.GetEntityType(documentEntityName));

            var valueTask = (ValueTask)method.Invoke(this, new object[] { context,recordId });



            await valueTask;



            return null;

        }
    }
}