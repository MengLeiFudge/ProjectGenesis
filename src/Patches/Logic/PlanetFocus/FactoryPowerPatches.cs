﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectGenesis.Utils;

namespace ProjectGenesis.Patches.Logic.PlanetFocus
{
    public static partial class PlanetFocusPatches
    {
        private static readonly ConcurrentDictionary<short, long> ModelPowerCosts = new ConcurrentDictionary<short, long>();

        [HarmonyPatch(typeof(FactorySystem), "GameTickBeforePower")]
        [HarmonyPatch(typeof(FactorySystem), "ParallelGameTickBeforePower")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FactorySystem_GameTickBeforePower_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(AssemblerComponent), "SetPCState")));
            matcher.MatchBack(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld));
            matcher.InsertAndAdvance(matcher.InstructionsWithOffsets(0, 4));
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.factory))));
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlanetFocusPatches), nameof(GetWorkEnergyPerTick))));
            return matcher.InstructionEnumeration();
        }

        public static void GetWorkEnergyPerTick(ref AssemblerComponent assembler, PowerConsumerComponent[] pcPool, PlanetFactory factory)
        {
            var modelIndex = factory.entityPool[assembler.entityId].modelIndex;

            if (!ModelPowerCosts.TryGetValue(modelIndex, out var workEnergyPerTick))
            {
                workEnergyPerTick = LDB.models.Select(modelIndex).prefabDesc.workEnergyPerTick;
                ModelPowerCosts.TryAdd(modelIndex, workEnergyPerTick);
            }

            if (ContainsFocus(factory.planetId, 6522)) workEnergyPerTick = (long)(workEnergyPerTick * 0.9f);

            pcPool[assembler.pcId].workEnergyPerTick = assembler.recipeId == ProtoIDUsedByPatches.R水电解 ? workEnergyPerTick * 10 : workEnergyPerTick;
            return;
        }
    }
}