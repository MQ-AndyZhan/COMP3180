# 视觉小说研究原型：Unity 使用与搭建指南

对应说明书：`视觉小说功能设计说明书_最小研究版_v1.2.docx`（2026-10-30）
Unity 版本：`6000.3.17f1`　Ink Unity Integration：1.1.8　Input System：1.19.0
内容语言：**英文**（正文、界面文案、问卷与指导语）

---

## 0. 当前工程已经做了什么

### 已删除的旧原型文件

| 文件 | 原因 |
|---|---|
| `Assets/Scripts/DialogueManager.cs` | 旧打字机对话控制器，与说明书的即时显示和状态机冲突 |
| `Assets/Scripts/GenerateDialogues.cs` | 逐字打字机效果，说明书明确禁止 |
| `Assets/Scripts/MainMenu.cs` | 旧主菜单，改用唯一的 StudyScene / MenuPanel |
| `Assets/Scripts/PlayerInput.cs` + `.inputactions` | 旧暂停菜单输入，说明书不提供暂停/返回按钮 |
| `Assets/Scenes/MainMenu.unity`、`Game.unity` | 旧的三个版本方案，改为单一 StudyScene |
| `Assets/Prefabs/Dialogue Choices.prefab` | 旧运行时 Instantiate 选项按钮，改为五个固定槽位 |
| `Assets/Dialogues/Test.ink`、`Test.json` | 旧测试剧本，标签格式与说明书不一致 |

Ink 插件本体（`Assets/Ink/...`）和 TMP 资源未删除，它们仍然需要。`Assets/Ink/Demos` 只是插件自带示例，不影响 StudyScene，可以保留作语法参考，也可以整目录删除。

### 新脚本

| 脚本 | 职责 |
|---|---|
| `Assets/Scripts/Study/StudyController.cs` | 唯一控制器：状态机、Story 生命周期、tag 解析、选项校验、结局确认、错误页、文字行数检查 |
| `Assets/Scripts/Study/SingleClickButton.cs` | 所有按钮的轻量输入封装：只转发左键单次点击，过滤双击后续点击；同时负责 Normal / Highlighted / Pressed / Disabled 四态颜色与文字描边 |
| `Assets/Scripts/Study/StudyConfig.cs` | `VersionConfig` 可序列化配置；`SpeakerCatalog` 姓名映射 |
| `Assets/Scripts/Study/StudyLineMeta.cs` | `speaker` / `line` / `choice_point` / `end` 标签解析与格式校验 |
| `Assets/Scripts/Study/DisplaySetup.cs` | 构建版启动时设置 1920×1080；启动两帧后断言客户区与 Canvas `scaleFactor` |
| `Assets/Scripts/Study/Editor/StudySceneBuilder.cs` | 编辑器菜单，一键生成完整 StudyScene、绑定引用、写入 Build Settings 与 Player Settings |
| `Assets/Scripts/Study/Editor/StudyFontLocator.cs` | 生成器与构建检查共用的字体查找：优先 StudySans，其次 LiberationSans/Roboto/Inter 等拉丁字体 |
| `Assets/Scripts/Study/Editor/FontCoverageCheck.cs` | 构建前读取 `story_*.json` 的全部非空白字符，按 Dynamic/Static 两种字体模式检查缺字；附带导出 `study_charset.txt` 的菜单 |

> 本次按你的选择**没有生成** `story_A/B/C.ink`。剧本内容属于你的研究材料，需要你按第 4 节的格式用英文编写。
>
> 项目已切换为英文内容：菜单、`Continue`、完成页、错误页等玩家可见文案已改为英文；Ink 正文用英文写，tag 的 key/value 保持不变。

---

## 1. 一次性准备（顺序很重要）

### 1.1 英文字体资产（先确定）

英文内容**不需要 Noto Sans SC**，也不需要预生成 600–1000 个汉字。有两种做法：

**最快：直接用 TMP 自带的 LiberationSans SDF**（`Assets/TextMesh Pro/Resources/Fonts & Materials/`）。场景生成器和构建检查共用的 `StudyFontLocator` 会优先找名字含 `StudySans` 的字体，找不到就回退到第一个非 Fallback 的 TMP 字体资产；不换字体也能直接跑。

**想换正式一点的拉丁字体（Roboto / Inter / Open Sans 等）：**

