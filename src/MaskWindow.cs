using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_ChaAlphaMask;

/// <summary>捏人界面里的「角色Alpha遮罩」窗口：按服装槽位导入 / 导出 / 删除存在卡片里的遮罩 PNG。</summary>
internal class MaskWindow : MonoBehaviour
{
	private const int WindowID = 1128355395;

	private const float WindowWidth = 420f;

	private const float WindowHeight = 520f;

	private const float TitleBarHeight = 28f;

	private const float ThumbSize = 80f;

	private const float Pad = 8f;

	private const int Radius = 10;

	private static readonly int[] ClothesKindList = { 1, 2, 3, 4, 5, 6, 7, 8 };

	private static readonly string[] ClothesKindNames = { "下衣", "胸罩", "短裤", "手套", "裤袜", "袜子", "室内鞋", "室外鞋" };

	private static readonly string[] ClothesKindNamesNone = { "下衣（未装备）", "胸罩（未装备）", "短裤（未装备）", "手套（未装备）", "裤袜（未装备）", "袜子（未装备）", "室内鞋（未装备）", "室外鞋（未装备）" };

	private static readonly GUIContent TitleContent = new GUIContent("角色Alpha遮罩");

	private static readonly GUIContent CloseContent = new GUIContent("×", "关闭窗口（也可点右侧控制栏的开关或按快捷键）");

	private static readonly GUIContent ShowAllContent = new GUIContent(" 全部显示", "显示当前未装备的槽位");

	private static readonly GUIContent DeleteContent = new GUIContent("删除", "移除该槽位的遮罩");

	private static readonly GUIContent ExportContent = new GUIContent("导出", "把当前遮罩保存为 PNG");

	private static readonly GUIContent ImportContent = new GUIContent("导入", "导入 PNG 遮罩（推荐 512x512）");

	private const string HintText = "遮罩画法：黑底 + 纯红 = 要遮蔽的身体部位，尺寸推荐 512x512";

	// 配色：中灰蓝底 + 浅灰文字 + 蓝色主按钮
	private static readonly Color ColWindow = new Color(0.247f, 0.275f, 0.337f, 0.97f);

	private static readonly Color ColRow = new Color(0.196f, 0.224f, 0.282f, 1f);

	private static readonly Color ColLine = new Color(0.322f, 0.357f, 0.427f, 1f);

	private static readonly Color ColButton = new Color(0.302f, 0.337f, 0.408f, 1f);

	private static readonly Color ColButtonHover = new Color(0.380f, 0.420f, 0.498f, 1f);

	private static readonly Color ColButtonActive = new Color(0.251f, 0.282f, 0.349f, 1f);

	private static readonly Color ColPrimary = new Color(0.231f, 0.478f, 0.855f, 1f);

	private static readonly Color ColPrimaryHover = new Color(0.298f, 0.549f, 0.902f, 1f);

	private static readonly Color ColPrimaryActive = new Color(0.180f, 0.396f, 0.729f, 1f);

	private static readonly Color ColText = new Color(0.902f, 0.918f, 0.949f, 1f);

	private static readonly Color ColTextDim = new Color(0.651f, 0.690f, 0.761f, 1f);

	/// <summary>窗口开关状态，跨 maker 会话保留。</summary>
	internal static bool WindowVisible;

	private Rect windowRect;

	private Vector2 scrollPos;

	private bool showAllSlots;

	private bool visible;

	private bool dragging;

	private bool posDirty;

	private float posDirtyTime;

	private GUIStyle windowStyle;

	private GUIStyle titleStyle;

	private GUIStyle boxStyle;

	private GUIStyle labelStyle;

	private GUIStyle thumbStyle;

	private GUIStyle buttonStyle;

	private GUIStyle primaryButtonStyle;

	private GUIStyle closeButtonStyle;

	private GUIStyle toggleStyle;

	private GUIStyle hintStyle;

	private GUIStyle lineStyle;

	private Texture2D texWindow;

	private Texture2D texBox;

	private Texture2D texButton;

	private Texture2D texButtonHover;

	private Texture2D texButtonActive;

	private Texture2D texPrimary;

	private Texture2D texPrimaryHover;

