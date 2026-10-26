using System;
using System.Linq;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_ChaAlphaMask;

internal class Patch
{
	[HarmonyPrefix]
	[HarmonyPriority(300)]
	[HarmonyPatch(typeof(Material), "SetTexture", new Type[]
	{
		typeof(int),
		typeof(Texture)
	})]
	private static void KCOXMaskHook_Post(Material __instance, int nameID, ref Texture value)
	{
		if (nameID == ChaShader._AlphaMask)
		{
			AlphaMaskController controller = Util.GetController(__instance);
			if (controller != null && controller.MaskTexPathExists && controller.TexBodyMask != null)
			{
				value = controller.TexBodyMask;
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ChaControl), "SetClothesState")]
	private static void SetClothesState_Post(ChaControl __instance, int clothesKind, byte state)
	{
		Util.GetController(__instance)?.ChangeClothesState(clothesKind, state);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ChaControl), "ChangeCustomClothes")]
	private static void ChangeCustomClothes_Post(ChaControl __instance, int kind, bool updateTex01, bool updateTex02, bool updateTex03, bool updateTex04)
	{
		if (MakerAPI.InsideMaker && (updateTex01 | updateTex02 | updateTex03 | updateTex04))
		{
			Util.GetController(__instance)?.ChangeClothesState(kind, null);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ChaControl), "ChangeAlphaMask")]
	private static void ChangeAlphaMask_Post(ChaControl __instance)
	{
		if (((ChaFile)((ChaInfo)__instance).chaFile).status.clothesState[0] == 3)
		{
			Util.GetController(__instance)?.SetFloatBodyAlpha(3);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CvsClothes), "Start")]
	private static void CvsClothes_Start_Post(CvsClothes __instance)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected I4, but got Unknown
		Toggle[] array = ((Component)__instance).GetComponentsInChildren<Toggle>()?.Where((Toggle x) => ((Object)x).name.IndexOf("tglOpt0", StringComparison.OrdinalIgnoreCase) >= 0)?.ToArray();
		if (array != null && array.Length != 0)
		{
			MakerGui.Init_TglOpt((int)__instance.typeClothes, array);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CvsClothes), "UpdateSelectClothes")]
	private static void UpdateSelectClothes_Post(CvsClothes __instance, int index)
	{
		ObservableExtensions.Subscribe<long>(Observable.TimerFrame(2, (FrameCountType)0), (Action<long>)((long x) =>
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected I4, but got Unknown
			MakerGui.UpdateGui((int)__instance.typeClothes, index);
		}));
	}
}