1. 把 TTF 放到 `Assets/Art/Font/`。
2. 打开 `Window > TextMeshPro > Font Asset Creator`：

| 参数 | 值 |
|---|---|
| Source Font File | 你的 Latin TTF |
| Sampling Point Size | 64 |
| Padding | 6 |
| Atlas Resolution | 1024 × 1024（ASCII 足够；2048 也可以） |
| Character Set | ASCII + 常用标点（或第 1.4 步导出的 `study_charset.txt`） |
| Render Mode | SDFAA |
| Atlas Population Mode | **Dynamic 或 Static 都可以**（构建检查两种都支持） |
| Multi Atlas Textures | 选 Dynamic 时建议开 |

3. 保存为：

```
Assets/Art/Font/StudySans SDF.asset
```

文件名包含 `StudySans` 会被优先选中；没有 StudySans 时，LiberationSans / Roboto / Inter 等拉丁字体也会被识别。

4. 生成后**冻结参数**：三版共用这一个字体资产；不要配置其他字族 fallback；TMP 文本的 Auto Size 必须关闭（控制器已强制关闭）。
5. 正文 36 号、选项 28 号、姓名 32 号；行距/字距/段距必须为 0。正文框 192 高要放 4 行，选项框 68 高要放 2 行；选 line height ≤1.2 em 左右的拉丁字体，实际以运行时 `Fits` 检查为准。若行高太大，换字体或缩短文案，不要缩字号。

> 纯英文不需要预生成中文字符集。构建时的 `FontCoverageCheck` 会检查剧本 JSON 里的全部非空白字符：Dynamic 字体自动补图集，Static 字体用 `HasCharacters` 检查；缺字会直接阻断构建。默认 LiberationSans 若缺少弯引号/长破折号，改用直引号/短横线，或做一个包含这些标点的自定义字体资产。
>
> 英文菜单说明和完成页说明比中文长。`InstructionText` 和 `CompleteInstructionText` 的框是 960×120，生成场景后要实际看一眼；超出时缩短文案，不要改字号或布局。

### 1.2 Ink 剧本

在 `Assets/Dialogues/` 建立三个独立主文件：

```
Assets/Dialogues/story_A.ink   → 编译为 story_A.json
Assets/Dialogues/story_B.ink   → 编译为 story_B.json
Assets/Dialogues/story_C.ink   → 编译为 story_C.json
```

Ink Unity Integration 会在保存 `.ink` 时自动编译出同名 `.json`。确认 Project 窗口里能看到三个 JSON，并且类型是 `TextAsset`。**不要手工编辑编译后的 JSON**。如果保存后没有出现 JSON，用 Ink 的 Player 窗口或右键 `.ink` 的 Recompile 菜单手动编译一次。

三版必须共享：相同角色、相同公共正文、相同四个决策点位置、相同共同结局。只有每个决策点的选项数量不同：A=2、B=3、C=5。

正文全部用英文；tag 的 key 和值（`speaker:player` / `friend` / `narrator`、`line`、`choice_point`、`end:common`）**不要翻译**，只把 tag 后面的正文写成英文。

### 1.3 美术素材（可以先占位）

| 对象 | 建议尺寸 | 说明 |
|---|---|---|
| `BackgroundImage` | 1920 × 1080 | 一张固定背景，三版完全相同 |
| `PlayerPortrait` | 600 × 920 | 玩家角色固定立绘，不移动、不换表情 |
| `FriendPortrait` | 600 × 920 | 另一角色固定立绘，不移动、不换表情 |

场景生成器会放纯色占位块。把正式 Sprite 拖到对应 Image 的 `Source Image` 即可；保持 `Preserve Aspect = true`，`Raycast Target = false`。

### 1.4 导出剧本字符集（可选）

纯英文通常用默认字体的 ASCII 覆盖就够了，可以跳过这一步。如果你换了自定义字体，或想确保弯引号、长破折号、省略号等标点也预生成，三个 JSON 编译完成后执行：

```
Tools > Visual Novel Study > Export Story Charset (study_charset.txt)
```

会在 `Assets/Art/Font/study_charset.txt` 写出全部非空白字符（去重排序；不再只筛非 ASCII）。在 Font Asset Creator 的 `Characters from File` 里选这个文件即可。