	private Texture2D texPrimaryActive;

	private Texture2D texThumb;

	private Texture2D texLine;

	private AlphaMaskController ctrl;

	internal bool Visible
	{
		get
		{
			return visible;
		}
		set
		{
			SetVisible(value, false);
		}
	}

	private void Awake()
	{
		windowRect = new Rect(ChaAlphaMask.WindowPosX.Value, ChaAlphaMask.WindowPosY.Value, WindowWidth, WindowHeight);
		visible = WindowVisible;
	}

	private void OnDestroy()
	{
		dragging = false;
		InputBlock.Clear();
		DestroyTexture(ref texWindow);
		DestroyTexture(ref texBox);
		DestroyTexture(ref texButton);
		DestroyTexture(ref texButtonHover);
		DestroyTexture(ref texButtonActive);
		DestroyTexture(ref texPrimary);
		DestroyTexture(ref texPrimaryHover);
		DestroyTexture(ref texPrimaryActive);
		DestroyTexture(ref texThumb);
		DestroyTexture(ref texLine);
	}

	private static void DestroyTexture(ref Texture2D texture)
	{
		if (texture != null)
		{
			UnityEngine.Object.Destroy(texture);
			texture = null;
		}
	}

	private void SetVisible(bool value, bool fromToggle)
	{
		if (visible == value)
		{
			return;
		}
		visible = value;
		WindowVisible = value;
		if (!value)
		{
			dragging = false;
			InputBlock.Clear();
		}
		if (!fromToggle)
		{
			ChaAlphaMask.SetSidebarToggleSilently(value);
		}
	}

	private void Update()
	{
		if (ChaAlphaMask.ToggleWindowKey.Value.IsDown())
		{
			SetVisible(!visible, false);
			if (visible)
			{
				RefreshController();
			}
		}
		InputBlock.Visible = visible;
		InputBlock.WindowRect = windowRect;
		InputBlock.Dragging = dragging;

		// 拖完 1 秒再写配置：BepInEx 每次设置改变都会落盘，紧贴松手写会有一下顿挫
		if (posDirty && Time.unscaledTime - posDirtyTime > 1f)
		{
			posDirty = false;
			SaveWindowPos();
		}
	}

	private void RefreshController()
	{
		ctrl = null;
		try
		{
			if (MakerAPI.InsideMaker)
			{
				ctrl = Util.GetController(MakerAPI.GetCharacterControl());
			}
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning(ex);
		}
	}

	private void OnGUI()
	{
		if (!visible || !MakerAPI.InsideMaker)
		{
			return;
		}

		// 鼠标位置被冻结后 Input.mousePosition 不再可信，用 IMGUI 事件里的真实位置
		Event current = Event.current;
		if (current != null && current.type != EventType.Used)
		{
			InputBlock.RawMouse = new Vector3(current.mousePosition.x, (float)Screen.height - current.mousePosition.y, 0f);
		}

		if (ctrl == null)
		{
			RefreshController();
		}
		if (ctrl == null)
		{
			return;
		}
		if (windowStyle == null)
		{
			EnsureStyle();
		}
		windowRect = GUI.Window(WindowID, windowRect, DrawWindow, GUIContent.none, windowStyle);
		InputBlock.WindowRect = windowRect;
	}

