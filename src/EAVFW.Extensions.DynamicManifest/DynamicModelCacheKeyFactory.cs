using EAVFramework;
using EAVFramework.Endpoints;
using EAVFramework.Extensions;
using EAVFramework.Shared;
using EAVFramework.Validation;
using EAVFW.Extensions.Documents;
using EAVFW.Extensions.SecurityModel;
using ExpressionEngine;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WorkflowEngine;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DynamicManifest
{
    public class DynamicModelCacheKeyFactory<TStaticContext,TDynamicContext, TDynamicManifestContextFeature, TModel, TDocument> : IModelCacheKeyFactory
        where TDynamicManifestContextFeature : IExtendedFormContextFeature<TStaticContext,TModel>
        where TStaticContext : DynamicContext
        where TDynamicContext : DynamicManifestContext<TStaticContext,TModel, TDocument>
        where TModel : DynamicEntity, IDynamicManifestEntity<TDocument>
        where TDocument : DynamicEntity, IDocumentEntity, IAuditFields
    {

        public object Create(DbContext context, bool designTime)
            => context is IHasModelCacheKey dynamicContext
                ? (context.GetType(), dynamicContext.ModelCacheKey, designTime)
                : (object)context.GetType();

        public object Create(DbContext context)
            => Create(context, false);
    }
}