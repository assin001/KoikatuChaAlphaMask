<h1 align="center">ChaAlphaMask</h1>

给衣服加「身体遮罩 / Alpha 遮罩」的插件。遮罩有两种来源：zipmod 在 `manifest.xml` 里指定，或者直接在捏人界面把 PNG 导入到卡片里（**不需要写 XML**）。

本仓库是反编译适配后的源码版本，一套源码同时构建 Koikatu 与 KoikatuSunshine 两个插件。

## 功能

- **卡片内遮罩（免 XML）**：捏人界面里打开「角色Alpha遮罩」窗口，按服装槽位导入 / 导出 / 删除 PNG 遮罩，遮罩直接存进角色卡（含各套装）与服装卡，换机器、换人、H 场景都能正常生效
- 捏人界面里每个服装栏新增 `ボディマスク`（身体遮罩）复选开关
- 每个服装栏新增 `Alpha 遮罩` 下拉框，可以把某个部位的遮罩同步到其它部位的服装状态
- 支持 zipmod 通过 `manifest.xml` 提供遮罩贴图：可直接用 PNG，也可从 AssetBundle 中取资源
- 兼容 Madevil `PlusPack - ChaAlphaMask` 存的旧遮罩数据（`madevil.kk.PPCAM`），旧卡片无需重做
- 与 Material Editor / Clothes Overlay（KCOX）联动，遮罩变化会跟随材质与衣服覆盖层更新

## 卡片内遮罩使用

1. 进捏人界面，右侧 Control Panel 底部勾选 **`Alpha遮罩窗口`**（或按 `F5`，快捷键可改）
2. 按服装槽位点 **导入**，选一张 PNG：
   - 尺寸推荐 `512×512`（与身体 UV 对齐），其它尺寸也能用，日志会提示
   - 黑底，用**纯红**画出要遮蔽（隐藏）的身体部位，其余保持黑色
3. 槽位里出现缩略图即生效；**导出** 可把卡片里的遮罩存回 PNG，**删除** 移除该槽位的遮罩
4. 保存角色卡（或服装卡）即可，遮罩跟着卡片走

要点：

- 遮罩按「套装（坐标）× 服装槽位」保存：只对当前套装生效，其它套装需要在对应套装里分别导入
- 窗口只对已装备的槽位显示行，勾选 **全部显示** 可以看到空槽位并预先导入
- 窗口开关同时支持：右侧 Control Panel 复选框、`F5` 快捷键、窗口右上角 `×`
- 鼠标压在窗口上时插件会临时屏蔽游戏对鼠标的读取，所以拖动窗口不会带动人物 / 相机，也不会误触窗口后面的界面

支持的槽位：下衣、胸罩、短裤、手套、裤袜、袜子、室内鞋、室外鞋（上衣槽位没有遮罩，与原插件一致）。

## 配置

`BepInEx/config/nakay.kk.ChaAlphaMask.cfg`：

| 段 | 键 | 默认值 | 说明 |
|---|---|---|---|
| Maker | `Toggle Mask Window` | `F5` | 开关窗口的快捷键 |
| Maker | `Window X` / `Window Y` | `560` / `100` | 窗口位置，拖动后会自动记住 |
| General | `Export Path` | `UserData\Overlays` | 导出遮罩的目录，也是导入对话框的起始目录 |
| General | `Import PlusPack Mask Data` | `true` | 读取 Madevil PlusPack 存在卡片里的遮罩数据 |

## 使用教程

**独立遮罩完整图文教程（绘制遮罩 → 放进 unity3d → 填 manifest → 游戏内验证）：**

**本地文件：[docs/独立遮罩使用方法.md](docs/独立遮罩使用方法.md)**

## 目录结构

```
src/                                       共享源码
  ChaAlphaMask.cs                          插件入口 / 配置
  AlphaMaskController.cs                   角色行为 / 卡片数据存取 / 遮罩应用
  MaskWindow.cs                            捏人界面的「角色Alpha遮罩」窗口（导入/导出/删除）
  InputBlock.cs                            鼠标输入屏蔽（拖窗口不影响人物与相机）
  MakerGui.cs                              捏人界面控件
  MaskProperty.cs                          zipmod manifest 解析
  MultiplyShader.cs                        遮罩相乘着色器属性 ID
  Patch.cs                                 Harmony 补丁
  Util.cs                                  工具方法
  KK_ChaAlphaMask.resources.alphamaskmultiply   内嵌遮罩相乘 AssetBundle
KK_ChaAlphaMask/KK_ChaAlphaMask.csproj      Koikatu 版（net35）
KKS_ChaAlphaMask/KKS_ChaAlphaMask.csproj    KoikatuSunshine 版（net46）
docs/独立遮罩使用方法.md                    独立遮罩图文教程
docs/images/                               教程配图
```

两个工程都通过 `..\src\*.cs` 共享同一份源码，只是引用的游戏程序集与目标框架不同，改代码只需改 `src/`

## 编译

需要 .NET SDK（或 Visual Studio 2022 + .NET Framework 目标包）：

```powershell
dotnet build KK_ChaAlphaMask\KK_ChaAlphaMask.csproj -c Release -p:GameDir="E:\game\GDAkk\Koikatu"
dotnet build KKS_ChaAlphaMask\KKS_ChaAlphaMask.csproj -c Release -p:GameDir="E:\game\GDAkk\KoikatsuSunshine"
```

