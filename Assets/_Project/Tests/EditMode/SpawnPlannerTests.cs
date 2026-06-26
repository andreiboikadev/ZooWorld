#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Spawning;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="SpawnPlanner"/> over hand-built species/tuning: interval range, weighted
    /// selection, the cap pause, the anti-deadlock predator floor (incl. the cap override), and
    /// placement (clear / all-blocked / retry). All over T01 seams — no Unity physics.
    /// </summary>
    public sealed class SpawnPlannerTests
    {
        private static SpeciesWeight Prey(float weight)
        {
            return new SpeciesWeight(Role.Prey, weight);
        }

        private static SpeciesWeight Pred(float weight)
        {
            return new SpeciesWeight(Role.Predator, weight);
        }

        private static SpawnPlanner Planner()
        {
            // Frog 0.45 (Prey), Snake 0.30 (Predator), Rabbit 0.25 (Prey) — §9 weights, total 1.0.
            SpeciesWeight[] species = { Prey(0.45f), Pred(0.30f), Prey(0.25f) };
            SpawnTuning tuning = new SpawnTuning(1f, 2f, 120, 1, 1f, 10);
            return new SpawnPlanner(species, tuning);
        }

        private static FieldBounds Bounds()
        {
            return new FieldBounds(Vector3.zero, new Vector2(10f, 6f), 1f);
        }

        [Test]
        public void NextInterval_DrawsWithinConfiguredRange()
        {
            SpawnPlanner planner = Planner();

            Assert.That(planner.NextInterval(new FakeRandom(0f)), Is.EqualTo(1f));
            Assert.That(planner.NextInterval(new FakeRandom(0.5f)), Is.EqualTo(1.5f));
        }

        [Test]
        public void SelectSpeciesIndex_BelowCap_WeightedByCumulativeWeight()
        {
            SpawnPlanner planner = Planner();
            PopulationSnapshot pop = new PopulationSnapshot(total: 5, predatorCount: 2);

            Assert.That(planner.SelectSpeciesIndex(pop, new FakeRandom(0.1f)), Is.EqualTo(0));
            Assert.That(planner.SelectSpeciesIndex(pop, new FakeRandom(0.5f)), Is.EqualTo(1));
            Assert.That(planner.SelectSpeciesIndex(pop, new FakeRandom(0.9f)), Is.EqualTo(2));
        }

        [Test]
        public void SelectSpeciesIndex_AtCapacity_FloorSatisfied_ReturnsNull()
        {
            SpawnPlanner planner = Planner();

            Assert.That(planner.SelectSpeciesIndex(new PopulationSnapshot(120, 2), new FakeRandom(0.5f)), Is.Null);
            Assert.That(planner.SelectSpeciesIndex(new PopulationSnapshot(121, 2), new FakeRandom(0.5f)), Is.Null);
        }

        [Test]
        public void SelectSpeciesIndex_PredatorFloorUnmet_ForcesPredator()
        {
            // PredatorCount 0 < floor 1 → only Snake (index 1) is a candidate, regardless of the draw.
            Assert.That(Planner().SelectSpeciesIndex(new PopulationSnapshot(5, 0), new FakeRandom(0.99f)), Is.EqualTo(1));
        }

        [Test]
        public void SelectSpeciesIndex_PredatorFloorOverridesCapacity()
        {
            // At the cap AND no predators → the floor beats the cap (anti-deadlock), not null.
            Assert.That(Planner().SelectSpeciesIndex(new PopulationSnapshot(120, 0), new FakeRandom(0f)), Is.EqualTo(1));
        }

        [Test]
        public void TryFindSpawnPosition_ClearSpot_ReturnsInBoundsPosition()
        {
            FieldBounds bounds = Bounds();

            bool found = Planner().TryFindSpawnPosition(bounds, FakeOccupancy.AlwaysClear(), new FakeRandom(0.5f, 0.5f), out Vector3 position);

            Assert.That(found, Is.True);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            Assert.That(bounds.Contains(position), Is.True);
        }

        [Test]
        public void TryFindSpawnPosition_AllBlocked_ReturnsFalse()
        {
            bool found = Planner().TryFindSpawnPosition(Bounds(), FakeOccupancy.AlwaysBlocked(), new FakeRandom(), out Vector3 position);

            Assert.That(found, Is.False);
            Assert.That(position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TryFindSpawnPosition_RetriesUntilClear()
        {
            FieldBounds bounds = Bounds();
            FakeOccupancy occupancy = FakeOccupancy.WithCircles((Vector3.zero, 1f));

            // Attempt 1 (0,0,0) is blocked (clearance 1 + body radius 1 = min-sep 2); attempt 2 (9,0,3) clears.
            bool found = Planner().TryFindSpawnPosition(bounds, occupancy, new FakeRandom(0.5f, 0.5f, 0.95f, 0.75f), out Vector3 position);

            Assert.That(found, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(9f, 0f, 3f)));
        }
    }
}
