using System;
using EAVFramework.Shared;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TBase", ManifestKey = "BaseTargetMigrationEntity")]
    public class BaseTargetMigrationEntity : BaseMigrationEntity
    {
        [DataMember(Name = "id")]
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [DataMember(Name = "targetid")]
        [JsonProperty("targetid")]
        [JsonPropertyName("targetid")]
        public string TargetId { get; set; }

        [DataMember(Name = "sourceid")]
        [JsonProperty("sourceid")]
        [JsonPropertyName("sourceid")]
        public Guid? SourceId { get; set; }
    }
}