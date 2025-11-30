// 说明：
// 本文件用于汇总原版 RimWorld 中 StatDef（属性定义）的常用字段、显示格式与计算流程。
// 仅包含中文文档注释与常量清单，不参与游戏运行逻辑，也不会被 Def 加载器读取。
// 你可以在需要时参考本文件编写/审查自定义 StatDef。

namespace PromotionLib.Documentation
{
    /// <summary>
    /// StatDef 文档说明（仅供阅读）。
    /// </summary>
    public static class StatDefDocumentation
    {
        // =========================
        // 概述
        // =========================
        // StatDef 用于定义“数值型属性”的规则、显示与计算方式（如工速、精准、耐久、清洁度、火焰蔓延率等）。
        // 最终数值由基础值、倍率/加成、部件因素、技能因素、能力因素、环境因素等共同决定，并由 workerClass 统一解释呈现。

        // =========================
        // 核心字段（常用）
        // =========================
        // - defName: 唯一标识。被其他 XML/C# 引用此 Stat 时使用。
        // - label: UI 显示名（可本地化）。
        // - labelForFullStatList: 全部属性列表中的专用显示名，覆盖 label。
        // - description: 说明文本（鼠标悬停提示等）。
        // - category: 属性分类，决定该属性展示分组和排序范围。
        // - defaultBaseValue: 基础值（所有因素前的起点）。
        // - minValue / maxValue: 最小/最大值约束。
        // - hideAtValue: 当最终值等于该值时隐藏此属性行。
        // - valueIfMissing: 当目标对象缺失该值来源时，使用的替代值。
        // - roundValue: 是否将最终值四舍五入为整数显示。
        // - roundToFiveOver: 大于该阈值时按 5 的倍数四舍五入（常用于工作量等大数）。
        // - toStringStyle: 数值显示格式（见下文“显示格式 toStringStyle”）。
        // - toStringStyleUnfinalized: “未最终结算前”的显示风格（少见，特例）。
        // - formatString: 自定义格式化字符串，例如 "{0} kg"、"{0}%"、"{0} / day"。
        // - displayPriorityInCategory: 在所属分类中的显示优先级，原版通过该值进行相对排序。
        // - alwaysHide: 永远隐藏（一般用于中间值或调试用，仍可被其他系统读取）。
        // - showIfUndefined: 若无定义/无来源是否仍显示该行。
        // - showOnPawns / showOnAnimals / showOnHumanlikes: 控制该属性是否在人形/动物等目标上显示。
        // - showOnUntradeables / showOnNonWorkTables / showOnNonPowerPlants 等：基于对象类型的可见性控制。
        // - showNonAbstract: 抽象定义上的显示控制。
        // - showIfModsLoaded: 指定仅当某些 DLC/模组加载时显示。
        // - minifiedThingInherits: 迷你化物体沿用（家具打包成迷你件后，属性是否继承显示）。
        // - scenarioRandomizable: 是否可在场景编辑器中随机化。
        // - cacheable: 该属性是否缓存（对性能有利，适合稳定计算的属性）。
        // - workerClass: 计算/解释该属性的 C# 处理类，负责最终值计算、说明面板的解释文本等。
        // - Abstract / Name / ParentName: 抽象与继承。Abstract="True" 表示为模板定义，ParentName 继承模板内容，减少重复字段。
        // - parts: 细分“部件因素”，每个 <li Class="StatPart_*"> 用于修改最终值或解释。例如：
        //   * StatPart_Quality（品质倍率/偏移）
        //   * StatPart_BodySize、StatPart_GearAndInventoryMass（体型、装备质量）
        //   * StatPart_WorkTableTemperature、StatPart_RoomStat（温度、房间属性）
        //   * StatPart_Glow（光照）、StatPart_Outdoors（室内/室外）等
        // - statFactors / statOffsets: 由其他 Stat 提供的乘法/加法影响（如 WorkSpeedGlobal）。
        // - capacityFactors: 受“能力值”（如 Sight、Manipulation）影响的加权因子集合。
        // - skillNeedFactors: 技能相关因子（如基础加成与每级增益）。

