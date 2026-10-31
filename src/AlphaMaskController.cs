using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using IllusionUtility.GetUtility;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KK_Plugins.MaterialEditor;
using KoiClothesOverlayX;
using MessagePack;
using Sideloader;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace KK_ChaAlphaMask;

public class AlphaMaskController : CharaCustomFunctionController
{
	[MessagePackObject(false)]
	public class KindPair
	{
		[Key(0)]
		public int coordinateType;

		[Key(1)]
		public int clothesKind;

		[Key(2)]
		public bool isBotMask;
	}

	[MessagePackObject(false)]
	public class CustomMask
	{
		[Key(0)]
		public int coordinateType;

		[Key(1)]
		public int clothesKind;

		[Key(2)]
		public byte[] png;
	}

	internal List<KindPair> kindPairBodyMask = new List<KindPair>();

	/// <summary>存在角色卡（或服装卡）里的遮罩 PNG，按坐标 × 服装种类保存。</summary>
	internal List<CustomMask> customMasks = new List<CustomMask>();

	private readonly Dictionary<int, Texture2D> customMaskTex = new Dictionary<int, Texture2D>();

	private readonly Dictionary<int, Texture2D> customMaskPreview = new Dictionary<int, Texture2D>();

	private readonly Dictionary<int, MaskProperty> bodyMaskProperty = new Dictionary<int, MaskProperty>(8)
	{
		{ 1, null },
		{ 2, null },
		{ 3, null },
		{ 4, null },
		{ 5, null },
		{ 6, null },
		{ 7, null },
		{ 8, null }
	};

	private GameObject goRendTex;

	private bool hasChangedBodyMaskProp;

	private KoiClothesOverlayController KCOCtrl;

	private Texture texBodyMask;

	internal int[,] SyncClothesKind = new int[7, 9];

	private IDisposable[] ObsMEChanging;

	private IDisposable[] ObsCharacter;

	private MaterialEditorCharaController MECtrl;

	internal bool MaskTexPathExists
	{
		get
		{
			foreach (KeyValuePair<int, MaskProperty> item in bodyMaskProperty)
			{
				if (item.Value != null)
				{
					return true;
				}
			}
			return HasAnyCustomMask;
		}
	}

	public Texture TexBodyMask => texBodyMask;

	private int CurrentCoordinateType => ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType;