构建时 `FontCoverageCheck` 还会再检查一次：Dynamic 字体会自动把缺字写入图集，Static 字体会用 `HasCharacters` 检查；任何缺字都会让构建直接失败，并列出缺字。正式测试前必须做到“缺字 0”。

---

## 2. 一键生成 StudyScene

字体资产（默认 LiberationSans SDF 即可）和三个 JSON 就位后：

```
Tools > Visual Novel Study > Build StudyScene
```

该命令会：

1. 新建并保存 `Assets/Scenes/StudyScene.unity`（若已存在会先确认是否覆盖）。
2. 生成完整 Hierarchy、固定坐标、按钮四态颜色、五个选项槽位。
3. 自动绑定 `studyFont` 和 `story_A/B/C.json`（前提是文件名匹配）。
4. 把 Build Settings 设为**只有** StudyScene 一个场景。
5. 写入 Player Settings：1920×1080、Windowed、不可调整窗口、禁止 Alt+Enter、Run In Background 开。

生成后打开场景，选中 `StudyController`，检查 Inspector：

| 字段 | 应为 |
|---|---|
| `Version Configs` | 3 项，label=A/B/C，expectedChoiceCount=2/3/5，inkJson 分别指向三个 JSON |
| `Menu Panel` / `Game Panel` / `Complete Panel` / `Error Panel` | 四个面板 |
| `Version Buttons` / `Version Button Labels` | 各 3 项 |
| `Speaker Name Text` / `Dialogue Text` / `Continue Button` | DialoguePanel 内的三个对象 |
| `Canvas Scaler` | Canvas 上的 CanvasScaler |
| `Choices Panel` | ChoicesPanel |
| `Choice Slots` / `Choice Labels` | 各 5 项 |
| `Complete Title Text` / `Complete Instruction Text` / `Complete Return Button` | CompletePanel 内 |
| `Error Message Text` / `Error Return Button` | ErrorPanel 内 |
| `Study Font` | `LiberationSans SDF`（默认）或你生成的拉丁字体 `StudySans SDF` |
| `Player Display Name` / `Friend Display Name` | `You` / `Classmate`（可改成实际角色名） |
| `Expected Decision Point Count` | 正式构建固定 `4` |

如果生成器提示没有找到字体或 JSON，手动把资产拖到对应字段即可。**三个 JSON 必须全部绑定**，否则启动任何一版都会进入错误页（这是说明书“确认三个配置没有误绑/缺失”的要求）。

> `StudyFontLocator` 会优先找名字含 `StudySans` 的字体，其次 LiberationSans / Roboto / Inter 等，最后回退到第一个非 Fallback 的 TMP 字体资产。生成器和构建检查共用同一个 locator；如果你手动换字体，请把它命名为 `StudySans SDF` 或修改 `StudyFontLocator`，确保控制器、生成器和构建检查指向同一个资产。

---

## 3. Hierarchy 与布局

### 3.1 对象树

```
StudyScene
├── Main Camera                 Camera + AudioListener + URP Camera Data（仅用于清屏，UI 是 Overlay）
├── StudyController             StudyController（唯一控制器）
├── EventSystem                 EventSystem + InputSystemUIInputModule（全场景唯一 UI 输入模块）
└── Canvas                      Canvas (Screen Space Overlay) + CanvasScaler + GraphicRaycaster
    ├── MenuPanel               全屏纯色底板
    │   ├── TitleText           “Reading Study”
    │   ├── InstructionText     “Please choose the version in the order given by the researcher. Click Continue to read; click one option when a response appears.”
    │   ├── VersionButton_A
    │   ├── VersionButton_B
    │   └── VersionButton_C
    ├── GamePanel               透明容器（不显示底板）
    │   ├── BackgroundImage     全屏
    │   ├── PlayerPortrait
    │   ├── FriendPortrait
    │   ├── DialoguePanel       #151A24，alpha 235/255
    │   │   ├── SpeakerNameText
    │   │   ├── DialogueText
    │   │   └── ContinueButton
    │   └── ChoicesPanel        无 Image、无底板、不接收射线；仅布局容器
    │       ├── ChoiceButton_1
    │       ├── ChoiceButton_2
    │       ├── ChoiceButton_3
    │       ├── ChoiceButton_4
    │       └── ChoiceButton_5
    ├── CompletePanel
    │   ├── CompleteTitleText
    │   ├── CompleteInstructionText
    │   └── ReturnButton
    └── ErrorPanel
        ├── ErrorMessageText
        └── ReturnButton
```

