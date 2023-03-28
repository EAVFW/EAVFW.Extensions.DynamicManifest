using System;
using EAVFramework.Shared;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TBase", ManifestKey = "BaseSourceMigrationEntity")]
    public class BaseSourceMigrationEntity : BaseMigrationEntity
    {
        [DataMember(Name = "id")]
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}