	private void DrawWindow(int windowID)
	{
		Rect titleRect = new Rect(0f, 0f, windowRect.width, TitleBarHeight);
		Event current = Event.current;
		if (current != null)
		{
			if (current.type == EventType.MouseDown && titleRect.Contains(current.mousePosition))
			{
				dragging = true;
				InputBlock.Dragging = true;
			}
			else if (current.type == EventType.MouseUp && dragging)
			{
				dragging = false;
				InputBlock.Dragging = false;
				posDirty = true;
				posDirtyTime = Time.unscaledTime;
			}
		}

		GUI.Label(new Rect(12f, 0f, windowRect.width - 60f, TitleBarHeight), TitleContent, titleStyle);
		if (GUI.Button(new Rect(windowRect.width - TitleBarHeight + 2f, 3f, TitleBarHeight - 6f, TitleBarHeight - 6f), CloseContent, closeButtonStyle))
		{
			SetVisible(false, false);
			return;
		}
		GUI.DrawTexture(new Rect(Pad, TitleBarHeight, windowRect.width - Pad * 2f, 1f), texLine);
		GUI.DragWindow(titleRect);

		GUILayout.BeginArea(new Rect(Pad, TitleBarHeight + 7f, windowRect.width - Pad * 2f, windowRect.height - TitleBarHeight - 15f));
		showAllSlots = GUILayout.Toggle(showAllSlots, ShowAllContent, toggleStyle, GUILayout.ExpandWidth(false));
		GUILayout.Space(4f);
		scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		for (int i = 0; i < ClothesKindList.Length; i++)
		{
			DrawSlot(ClothesKindList[i], ClothesKindNames[i], ClothesKindNamesNone[i]);
		}
		GUILayout.EndScrollView();
		GUILayout.Label(HintText, hintStyle, GUILayout.ExpandWidth(true));
		GUILayout.EndArea();
	}

	private void SaveWindowPos()
	{
		if (ChaAlphaMask.WindowPosX.Value != windowRect.x)
		{
			ChaAlphaMask.WindowPosX.Value = windowRect.x;
		}
		if (ChaAlphaMask.WindowPosY.Value != windowRect.y)
		{
			ChaAlphaMask.WindowPosY.Value = windowRect.y;
		}
	}

	private void DrawSlot(int clothesKind, string clothesName, string clothesNameNone)
	{
		bool flag = IsSlotEquipped(clothesKind);
		if (!showAllSlots && !flag)
		{
			return;
		}
		byte[] customMaskPng = ctrl.GetCustomMaskPng(clothesKind);
		bool flag2 = customMaskPng != null && customMaskPng.Length != 0;
		Texture2D customMaskPreview = ctrl.GetCustomMaskPreview(clothesKind);
		GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(ThumbSize + 12f), GUILayout.ExpandWidth(true));
		GUILayout.Box(customMaskPreview != null ? (Texture)customMaskPreview : texThumb, thumbStyle, GUILayout.Width(ThumbSize), GUILayout.Height(ThumbSize));
		GUILayout.Label(flag ? clothesName : clothesNameNone, labelStyle, GUILayout.Width(96f));
		GUILayout.FlexibleSpace();
		bool enabled = GUI.enabled;
		GUI.enabled = flag2;
		if (GUILayout.Button(DeleteContent, buttonStyle, GUILayout.Width(44f), GUILayout.Height(26f)))
		{
			ctrl.SetCustomMask(clothesKind, null);
		}
		if (GUILayout.Button(ExportContent, buttonStyle, GUILayout.Width(44f), GUILayout.Height(26f)))
		{
			Export(clothesKind, clothesName, customMaskPng);
		}
		GUI.enabled = enabled;
		if (GUILayout.Button(ImportContent, primaryButtonStyle, GUILayout.Width(44f), GUILayout.Height(26f)))
		{
			Import(clothesKind);
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(6f);
	}

	private bool IsSlotEquipped(int clothesKind)
	{
		try
		{
			ChaFileClothes.PartsInfo partsInfo = ctrl.ChaControl.nowCoordinate.clothes.parts[clothesKind];
			return partsInfo != null && partsInfo.id != 0;
		}
		catch
		{
			return false;
		}
	}