        // =========================
        // 显示格式 toStringStyle（原版常见值）
        // =========================
        // 百分比类：PercentZero、PercentOne、PercentTwo
        // 浮点类：FloatOne、FloatTwo、FloatMaxOne、FloatMaxTwo、FloatMaxThree、FloatTwoOrThree
        // 整数类：Integer
        // 货币：Money
        // 工作量：WorkAmount
        // 温度：Temperature、TemperatureOffset
        // 说明：
        // - 可配合 formatString 加单位或符号，例如 "{0}%"、"{0} W"、"{0} kg"。
        // - 需要整数显示可用 Integer；或开启 roundValue 以配合其他风格四舍五入。

        /// <summary>
        /// 原版中常见的 toStringStyle 取值清单（供检索参考）。
        /// </summary>
        public static readonly string[] CommonToStringStyles = new[]
        {
            // 百分比
            "PercentZero", "PercentOne", "PercentTwo",
            // 浮点
            "FloatOne", "FloatTwo", "FloatMaxOne", "FloatMaxTwo", "FloatMaxThree", "FloatTwoOrThree",
            // 整数与货币
            "Integer", "Money",
            // 工作量与温度
            "WorkAmount", "Temperature", "TemperatureOffset"
        };

        // =========================
        // 计算流程（简化）
        // =========================
        // 1) 取 defaultBaseValue 为起点。
        // 2) 套用 statFactors（乘法）、statOffsets（加法）。
        // 3) 应用 parts（StatPart_*）对值进行上下文修正（品质、体型、温度、环境等）。
        // 4) 应用 capacityFactors、skillNeedFactors 等进一步修正。
        // 5) 应用 minValue/maxValue 裁剪。
        // 6) 由 workerClass 生成解释文本和最终展示（包括说明面板）。
        // 7) 由 toStringStyle + formatString 控制显示格式；roundValue/roundToFiveOver 控制取整与分位。

        // =========================
        // 原版权威用例引用（节选，行号随版本可能变动，仅供定位）
        // =========================
        // 1) 整数显示（最大耐久）
        //   文件：Data/Core/Defs/Stats/Stats_Basics_General.xml
        //   参考：
        //   <roundValue>true</roundValue>
        //   <toStringStyle>Integer</toStringStyle>
        //
        // 2) 建筑陷阱近战伤害（整数显示）
        //   文件：Data/Core/Defs/Stats/Stats_Building_Special.xml
        //   参考：
        //   <minValue>1</minValue>
        //   <toStringStyle>Integer</toStringStyle>
        //   <showIfUndefined>false</showIfUndefined>
        //
        // 3) 意识形态恐惧值（整数 + 自定义百分号）
        //   文件：Data/Ideology/Defs/StatDefs/Stats_Basics_Special.xml
        //   参考：
        //   <hideAtValue>0</hideAtValue>
        //   <toStringStyle>Integer</toStringStyle>
        //   <formatString>{0}%</formatString>
        //
        // 4) 未最终结算前显示风格（特例）
        //   文件：Data/Core/Defs/Stats/Stats_Building_Special.xml
        //   参考：
        //   <toStringStyle>PercentOne</toStringStyle>
        //   <toStringStyleUnfinalized>FloatOne</toStringStyleUnfinalized>
        //
        // 5) 工作量风格与四舍五入到 5 的倍数（示例）
        //   文件：Data/Core/Defs/Stats/Stats_Basics_General.xml
        //   参考：
        //   <toStringStyle>WorkAmount</toStringStyle>
        //   <roundToFiveOver>300</roundToFiveOver>

        // =========================
        // 使用建议
        // =========================
        // - 定义新属性时，优先明确：
        //   * category、defaultBaseValue、minValue/maxValue。
        //   * toStringStyle 与是否需要 formatString 单位。
        //   * 是否需要 roundValue / roundToFiveOver。
        //   * 是否需要上下文因素（在 parts 中引入合适的 StatPart_*）。
        //   * 是否需要与其他属性联动（statFactors/statOffsets/capacityFactors/skillNeedFactors）。
        //   * 是否需要特定 workerClass 提供自定义解释与结算逻辑。
        // - 例：你的“病毒防护等级”若希望以整数显示，使用 toStringStyle = Integer；
        //   若希望百分比无小数，使用 PercentZero，并可配合 formatString 追加单位或符号。
    }
}