`-p:GameDir=` 指向游戏根目录（含 `Koikatu_Data\Managed` 与 `BepInEx`），不传则用 csproj 里的默认路径。

## 依赖

- BepInEx 5
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI)（KKAPI / KKSAPI）
- [Material Editor](https://github.com/IllusionMods/IllusionMaterialEditor)
- [Overlay Mods](https://github.com/ManlyMarco/Illusion-Overlay-Mods)（KCOX）
- Sideloader、ExtensibleSaveFormat（BepisPlugins）
- Force High Poly（可选，装了才会自动处理高模；插件用反射读取，没装也能正常运行）

## zipmod 遮罩配置

在 zipmod 的 `manifest.xml` 里加一段 `ChaAlphaMask`，每个 `<mask>` 对应一件衣服：

```xml
<manifest schema-ver="1">
  <guid>你的mod的guid</guid>
  <name>Some Mod</name>
  <version>1.0</version>

  <ChaAlphaMask>
    <mask>
      <category>109</category>            <!-- 衣服种类，见下表 -->
      <id>58809</id>                      <!-- 衣服在 csv 里的 ID -->
      <abPath>chara/xxx.unity3d</abPath>  <!-- 遮罩贴图所在 bundle（相对 abdata） -->
      <abBodyMaskName>alpnamark_gloves</abBodyMaskName>  <!-- bundle 内贴图资源名 -->
    </mask>

    <mask>
      <category>109</category>
      <id>58811</id>
      <pngBodyMaskPath>chara/xxx/mask.png</pngBodyMaskPath> <!-- 也可以直接用 PNG -->
    </mask>
  </ChaAlphaMask>
</manifest>
```

| 字段 | 说明 |
|---|---|
| `category` | 服装种类编号（如 109 = 手套），用于匹配衣服 |
| `id` | 衣服 ID，Sideloader 会自动换算成本地槽位 |
| `abPath` | 遮罩贴图所在 AssetBundle，相对 `abdata/` |
| `abBodyMaskName` | bundle 内的贴图资源名 |
| `pngBodyMaskPath` | 直接用 PNG 时的路径，相对 `abdata/` |
| `botMask` | 标记该遮罩用于下半身（ボトムス / ショーツ） |
| `objOpt01` / `objOpt02` | 与衣服的 Option 开关绑定，随开关状态启用遮罩 |
| `guid` | 一般省略，默认取 manifest 的 `<guid>` |

`abPath` 与 `pngBodyMaskPath` 二选一即可，都不是必需项：不配就只是没有遮罩贴图。

完整的图文流程（绘制遮罩 → 放进 unity3d → 填 manifest → 游戏内验证）见 [独立遮罩使用方法](docs/独立遮罩使用方法.md)。

## 卡片数据

遮罩存在 ExtensibleSaveFormat 的扩展数据里，键名 `CustomMasks`（`List<CustomMask>`，每项含 `coordinateType` / `clothesKind` / `png` 原始字节）：

- 角色卡：保存全部套装的遮罩
- 服装卡：只保存当前套装的遮罩
- 与 zipmod manifest 遮罩同时存在时，**卡片里的遮罩优先**，方便临时修改别人做的衣服而不动 mod 本身

## 与 KoikatuSunshine 的差异

- 插件 GUID 保持 `nakay.kk.ChaAlphaMask` 不变，这样 KK 卡片里已存的遮罩设置能在 KKS 直接读取
- KK 的 `MaskProperty` 用 `XmlSerializer` 反序列化，属性 setter 需为 public；旧 Mono 能容忍 private setter，Unity 2019 的 Mono 会直接抛异常
- KKS 的服装界面没有 `tglInnerShoes` 子分类（内履き已并入皮鞋页），因此内履き槽位没有身体遮罩开关，其余 8 个槽位正常
- `CvsClothes.UpdateSelectClothes`、`ChaControl.SetClothesState` 等 API 签名在 KKS 中多了参数，Harmony 按参数名绑定，补丁逻辑无需改动

## 更新日志

### 1.1.1

- 修复窗口里的遮罩缩略图把整张不透明黑底画成白底的问题（只用 RGB 亮度算可见度，黑底正确透明）
- 导出目录默认改为游戏根目录下的 `UserData\Overlays`，旧配置里的 `UserData\AlphaMask` 会自动迁移

### 1.1

- 新增捏人界面「角色Alpha遮罩」窗口：按服装槽位导入 / 导出 / 删除存在卡片里的 PNG 遮罩，不再需要写 manifest
- 新增右侧 Control Panel 的窗口开关、`F5` 快捷键、窗口位置记忆
- 窗口打开时屏蔽鼠标输入，拖动窗口不会带动人物 / 相机，也不会点穿到后面的界面
- 支持读取 Madevil `PlusPack - ChaAlphaMask` 的旧卡片遮罩数据（`madevil.kk.PPCAM`）
- Force High Poly 改为反射读取，没装该可选插件也能编译运行

### 1.0

- 首次发布：zipmod manifest 遮罩、捏人界面身体遮罩开关与遮罩同步

## 说明

本仓库源码由原插件反编译整理而来，仅作维护与移植用途。原始版权归原作者所有。
