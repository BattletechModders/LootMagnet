using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LootMagnetTests
{
    [TestClass]
    public class MRBLevelTests
    {
        public TestContext TestContext { get; set; }

        public SimGameState SimGameState;

        const string FactionName = "MRB";

        [TestInitialize]
        public void TestInitialize()
        {
            // SGS tries to invoke a LazyInitialize for the queue, which will throw a security error. Work around this.
            SimGameState = (SimGameState)FormatterServices.GetUninitializedObject(typeof(SimGameState));

            // Init story constants
            StoryConstantsDef storyConstantsDef = new StoryConstantsDef();
            storyConstantsDef.MRBRepCap = new float[] { 50f, 200f, 500f, 700f, 900f };
            SimGameConstants constants = new SimGameConstants(null, null, null, null, null, null, null, storyConstantsDef, null, null, null, null, null);

            Traverse constantsT = Traverse.Create(SimGameState).Property("Constants");
            constantsT.SetValue(constants);

            // Init the MRB faction
            FactionValue mrbFactionValue = new FactionValue();
            Traverse factionValueNameT = Traverse.Create(mrbFactionValue).Property("Name");
            factionValueNameT.SetValue(FactionName);

            Dictionary<int, FactionValue> factionValuesDict = new Dictionary<int, FactionValue>
            {
                [12] = mrbFactionValue
            };

            FactionEnumeration factionEnum = FactionEnumeration.Instance;
            Traverse initFactionDictT = Traverse.Create(factionEnum).Field("intFactionDict");
            initFactionDictT.SetValue(factionValuesDict);

            // Add the company stat manually (since constructor did not run)
            StatCollection companyStats = new StatCollection();
            Traverse companyStatsT = Traverse.Create(SimGameState).Field("companyStats");
            companyStatsT.SetValue(companyStats);
            SimGameState.CompanyStats.AddStatistic<int>($"Reputation.{FactionName}", 0);
        }

        // RT and BTA both use: MRBRepCap" : [ 50, 200, 500, 700, 900 ],
        [TestMethod]
        public void TestMRBLevels()
        {
            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 0);
            Assert.AreEqual(0, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 49);
            Assert.AreEqual(0, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 50);
            Assert.AreEqual(1, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 199);
            Assert.AreEqual(1, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 200);
            Assert.AreEqual(2, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 499);
            Assert.AreEqual(2, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 500);
            Assert.AreEqual(3, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 699);
            Assert.AreEqual(3, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 700);
            Assert.AreEqual(4, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 899);
            Assert.AreEqual(4, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 900);
            Assert.AreEqual(5, SimGameState.GetCurrentMRBLevel());

            SimGameState.CompanyStats.Set<int>($"Reputation.{FactionName}", 1500);
            Assert.AreEqual(5, SimGameState.GetCurrentMRBLevel());

        }

    }
}
