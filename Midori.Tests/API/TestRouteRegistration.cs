using System.Threading.Tasks;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Tests.Preset;
using NUnit.Framework;

namespace Midori.Tests.API;

public class TestRouteRegistration : BaseAPITest
{
    [Test]
    public void TestRegister()
    {
        // Server.RegisterController<APIInteraction, TestController>();
        Start();
        Task.Delay(-1).Wait();
    }

    [Controller("/")]
    public class TestController
    {
        [HttpRoute("/test")]
        public APIReturn<string> Test()
        {
            return Returns.NotFound();
        }
    }
}
