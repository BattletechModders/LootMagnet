using LootMagnet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LootMagnetTests
{
    [TestClass]
    public static class TestGlobalInit
    {
        [AssemblyInitialize]
        public static void TestInitialize(TestContext testContext)
        {
            Mod.Config = new ModConfig();
            Mod.Log = new DeferringLogger(testContext.DeploymentDirectory, "tests", true, true);
        }

        [AssemblyCleanup]
        public static void TearDown()
        {

        }
    }
}