	private void EnsureStyle()
	{
		texWindow = MakeRoundedBox(96, Radius, ColWindow, ColLine, 1);
		texBox = MakeRoundedBox(64, 8, ColRow, new Color(0f, 0f, 0f, 0f), 0);
		texButton = MakeRoundedBox(64, 8, ColButton, new Color(0f, 0f, 0f, 0f), 0);
		texButtonHover = MakeRoundedBox(64, 8, ColButtonHover, new Color(0f, 0f, 0f, 0f), 0);
		texButtonActive = MakeRoundedBox(64, 8, ColButtonActive, new Color(0f, 0f, 0f, 0f), 0);
		texPrimary = MakeRoundedBox(64, 8, ColPrimary, new Color(0f, 0f, 0f, 0f), 0);
		texPrimaryHover = MakeRoundedBox(64, 8, ColPrimaryHover, new Color(0f, 0f, 0f, 0f), 0);
		texPrimaryActive = MakeRoundedBox(64, 8, ColPrimaryActive, new Color(0f, 0f, 0f, 0f), 0);
		texThumb = MakeRoundedBox(48, 6, new Color(0.149f, 0.176f, 0.227f, 1f), new Color(0.263f, 0.298f, 0.361f, 1f), 1);
		texLine = MakeBox(ColLine);

		windowStyle = new GUIStyle(GUI.skin.box);
		windowStyle.normal.background = texWindow;
		windowStyle.border = new RectOffset(Radius, Radius, Radius, Radius);
		windowStyle.padding = new RectOffset(0, 0, 0, 0);
		windowStyle.margin = new RectOffset(0, 0, 0, 0);

		titleStyle = new GUIStyle(GUI.skin.label);
		titleStyle.normal.background = null;
		titleStyle.normal.textColor = ColText;
		titleStyle.alignment = TextAnchor.MiddleLeft;
		titleStyle.fontStyle = FontStyle.Bold;
		titleStyle.fontSize = 13;
		titleStyle.padding = new RectOffset(0, 0, 0, 0);

		boxStyle = new GUIStyle(GUI.skin.box);
		boxStyle.normal.background = texBox;
		boxStyle.border = new RectOffset(8, 8, 8, 8);
		boxStyle.padding = new RectOffset(6, 6, 6, 6);
		boxStyle.margin = new RectOffset(0, 0, 0, 0);

		labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.normal.textColor = ColText;
		labelStyle.alignment = TextAnchor.MiddleLeft;
		labelStyle.fontSize = 12;

		hintStyle = new GUIStyle(GUI.skin.label);
		hintStyle.normal.textColor = ColTextDim;
		hintStyle.fontSize = 11;
		hintStyle.wordWrap = true;

		thumbStyle = new GUIStyle(GUI.skin.box);
		thumbStyle.normal.background = texThumb;
		thumbStyle.border = new RectOffset(6, 6, 6, 6);
		thumbStyle.alignment = TextAnchor.MiddleCenter;

		buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.normal.background = texButton;
		buttonStyle.hover.background = texButtonHover;
		buttonStyle.active.background = texButtonActive;
		buttonStyle.normal.textColor = ColText;
		buttonStyle.hover.textColor = Color.white;
		buttonStyle.active.textColor = Color.white;
		buttonStyle.alignment = TextAnchor.MiddleCenter;
		buttonStyle.fontSize = 12;
		buttonStyle.border = new RectOffset(8, 8, 8, 8);

		primaryButtonStyle = new GUIStyle(buttonStyle);
		primaryButtonStyle.normal.background = texPrimary;
		primaryButtonStyle.hover.background = texPrimaryHover;
		primaryButtonStyle.active.background = texPrimaryActive;
		primaryButtonStyle.normal.textColor = Color.white;
		primaryButtonStyle.hover.textColor = Color.white;
		primaryButtonStyle.active.textColor = Color.white;

		closeButtonStyle = new GUIStyle(buttonStyle);
		closeButtonStyle.fontSize = 15;
		closeButtonStyle.normal.textColor = ColTextDim;
		closeButtonStyle.hover.textColor = Color.white;
		closeButtonStyle.padding = new RectOffset(0, 0, 0, 0);
		closeButtonStyle.alignment = TextAnchor.MiddleCenter;

		toggleStyle = new GUIStyle(GUI.skin.toggle);
		toggleStyle.normal.textColor = ColText;
		toggleStyle.onNormal.textColor = Color.white;
		toggleStyle.hover.textColor = Color.white;
		toggleStyle.fontSize = 12;

		lineStyle = new GUIStyle(GUI.skin.box);
		lineStyle.normal.background = texLine;
	}

	private static Texture2D MakeBox(Color color)
	{
		Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture2D.SetPixel(0, 0, color);
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.Apply();
		return texture2D;
	}