层级顺序固定为：`BackgroundImage → PlayerPortrait → FriendPortrait → DialoguePanel → ChoicesPanel`。不要为了让 A/B 的空位“好看”而加底板、空槽位或暗化遮罩；B.1 明确把它作为研究限制处理。

### 3.2 坐标表（1920×1080，左上角原点，y 向下）

| 对象 | x | y | 宽 | 高 | 说明 |
|---|---:|---:|---:|---:|---|
| BackgroundImage | 0 | 0 | 1920 | 1080 | 全屏 |
| PlayerPortrait | 120 | 96 | 600 | 920 | Preserve Aspect |
| FriendPortrait | 1200 | 96 | 600 | 920 | Preserve Aspect |
| ChoicesPanel | 780 | 72 | 1044 | 504 | 无 Image |
| DialoguePanel | 96 | 600 | 1728 | 384 | 半透明底板 |
| SpeakerNameText | 144 | 624 | 1200 | 48 | 32 号 |
| DialogueText | 144 | 686 | 1632 | 192 | 36 号，左上对齐 |
| ContinueButton | 1480 | 912 | 296 | 56 | 28 号，`Continue` |
| ChoiceButton 1–5 | 798 | 90 / 186 / 282 / 378 / 474 | 1008 | 84 | 垂直间距 12，槽位内边距 18 |
| Menu 标题 | 480 | 260 | 960 | 80 | 48 号 |
| Menu 说明 | 480 | 370 | 960 | 120 | 28 号 |
| Version 按钮 | 540 / 840 / 1140 | 570 | 240 | 88 | A/B/C |
| Complete 标题 | 480 | 300 | 960 | 80 | 44 号 |
| Complete 说明 | 480 | 420 | 960 | 120 | 28 号 |
| Complete 返回 | 720 | 720 | 480 | 80 | 28 号 |
| Error 文案 | 360 | 360 | 1200 | 240 | 28 号，居中 |
| Error 返回 | 720 | 720 | 480 | 80 | 与 Complete 返回同一位置 |

ChoiceButton 宽 1008 + 左右内边距 18×2 = 1044（ChoicesPanel 宽）；高 84×5 + 间距 12×4 + 上下 18×2 = 504。A 只启用前 2 个（y 90–270），B 前 3 个（到 366），C 全部 5 个（到 558）。未启用槽位 `SetActive(false)`。

### 3.3 按钮的四态颜色

所有按钮由 `Image + SingleClickButton + TMP_Text 子对象` 组成（**没有** `UnityEngine.UI.Button`）。这是为了从结构上杜绝键盘 Submit 成为第二条操作路径，同时完全按说明书控制四种状态：

| 状态 | 底色 | 文字 | 描边 |
|---|---|---|---|
| Normal | `#273142` | 白 | `#778399` |
| Highlighted | `#3B4A63` | 白 | `#B5C6E2` |
| Pressed | `#1D2634` | 白 | `#D1D9E6` |
| Disabled | `#20252D` | `#9BA4B2` | 无（宽度 0） |

只有按钮 Image 的 `Raycast Target = true`；背景、立绘、DialoguePanel、所有 TMP 文字都是 `false`。因此点击正文、背景、立绘、空白处不会推进剧情。

---

## 4. Ink 写作规范（最容易出错的部分）

### 4.1 每行 tag 契约

可见正文同一行末尾写 tag，多个 tag 用新的 `#` 分隔；key 与枚举值小写；key 与 value 之间用英文冒号，解析时在**第一个**冒号处分割并 Trim。

| tag | 必须出现的位置 | 允许值 |
|---|---|---|
| `speaker` | **每个可见单元** | `player` / `friend` / `narrator` |
| `line` | **每个可见单元** | 例：`intro_01`、`cp01_o1_reply`；一个版本内唯一 |
| `choice_point` | 四个提问单元 | `cp01` ~ `cp04`，按顺序 |
| `end` | 共同结局最后单元 | `common` |

硬规则：

