using EAVFramework;
using System;
using EAVFramework.Shared;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TBase", ManifestKey = "BaseMigrationEntity")]
    public class BaseMigrationEntity : DynamicEntity
    {
        [DataMember(Name = "migrationentityid")]
        [JsonProperty("migrationentityid")]
        [JsonPropertyName("migrationentityid")]
        public Guid? MigrationEntityId { get; set; }

        //[ForeignKey("MigrationEntityId")]
        //[JsonProperty("migrationentity")]
        //[JsonPropertyName("migrationentity")]
        //[DataMember(Name = "migrationentity")]
        //public MigrationProject MigrationEntity { get; set; }
    }
}