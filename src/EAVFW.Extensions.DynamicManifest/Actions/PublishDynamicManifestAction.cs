using EAVFramework;
using EAVFramework.Endpoints;
using EAVFramework.Extensions;
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
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EAVFW.Extensions.DynamicManifest
{
    public class PublishDynamicManifestAction<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument> : IActionImplementation
        where TStaticContext : DynamicContext
        where TDynamicManifestContextFeature : DynamicManifestContextFeature<TStaticContext, TDynamicContext, TModel, TDocument>
        where TDynamicContext : DynamicManifestContext<TStaticContext, TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>, IAuditFields
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields, new()
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

        public async ValueTask PublishAsync(
            Guid id, string identityid, bool enrich, bool runscript)

        {

            var (feat, context) = await _serviceProvider.GetDynamicManifestContext<TStaticContext, TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument>(id, true);

            var manifest = enrich ?
                JToken.Parse((await _serviceProvider.GetRequiredService<IManifestEnricher>().LoadJsonDocumentAsync(feat.Manifest, "", _logger)).RootElement.ToString())
                : feat.Manifest;

            var version = feat.Version;

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


            var conn = context.Context.Database.GetDbConnection();

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();


            try
            {
                ///Fix old
                {

                    if (!string.IsNullOrEmpty(feat.SchemaName))
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE [{feat.SchemaName}].[__MigrationsHistory] SET [MigrationId] = '{feat.SchemaName}_1_0_0' WHERE[MigrationId] = '{feat.SchemaName}_Initial'";
                        var r = await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                //Swallow for none existing 
            }


            context.Context.AddNewManifest(manifest);
            //   await context.MigrateAsync();


            var migrator = context.Context.Database.GetInfrastructure().GetRequiredService<IMigrator>();
            var sqlscript = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent| MigrationsSqlGenerationOptions.Default| MigrationsSqlGenerationOptions.NoTransactions);

            foreach (var sql in sqlscript.Split("GO"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 300;
                cmd.CommandText = sql;
                //  await context.Context.Database.ExecuteSqlRawAsync(sql);


                var r = await cmd.ExecuteNonQueryAsync();
            }

            if (runscript)
            {


                ///Fix old
                {
                    var entry = await _database.FindAsync<TModel>(id);

                    if (entry is IHasAdminEmail adminemailRecord)
                    {
                        var permis = _serviceProvider.GetRequiredService<IManifestPermissionGenerator>();
                        var cmdTxt = await permis.CreateInitializationScript(JsonSerializer.Deserialize<ManifestDefinition>(manifest.ToString()), "systemusers");

                        cmdTxt = cmdTxt.Replace("@DBName", conn.Database)
                            .Replace("@DBSchema", feat.SchemaName);

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = cmdTxt;

                        var dbname = cmd.CreateParameter();
                        dbname.ParameterName = "@DBName";
                        dbname.Value = conn.Database;
                        cmd.Parameters.Add(dbname);

                        var dbschema = cmd.CreateParameter();
                        dbschema.ParameterName = "@DBSchema";
                        dbschema.Value = feat.SchemaName;
                        cmd.Parameters.Add(dbschema);


                        using (MD5 md5 = MD5.Create())
                        {
                            //  byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(feat.SchemaName+ entry.Id.ToString().ToLower()));
                            //  Guid result = new Guid(hash);


                            var userGuid = cmd.CreateParameter();
                            userGuid.ParameterName = "@UserGuid";
                            userGuid.Value = entry.Id;
                            cmd.Parameters.Add(userGuid);


                        }

                        var userEmail = cmd.CreateParameter();
                        userEmail.ParameterName = "@UserEmail";
                        userEmail.Value = adminemailRecord.AdminEmail;
                        cmd.Parameters.Add(userEmail);

                        var userName = cmd.CreateParameter();
                        userName.ParameterName = "@UserName";
                        userName.Value = adminemailRecord.AdminEmail;
                        cmd.Parameters.Add(userName);

                        var systemAdminSecurityGroupId = cmd.CreateParameter();
                        systemAdminSecurityGroupId.ParameterName = "@SystemAdminSecurityGroupId";
                        systemAdminSecurityGroupId.Value = Guid.Parse("1b714972-8d0a-4feb-b166-08d93c6ae328");
                        cmd.Parameters.Add(systemAdminSecurityGroupId);

                        if (conn.State != System.Data.ConnectionState.Open)
                            await conn.OpenAsync();

                        var sqlinit = new TDocument
                        {
                            Name = $"manifest.{version.ToString()}.sql",
                            Path = $"/{feat.EntityId}/manifests/{version.ToString()}/permisions.sql",
                            Container = "manifests",
                            Compressed = true,
                            ContentType = "application/json",
                        };
                        await sqlinit.SaveTextAsync(sqlscript);
                        _database.Set<TDocument>().Add(sqlinit);

                        cmd.CommandTimeout = 300;
                        var r = await cmd.ExecuteNonQueryAsync();



                    }

                }



            }





            var sqldoc = new TDocument
            {
                Name = $"manifest.{version.ToString()}.sql",
                Path = $"/{feat.EntityId}/manifests/{version.ToString()}/migration.sql",
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

            //We should not save the enriched manifest
            var existingManifest = await document.LoadJsonAsync();
            existingManifest["version"] = record.Version;
            await document.SaveJsonAsync(existingManifest);

            await _database.SaveChangesAsync(
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub", identityid ?? "1b714972-8d0a-4feb-b166-08d93c6ae328")
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme)));


            _database.Context.ResetMigrationsContext();


        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {

            var inputs = action.Inputs;
            var recordId = Guid.Parse(inputs["recordId"]?.ToString());
            var dynamicManifestEntityName = inputs["dynamicManifestEntityCollectionSchemaName"]?.ToString() ?? "DataModelProjects";
            var documentEntityName = inputs["documentEntityCollectionSchemaName"]?.ToString() ?? "Documents";
            var enrichParsed = bool.TryParse(inputs["enrichManifest"].ToString(), out var enrich);
            var runSecurityModelInitializationScriptParsed = bool.TryParse(inputs["runSecurityModelInitializationScript"].ToString(), out var runSecurityModelInitializationScript);

            //   var method = _propertyInfo.MakeGenericMethod(_database.Context.GetEntityType(dynamicManifestEntityName), _database.Context.GetEntityType(documentEntityName));

            // var valueTask = (ValueTask)method.Invoke(this, new object[] { context,recordId });



            await PublishAsync(recordId, context.PrincipalId, enrichParsed && enrich, runSecurityModelInitializationScriptParsed && runSecurityModelInitializationScript);





            return null;

        }
    }
}