	internal bool HasAnyCustomMask
	{
		get
		{
			int coordinateType = CurrentCoordinateType;
			for (int i = 0; i < customMasks.Count; i++)
			{
				CustomMask customMask = customMasks[i];
				if (customMask != null && customMask.coordinateType == coordinateType && customMask.png != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	internal byte[] GetCustomMaskPng(int clothesKind)
	{
		int coordinateType = CurrentCoordinateType;
		for (int i = 0; i < customMasks.Count; i++)
		{
			CustomMask customMask = customMasks[i];
			if (customMask != null && customMask.coordinateType == coordinateType && customMask.clothesKind == clothesKind)
			{
				return customMask.png;
			}
		}
		return null;
	}

	internal void SetCustomMask(int clothesKind, byte[] png)
	{
		int coordinateType = CurrentCoordinateType;
		customMasks.RemoveAll((CustomMask x) => x.coordinateType == coordinateType && x.clothesKind == clothesKind);
		if (png != null && png.Length != 0)
		{
			customMasks.Add(new CustomMask
			{
				coordinateType = coordinateType,
				clothesKind = clothesKind,
				png = png
			});
		}
		InvalidateMaskTexture(clothesKind);
		hasChangedBodyMaskProp = true;
	}

	internal Texture2D GetCustomMaskTexture(int clothesKind)
	{
		byte[] customMaskPng = GetCustomMaskPng(clothesKind);
		if (customMaskPng == null)
		{
			return null;
		}
		if (customMaskTex.TryGetValue(clothesKind, out Texture2D value) && value != null)
		{
			return value;
		}
		try
		{
			value = KKAPI.Utilities.TextureUtils.LoadTexture(customMaskPng, TextureFormat.RGBA32, false);
			value.wrapMode = TextureWrapMode.Clamp;
			value.name = $"ChaAlphaMask_{clothesKind}";
			customMaskTex[clothesKind] = value;
			return value;
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError((object)("Failed to create mask texture for clothes kind " + clothesKind + ": " + ex));
			return null;
		}
	}

	/// <summary>给窗口用的缩略图：把单通道遮罩转成肉眼可见的图片。</summary>
	internal Texture2D GetCustomMaskPreview(int clothesKind)
	{
		if (GetCustomMaskPng(clothesKind) == null)
		{
			return null;
		}
		if (customMaskPreview.TryGetValue(clothesKind, out Texture2D value) && value != null)
		{
			return value;
		}
		Texture2D customMaskTexture = GetCustomMaskTexture(clothesKind);
		if (customMaskTexture == null)
		{
			return null;
		}
		try
		{
			Color32[] pixels = customMaskTexture.GetPixels32();
			for (int i = 0; i < pixels.Length; i++)
			{
				Color32 color = pixels[i];
				int num = Math.Max(color.r, Math.Max(color.g, color.b));
				byte a = (byte)Math.Max(num, color.a);
				if (num == 0)
				{
					color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, a);
				}
				else
				{
					color.a = a;
				}
				pixels[i] = color;
			}
			Texture2D texture2D = new Texture2D(customMaskTexture.width, customMaskTexture.height, TextureFormat.ARGB32, false);
			texture2D.SetPixels32(pixels);
			texture2D.Apply(false);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.name = $"ChaAlphaMask_preview_{clothesKind}";
			customMaskPreview[clothesKind] = texture2D;
			return texture2D;
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning((object)("Failed to build mask preview: " + ex));
			return null;
		}
	}

	private void InvalidateMaskTexture(int clothesKind)
	{
		if (customMaskTex.TryGetValue(clothesKind, out Texture2D value))
		{
			customMaskTex.Remove(clothesKind);
			if (value != null)
			{
				Object.Destroy(value);
			}
		}
		if (customMaskPreview.TryGetValue(clothesKind, out Texture2D value2))
		{
			customMaskPreview.Remove(clothesKind);
			if (value2 != null)
			{
				Object.Destroy(value2);
			}
		}
	}

	private void InvalidateAllMaskTextures()
	{
		foreach (Texture2D value in customMaskTex.Values)
		{
			if (value != null)
			{
				Object.Destroy(value);
			}
		}
		customMaskTex.Clear();
		foreach (Texture2D value2 in customMaskPreview.Values)
		{
			if (value2 != null)
			{
				Object.Destroy(value2);
			}
		}
		customMaskPreview.Clear();
	}

	private bool IsHighPoly
	{
		get
		{
			if (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).hiPoly)
			{
				return true;
			}
			if (ChaAlphaMask.ForceHighPolyExists)
			{
				return ChaAlphaMask.GetForceHighPolyEnabled;
			}
			return false;
		}
	}

	internal bool IsBodyMaskActive(int clothesKind)
	{
		return !kindPairBodyMask.Any((KindPair x) => x.clothesKind == clothesKind && x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType && x.isBotMask == IsBotMask(clothesKind));
	}

	internal int ToCategoryNo(int clothesKind)
	{
		return 105 + clothesKind - ((clothesKind == 8) ? 1 : 0);
	}

	private int ToLocalSlot(int clothesKind)
	{
		ListInfoBase obj = ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).infoClothes[NotObjClothesKind(clothesKind)];
		if (obj == null)
		{
			return 0;
		}
		return obj.Id;
	}

	private int NotObjClothesKind(int clothesKind)
	{
		switch (clothesKind)
		{
		case 1:
			if (((CharaCustomFunctionController)this).ChaControl.notBot)
			{
				clothesKind = 0;
			}
			break;
		case 2:
			if (((CharaCustomFunctionController)this).ChaControl.notBra)
			{
				clothesKind = 0;
			}
			break;
		case 3:
			if (((CharaCustomFunctionController)this).ChaControl.notShorts)
			{
				clothesKind = 2;
			}
			break;
		}
		return clothesKind;
	}

	private void LateUpdate()
	{
		if (hasChangedBodyMaskProp)
		{
			UpdateBodyMask();
		}
	}

	private void UpdateBodyMask()
	{
		hasChangedBodyMaskProp = false;
		BodyMaskMultiply(Util.MaskKind.BodyMask, ref texBodyMask);
		((ChaInfo)((CharaCustomFunctionController)this).ChaControl).customMatBody.SetTexture(ChaShader._AlphaMask, Util.GetTexMask(((CharaCustomFunctionController)this).ChaControl, Util.MaskKind.BodyMask));
		SetFloatBodyAlpha(((ChaFile)((ChaInfo)((CharaCustomFunctionController)this).ChaControl).chaFile).status.clothesState[0]);
	}

	private Texture2D GetKCOXMask(Util.MaskKind maskKind)
	{
		object obj = KCOCtrl;
		if (obj == null)
		{
			CharaCustomFunctionController val = CharacterApi.GetBehaviours(((CharaCustomFunctionController)this).ChaControl).FirstOrDefault((CharaCustomFunctionController x) => ((object)x).GetType() == typeof(KoiClothesOverlayController));
			obj = ((val is KoiClothesOverlayController) ? val : null);
		}
		KCOCtrl = (KoiClothesOverlayController)obj;
		KoiClothesOverlayController kCOCtrl = KCOCtrl;
		if (kCOCtrl == null)
		{
			return null;
		}
		ClothesTexData overlayTex = kCOCtrl.GetOverlayTex(maskKind.ToString(), false);
		if (overlayTex == null)
		{
			return null;
		}
		return overlayTex.Texture;
	}

	private bool BodyMaskMultiply(Util.MaskKind maskKind, ref Texture maskBase)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Expected Obj, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		if (goRendTex == null)
		{
			if (ChaAlphaMask.MaskGo == null)
			{
				maskBase = null;
				return false;
			}
			GameObject val = TransformFindEx.FindLoop(((Component)this).gameObject.transform, "o_body_a");
			if (val == null)
			{
				maskBase = null;
				return false;
			}
			goRendTex = Object.Instantiate<GameObject>(ChaAlphaMask.MaskGo);
			goRendTex.transform.parent = val.transform;
			goRendTex.transform.localPosition = Vector3.zero;
			if (ChaAlphaMask.TexDebug.Value)
			{
				goRendTex.transform.localScale = new Vector3(100f, 1f, 100f);
				goRendTex.transform.localPosition = new Vector3(0.5f, 0f, 0f);
				goRendTex.SetActive(true);
			}
		}
		Renderer component = goRendTex.GetComponent<Renderer>();
		if (!MaskTexPathExists)
		{
			component.material.mainTexture = (maskBase = null);
			return false;
		}
		int num = 0;
		foreach (KeyValuePair<int, MaskProperty> item in bodyMaskProperty)
		{
			bool flag = false;
			try
			{
				if (item.Value != null)
				{
					flag = item.Key < 7 || ((item.Key == 7) ? (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.shoesType == 0) : (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.shoesType == 1));
					if (flag)
					{
						if (!string.IsNullOrEmpty(item.Value.ABBodyMask))
						{
							component.material.SetTexture(MultiplyShader.MaskTex[num], (Texture)(object)CommonLib.LoadAsset<Texture2D>(item.Value.ABPath, item.Value.ABBodyMask, false, ""));
						}
						else
						{
							component.material.SetTexture(MultiplyShader.MaskTex[num], (Texture)(object)global::Sideloader.Sideloader.GetPng(item.Value.PngBodyMaskPath, (TextureFormat)3, true));
						}
						flag = component.material.GetTexture(MultiplyShader.MaskTex[num]) != null;
						if (flag)
						{
							component.material.SetVector(MultiplyShader.MaskState[num], (Vector4)MultiplyShader.GetState(((ChaFile)((ChaInfo)((CharaCustomFunctionController)this).ChaControl).chaFile).status.clothesState[item.Key]));
						}
						else
						{
							ChaAlphaMask.Logger.LogInfo((object)$"Mask Texture missing. guid:{item.Value.Guid} category:{item.Value.CategoryNo} id:{item.Value.ID}");
						}
					}
				}
				if (!flag)
				{
					component.material.SetTexture(MultiplyShader.MaskTex[num], (Texture)null);
					component.material.SetVector(MultiplyShader.MaskState[num], (Vector4)Color.black);
				}
			}
			catch (Exception ex)
			{
				ChaAlphaMask.Logger.LogWarning((object)ex);
			}
			num++;
		}
		ApplyCustomMasks(component.material);
		if (((ChaFile)((ChaInfo)((CharaCustomFunctionController)this).ChaControl).chaFile).status.clothesState[0] == 3)
		{
			maskBase = (Texture)(object)ChaAlphaMask.MaskTex55;
		}
		else
		{
			maskBase = (Texture)(GetKCOXMask(maskKind) ?? ((object)Util.GetTexMask(((CharaCustomFunctionController)this).ChaControl, maskKind)) ?? ((object)ChaAlphaMask.MaskTex55));
		}
		component.material.mainTexture = maskBase;
		RenderTexture val2 = (RenderTexture.active = new RenderTexture(maskBase.width, maskBase.height, 0));
		Graphics.Blit(maskBase, val2, component.material);
		maskBase = (Texture)(object)val2;
		return true;
	}