- 同一单元不得有两个 `speaker` / `line` / `choice_point` / `end`。
- `choice_point` 与 `end` 不得共存。
- 普通单元不得依赖上一个单元的 tag；控制器每次 `Continue()` 后立即复制 `currentTags`，缺 tag 直接进错误页。
- 不支持 `emotion` / `scene` / `bgm` / 动画 tag。控制器对未知 tag **直接报错**（防止拼写错误静默通过）。
- 旁白也必须写 `# speaker:narrator`，姓名栏留空并隐藏，但正文位置不移动。

### 4.2 一个决策点的正确写法

```ink
-> start

=== start ===
After class, you and your classmate stay in the room. # speaker:narrator # line:intro_01
Where should we start? # speaker:friend # line:q01 # choice_point:cp01
* [I want to hear your thoughts first.] -> cp01_o1
* [We can list the questions together.] -> cp01_o2

=== cp01_o1 ===
I want to hear your thoughts first. # speaker:player # line:cp01_o1_selected
Okay. Let me start with what worries me most. # speaker:friend # line:cp01_o1_reply
-> cp01_after

=== cp01_o2 ===
We can list the questions together. # speaker:player # line:cp01_o2_selected
Good. That will help me organize my thoughts too. # speaker:friend # line:cp01_o2_reply
-> cp01_after

=== cp01_after ===
You continue sorting the notes on the desk. # speaker:narrator # line:shared_cp01
-> next_question
```

关键点：

1. **选项用方括号写法** `* [显示文字] -> 目标`。方括号会阻止 Ink 自动把选项文字再输出成正文。
2. 分支开头**显式写玩家台词**，带 `# speaker:player`。控制器不会自己拼接答案；如果 Ink 不输出，正文就没有玩家回答。
3. 每个选项 1 句玩家回答 + 1–2 句角色回应，然后全部 divert 到**同一个**公共 knot。回应篇幅、信息量、情绪强度要相近。
4. 提问行与第一个 `*` 选项之间**不能再有非空正文**。控制器在读到 `choice_point` 后会一直消费空输出，遇到非空正文直接判为剧本格式错误。
5. A、B、C 三个文件在同一决策点的共享选项文字、顺序、回应必须逐字一致；B 在 A 的前两项后加第 3 项，C 在 B 的前三项后加第 4、5 项。A 文件只写 2 个选项，B 写 3 个，C 写 5 个。
6. `line` 在一个版本内唯一；共享正文和共享分支在三版使用相同 ID，便于逐字核对。
7. 结局最后一句必须带 `# end:common`，并且之前已经完成 cp01–cp04。`end:common` 之后只能有 `-> END` 之类的纯控制输出，不能还有正文或选项。

### 4.3 四个决策点与结束

每版结构：

```
intro 若干单元
cp01 提问 + 2/3/5 个选项 → 各自局部反馈 → cp01_after
cp02 提问 + 2/3/5 个选项 → 各自局部反馈 → cp02_after
cp03 提问 + 2/3/5 个选项 → 各自局部反馈 → cp03_after
cp04 提问 + 2/3/5 个选项 → 各自局部反馈 → cp04_after
公共收尾若干单元
最后一句 # end:common
-> END
```

每个可见单元按 1–3 句英文写，最终以实际渲染 ≤4 行、≤192 高为准；选项控制在 ≤2 行、≤68 高，长度尽量相近。超限控制器会进错误页，不会缩字、截断或滚动。英文没有“40–120 字 / 选项 ≤30 字”这种硬字符数，按实际行数和阅读时长调。

拆段规则：

- 一个显示单元只能有一个说话者。不要把两个角色的台词写在同一行。
- 需要玩家再点一次 `Continue` 才出现的台词，必须写成**新的 Ink 内容行**；不要在同一个单元里用换行假装分段。
- 长段落拆成多个单元，而不是让它自动溢出正文框。
- 每个单元都单独写 `speaker` 和 `line`，不要依赖上一行的 tag。

### 4.4 编译与检查

- 保存 `.ink` 后 Ink Unity Integration 自动编译；Project 窗口出现同名 `.json`。
- 用 Ink 的 Player 窗口预览时，确认选项文字、括号回显、玩家台词都符合预期。
- 如果换了自定义字体，可先执行 `Tools > Visual Novel Study > Export Story Charset`；构建日志仍应显示“缺字 0”。
- 临时联调只有 1 个决策点时，把 `StudyController.expectedDecisionPointCount` 改为 1；正式构建必须改回 4。控制器会校验 `cp01` 的顺序，不能跳过或重复。

