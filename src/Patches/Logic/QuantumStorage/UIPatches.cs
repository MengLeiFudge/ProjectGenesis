﻿using System;
using HarmonyLib;
using ProjectGenesis.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace ProjectGenesis.Patches.Logic.QuantumStorage
{
    public static partial class QuantumStoragePatches
    {
        private static UIOrbitPicker orbitPicker;

        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnInit))]
        [HarmonyPostfix]
        public static void Init()
        {
            orbitPicker = Object.Instantiate(UIRoot.instance.uiGame.ejectorWindow.orbitPicker,
                UIRoot.instance.uiGame.storageWindow.transform);

            var orbitPickerTransform = orbitPicker.transform;
            ((RectTransform)orbitPickerTransform).localPosition = new Vector3(90, -443, 0);

            var titleTransform = orbitPickerTransform.GetChild(0).transform;
            ((RectTransform)titleTransform).localPosition = new Vector3(-120, -33, 0);

            Object.DestroyImmediate(titleTransform.GetComponent<Localizer>());
            Text component = titleTransform.GetComponent<Text>();
            component.fontSize = 16;
            component.text = "选择量子频道".TranslateFromJson();

            for (int i = 1; i < orbitPickerTransform.childCount; i++)
            {
                orbitPickerTransform.GetChild(i).gameObject.SetActive(false);
            }

            for (int i = 3; i < 13; i++)
            {
                var transform = orbitPickerTransform.GetChild(i);
                transform.gameObject.SetActive(true);
                transform.GetComponent<Button>().interactable = true;
            }

            Array.Resize(ref orbitPicker.orbitButtons, 10);

            orbitPicker.UnregInnerEvents();
            orbitPicker.RegInnerEvents();
            orbitPicker.onOrbitButtonClick += OnOrbitPickerButtonClick;
        }

        private static void OnOrbitPickerButtonClick(int orbitId)
        {
            var uiGameStorageWindow = UIRoot.instance.uiGame.storageWindow;

            if (!uiGameStorageWindow.active || uiGameStorageWindow.factory == null)
                return;

            QuantumStorageOrbitChange(uiGameStorageWindow.factory.planetId, uiGameStorageWindow.storageId, orbitId);
            uiGameStorageWindow.OnStorageIdChange();
        }

        [HarmonyPatch(typeof(UIStorageWindow), nameof(UIStorageWindow.OnStorageIdChange))]
        [HarmonyPostfix]
        public static void UIStorageWindow_OnStorageIdChange_Postfix(UIStorageWindow __instance)
        {
            if (!__instance.active || __instance.factory == null)
            {
                return;
            }

            StorageComponent component = __instance.factoryStorage.storagePool[__instance.storageId];

            if (component.size == QuantumStorageSize)
            {
                __instance.bansSlider.transform.parent.gameObject.SetActive(false);
                orbitPicker.gameObject.SetActive(true);
                var orbitId = QueryOrbitId(__instance.factory.planetId, __instance.storageId);
                orbitPicker.SetOrbitId(orbitId);
            }
            else
            {
                __instance.bansSlider.transform.parent.gameObject.SetActive(true);
                orbitPicker.gameObject.SetActive(false);
            }
        }
    }
}