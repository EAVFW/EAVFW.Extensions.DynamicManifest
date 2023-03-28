using EAVFramework;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using EAVFramework.Endpoints;
using EAVFW.Extensions.Manifest.SDK;
using Microsoft.Extensions.Logging.Abstractions;
using EAVFW.Extensions.DynamicManifest.UnitTests.Helpers;
using EAVFW.Extensions.DynamicManifest.UnitTests.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{

    [TestClass]
    public class MaasTests : BaseDynamicFormUnitTest
    {
        [TestMethod]
        [DeploymentItem(@"specs/maas/maas.g.json")]
        [DeploymentItem(@"specs/maas/project001.json")]
        public async Task TestMaas()
        {
            var (rootServiceProvider, principalId, prinpal) = await Setup< MigrationProject,Document>("specs/maas/maas.g.json","maas");

            var test = JToken.Parse(File.ReadAllText("specs/maas/project001.json"));
            var enrich = rootServiceProvider.GetService<IManifestEnricher>();

            var a = await enrich.LoadJsonDocumentAsync(test, "", NullLogger.Instance);
            var b = a.ToString();
            
            Guid? formid = await CreateFormAsync<MigrationProject,Document>(rootServiceProvider, prinpal, new MigrationProject
            {
                Schema = "BK-001",
                Name = "Test Form",               
                Manifest = new Document
                {
                    Name = "manifest.json",
                    Container = "manifests",
                    Compressed = false,
                    Data = File.ReadAllBytes("specs/maas/project001.json")
                },
                MigrationEntities = new[]
                {
                    new MigrationEntity{ SourceName = "Source 1", TargetName="Target 1"},
                    new MigrationEntity{ SourceName = "Source 2", TargetName="Target 2"}
                }
            });

            await PublishAsync<MigrationProject,Document>(rootServiceProvider, formid, true, true);


            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                var (feature, context) = await sp.GetDynamicManifestContext<MigrationProject, Document>(formid.Value, false);

                var source = context.Context.GetEntityType("SourceAvailibleCheckers");
                var target = context.Context.GetEntityType("TargetConnections");


            }



        }
        
    }

    
    [TestClass]
    public class CDMTests : BaseDynamicFormUnitTest
    {  
        [TestMethod]
        [DeploymentItem(@"specs/manifest.cdm.json")]
        public async Task TestCMDFile()
        {
            var (rootServiceProvider, principalId, prinpal) = await Setup<Form,Document>("specs/dynamicmodelmanifest.json");


            var test = JToken.Parse(File.ReadAllText("specs/manifest.cdm.json"));
            var enrich = rootServiceProvider.GetService<IManifestEnricher>();

            var a = await enrich.LoadJsonDocumentAsync(test,"",NullLogger.Instance);
            var b = a.ToString();

            Guid? formid = await CreateFormAsync<Form,Document>(rootServiceProvider, prinpal, new Form
            {
                Schema = "BK-001",
                Name = "Test Form",
                AdminEmail ="poul@kjeldager.com",
                Manifest = new Document
                {
                    Name = "manifest.json",
                    Container = "manifests",
                    Compressed = false,
                    Data = File.ReadAllBytes("specs/manifest.cdm.json")
                }
            });

            await PublishAsync<Form, Document>(rootServiceProvider, formid,true,true);

        }
           
        [TestMethod]
        [DeploymentItem(@"specs/dynamicmodelmanifest.sql")]
        [DeploymentItem(@"specs/dyn_form_manifest_1_0_0.json")]
        [DeploymentItem(@"specs/dyn_form_manifest_1_0_1.json")]
        public async Task TestNoSchemaName()
        {
            var (rootServiceProvider, principalId, prinpal) = await Setup<Form,Document>("specs/dynamicmodelmanifest.json");


            Guid? formid = await CreateFormAsync<Form, Document>(rootServiceProvider, prinpal, new Form
            {
                Schema = "BK-001",
                Name = "Test Form",
                Manifest = new Document
                {
                    Name = "manifest.json",
                    Container = "manifests",
                    Compressed = false,
                    Data = File.ReadAllBytes("specs/dyn_form_manifest_1_0_0.json")
                }
            });

          //  await PublishAsync<Form, Document>(rootServiceProvider, formid);
         //   await UpdateForm(rootServiceProvider, prinpal, formid, (form,ctx) => { form.Schema = "BK-001"; });

             await PublishAsync<Form, Document>(rootServiceProvider, formid); //Nothing happens here as version has not changed.


            var recordId = Guid.Empty;

            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                var (feature, formContext) = await sp.GetDynamicManifestContext<Form, Document>(formid.Value, false);


               var entry= formContext.Add("FormSubmissions", JToken.FromObject(new { name = "Poul" }));
                recordId = (Guid)entry.Property("Id").CurrentValue;

                var operation = await formContext.SaveChangesAsync(prinpal);

            }

          

            await UpdateForm(rootServiceProvider, prinpal, formid, (form,ctx) => {
                form.Manifest = ctx.Context.Find<Document>(form.ManifestId);
                form.Manifest.Compressed = false;
                form.Manifest.Data = File.ReadAllBytes("specs/dyn_form_manifest_1_0_1.json");

                });

            await PublishAsync<Form, Document>(rootServiceProvider, formid); //Nothing happens here as version has not changed.


            using (var scope = rootServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {

                var sp = scope.ServiceProvider;
                var ctx = sp.GetRequiredService<EAVDBContext<DynamicContext>>();

                var (feature, formContext) = await sp.GetDynamicManifestContext<Form, Document>(formid.Value, false);

                await formContext.PatchAsync("FormSubmissions", recordId, JToken.FromObject(new { name = "Poul", email="poul@kjeldager.com" }));
                 
                var operation = await formContext.SaveChangesAsync(prinpal);

            }

        }

        
    }
}