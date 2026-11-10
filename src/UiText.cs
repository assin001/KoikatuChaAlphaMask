namespace KK_ChaAlphaMask;

/// <summary>
/// 插件界面文本。默认中文；用 -p:EnUi=true 构建时定义 CHA_ALPHA_MASK_EN，切换成英文界面（输出 *_EN.dll）。
/// 两种版本只有界面文本不同，插件 GUID、卡片数据与功能完全一致，安装时二选一。
/// </summary>
internal static class UiText
{
#if CHA_ALPHA_MASK_EN
	// ---------- 捏人界面窗口 ----------

	internal const string WindowTitle = "Character Alpha Mask";

	internal const string CloseTooltip = "Close the window (or use the Control Panel toggle / hotkey)";

	internal const string ShowAll = " Show all";

	internal const string ShowAllTooltip = "Show slots that are not equipped";

	internal const string Delete = "Delete";

	internal const string DeleteTooltip = "Remove the mask from this slot";

	internal const string Export = "Export";

	internal const string ExportTooltip = "Save the current mask as a PNG";

	internal const string Import = "Import";

	internal const string ImportTooltip = "Import a PNG mask (512x512 recommended)";

	internal const string Hint = "Mask: black background, pure red = body parts to hide. 512x512 recommended";

	internal const string ImportDialogTitle = "Select an alpha mask PNG (512x512 recommended)";

	internal const string ImportDialogFilter = "PNG image (*.png)|*.png";

	internal static readonly string[] SlotNames =
	{
		"Bottoms", "Bra", "Shorts", "Gloves", "Pantyhose", "Socks", "Indoor shoes", "Outdoor shoes"
	};

	internal static readonly string[] SlotNamesEmpty =
	{
		"Bottoms (none)", "Bra (none)", "Shorts (none)", "Gloves (none)",
		"Pantyhose (none)", "Socks (none)", "Indoor shoes (none)", "Outdoor shoes (none)"
	};

	// ---------- 捏人界面控件 ----------

	internal const string SidebarToggle = "Alpha Mask Window";

	internal const string BodyMask = "Body mask";

	internal const string BodyMaskBottoms = "Body mask  Bottoms";

	internal const string BodyMaskShorts = "Body mask  Shorts";

	internal const string AlphaMask = "Alpha mask";

	internal const string AlphaMaskBody = "Alpha mask  Body";

	internal const string AlphaMaskOuter = "Alpha mask  Outer";

	internal static readonly string[] SyncKindNames =
	{
		"None", "Top", "Bottoms", "Bra", "Shorts", "Gloves", "Pantyhose", "Socks", "Shoes",
		"Top (inv)", "Bottoms (inv)", "Bra (inv)", "Shorts (inv)", "Gloves (inv)", "Pantyhose (inv)", "Socks (inv)", "Shoes (inv)"
	};

	// ---------- 配置说明 ----------

	internal const string CfgToggleWindowKey = "Hotkey that opens / closes the Character Alpha Mask window inside the maker (default F5)";

	internal const string CfgWindowX = "Initial window X position";

	internal const string CfgWindowY = "Initial window Y position";

	internal const string CfgExportPath = "Folder masks are exported to; also the starting folder of the import dialog (default UserData\\Overlays under the game root)";

	internal const string CfgImportPlusPackMasks = "Read mask data stored in cards by Madevil PlusPack - ChaAlphaMask (madevil.kk.PPCAM)";
#else
	// ---------- 捏人界面窗口 ----------

	internal const string WindowTitle = "角色Alpha遮罩";

	internal const string CloseTooltip = "关闭窗口（也可点右侧控制栏的开关或按快捷键）";

	internal const string ShowAll = " 全部显示";

	internal const string ShowAllTooltip = "显示当前未装备的槽位";

	internal const string Delete = "删除";

	internal const string DeleteTooltip = "移除该槽位的遮罩";

	internal const string Export = "导出";

	internal const string ExportTooltip = "把当前遮罩保存为 PNG";

	internal const string Import = "导入";

	internal const string ImportTooltip = "导入 PNG 遮罩（推荐 512x512）";

	internal const string Hint = "遮罩画法：黑底 + 纯红 = 要遮蔽的身体部位，尺寸推荐 512x512";

	internal const string ImportDialogTitle = "选择 Alpha 遮罩 PNG（推荐 512x512）";

	internal const string ImportDialogFilter = "PNG 图片 (*.png)|*.png";

	internal static readonly string[] SlotNames =
	{
		"下衣", "胸罩", "短裤", "手套", "裤袜", "袜子", "室内鞋", "室外鞋"
	};

	internal static readonly string[] SlotNamesEmpty =
	{
		"下衣（未装备）", "胸罩（未装备）", "短裤（未装备）", "手套（未装备）",
		"裤袜（未装备）", "袜子（未装备）", "室内鞋（未装备）", "室外鞋（未装备）"
	};

	// ---------- 捏人界面控件 ----------

	internal const string SidebarToggle = "Alpha遮罩窗口";

	internal const string BodyMask = "ボディマスク";

	internal const string BodyMaskBottoms = "ボディマスク　ボトムス";

	internal const string BodyMaskShorts = "ボディマスク　ショーツ";

	internal const string AlphaMask = "アルファマスク";

	internal const string AlphaMaskBody = "アルファマスク　胴";

	internal const string AlphaMaskOuter = "アルファマスク　アウター";

	internal static readonly string[] SyncKindNames =
	{
		"無し", "トップス", "ボトムス", "ブラ", "ショーツ", "手袋", "パンスト", "靴下", "靴",
		"トップス(反転)", "ボトムス(反転)", "ブラ(反転)", "ショーツ(反転)", "手袋(反転)", "パンスト(反転)", "靴下(反転)", "靴(反転)"
	};

	// ---------- 配置说明 ----------

	internal const string CfgToggleWindowKey = "捏人界面内打开 / 关闭「角色Alpha遮罩」窗口的快捷键（默认 F5）";

	internal const string CfgWindowX = "窗口初始 X 坐标";

	internal const string CfgWindowY = "窗口初始 Y 坐标";

	internal const string CfgExportPath = "遮罩导出目录，同时也是导入对话框的起始目录（默认游戏根目录下的 UserData\\Overlays）";

	internal const string CfgImportPlusPackMasks = "读取 Madevil PlusPack - ChaAlphaMask（madevil.kk.PPCAM）存在卡片里的遮罩数据";
#endif
}