	/// <summary>把卡片里导入的遮罩覆盖到对应服装槽位上（优先级高于 zipmod manifest 里的遮罩）。</summary>
	private void ApplyCustomMasks(Material mat)
	{
		for (int i = 0; i < 8; i++)
		{
			try
			{
				int clothesKind = i + 1;
				bool flag = clothesKind < 7 || ((clothesKind == 7) ? (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.shoesType == 0) : (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.shoesType == 1));
				if (!flag)
				{
					continue;
				}
				ChaFileClothes.PartsInfo partsInfo = ((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.parts[clothesKind];
				if (partsInfo == null || partsInfo.id == 0)
				{
					continue;
				}
				Texture2D customMaskTexture = GetCustomMaskTexture(clothesKind);
				if (!(customMaskTexture != null))
				{
					continue;
				}
				mat.SetTexture(MultiplyShader.MaskTex[i], (Texture)(object)customMaskTexture);
				byte state = ((ChaFile)((ChaInfo)((CharaCustomFunctionController)this).ChaControl).chaFile).status.clothesState[clothesKind];
				if (state <= 3)
				{
					mat.SetVector(MultiplyShader.MaskState[i], (Vector4)MultiplyShader.GetState(state));
				}
			}
			catch (Exception ex)
			{
				ChaAlphaMask.Logger.LogWarning((object)ex);
			}
		}
	}

	internal void ChangeClothesState(int clothesKind, byte? state)
	{
		if (!((ChaInfo)((CharaCustomFunctionController)this).ChaControl).loadEnd)
		{
			return;
		}
		if (HasAnyCustomMask)
		{
			hasChangedBodyMaskProp = true;
		}
		int category = ToCategoryNo(clothesKind);
		int localSlot = ToLocalSlot(clothesKind);
		ChangeBodyMaskProperty(clothesKind, category, localSlot);
		if (!state.HasValue)
		{
			return;
		}
		MaterialEditorCharaController mECtrl = MECtrl;
		if (mECtrl != null && !mECtrl.CoordinateChanging)
		{
			ObservableExtensions.Subscribe<long>(Observable.TimerFrame(1, (FrameCountType)0), (Action<long>)((long x) =>
			{
				ChangeInnerMask(clothesKind, state.Value);
			}));
		}
	}

	private void ReloadClothesState()
	{
		ObservableExtensions.Subscribe<long>(Observable.TimerFrame(4, (FrameCountType)0), (Action<long>)((long x) =>
		{
			for (int i = 0; i < 9; i++)
			{
				ChangeClothesState(i, ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.clothesState[i]);
			}
		}));
	}

	private void ChangeBodyMaskProperty(int clothesKind, int category, int localSlot)
	{
		if (!IsHighPoly)
		{
			return;
		}
		ListInfoBase obj = ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).infoClothes[NotObjClothesKind(clothesKind)];
		if (obj == null || obj.Id != localSlot)
		{
			return;
		}
		if (clothesKind != 0)
		{
			if (localSlot != 0)
			{
				GetDrawOption(clothesKind, out var op01, out var op2);
				MaskProperty maskProperty = ((!IsBotMask(clothesKind, ref category)) ? MaskProperty.DataSet.FirstOrDefault((MaskProperty x) => x.CategoryNo == category && x.LocalSlot == localSlot && !x.BotMask && x.ObjOpt01 == op01 && x.ObjOpt02 == op2) : MaskProperty.DataSet.FirstOrDefault((MaskProperty x) => x.CategoryNo == category && x.LocalSlot == localSlot && x.BotMask));
				if (maskProperty == null || !IsBodyMaskActive(clothesKind))
				{
					bodyMaskProperty[clothesKind] = null;
				}
				else
				{
					bodyMaskProperty[clothesKind] = maskProperty;
				}
			}
			else
			{
				bodyMaskProperty[clothesKind] = null;
			}
		}
		hasChangedBodyMaskProp = true;
	}

	internal void SetFloatBodyAlpha(byte state)
	{
		if (state == 3)
		{
			((ChaInfo)((CharaCustomFunctionController)this).ChaControl).customMatBody.SetFloat(ChaShader._alpha_a, MaskTexPathExists ? 1f : 0f);
			((ChaInfo)((CharaCustomFunctionController)this).ChaControl).customMatBody.SetFloat(ChaShader._alpha_b, 0f);
		}
	}

	private void GetDrawOption(int clothesKind, out bool? op01, out bool? op02)
	{
		switch (clothesKind)
		{
		case 2:
			op01 = !((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.hideBraOpt[0];
			op02 = !((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.hideBraOpt[1];
			break;
		case 3:
			op01 = !((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.hideShortsOpt[0];
			op02 = !((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.hideShortsOpt[1];
			break;
		default:
		{
			bool[] array = ((object)((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.parts[clothesKind]).GetType().GetProperty("hideOpt")?.GetValue(((CharaCustomFunctionController)this).ChaControl.nowCoordinate.clothes.parts[clothesKind], null) as bool[];
			op01 = array == null || !array[0];
			op02 = array == null || !array[1];
			break;
		}
		}
		if (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).cusClothesCmp[clothesKind] != null)
		{
			op01 = ((((ChaInfo)((CharaCustomFunctionController)this).ChaControl).cusClothesCmp[clothesKind].objOpt01.Length == 0) ? ((bool?)null) : op01);
			op02 = ((((ChaInfo)((CharaCustomFunctionController)this).ChaControl).cusClothesCmp[clothesKind].objOpt02.Length == 0) ? ((bool?)null) : op02);
		}
	}

	internal bool IsBotMask(int clothesKind, ref int category)
	{
		switch (clothesKind)
		{
		case 1:
			if (((CharaCustomFunctionController)this).ChaControl.notBot)
			{
				category--;
				return true;
			}
			break;
		case 3:
			if (((CharaCustomFunctionController)this).ChaControl.notShorts)
			{
				category--;
				return true;
			}
			break;
		}
		return false;
	}

	private bool IsBotMask(int clothesKind)
	{
		return clothesKind switch
		{
			1 => ((CharaCustomFunctionController)this).ChaControl.notBot, 
			3 => ((CharaCustomFunctionController)this).ChaControl.notShorts, 
			_ => false, 
		};
	}

	private void ChangeInnerMask(int clothesKind, byte state)
	{
		if (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).objClothes[clothesKind] == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < 9; i++)
			{
				int num = SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType, i];
				if (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).infoClothes[clothesKind].Id == 0 || num == -1)
				{
					continue;
				}
				bool flag = num > 7;
				num = (flag ? (num - 8) : num);
				if (num != clothesKind)
				{
					continue;
				}
				GameObject val = ((i != 0) ? ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).objClothes[i] : (((ChaInfo)((CharaCustomFunctionController)this).ChaControl).infoClothes[0].Id switch
				{
					1 => ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).objParts[0], 
					2 => ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).objParts[1], 
					_ => ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).objClothes[i], 
				}));
				if (val == null)
				{
					continue;
				}
				Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>();
				for (int j = 0; j < componentsInChildren?.Length; j++)
				{
					if (componentsInChildren[j].sharedMaterial.GetTexture(ChaShader._AlphaMask) != null)
					{
						SetFloatClothesAlpha(state, componentsInChildren[j], flag);
					}
				}
			}
		}
		catch
		{
		}
	}

	private void SetFloatClothesAlpha(byte state, Renderer renderer, bool invert = false)
	{
		for (int i = 0; i < ((renderer != null) ? new int?(renderer.sharedMaterials.Length) : ((int?)null)); i++)
		{
			Material val = renderer.sharedMaterials[i];
			int alpha_a = ChaShader._alpha_a;
			float num;
			if (invert)
			{
				num = ((state == 3) ? 1f : 0f);
			}
			else
			{
				num = ((state == 0) ? 1f : 0f);
			}
			((Material)val).SetFloat(alpha_a, num);
			renderer.sharedMaterials[i].SetFloat(ChaShader._alpha_b, (state == 1 || state == 2) ? 1f : 0f);
		}
	}

	private IEnumerator InitObserve()
	{
		yield return null;
		try
		{
			AlphaMaskController alphaMaskController = this;
			CharaCustomFunctionController val = CharacterApi.GetBehaviours(((CharaCustomFunctionController)this).ChaControl).FirstOrDefault((CharaCustomFunctionController x) => ((object)x).GetType() == typeof(MaterialEditorCharaController));
			alphaMaskController.MECtrl = (MaterialEditorCharaController)(object)((val is MaterialEditorCharaController) ? val : null);
			ObsMEChanging = new IDisposable[1];
			ObsMEChanging[0] = ObservableExtensions.Subscribe<bool>(Observable.Where<bool>(Observable.Skip<bool>(ObserveExtensions.ObserveEveryValueChanged<MaterialEditorCharaController, bool>(MECtrl, (Func<MaterialEditorCharaController, bool>)((MaterialEditorCharaController x) => x.CoordinateChanging), (FrameCountType)0, false), 1), (Func<bool, bool>)((bool x) => !x)), (Action<bool>)((bool x) =>
			{
				for (int i = 0; i < 9; i++)
				{
					ChangeClothesState(i, ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.clothesState[i]);
				}
			}));
			Scene activeScene = SceneManager.GetActiveScene();
			if (!(activeScene.name == "CustomScene"))
			{
				yield break;
			}
			ObsCharacter = new IDisposable[3];
			ObsCharacter[0] = ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(ObserveExtensions.ObserveEveryValueChanged<ChaControl, bool>(((CharaCustomFunctionController)this).ChaControl, (Func<ChaControl, bool>)((ChaControl x) => x.notBot), (FrameCountType)0, false), 1), (Action<bool>)((bool x) =>
			{
				if (MaskTexPathExists)
				{
					ChangeClothesState(1, null);
				}
			}));
			ObsCharacter[1] = ObservableExtensions.Subscribe<bool>(Observable.Skip<bool>(ObserveExtensions.ObserveEveryValueChanged<ChaControl, bool>(((CharaCustomFunctionController)this).ChaControl, (Func<ChaControl, bool>)((ChaControl x) => x.notShorts), (FrameCountType)0, false), 1), (Action<bool>)((bool x) =>
			{
				if (MaskTexPathExists)
				{
					ChangeClothesState(3, null);
				}
			}));
			ObsCharacter[2] = ObservableExtensions.Subscribe<byte>(Observable.Skip<byte>(ObserveExtensions.ObserveEveryValueChanged<ChaControl, byte>(((CharaCustomFunctionController)this).ChaControl, (Func<ChaControl, byte>)((ChaControl x) => ((ChaInfo)x).fileStatus.shoesType), (FrameCountType)0, false), 1), (Action<byte>)((byte x) =>
			{
				if (MaskTexPathExists)
				{
					ChangeClothesState(7, null);
				}
			}));
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning((object)ex);
		}
	}

	protected override void OnCardBeingSaved(GameMode currentGameMode)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		bool flag = SyncClothesKind.Cast<int>().Any((int x) => x != -1);
		bool flag2 = kindPairBodyMask != null && kindPairBodyMask.Count > 0;
		bool flag3 = customMasks != null && customMasks.Count > 0;
		if (!flag && !flag2 && !flag3)
		{
			((CharaCustomFunctionController)this).SetExtendedData((PluginData)null);
			return;
		}
		try
		{
			PluginData val = new PluginData
			{
				version = 1
			};
			if (flag)
			{
				val.data.Add("SyncClothesKind", MessagePackSerializer.Serialize<int[,]>(SyncClothesKind));
			}
			if (flag2)
			{
				val.data.Add("kindPairBodyMask", MessagePackSerializer.Serialize<List<KindPair>>(kindPairBodyMask));
			}
			if (flag3)
			{
				val.data.Add("CustomMasks", MessagePackSerializer.Serialize<List<CustomMask>>(customMasks));
			}
			((CharaCustomFunctionController)this).SetExtendedData(val);
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError((object)ex);
		}
	}

