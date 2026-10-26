using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Utilities;
using KK_Plugins;
using Sideloader;
using UnityEngine;

namespace KK_ChaAlphaMask;

[BepInPlugin("nakay.kk.ChaAlphaMask", "Character Alpha Mask", "1.0.0")]
[BepInDependency("marco.kkapi", "1.17")]
[BepInDependency("com.deathweasel.bepinex.materialeditor")]
[BepInDependency("KCOX")]
public class ChaAlphaMask : BaseUnityPlugin
{
	public const string GUID = "nakay.kk.ChaAlphaMask";

	public const string PluginName = "Character Alpha Mask";

	public const string PluginVersion = "1.0.0";

	internal static new ManualLogSource Logger;

	private readonly Harmony harmony = new Harmony("nakay.kk.ChaAlphaMask");

	internal static ChaAlphaMask Instance;

	private static Texture2D maskTex55;

	internal static ConfigEntry<bool> TexDebug { get; private set; }

	internal static bool ForceHighPolyExists { get; private set; }

	internal static bool GetForceHighPolyEnabled => ForceHighPoly.Enabled.Value;

	internal static Texture2D MaskTex55
	{
		get
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			maskTex55 = maskTex55 ?? Util.CreateTexture(512, 512, new Color32(byte.MaxValue, byte.MaxValue, (byte)0, (byte)0));
			return maskTex55;
		}
	}

	internal static GameObject MaskGo { get; private set; }

	private void Start()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected Obj, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected Obj, but got Unknown
		Instance = this;
		Logger = base.Logger;
		TexDebug = ((BaseUnityPlugin)this).Config.Bind<bool>("", "Mask Texture Debug", false, new ConfigDescription("", (AcceptableValueBase)null, new object[1] { (object)new ConfigurationManagerAttributes
		{
			IsAdvanced = true
		} }));
		harmony.PatchAll(typeof(Patch));
		ForceHighPolyExists = Chainloader.PluginInfos.ContainsKey("com.deathweasel.bepinex.forcehighpoly");
		CharacterApi.RegisterExtraBehaviour<AlphaMaskController>("nakay.kk.ChaAlphaMask");
#pragma warning disable CS0618 // KKS 的 Sideloader 中该字段已过时，但 KK 的 Sideloader 没有替代属性
		MaskProperty.Init(global::Sideloader.Sideloader.Manifests.Values.Select((Manifest x) => x.manifestDocument));
#pragma warning restore CS0618
		LoadResource();
		MultiplyShader.Init();
		MakerGui.Init();
	}

	private void LoadResource()
	{
		try
		{
			AssetBundle val = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("alphamaskmultiply", (Assembly)null));
			MaskGo = val.LoadAsset<GameObject>("o_Mask");
			MaskGo.transform.parent = ((Component)this).gameObject.transform;
			MaskGo.SetActive(false);
			val.Unload(false);
		}
		catch (Exception ex)
		{
			Logger.LogError((object)("Failed to load the embedded asset bundle 'alphamaskmultiply'. Alpha mask rendering is disabled. " + ex));
		}
	}

	private void OnDestroy()
	{
		harmony.UnpatchSelf();
	}

	public ChaAlphaMask()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
	}
}