	/// <summary>生成圆角矩形贴图，供 GUIStyle 九宫格拉伸使用（border 必须等于 radius）。</summary>
	private static Texture2D MakeRoundedBox(int size, int radius, Color fill, Color border, int borderWidth)
	{
		Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Color32[] array = new Color32[size * size];
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num = Coverage(j, i, 0f, size, radius);
				float t = ((borderWidth > 0) ? Coverage(j, i, borderWidth, size, Math.Max(1, radius - borderWidth)) : num);
				Color color = Color.Lerp(border, fill, t);
				color.a *= num;
				array[i * size + j] = color;
			}
		}
		texture2D.SetPixels32(array);
		texture2D.filterMode = FilterMode.Bilinear;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.Apply();
		return texture2D;
	}

	private static float Coverage(int x, int y, float inset, int size, int radius)
	{
		const int Samples = 4;
		int num = 0;
		for (int i = 0; i < Samples; i++)
		{
			for (int j = 0; j < Samples; j++)
			{
				float px = x + (j + 0.5f) / Samples;
				float py = y + (i + 0.5f) / Samples;
				if (InsideRounded(px, py, inset, size, radius))
				{
					num++;
				}
			}
		}
		return (float)num / (float)(Samples * Samples);
	}

	private static bool InsideRounded(float px, float py, float inset, int size, int radius)
	{
		float num = inset;
		float num2 = size - inset;
		if (px < num || px > num2 || py < num || py > num2)
		{
			return false;
		}
		float num3 = Math.Max(0f, radius - inset);
		if (num3 <= 0f)
		{
			return true;
		}
		float num4 = 0f;
		float num5 = 0f;
		if (px < num + num3)
		{
			num4 = num + num3 - px;
		}
		else if (px > num2 - num3)
		{
			num4 = px - (num2 - num3);
		}
		if (py < num + num3)
		{
			num5 = num + num3 - py;
		}
		else if (py > num2 - num3)
		{
			num5 = py - (num2 - num3);
		}
		return num4 * num4 + num5 * num5 <= num3 * num3;
	}

	private void Import(int clothesKind)
	{
		string initialDir = ChaAlphaMask.ExportPath.Value;
		if (!Directory.Exists(initialDir))
		{
			initialDir = BepInEx.Paths.GameRootPath;
		}
		try
		{
			OpenFileDialog.Show(delegate(string[] paths)
			{
				if (paths != null && paths.Length != 0 && !string.IsNullOrEmpty(paths[0]))
				{
					string path = paths[0];
					ThreadingHelper.Instance.StartSyncInvoke(delegate
					{
						ImportFile(clothesKind, path);
					});
				}
			}, "选择 Alpha 遮罩 PNG（推荐 512x512）", initialDir, "PNG 图片 (*.png)|*.png", "png", OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_EXPLORER | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_LONGNAMES);
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError(ex);
		}
	}

	private void ImportFile(int clothesKind, string path)
	{
		try
		{
			byte[] data = File.ReadAllBytes(path);
			if (!IsPng(data))
			{
				ChaAlphaMask.Logger.LogWarning("Not a valid PNG file: " + path);
				return;
			}
			RefreshController();
			if (ctrl == null)
			{
				return;
			}
			ctrl.SetCustomMask(clothesKind, data);
			Texture2D texture = ctrl.GetCustomMaskTexture(clothesKind);
			if (texture != null && (texture.width != 512 || texture.height != 512))
			{
				ChaAlphaMask.Logger.LogMessage($"Mask imported with size {texture.width}x{texture.height}, 512x512 is recommended");
			}
			ChaAlphaMask.Logger.LogInfo("Mask imported for clothes kind " + clothesKind + ": " + path);
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError(ex);
		}
	}

	private void Export(int clothesKind, string clothesName, byte[] png)
	{
		try
		{
			string dir = ChaAlphaMask.ExportPath.Value;
			Directory.CreateDirectory(dir);
			string file = Path.Combine(dir, $"AlphaMask_{clothesKind}_{clothesName}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");
			File.WriteAllBytes(file, png);
			ChaAlphaMask.Logger.LogMessage("Mask exported to " + file);
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogError(ex);
		}
	}

	private static bool IsPng(byte[] data)
	{
		if (data == null || data.Length < 8)
		{
			return false;
		}
		return data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71 && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10;
	}
}
