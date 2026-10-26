using System;
using System.Linq;
using BepInEx;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_ChaAlphaMask;

internal class MakerGui
{
	private static AlphaMaskController ctrl;

	internal const int length_subCategory = 9;

	internal const int length_SyncKind = 17;

	internal const int noneSyncKind = -1;

	private static MakerDropdown[] alphaMaskDropdown = new MakerDropdown[9];

	private static MakerToggle[] bodyMaskToggle01 = new MakerToggle[9];

	private static MakerToggle[] bodyMaskToggle02 = new MakerToggle[3];

	private static TextMeshProUGUI textMeshProTop;

	internal static void Init()
	{
		MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
		MakerAPI.MakerExiting += MakerAPI_MakerExiting;
	}

	private static void MakerAPI_MakerExiting(object sender, EventArgs e)
	{
		alphaMaskDropdown = null;
		bodyMaskToggle01 = null;
		bodyMaskToggle02 = null;
		textMeshProTop = null;
		ctrl = null;
	}

	private static void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
	{
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected Obj, but got Unknown
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Expected Obj, but got Unknown
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected Obj, but got Unknown
		alphaMaskDropdown = new MakerDropdown[9];
		bodyMaskToggle01 = new MakerToggle[9];
		bodyMaskToggle02 = new MakerToggle[3];
		string[] array = new string[9] { "tglTop", "tglBot", "tglBra", "tglShorts", "tglGloves", "tglPanst", "tglSocks", "tglInnerShoes", "tglOuterShoes" };
		string[] array2 = new string[17]
		{
			"無し", "トップス", "ボトムス", "ブラ", "ショーツ", "手袋", "パンスト", "靴下", "靴", "トップス(反転)",
			"ボトムス(反転)", "ブラ(反転)", "ショーツ(反転)", "手袋(反転)", "パンスト(反転)", "靴下(反転)", "靴(反転)"
		};
		for (int i = 0; i < array.Length; i++)
		{
			int sc = i;
			MakerCategory builtInCategory = MakerConstants.GetBuiltInCategory("03_ClothesTop", array[sc]);
			if (builtInCategory == null)
			{
				ChaAlphaMask.Logger.LogWarning((object)$"Maker category not found: 03_ClothesTop/{array[sc]}. Skipping mask controls for clothes kind {sc}.");
				continue;
			}
			if (i != 0)
			{
				bodyMaskToggle01[sc] = ((RegisterCustomControlsEvent)e).AddControl<MakerToggle>(new MakerToggle(builtInCategory, "ボディマスク", true, (BaseUnityPlugin)(object)ChaAlphaMask.Instance));
				CharacterExtensions.BindToFunctionController<AlphaMaskController, bool>((BaseEditableGuiEntry<bool>)(object)bodyMaskToggle01[sc], (AlphaMaskController ctrl) => ctrl.IsBodyMaskActive(sc), (AlphaMaskController ctrl, bool tgl) =>
				{
					if (tgl)
					{
						ctrl.kindPairBodyMask.RemoveAll((AlphaMaskController.KindPair x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType && x.clothesKind == sc && !x.isBotMask);
					}
					else if (ctrl.IsBodyMaskActive(sc))
					{
						ctrl.kindPairBodyMask.Add(new AlphaMaskController.KindPair
						{
							coordinateType = ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType,
							clothesKind = sc,
							isBotMask = false
						});
					}
					ctrl.ChangeClothesState(sc, null);
				});
			}
			if (i == 0 || 2 == i)
			{
				int sc2 = sc + 1;
				bodyMaskToggle02[sc] = ((RegisterCustomControlsEvent)e).AddControl<MakerToggle>(new MakerToggle(builtInCategory, (sc == 0) ? "ボディマスク\u3000ボトムス" : "ボディマスク\u3000ショーツ", true, (BaseUnityPlugin)(object)ChaAlphaMask.Instance));
				CharacterExtensions.BindToFunctionController<AlphaMaskController, bool>((BaseEditableGuiEntry<bool>)(object)bodyMaskToggle02[sc], (AlphaMaskController ctrl) => ctrl.IsBodyMaskActive(sc2), (AlphaMaskController ctrl, bool tgl) =>
				{
					if (tgl)
					{
						ctrl.kindPairBodyMask.RemoveAll((AlphaMaskController.KindPair x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType && x.clothesKind == sc2 && x.isBotMask);
					}
					else if (ctrl.IsBodyMaskActive(sc2))
					{
						ctrl.kindPairBodyMask.Add(new AlphaMaskController.KindPair
						{
							coordinateType = ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType,
							clothesKind = sc2,
							isBotMask = true
						});
					}
					ctrl.ChangeClothesState(sc2, null);
				});
			}
			if (i == 2)
			{
				continue;
			}
			alphaMaskDropdown[sc] = ((RegisterCustomControlsEvent)e).AddControl<MakerDropdown>(new MakerDropdown("アルファマスク", array2, builtInCategory, 0, (BaseUnityPlugin)(object)ChaAlphaMask.Instance));
			CharacterExtensions.BindToFunctionController<AlphaMaskController, int>((BaseEditableGuiEntry<int>)(object)alphaMaskDropdown[sc], (AlphaMaskController ctrl) => ctrl.SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType, sc] + 1, (AlphaMaskController ctrl, int sync) =>
			{
				sync--;
				ctrl.SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType, sc] = sync;
				if (sync < 0)
				{
					ctrl.ChangeClothesState(sc, 0);
				}
				else
				{
					ctrl.ChangeClothesState(ToKind(sync), ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.clothesState[ToKind(sync)]);
				}
			});
		}
		ObservableExtensions.Subscribe<long>(Observable.TimerFrame(10, (FrameCountType)0), (Action<long>)((long x) =>
		{
			ctrl = ctrl ?? Util.GetController(MakerAPI.GetCharacterControl());
			for (int j = 0; j < ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).infoClothes.Length; j++)
			{
				UpdateGui(j, ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).infoClothes[j].Id);
			}
		}));
	}

	private static int ToKind(int syncKind)
	{
		if (syncKind <= 7)
		{
			return syncKind;
		}
		return syncKind - 8;
	}

	internal static void Init_TglOpt(int clothesKind, Toggle[] tglOpts)
	{
		try
		{
			ChaAlphaMask.Logger.LogDebug((object)string.Format("{0}.{1}.AddListener Kind={2} Length={3}", "MakerGui", ((Object)tglOpts.First()).name, clothesKind, tglOpts.Length));
			for (int i = 0; i < tglOpts.Length; i++)
			{
				((UnityEvent<bool>)(object)tglOpts[i].onValueChanged).AddListener((UnityAction<bool>)delegate
				{
					if (MakerAPI.InsideAndLoaded)
					{
						ctrl = ctrl ?? Util.GetController(MakerAPI.GetCharacterControl());
						if (ctrl != null)
						{
							ctrl.ChangeClothesState(clothesKind, ((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.clothesState[clothesKind]);
						}
					}
				});
			}
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning((object)ex);
		}
	}

	internal static void UpdateGui(int clothesKind, int localSlot)
	{
		ctrl = ctrl ?? Util.GetController(MakerAPI.GetCharacterControl());
		if (!MakerAPI.InsideAndLoaded || ctrl == null)
		{
			return;
		}
		int category = ctrl.ToCategoryNo(clothesKind);
		bool flag = ctrl.IsBotMask(clothesKind, ref category);
		bool flag2 = ((!flag) ? MaskProperty.DataSet.Any((MaskProperty x) => x.CategoryNo == category && x.LocalSlot == localSlot && !x.BotMask) : MaskProperty.DataSet.Any((MaskProperty x) => x.CategoryNo == category && x.LocalSlot == localSlot && x.BotMask));
		if (bodyMaskToggle01[clothesKind] != null)
		{
			bool value = !flag2 || ctrl.IsBodyMaskActive(clothesKind);
			((BaseGuiEntry)bodyMaskToggle01[clothesKind]).Visible.OnNext(flag2);
			((BaseEditableGuiEntry<bool>)(object)bodyMaskToggle01[clothesKind]).SetValue(value);
		}
		if (clothesKind == 0 || clothesKind == 2)
		{
			flag2 = MaskProperty.DataSet.Any((MaskProperty x) => x.CategoryNo == category && x.LocalSlot == localSlot && x.BotMask);
			bool value = !flag2 || ctrl.IsBodyMaskActive(clothesKind + 1);
			((BaseGuiEntry)bodyMaskToggle02[clothesKind]).Visible.OnNext(flag2);
			((BaseEditableGuiEntry<bool>)(object)bodyMaskToggle02[clothesKind]).SetValue(value);
		}
		if (alphaMaskDropdown[clothesKind] == null)
		{
			return;
		}
		if (clothesKind == 0)
		{
			textMeshProTop = textMeshProTop ?? ((BaseGuiEntry)alphaMaskDropdown[0]).ControlObject.GetComponentInChildren<TextMeshProUGUI>();
			switch (((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).infoClothes[0].Id)
			{
			case 1:
				((TMP_Text)textMeshProTop).text = "アルファマスク\u3000胴";
				break;
			case 2:
				((TMP_Text)textMeshProTop).text = "アルファマスク\u3000アウター";
				break;
			default:
				((TMP_Text)textMeshProTop).text = "アルファマスク";
				break;
			}
		}
		((BaseGuiEntry)alphaMaskDropdown[clothesKind]).Visible.OnNext(localSlot != 0 || (clothesKind == 1 && !flag) || (clothesKind == 3 && !flag));
		((BaseEditableGuiEntry<int>)(object)alphaMaskDropdown[clothesKind]).SetValue(ctrl.SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)ctrl).ChaControl).fileStatus.coordinateType, clothesKind] + 1);
	}
}