---

## 5. Player Settings、构建与启动断言

场景生成器已经写入大部分设置。仍建议人工核对：

`Edit > Project Settings > Player > Resolution and Presentation`（Windows）：

| 设置 | 值 |
|---|---|
| Fullscreen Mode | Windowed |
| Default Screen Width / Height | 1920 / 1080 |
| Resizable Window | 关 |
| Allow Fullscreen Switch | 关 |
| Run In Background | 开 |

说明：

- Unity 6 的 Windows Player Settings 已不再提供旧版“High DPI Support”复选框，高 DPI 处理是自动的；如果你们的 Unity 版本仍显示该选项，就勾选。不要在构建后通过系统缩放改变窗口。
- `DisplaySetup` 在 `BeforeSceneLoad` 设置分辨率；构建版启动两帧后检查显示器至少 1920×1080、客户区是否 1920×1080、Canvas `scaleFactor` 是否 1.000。不符时进入 ErrorPanel 且**返回按钮不可用**（回到入口也无法修复分辨率）。
- Editor 下不调用 `SetResolution`，只跳过断言并在 Console 提示；正式构建才生效。
- Build Settings 只保留 `Assets/Scenes/StudyScene.unity`（生成器已设置）。
- 测试机推荐：1920×1080、系统缩放 100%、FullScreenWindow；或大于 1920×1080 的屏幕用 Windowed 1920×1080。小于 1920×1080 不允许，预测试前统一改成可用的 16:9 尺寸并重查界面。

---

## 6. 运行行为速查

| 情况 | 程序行为 |
|---|---|
| 启动/返回入口 | 显示 MenuPanel，三个版本按钮可用；Story 为空，旧正文/选项/进度已清空 |
| 点击版本 | 立即禁用全部版本按钮 → 新建 Story → 显示首段正文 |
| 普通正文 | 完整即时显示；只有 ContinueButton 可推进 |
| 点击背景/正文/立绘/空白 | 无反应（Raycast Target 已关闭） |
| 双击/多击按钮 | 只接受第一次 `clickCount == 1` 的左键点击；后续忽略 |
| 键盘 | 没有 Submit 路径；EventSystem 导航关闭 |
| 提问 | 提问正文与全部选项同屏出现；Continue 隐藏；选项数必须等于配置 |
| 选择 | 选项组同帧隐藏；Ink 输出玩家台词；随后显示回应与公共剧情 |
| 结局末句 | 仍停留在 Reading，Continue 仍显示 `Continue`；再点一次才进 Complete |
| 完成页 | `Version X complete` + 问卷提示 + 唯一 `Return to start` 按钮 |
| 错误 | 阻断 ErrorPanel，显示英文中性文案；Console 输出 `[Study]` 具体原因与 line ID |
| 失焦 | Run In Background 保持运行；不会自动推进；切回可继续 |
| 中途关闭 | 重新打开回到 Menu；不恢复进度、不记为完成 |

---

## 7. 验收测试清单（对应说明书）

### 7.1 每次改完先做的冒烟测试

1. 启动 A、B、C，确认首段正文、版本标签、后续决策点数量（2/3/5）。
2. 双击版本按钮、继续按钮、某个选项、返回按钮：只接受第一次有效输入。
3. 走完一版：四个决策点都出现，末句可停留阅读，再点继续进入完成页。
4. 完成页返回入口，再启动另一版：无旧正文、旧选项、旧进度残留。
5. 故意删除一个 JSON 引用：进入 `The version could not start.` 错误页，可返回干净入口。
6. 故意把某版选项数改错：进入错误页，不截去/补造选项。
7. 拖窗口边框、Alt+Enter：正式构建应无效；若客户区不是 1920×1080，进入显示错误页。

### 7.2 说明书 D / T 系列

