using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KK_ChaAlphaMask;

/// <summary>
/// 窗口打开且鼠标压在窗口上（或正在拖标题栏）时，整体屏蔽游戏对鼠标的读取。
/// 只挡相机或 uGUI 射线都不够：maker 里拖视角 / 拖人物是直接轮询 Input.mousePosition、Input.GetMouseButton、
/// Input.GetAxis 算位移的，所以这里在 UnityEngine.Input 层把鼠标相关接口一起冻结；
/// 顺带 uGUI 的 StandaloneInputModule 也读不到点击，不会误触窗口后面的界面。
/// </summary>
internal static class InputBlock
{
	public static bool Visible;

	public static bool Dragging;

	/// <summary>窗口矩形（IMGUI 屏幕坐标，Y 轴朝下）。</summary>
	public static Rect WindowRect;

	/// <summary>真实鼠标位置（屏幕坐标，Y 轴朝上，和 Input.mousePosition 同一套坐标）。</summary>
	public static Vector3 RawMouse;

	private static Vector3 frozenMouse;

	private static bool hasFrozen;

	public static bool Blocking
	{
		get
		{
			if (!Visible)
			{
				return false;
			}
			if (Dragging)
			{
				return true;
			}
			return WindowRect.Contains(new Vector2(RawMouse.x, (float)Screen.height - RawMouse.y));
		}
	}

	public static void Clear()
	{
		Visible = false;
		Dragging = false;
		hasFrozen = false;
	}

	/// <summary>返回 true 表示已经改写结果、跳过原方法。</summary>
	private static bool FreezeMouse(ref Vector3 result)
	{
		if (!Blocking)
		{
			hasFrozen = false;
			return false;
		}
		if (!hasFrozen)
		{
			frozenMouse = RawMouse;
			hasFrozen = true;
		}
		result = frozenMouse;
		return true;
	}

	internal static void Patch(Harmony harmony)
	{
		try
		{
			Type inputType = typeof(Input);
			MethodInfo prefixMousePosition = AccessTools.Method(typeof(InputBlock), "PrefixMousePosition");
			MethodInfo postfixMousePosition = AccessTools.Method(typeof(InputBlock), "PostfixMousePosition");
			harmony.Patch(AccessTools.Method(inputType, "get_mousePosition"), prefix: new HarmonyMethod(prefixMousePosition), postfix: new HarmonyMethod(postfixMousePosition));
			int num = 1;
			num += PatchSimple(harmony, inputType, "get_mouseScrollDelta", "PrefixVector2");
			num += PatchSimple(harmony, inputType, "GetMouseButton", "PrefixBool");
			num += PatchSimple(harmony, inputType, "GetMouseButtonDown", "PrefixBool");
			num += PatchSimple(harmony, inputType, "GetMouseButtonUp", "PrefixBool");
			num += PatchSimple(harmony, inputType, "GetAxis", "PrefixFloat");
			num += PatchSimple(harmony, inputType, "GetAxisRaw", "PrefixFloat");
			ChaAlphaMask.Logger.LogDebug($"Mouse input blocking patched ({num} hooks)");
		}
		catch (Exception ex)
		{
			ChaAlphaMask.Logger.LogWarning("Failed to patch mouse input: " + ex);
		}
	}

	private static int PatchSimple(Harmony harmony, Type inputType, string methodName, string prefixName)
	{
		MethodInfo methodInfo = AccessTools.Method(inputType, methodName);
		MethodInfo methodInfo2 = AccessTools.Method(typeof(InputBlock), prefixName);
		if (methodInfo == null || methodInfo2 == null)
		{
			ChaAlphaMask.Logger.LogWarning("Mouse input method not found: " + methodName);
			return 0;
		}
		harmony.Patch(methodInfo, prefix: new HarmonyMethod(methodInfo2));
		return 1;
	}

	private static bool PrefixMousePosition(ref Vector3 __result)
	{
		return !FreezeMouse(ref __result);
	}

	private static void PostfixMousePosition(ref Vector3 __result)
	{
		if (!Blocking)
		{
			RawMouse = __result;
		}
	}

	private static bool PrefixVector2(ref Vector2 __result)
	{
		if (!Blocking)
		{
			return true;
		}
		__result = Vector2.zero;
		return false;
	}

	private static bool PrefixBool(ref bool __result)
	{
		if (!Blocking)
		{
			return true;
		}
		__result = false;
		return false;
	}

	private static bool PrefixFloat(ref float __result)
	{
		if (!Blocking)
		{
			return true;
		}
		__result = 0f;
		return false;
	}
}
