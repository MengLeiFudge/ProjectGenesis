﻿using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace ProjectGenesis.Patches.UI
{
    public static class UILabWindowPatches
    {
        [HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow._OnCreate))]
        [HarmonyPostfix]
        public static void UILabWindow_OnCreate_Postfix(UILabWindow __instance)
        {
            __instance.GetComponent<RectTransform>().sizeDelta = new Vector2(620, 620);

            const int len = 9;
            
            Array.Resize(ref __instance.centerObjs, len);
            Array.Resize(ref __instance.itemButtons, len);
            Array.Resize(ref __instance.itemIcons, len);
            Array.Resize(ref __instance.itemPercents, len);
            Array.Resize(ref __instance.itemLocks, len);
            Array.Resize(ref __instance.itemCountTexts, len);
            Array.Resize(ref __instance.itemIncs, len * 3);

            var itemButtonsGameObject = __instance.itemButtons[0].gameObject;

            for (int i = 6; i < len; i++)
            {
                var newButton = Object.Instantiate(itemButtonsGameObject,
                    itemButtonsGameObject.transform.parent);

                __instance.centerObjs[i] = newButton;
                __instance.itemButtons[i] = newButton.GetComponent<UIButton>();
                __instance.itemIcons[i] = newButton.transform.Find("icon").GetComponent<Image>();
                __instance.itemPercents[i] = newButton.transform.Find("fg").GetComponent<Image>();
                __instance.itemLocks[i] = newButton.transform.Find("locked").GetComponent<Image>();
                __instance.itemCountTexts[i] = newButton.transform.Find("cnt-text").GetComponent<Text>();

                for (int j = 0; j < 3; j++)
                {
                    var transform = newButton.transform.Find("icon");
                    __instance.itemIncs[i * 3 + j] = transform.GetChild(j).GetComponent<Image>();
                }
            }LabRenderer
        }

        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble))]
        [HarmonyPostfix]
        public static void LabComponent_InternalUpdateAssemble_Postfix(ref LabComponent __instance, ref uint __result)
        {
            if (__result > 6) __result = 6;
        }
    }
}