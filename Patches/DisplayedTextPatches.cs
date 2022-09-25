﻿using System.Text;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ProjectGenesis.Patches
{
    public static class DisplayedTextPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RecipeProto), "madeFromString", MethodType.Getter)]
        public static void RecipeProto_madeFromString(ref RecipeProto __instance, ref string __result)
        {
            var type = (ERecipeType)__instance.Type;
            if (type == ERecipeType.电路蚀刻)
                __result = "电路蚀刻机".Translate();
            else if (type == ERecipeType.高精度加工)
                __result = "高精度装配线".Translate();
            else if (type == ERecipeType.矿物处理)
                __result = "矿物处理厂".Translate();
            else if (type == ERecipeType.精密组装)
                __result = "精密制造厂".Translate();
            else if (type == ERecipeType.聚变生产)
                __result = "紧凑式回旋加速器".Translate();
            else if (type == ERecipeType.垃圾回收) __result = "物质分解设施".Translate();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), "fuelTypeString", MethodType.Getter)]
        public static void ItemProto_fuelTypeString(ref ItemProto __instance, ref string __result)
        {
            var type = __instance.FuelType;
            if (type == 5) __result = "裂变能".Translate();
            if (type == 6) __result = "聚变能".Translate();
        }

        //发电类型
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), "GetPropValue")]
        public static void GetPropValuePatch(ref ItemProto __instance, int index, ref string __result)
        {
            if ((ulong)index >= (ulong)__instance.DescFields.Length)
            {
                Debug.Log("Genesis Book:Now Loading");
                __result = "";
                return;
            }

            switch (__instance.DescFields[index])
            {
                case 4:
                    if (__instance.prefabDesc.isPowerGen)
                    {
                        if (__instance.prefabDesc.fuelMask == 4) __result = "质能转换";
                        if (__instance.prefabDesc.fuelMask == 5) __result = "裂变能";
                        if (__instance.prefabDesc.fuelMask == 6) __result = "仿星器";
                    }

                    return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), "UnlockFunctionText")]
        public static void TechProto_UnlockFunctionText(ref TechProto __instance, ref string __result, StringBuilder sb)
        {
            switch (__instance.ID)
            {
                case 1502:
                    __result = "海洋排污文字描述".Translate();
                    break;

                case 1508: // (int)科技.任务完成
                    if (Localization.isCJK) __result = "欢迎加入创世之书讨论群:991895539";
                    break;

                case 1513:
                    __result = "化工技术革新文字描述".Translate();
                    break;

                case 1703:
                    __result = "海洋排污2文字描述".Translate();
                    break;
            }
        }
    }
}