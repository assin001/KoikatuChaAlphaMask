using System;
using UnityEngine;

namespace KK_ChaAlphaMask;

internal static class MultiplyShader
{
	public static int[] MaskTex { get; private set; } = new int[8];

	public static int[] MaskState { get; private set; } = new int[8];

	internal static void Init()
	{
		for (int i = 0; i < MaskTex.Length; i++)
		{
			MaskTex[i] = Shader.PropertyToID("_MaskTex0" + i);
			MaskState[i] = Shader.PropertyToID("_MaskState0" + i);
		}
	}

	public static Color GetState(byte state)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return state switch
		{
			0 => Color.red, 
			1 => Color.green, 
			2 => Color.blue, 
			3 => Color.black, 
			_ => throw new ArgumentOutOfRangeException("GetState(byte)", state, null), 
		};
	}
}
