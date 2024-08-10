using EAVFramework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DynamicManifest.UnitTests
{
    [TestClass]
    public class WorkFlowTests
    {
        [TestMethod]
        public async Task TestAction()
        {

            var test =  new JRaw("{\"test\":\"hello world\" }");
            var test1 = JToken.Parse(test.ToString());
            var test2 = JToken.FromObject(new { test = "hello world" });
            var test3 = JToken.FromObject(new { test2 = "test2" });
            var test4 = test.DeepClone();


            Assert.IsFalse(JToken.DeepEquals(test, test1));
            Assert.IsTrue(JToken.DeepEquals(test1, test2));
            Assert.IsFalse(JToken.DeepEquals(test4, test1));
           

        }
    }
}
