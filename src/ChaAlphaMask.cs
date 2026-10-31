using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Utilities;
using KK_Plugins;
using Sideloader;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_ChaAlphaMask;

[BepInPlugin("nakay.kk.ChaAlphaMask", "Character Alpha Mask", "1.1.1")]
[BepInDependency("marco.kkapi", "1.17")]
[BepInDependency("com.deathweasel.bepinex.materialeditor")]
[BepInDependency("KCOX")]
public class ChaAlphaMask : BaseUnityPlugin
{
	public const string GUID = "nakay.kk.ChaAlphaMask";

	public const string PluginName = "Character Alpha Mask";

	public const string PluginVersion = "1.1.1";

	internal static new ManualLogSource Logger;

	private readonly Harmony harmony = new Harmony("nakay.kk.ChaAlphaMask");

	internal static ChaAlphaMask Instance;

	private static Texture2D maskTex55;

	internal static ConfigEntry<bool> TexDebug { get; private set; }

	internal static bool ForceHighPolyExists { get; private set; }

	/// <summary>Force High Poly 是可选插件，这里用反射读取它的开关，没装也不会崩。</summary>
	internal static bool GetForceHighPolyEnabled
	{
		get
		{
			try
			{
				Type type = Type.GetType("KK_ForceHighPoly.ForceHighPoly, KK_ForceHighPoly") ?? Type.GetType("KKS_ForceHighPoly.ForceHighPoly, KKS_ForceHighPoly");
				PropertyInfo enabledProperty = type?.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
				object configEntry = enabledProperty?.GetValue(null, null);
				PropertyInfo valueProperty = configEntry?.GetType().GetProperty("Value");
				if (valueProperty == null)
				{
					return false;
				}
				object value = valueProperty.GetValue(configEntry, null);
				return value is bool && (bool)value;
			}
			catch
			{
				return false;
			}
		}
	}

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

	internal static ConfigEntry<KeyboardShortcut> ToggleWindowKey { get; private set; }

	internal static ConfigEntry<float> WindowPosX { get; private set; }

	internal static ConfigEntry<float> WindowPosY { get; private set; }

	internal static ConfigEntry<string> ExportPath { get; private set; }

	internal static ConfigEntry<bool> ImportPlusPackMasks { get; private set; }

	private static MaskWindow maskWindow;

	internal static SidebarToggle SidebarToggle { get; private set; }

	private static bool syncingSidebarToggle;

	private void InitConfig()
	{
		ToggleWindowKey = base.Config.Bind<KeyboardShortcut>("Maker", "Toggle Mask Window", new KeyboardShortcut(KeyCode.F5), "捏人界面内打开 / 关闭「角色Alpha遮罩」窗口的快捷键（默认 F5）");
		WindowPosX = base.Config.Bind<float>("Maker", "Window X", 560f, "窗口初始 X 坐标");
		WindowPosY = base.Config.Bind<float>("Maker", "Window Y", 100f, "窗口初始 Y 坐标");
		ExportPath = base.Config.Bind<string>("General", "Export Path", Path.Combine(Path.Combine(BepInEx.Paths.GameRootPath, "UserData"), "Overlays"), "遮罩导出目录，同时也是导入对话框的起始目录（默认游戏根目录下的 UserData\\Overlays）");
		// 旧版默认目录是 UserData\AlphaMask，这里自动迁移到 UserData\Overlays
		if (ExportPath.Value != null && ExportPath.Value.Replace('/', '\\').EndsWith("UserData\\AlphaMask", StringComparison.OrdinalIgnoreCase))
		{
			ExportPath.Value = Path.Combine(Path.Combine(BepInEx.Paths.GameRootPath, "UserData"), "Overlays");
		}
		ImportPlusPackMasks = base.Config.Bind<bool>("General", "Import PlusPack Mask Data", true, "读取 Madevil PlusPack - ChaAlphaMask（madevil.kk.PPCAM）存在卡片里的遮罩数据");
	}

	private void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
	{
		if (maskWindow != null)
		{
			return;
		}
		maskWindow = ((Component)this).gameObject.AddComponent<MaskWindow>();
	}

	private void MakerAPI_MakerExiting(object sender, EventArgs e)
	{
		if (maskWindow != null)
		{
			Object.Destroy((Object)(object)maskWindow);
			maskWindow = null;
		}
		SidebarToggle = null;
	}

	/// <summary>把开关放到捏人界面右侧的「Control Panel」栏里。</summary>
	private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
	{
		try
		{
			SidebarToggle = MakerAPI.AddSidebarControl(new SidebarToggle("Alpha遮罩窗口", MaskWindow.WindowVisible, this));
			SidebarToggle.ValueChanged.Subscribe(delegate(bool value)
			{
				if (syncingSidebarToggle)
				{
					return;
				}
				if (maskWindow != null)
				{
					maskWindow.Visible = value;
				}
				else
				{
					MaskWindow.WindowVisible = value;
				}
			});
		}
		catch (Exception ex)
		{
			Logger.LogWarning("Failed to add mask window toggle to the maker sidebar: " + ex);
		}
	}

	internal static void SetSidebarToggleSilently(bool value)
	{
		if (SidebarToggle == null || SidebarToggle.Value == value)
		{
			return;
		}
		syncingSidebarToggle = true;
		try
		{
			SidebarToggle.Value = value;
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex);
		}
		finally
		{
			syncingSidebarToggle = false;
		}
	}

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
		InitConfig();
		LoadResource();
		MultiplyShader.Init();
		MakerGui.Init();
		InputBlock.Patch(harmony);
		MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
		MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
		MakerAPI.MakerExiting += MakerAPI_MakerExiting;
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
