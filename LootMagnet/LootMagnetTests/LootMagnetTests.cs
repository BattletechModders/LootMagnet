using System;
using LootMagnet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LootMagnetTests {

    [TestClass]
    public class GetOutcomeTests {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestOutcomes() {

            Mod.Config = new ModConfig();
            Mod.Log = new DeferringLogger(TestContext.DeploymentDirectory, "test", true, true);
            Dispute dispute = new Dispute(100000, "TEST");

            // Test guaranteed failure
            dispute.SuccessChance = 0;
            Assert.AreEqual(Dispute.Outcome.FAILURE, dispute.GetOutcome());

            // Test guaranteed success
            dispute.SuccessChance = 100;
            Assert.AreEqual(Dispute.Outcome.SUCCESS, dispute.GetOutcome());
        }

        [TestMethod]
        public void TestAverageOutcome30() {

            Mod.Config = new ModConfig();
            Mod.Log = new DeferringLogger(TestContext.DeploymentDirectory, "test", true, true);
            Dispute dispute = new Dispute(100000, "TEST");

            dispute.SuccessChance = 30;
            int successCount = 0;
            int failCount = 0;
            int totalCount = 100000;
            for (int i = 0; i < totalCount; i++) {
                if (dispute.GetOutcome() == Dispute.Outcome.SUCCESS) { successCount++; } 
                else { failCount++;  }
            }
            Console.WriteLine($"totalCount: {totalCount}  successCount: {successCount}  failCount: {failCount}");
            float expectedSuccessRate = dispute.SuccessChance / 100f;
            float actualSuccessRate = (totalCount - failCount) / (float)totalCount;
            float paddedBound = actualSuccessRate - 0.02f; // Account for NRG drift
            bool withinBounds = paddedBound <= expectedSuccessRate;
            Console.WriteLine($"ExpectedRate: {expectedSuccessRate:P3}  actualRate: {actualSuccessRate:P3}  paddedBound: {paddedBound}  withinBounds: {withinBounds}");

            Assert.IsTrue(withinBounds);

        }

        [TestMethod]
        public void TestAverageOutcome50() {

            Mod.Config = new ModConfig();
            Mod.Log = new DeferringLogger(TestContext.DeploymentDirectory, "test", true, true);
            Dispute dispute = new Dispute(100000, "TEST");

            dispute.SuccessChance = 50;
            int successCount = 0;
            int failCount = 0;
            int totalCount = 100000;
            for (int i = 0; i < totalCount; i++) {
                if (dispute.GetOutcome() == Dispute.Outcome.SUCCESS) { successCount++; } else { failCount++; }
            }
            Console.WriteLine($"totalCount: {totalCount}  successCount: {successCount}  failCount: {failCount}");
            float expectedSuccessRate = dispute.SuccessChance / 100f;
            float actualSuccessRate = (totalCount - failCount) / (float)totalCount;
            float paddedBound = actualSuccessRate - 0.02f; // Account for NRG drift
            bool withinBounds = paddedBound <= expectedSuccessRate;
            Console.WriteLine($"ExpectedRate: {expectedSuccessRate:P3}  actualRate: {actualSuccessRate:P3}  paddedBound: {paddedBound}  withinBounds: {withinBounds}");

            Assert.IsTrue(withinBounds);

        }

        [TestMethod]
        public void TestAverageOutcome75() {

            Mod.Config = new ModConfig();
            Mod.Log = new DeferringLogger(TestContext.DeploymentDirectory, "test", true, true);
            Dispute dispute = new Dispute(100000, "TEST");

            dispute.SuccessChance = 75;
            int successCount = 0;
            int failCount = 0;
            int totalCount = 100000;
            for (int i = 0; i < totalCount; i++) {
                if (dispute.GetOutcome() == Dispute.Outcome.SUCCESS) { successCount++; } else { failCount++; }
            }
            Console.WriteLine($"totalCount: {totalCount}  successCount: {successCount}  failCount: {failCount}");
            float expectedSuccessRate = dispute.SuccessChance / 100f;
            float actualSuccessRate = (totalCount - failCount) / (float)totalCount;
            float paddedBound = actualSuccessRate - 0.02f; // Account for NRG drift
            bool withinBounds = paddedBound <= expectedSuccessRate;
            Console.WriteLine($"ExpectedRate: {expectedSuccessRate:P3}  actualRate: {actualSuccessRate:P3}  paddedBound: {paddedBound}  withinBounds: {withinBounds}");

            Assert.IsTrue(withinBounds);

        }

    }
}
