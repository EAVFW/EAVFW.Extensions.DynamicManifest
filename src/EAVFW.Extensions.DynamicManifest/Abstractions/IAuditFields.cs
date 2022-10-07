using EAVFramework.Shared;
using System;

namespace EAVFW.Extensions.DynamicManifest
{
    [EntityInterface(EntityKey = "*")]
    public interface IAuditFields
    {
        public Guid? ModifiedById { get; set; }
        public Guid? CreatedById { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public byte[] RowVersion { get; set; }
    }
}