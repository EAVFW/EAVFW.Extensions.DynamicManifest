using EAVFramework.Shared;
using EAVFW.Extensions.Documents;
using System;

namespace EAVFW.Extensions.DynamicManifest
{
    [EntityInterface(EntityKey = "*")]
    public interface IDynamicManifestEntity<T> where T : IDocumentEntity
    {
        public Guid Id { get; set; }
        public string Schema { get; set; }
        public string Version { get; set; }
        public Guid? ManifestId { get; set; }
        public T Manifest { get; set; }

        public DateTime? CreatedOn { get; set; }
        public byte[] RowVersion { get; set; }
    }
}