	protected override void OnReload(GameMode currentGameMode)
	{
		((MonoBehaviour)this).StartCoroutine(InitObserve());
		for (int i = 0; i < 7; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				SyncClothesKind[i, j] = -1;
			}
		}
		kindPairBodyMask.Clear();
		customMasks.Clear();
		InvalidateAllMaskTextures();
		hasChangedBodyMaskProp = true;
		PluginData extendedData = ((CharaCustomFunctionController)this).GetExtendedData();
		if (extendedData == null)
		{
			TryImportPlusPackMasks();
			ReloadClothesState();
			return;
		}
		try
		{
			if (extendedData.data.TryGetValue("SyncClothesKind", out var value) && value != null)
			{
				int[,] array = MessagePackSerializer.Deserialize<int[,]>((byte[])value);
				if (array.Length == SyncClothesKind.Length)
				{
					SyncClothesKind = array;
				}
			}
			if (extendedData.data.TryGetValue("kindPairBodyMask", out var value2) && value2 != null)
			{
				kindPairBodyMask = MessagePackSerializer.Deserialize<List<KindPair>>((byte[])value2);
			}
			bool flag = false;
			if (extendedData.data.TryGetValue("CustomMasks", out var value3) && value3 != null)
			{
				customMasks = MessagePackSerializer.Deserialize<List<CustomMask>>((byte[])value3);
				customMasks.RemoveAll((CustomMask x) => x == null || x.png == null);
				flag = customMasks.Count > 0;
			}
			if (!flag)
			{
				TryImportPlusPackMasks();
			}
			ReloadClothesState();
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError((object)ex);
		}
	}

	/// <summary>读取 Madevil PlusPack - ChaAlphaMask（madevil.kk.PPCAM）存在卡片里的遮罩，方便旧卡片继续用。</summary>
	private void TryImportPlusPackMasks()
	{
		try
		{
			if (!ChaAlphaMask.ImportPlusPackMasks.Value || customMasks.Count > 0)
			{
				return;
			}
			ChaFile val = ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).chaFile;
			PluginData pluginData = ((val != null) ? ExtendedSave.GetExtendedDataById(val, "madevil.kk.PPCAM") : null);
			if (pluginData == null && MakerAPI.InsideMaker && MakerAPI.LastLoadedChaFile != null)
			{
				pluginData = ExtendedSave.GetExtendedDataById(MakerAPI.LastLoadedChaFile, "madevil.kk.PPCAM");
			}
			if (pluginData == null || pluginData.data == null)
			{
				return;
			}
			if (!pluginData.data.TryGetValue("BodyMaskList", out object value) || value == null)
			{
				return;
			}
			if (!pluginData.data.TryGetValue("TextureDictionary", out object value2) || value2 == null)
			{
				return;
			}
			Dictionary<int, int?[]> dictionary = MessagePackSerializer.Deserialize<Dictionary<int, int?[]>>((byte[])value);
			Dictionary<int, byte[]> dictionary2 = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])value2);
			int num = 0;
			foreach (KeyValuePair<int, int?[]> item in dictionary)
			{
				for (int i = 0; i < item.Value.Length && i < 8; i++)
				{
					if (!item.Value[i].HasValue)
					{
						continue;
					}
					if (!dictionary2.TryGetValue(item.Value[i].Value, out byte[] value3) || value3 == null)
					{
						continue;
					}
					customMasks.Add(new CustomMask
					{
						coordinateType = item.Key,
						clothesKind = i + 1,
						png = value3
					});
					num++;
				}
			}
			if (num > 0)
			{
				ChaAlphaMask.Logger.LogInfo((object)$"Imported {num} mask texture(s) from PlusPack data (madevil.kk.PPCAM)");
				hasChangedBodyMaskProp = true;
			}
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning((object)("Failed to import PlusPack mask data: " + ex.Message));
		}
	}

	protected override void OnDestroy()
	{
		for (int i = 0; i < ObsMEChanging?.Length; i++)
		{
			ObsMEChanging[i].Dispose();
		}
		for (int j = 0; j < ObsCharacter?.Length; j++)
		{
			ObsCharacter[j].Dispose();
		}
		InvalidateAllMaskTextures();
		base.OnDestroy();
	}

	protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
	{
		PluginData coordinateExtendedData = ((CharaCustomFunctionController)this).GetCoordinateExtendedData(coordinate);
		if (coordinateExtendedData != null)
		{
			try
			{
				if (coordinateExtendedData.data.TryGetValue("SyncClothesKind", out var value) && value != null)
				{
					int[] array = MessagePackSerializer.Deserialize<int[]>((byte[])value);
					for (int i = 0; i < 9; i++)
					{
						SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType, i] = array[i];
					}
				}
				if (coordinateExtendedData.data.TryGetValue("kindPairBodyMask", out var value2) && value2 != null)
				{
					List<KindPair> list = MessagePackSerializer.Deserialize<List<KindPair>>((byte[])value2);
					list.ForEach((KindPair x) =>
					{
						x.coordinateType = ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType;
					});
					kindPairBodyMask.RemoveAll((KindPair x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType);
					kindPairBodyMask.AddRange(list);
				}
				customMasks.RemoveAll((CustomMask x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType);
				if (coordinateExtendedData.data.TryGetValue("CustomMasks", out var value3) && value3 != null)
				{
					List<CustomMask> list2 = MessagePackSerializer.Deserialize<List<CustomMask>>((byte[])value3);
					list2.ForEach((CustomMask x) =>
					{
						x.coordinateType = ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType;
					});
					list2.RemoveAll((CustomMask x) => x == null || x.png == null);
					customMasks.AddRange(list2);
				}
				InvalidateAllMaskTextures();
				hasChangedBodyMaskProp = true;
				ReloadClothesState();
				return;
			}
			catch (Exception ex)
			{
				ChaAlphaMask.Logger.LogError((object)ex);
				return;
			}
		}
		for (int num = 0; num < 9; num++)
		{
			SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType, num] = -1;
		}
		kindPairBodyMask.RemoveAll((KindPair x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType);
		customMasks.RemoveAll((CustomMask x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType);
		InvalidateAllMaskTextures();
		hasChangedBodyMaskProp = true;
	}

	protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected Obj, but got Unknown
		int[] array = new int[9];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = SyncClothesKind[((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType, i];
		}
		bool flag = array.Any((int x) => x != -1);
		List<KindPair> list = kindPairBodyMask.Where((KindPair x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType).ToList();
		bool flag2 = list != null && list.Count > 0;
		List<CustomMask> list2 = customMasks.Where((CustomMask x) => x.coordinateType == ((ChaInfo)((CharaCustomFunctionController)this).ChaControl).fileStatus.coordinateType).ToList();
		bool flag3 = list2 != null && list2.Count > 0;
		if (!flag && !flag2 && !flag3)
		{
			((CharaCustomFunctionController)this).SetCoordinateExtendedData(coordinate, (PluginData)null);
			return;
		}
		PluginData val = new PluginData
		{
			version = 1
		};
		try
		{
			if (flag)
			{
				val.data.Add("SyncClothesKind", MessagePackSerializer.Serialize<int[]>(array));
			}
			if (flag2)
			{
				val.data.Add("kindPairBodyMask", MessagePackSerializer.Serialize<List<KindPair>>(list));
			}
			if (flag3)
			{
				val.data.Add("CustomMasks", MessagePackSerializer.Serialize<List<CustomMask>>(list2));
			}
			((CharaCustomFunctionController)this).SetCoordinateExtendedData(coordinate, val);
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError((object)ex);
		}
	}
}