| 编号 | 检查 | 通过条件 |
|---|---|---|
| D01 | 提问在当次读取出现 | 提问与全部答案同次刷新，无额外继续点击 |
| D02 | 方括号选项被选择 | 玩家回答正文只显示一次，姓名正确 |
| D03 | 普通文本停止但无选项 | 未到共同末句时转 Error，不误报完成 |
| D04 | end tag 后意外还有正文 | 阻断该轮 |
| D05 | 某版选项数配置错误 | 不截去/补造答案，转 Error |
| D06 | 缺 speaker / 未知角色值 | 不沿用上次姓名，能定位问题单元 |
| D07 | 双击继续/答案/返回/版本 | 仅接受首个有效输入，不跨界面误点 |
| D08 | 失焦再切回 | 画面和选择保持，无自动推进 |
| D09 | 最长正文与最长五条答案 | 固定字体下完整可读，无滚动/遮挡 |
| D10 | 错误页返回后重启任意版本 | 无旧 Story、旧监听、旧结局状态 |
| T01 | 逐一启动 A/B/C | 均从首句开始，配置与标签一致 |
| T02 | 全部决策点 | 同一位置分别 2/3/5 个选项，无隐藏项 |
| T03 | 遍历 40 个局部选择 | 每个选项都有正确回应并回到公共剧情 |
| T04 | 比较共享内容 | 公共文本、共享回应、结局、展示规则一致 |
| T05 | 快速连续点击 | 不重复提交、不跳过显示单元、不启动重复实例 |
| T06 | 完成后返回并启动另一版 | 无残留 |
| T07 | 最长文本与五选项界面 | 窗口中完整可读，无遮挡/滚动 |
| T08 | 一次完整模拟 | 三版本、三问卷、匿名编号对应 |
| T09 | 模拟中途关闭 | 记录中断；重开回到入口，不误标完成 |
| E01–E07 | 布局/对比度/缺字/行数/显示/窗口/透明容器 | 按说明书附录 B.6 |

---

## 8. 还需要什么才能完成这个视觉小说

按优先级：

1. **三个 Ink 剧本**（当前最大缺口）
   - `story_A.ink`：4 个决策点 × 2 个选项 = 8 个局部入口
   - `story_B.ink`：4 × 3 = 12
   - `story_C.ink`：4 × 5 = 20
   - 共 40 个局部选项入口；共享文本逐字一致；同一共同结局。
   - 先写 C 的 5 个选项，再删减出 A/B，最容易保证共享选项的文字和顺序一致。
2. **英文字体资产**（默认 LiberationSans SDF 即可；想换正式一点的字体就生成一个拉丁字体资产，命名为 `StudySans SDF`）。
3. **一张背景 + 两张固定立绘**（占位图可以先跑流程；正式测试前换成定稿）。
4. **外部英文问卷**（纸质或外部表单）：叙事投入度、感知能动性、选择负担，1–7 分；不要写“改变结局”类题目，因为结局固定。参与说明、知情同意、问卷题目和记录表全部用英文，三版使用同一份。
5. **英文人工记录表**：匿名编号、预定顺序、当前版本、完成/中断、问卷是否提交、技术异常、是否重测。
6. **测试流程**：六种顺序（ABC/ACB/BAC/BCA/CAB/CBA）尽量均衡；每版前给相同说明；每版结束填问卷；测试者在游戏外管理编号与顺序。
7. **正式构建与冻结**：Windows x64 构建，记录 Unity / Ink / 构建版本；预测试后冻结内容、字体、窗口、槽位，不再改 A/B/C 任意一版的画面或布局。

不需要开发（说明书明确排除）：存读档、回滚、对话历史、章节跳转、自动播放、文本跳过、结局收集、成就、好感度、地图、背包、电话、邮件、配音、动画、音量设置、账户、联网问卷、自动统计、自动分配测试顺序、多个结局。

---

## 9. 常见错误与处理

