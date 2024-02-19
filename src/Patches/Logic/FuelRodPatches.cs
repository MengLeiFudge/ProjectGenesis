﻿using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectGenesis.Utils;

// ReSharper disable InconsistentNaming

namespace ProjectGenesis.Patches.Logic
{
    public static class FuelRodPatches
    {
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.GenerateEnergy))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SetTargetCargoBytes_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_I4_M1), new CodeMatch(OpCodes.Stloc_S));


            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FuelRodPatches), nameof(GenerateEnergy_Patch))));

            return matcher.InstructionEnumeration();
        }

        public static void GenerateEnergy_Patch(Mecha mecha)
        {
            int count = GetEmptyRodCount(mecha.reactorItemId);

            if (count == 0) return;

            mecha.player.TryAddItemToPackage(ProtoID.I空燃料棒, count, mecha.reactorItemInc, true);
            UIItemup.Up(ProtoID.I空燃料棒, count);
        }

        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GenEnergyByFuel))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PowerGeneratorComponent_GenEnergyByFuel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldarg_2));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FuelRodPatches), nameof(GenEnergyByFuel_Patch_Method))));

            return matcher.InstructionEnumeration();
        }

        public static void GenEnergyByFuel_Patch_Method(ref PowerGeneratorComponent component, int[] consumeRegister)
        {
            int count = GetEmptyRodCount(component.fuelId);

            if (count == 0) return;

            component.productCount += count;
            component.catalystIncPoint += component.fuelIncLevel;

            if (component.productCount > 30)
            {
                var instanceProductCount = (int)component.productCount;
                component.split_inc(ref instanceProductCount, ref component.catalystIncPoint, instanceProductCount - 30);
                component.productCount = instanceProductCount;
            }

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (FactoryProductionStat factoryProductionStat in GameMain.data.statistics.production.factoryStatPool)
            {
                if (factoryProductionStat.consumeRegister != consumeRegister) continue;

                factoryProductionStat.productRegister[ProtoID.I空燃料棒] += count;

                return;
            }
        }

        private static int GetEmptyRodCount(int itemId)
        {
            var count = 0;

            switch (itemId)
            {
                case ProtoID.I氢燃料棒:
                case ProtoID.I煤油燃料棒:
                case ProtoID.I四氢双环戊二烯燃料棒:
                case ProtoID.I铀燃料棒:
                case ProtoID.I钚燃料棒:
                case ProtoID.I氘核燃料棒:
                case ProtoID.I氦三燃料棒:
                case ProtoID.I反物质燃料棒:
                    count = 1;

                    break;

                case ProtoID.I氘氦混合聚变燃料棒:
                    count = 2;

                    break;

                case ProtoID.IMOX燃料棒:
                    count = 3;

                    break;

                case ProtoID.I奇异燃料棒:
                    count = 4;

                    break;
            }

            return count;
        }

        [HarmonyPatch(typeof(UIInserterBuildTip), nameof(UIInserterBuildTip.SetOutputEntity))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UIInserterBuildTip_SetOutputEntity_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldloc_3), new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.beltId))),
                new CodeMatch(OpCodes.Ldc_I4_0));

            object label = matcher.Advance(1).Operand;

            matcher.Advance(1).CreateLabel(out Label label2);

            matcher.Advance(-1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Bgt, label2));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc_0), new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FuelRodPatches), nameof(SetOutputEntity_Patch_Method))),
                new CodeInstruction(OpCodes.Br, label));

            return matcher.InstructionEnumeration();
        }

        public static void SetOutputEntity_Patch_Method(UIInserterBuildTip buildTip, PlanetFactory factory, EntityData entityData)
        {
            int entityDataPowerGenId = entityData.powerGenId;

            if (entityDataPowerGenId > 0)
            {
                PowerGeneratorComponent component = factory.powerSystem.genPool[entityDataPowerGenId];

                if (component.id == entityDataPowerGenId && component.fuelMask != 0)
                {
                    buildTip.filterItems.Add(ProtoID.I空燃料棒);
                    buildTip.filterItems.Add(component.curFuelId);
                }
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PickFrom))]
        [HarmonyPostfix]
        public static void PlanetFactory_PickFrom_Postfix(PlanetFactory __instance, int entityId, int offset, int filter, int[] needs, ref byte stack, ref byte inc,
            ref int __result)
        {
            if (__result != 0) return;

            if (filter != ProtoID.I空燃料棒 && filter != 0) return;

            EntityData entityData = __instance.entityPool[entityId];
            int powerGenId = entityData.powerGenId;

            if (powerGenId == 0) return;

            PowerGeneratorComponent powerGeneratorComponent = __instance.powerSystem.genPool[offset];

            if ((offset <= 0 || powerGeneratorComponent.id != offset) && filter == 0) return;

            lock (__instance.entityMutexs[entityId])
            {
                int num = __instance.powerSystem.genPool[powerGenId].PickFuelFrom(filter, out int inc3);
                inc = (byte)inc3;
                __result = num;
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
        [HarmonyPostfix]
        public static void PlanetFactory_InsertInto_Postfix(PlanetFactory __instance, int entityId, int itemId, ref byte itemCount, ref byte itemInc, ref byte remainInc,
            ref int __result)
        {
            if (__result != 0 || itemId != ProtoID.I空燃料棒) return;

            EntityData entityData = __instance.entityPool[entityId];

            int powerGenId = entityData.powerGenId;

            if (powerGenId == 0) return;

            PowerGeneratorComponent[] genPool = __instance.powerSystem.genPool;
            ref PowerGeneratorComponent component = ref genPool[powerGenId];

            if (component.fuelMask == 0) return;

            Mutex obj = __instance.entityMutexs[entityId];

            lock (obj)
            {
                component.productCount += itemCount;
                component.catalystIncPoint += itemInc;

                if (component.productCount > 30)
                {
                    var instanceProductCount = (int)component.productCount;
                    component.split_inc(ref instanceProductCount, ref component.catalystIncPoint, instanceProductCount - 30);
                    component.productCount = instanceProductCount;
                }

                remainInc = 0;
                __result = itemCount;
            }
        }

        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.PickFuelFrom))]
        [HarmonyPostfix]
        public static void PowerGeneratorComponent_PickFuelFrom_Postfix(ref PowerGeneratorComponent __instance, int filter, ref int inc, ref int __result)
        {
            if (__result != 0) return;

            if (filter != ProtoID.I空燃料棒 && filter != 0) return;

            var instanceProductCount = (int)__instance.productCount;

            if (instanceProductCount <= 0) return;

            __result = ProtoID.I空燃料棒;
            inc = __instance.split_inc(ref instanceProductCount, ref __instance.catalystIncPoint, 1);
            __instance.productCount = instanceProductCount;
        }
    }
}
