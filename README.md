# ChaAlphaMask（Koikatu / KoikatuSunshine）

给衣服加「身体遮罩 / Alpha 遮罩」的插件，衣服可以通过 zipmod 的 `manifest.xml` 指定自己的遮罩贴图，用于让同一件衣服的不同部位（或不同状态）显示不同的透明区域。

本仓库是反编译 + 双版本适配后的源码版本，一套源码同时构建 Koikatu 与 KoikatuSunshine 两个插件。

## 功能

- 捏人界面里每个服装栏新增 `ボディマスク`（身体遮罩）复选开关
- 每个服装栏新增 `Alpha 遮罩` 下拉框，可以把某个部位的遮罩同步到其它部位的服装状态
- 支持 zipmod 通过 `manifest.xml` 提供遮罩贴图：可直接用 PNG，也可从 AssetBundle 中取资源
- 与 Material Editor / Clothes Overlay（KCOX）联动，遮罩变化会跟随材质与衣服覆盖层更新

## 目录结构

```
src/                                       共享源码
  ChaAlphaMask.cs                          插件入口
  AlphaMaskController.cs                   角色行为 / 卡片数据存取
  MakerGui.cs                              捏人界面控件
  MaskProperty.cs                          zipmod manifest 解析
  MultiplyShader.cs                        遮罩相乘着色器属性 ID
  Patch.cs                                 Harmony 补丁
  Util.cs                                  工具方法
  KK_ChaAlphaMask.resources.alphamaskmultiply   内嵌遮罩相乘 AssetBundle
KK_ChaAlphaMask/KK_ChaAlphaMask.csproj      Koikatu 版（net35）
KKS_ChaAlphaMask/KKS_ChaAlphaMask.csproj    KoikatuSunshine 版（net46）
```

两个工程都通过 `..\src\*.cs` 共享同一份源码，只是引用的游戏程序集与目标框架不同，改代码只需改 `src/`。

## 编译

需要 .NET SDK（本项目使用 SDK 8 验证过）。游戏目录默认值写在 csproj 里，可通过 `GameDir` 覆盖：

```bash
# Koikatu
dotnet build KK_ChaAlphaMask/KK_ChaAlphaMask.csproj -c Release

# KoikatuSunshine
dotnet build KKS_ChaAlphaMask/KKS_ChaAlphaMask.csproj -c Release

# 游戏装在别处时
dotnet build KKS_ChaAlphaMask/KKS_ChaAlphaMask.csproj -c Release -p:GameDir="D:\KoikatuSunshine"
```

输出：

| 工程 | 产物 | 放置位置 |
|---|---|---|
| `KK_ChaAlphaMask` | `KK_ChaAlphaMask.dll` | `Koikatu\BepInEx\plugins\` |
| `KKS_ChaAlphaMask` | `KKS_ChaAlphaMask.dll` | `KoikatuSunshine\BepInEx\plugins\` |

两个 DLL 不能互换：KK 版是 net35 + Unity 5.6 程序集，KKS 版是 net46 + Unity 2019 程序集。

## 依赖

- BepInEx 5
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI)（KKAPI / KKSAPI）
- [Material Editor](https://github.com/IllusionMods/IllusionMaterialEditor)
- [Overlay Mods](https://github.com/ManlyMarco/Illusion-Overlay-Mods)（KCOX）
- Sideloader、ExtensibleSaveFormat（BepisPlugins）
- Force High Poly（可选，装了才会自动处理高模）

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

## 与 KoikatuSunshine 的差异

- 插件 GUID 保持 `nakay.kk.ChaAlphaMask` 不变，这样 KK 卡片里已存的遮罩设置能在 KKS 直接读取
- KK 的 `MaskProperty` 用 `XmlSerializer` 反序列化，属性 setter 需为 public；旧 Mono 能容忍 private setter，Unity 2019 的 Mono 会直接抛异常
- KKS 的服装界面没有 `tglInnerShoes` 子分类（内履き已并入皮鞋页），因此内履き槽位没有身体遮罩开关，其余 8 个槽位正常
- `CvsClothes.UpdateSelectClothes`、`ChaControl.SetClothesState` 等 API 签名在 KKS 中多了参数，Harmony 按参数名绑定，补丁逻辑无需改动

## 说明

本仓库源码由原插件（作者 nakay）反编译整理而来，仅作维护与移植用途。原始版权归原作者所有。
