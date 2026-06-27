#nullable enable

using System.Collections.Generic;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Composition
{
    /// <summary>
    /// Composition-time projection of the config ScriptableObjects (<see cref="AnimalCatalog"/> +
    /// <see cref="SimConfig"/>) into the plain value structs the pure rules consume — so no rule ever
    /// touches an SO. Pure (reads SO getters only) and EditMode-tested. Built once at composition (T08),
    /// in catalog order, so <c>specs[i]</c>, <c>weights[i]</c>, and the <c>AnimalFactory</c> pool index all
    /// address the same catalog species (the 1:1 index contract).
    /// </summary>
    public static class CatalogProjection
    {
        /// <summary>
        /// Projects every <see cref="AnimalCatalog.Definitions"/> entry, in catalog order, into a parallel
        /// <see cref="AnimalSpec"/> (for the factory/pool) and <see cref="SpeciesWeight"/> (for the planner).
        /// </summary>
        public static (AnimalSpec[] specs, SpeciesWeight[] weights) Build(AnimalCatalog catalog, SimConfig config)
        {
            IReadOnlyList<AnimalDefinition> definitions = catalog.Definitions;
            AnimalSpec[] specs = new AnimalSpec[definitions.Count];
            SpeciesWeight[] weights = new SpeciesWeight[definitions.Count];

            for (int i = 0; i < definitions.Count; i++)
            {
                AnimalDefinition def = definitions[i];
                MovementTuning tuning = new MovementTuning(def.Speed, def.JumpDistance, def.JumpInterval,
                    config.LinearDamping, config.WanderRerollMin, config.WanderRerollMax);
                specs[i] = new AnimalSpec(def.Role, def.Strength, def.Size, def.Mass, def.Color,
                    def.SpawnWeight, def.Movement, tuning);
                weights[i] = new SpeciesWeight(def.Role, def.SpawnWeight);
            }

            return (specs, weights);
        }

        /// <summary>The global spawn constants the <c>SpawnPlanner</c> reads (game-design.md §9).</summary>
        public static SpawnTuning BuildSpawnTuning(SimConfig config)
        {
            return new SpawnTuning(config.SpawnIntervalMin, config.SpawnIntervalMax, config.MaxPopulation,
                config.PredatorFloor, config.ClearanceRadius, config.MaxPlacementAttempts);
        }

        /// <summary>The two combat constants the <c>Simulation</c> reads (game-design.md §9).</summary>
        public static SimulationTuning BuildSimulationTuning(SimConfig config)
        {
            return new SimulationTuning(config.BounceKick, config.GraceSeconds);
        }

        /// <summary>The feedback constants the <c>Simulation</c> samples in the tick (game-design.md §9, T09).</summary>
        public static FeedbackTuning BuildFeedbackTuning(SimConfig config)
        {
            return new FeedbackTuning(config.JumpArcHeight, config.JumpArcDuration, config.JumpArcCurve,
                config.SpawnPopDuration, config.RiseEase);
        }
    }
}
