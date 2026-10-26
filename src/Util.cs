using System.Linq;
using HarmonyLib;
using KKAPI.Chara;
using UnityEngine;

namespace KK_ChaAlphaMask;

internal class Util
{
	internal enum MaskKind
	{
		BodyMask,
		InnerMask,
		BraMask
	}

	internal static string GetMaskFieldName(MaskKind kind)
	{
		return kind switch
		{
			MaskKind.BodyMask => "texBodyAlphaMask", 
			MaskKind.InnerMask => "texInnerAlphaMask", 
			MaskKind.BraMask => "texBraAlphaMask", 
			_ => null, 
		};
	}

	internal static Texture GetTexMask(ChaControl chaControl, MaskKind maskKind)
	{
		return Traverse.Create((object)chaControl).Property(GetMaskFieldName(maskKind), (object[])null).GetValue<Texture>();
	}

	internal static Texture2D CreateTexture(int? width, int? height, Color32 color)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected Obj, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width ?? 512, height ?? 512, (TextureFormat)3, false);
		Color32[] pixels = val.GetPixels32();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = color;
		}
		val.SetPixels32(pixels);
		val.Apply();
		return val;
	}

	internal static AlphaMaskController GetController(ChaControl chaControl)
	{
		if (!((Object)(object)chaControl == (Object)null))
		{
			return CharacterApi.GetBehaviours(chaControl).FirstOrDefault((CharaCustomFunctionController x) => ((object)x).GetType() == typeof(AlphaMaskController)) as AlphaMaskController;
		}
		return null;
	}

	internal static AlphaMaskController GetController(Material customMatBody)
	{
		var registeredBehaviour = CharacterApi.GetRegisteredBehaviour(typeof(AlphaMaskController));
		if (registeredBehaviour == null)
		{
			return null;
		}
		foreach (AlphaMaskController item in registeredBehaviour.Instances.Cast<AlphaMaskController>())
		{
			if ((Object)(object)((ChaInfo)((CharaCustomFunctionController)item).ChaControl).customMatBody == (Object)(object)customMatBody)
			{
				return item;
			}
		}
		return null;
	}
}