| 界面/Console | 原因 | 处理 |
|---|---|---|
| `The study program is not configured correctly. Please tell the researcher.` | 控制器引用缺失，或 `studyFont` 未绑定 | 看 Console 的 `[Study] 引用未绑定：...` 逐项绑定；重新运行生成器 |
| `The version could not start. Please tell the researcher.` | JSON 未绑定/不是 JSON/解析失败；三版配置缺失、重复标签或共用同一资产 | 检查 Version Configs；确认三个 JSON 都是 TextAsset；看 Console `[Study]` |
| `This run has stopped. Please tell the researcher.` | tag 缺失/未知/重复、决策点顺序错、选项数不符、正文/选项超行、结局后还有内容 | 看 Console `[Study] ... line=...`；按 line ID 修 Ink |
| `choice count X != expected Y` | B/C 的选项没写够，或 `expectedChoiceCount` 配错 | 每版固定 2/3/5；不要自动复制选项 |
| `选择之后的第一个显示单元必须是 speaker:player` | 方括号选项后没有显式写玩家台词，或第一个单元是旁白/对方 | 在分支第一行写玩家回答并加 `# speaker:player` |
| `选择之后的局部分支不得立刻出现新的决策点` | 选项分支里又写了 `choice_point` | 局部分支只做一次回应，然后 divert 到公共节点 |
| `决策点顺序错误：预期 cp02，实际 cp03` | 漏写/跳号/重复 `choice_point` | 四个决策点必须 cp01→cp04 顺序出现 |
| `line ID 重复` | 复制分支时忘了改 line | 每个可见单元唯一；共享分支三版用相同 ID |
| `end:common 之后仍有正文/选项` | 结局标记后还有内容 | 删除；`end:common` 只放在最后一句 |
| `正文超过 4 行` / `选项超过 2 行` | 文字太长或字号/间距不对 | 缩短/拆段；不要把 lineSpacing 改成正值 |
| 构建失败“字体缺字 N 个” | 字体资产没有覆盖剧本字符 | 用 1.4 导出字符集，重新生成字体；或换字 |
| 显示错误页且返回按钮不可用 | 屏幕小于 1920×1080、客户区不是 1920×1080，或 scaleFactor ≠ 1 | 调整显示器/系统缩放/窗口；见第 5 节 |
| 英文字符显示为方框/缺字 | `studyFont` 没有覆盖全部字符（常见于弯引号、长破折号） | 改用直引号/短横线，或生成包含这些标点的自定义字体资产；看构建的 `[FontCheck]` 缺字列表 |
| 按钮点击没反应 | `SingleClickButton` 所在对象 Image 的 Raycast Target 被关，或面板被遮挡 | 按钮 Image 开 Raycast；确认没有全屏透明 Image 盖住 |
| 选项出现后还能点继续 | Continue 没有隐藏/禁用 | 生成器的引用是否绑对；不要手工改控制器逻辑 |

---

## 10. 几个实现取舍（与说明书一致）

1. **按钮没有用 `UnityEngine.UI.Button`。** 说明书要求“只转发左键 clickCount == 1、忽略键盘 Submit”，并列出 ChoiceButton 由 `Image + SingleClickButton + TMP_Text` 组成。用 `Button` 会自带 Submit 路径，还要额外关导航；直接实现指针接口更干净，四态颜色也完全可控。
2. **ChoicesPanel 没有 Image。** 按 B.1，它只是布局容器；未使用槽位 `SetActive(false)`，A/B 不会出现空底板。
3. **严格拒绝未知 tag。** 这是为了让 `# Speeker:friend` 这类拼写错误立刻暴露，而不是静默显示错误姓名。
4. **三个版本配置必须全部绑定才能启动任意一版。** 对应 F01 的“校验配置标签、JSON 引用和预期选项数；确认三个配置没有误绑为同一资产”。
5. **字体查找统一走 `StudyFontLocator`。** 优先 StudySans，其次 LiberationSans / Roboto / Inter 等拉丁字体，最后回退到第一个 TMP 字体资产；生成器和构建检查共用同一逻辑。英文版可以直接用 LiberationSans，不再强制 Noto Sans SC。
6. **生成器会加一个 Main Camera。** Screen Space Overlay 本身不需要相机渲染 UI，但空场景没有相机时 Game 视图会显示 “No cameras rendering”；相机只负责清屏，`cullingMask = 0`。
7. **构建期缺字阻断 + 运行时行数阻断。** `FontCoverageCheck` 对 Dynamic 字体调用 `TryAddCharacters`，对 Static 字体调用 `HasCharacters`；缺字直接构建失败。运行时 `Fits` 不缩字、不截断、不滚动。

---

## 11. 参考

- Ink 运行时接口：<https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md>
- Ink 写作语法：<https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md>
- Ink Unity Integration：<https://github.com/inkle/ink-unity-integration>
- Unity Canvas Scaler：<https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html>
- Unity Selectable / Navigation：<https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Selectable.html>
- Unity 6 Windows Player Settings（High DPI 在 Unity 6 中为自动处理）：<https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/windows/playersettings>
