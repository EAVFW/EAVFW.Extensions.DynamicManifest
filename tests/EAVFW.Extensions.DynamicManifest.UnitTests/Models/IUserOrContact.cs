using EAVFramework.Shared;

namespace EAVFW.Extensions.DynamicManifest.UnitTests.Models
{
    [EntityInterface(EntityKey = "Contact")]
    [EntityInterface(EntityKey = "System User")]

    public interface IUserOrContact
    {
        public string Email { get; set; }
        public string Name { get; set; }

        //  public Guid Id { get; set; }
    }
}