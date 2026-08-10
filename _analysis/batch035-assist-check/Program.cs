using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

namespace Batch035AssistCheck
{
    /// <summary>BATCH-035 助战自动化验收 harness（无游戏运行环境）：
    /// 直接加载 FishingExpanded.dll，对助战纯函数/难度排位/等级分布做确定性统计验证。
    /// 用法: Batch035AssistCheck &lt;FishingExpanded.dll&gt; [i18nDir]</summary>
    internal static class Program
    {
        private static Assembly _fe;
        private static Type _calculator;
        private static Type _manager;
        private static Type _dataType;
        private static Type _statsType;
        private static int _pass;
        private static int _fail;

        /// <summary>模块初始化器：在 Main 被 JIT 之前注册程序集解析（Main 方法体引用了游戏类型）。</summary>
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void InitResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveGameAssemblies;
        }

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("usage: Batch035AssistCheck <FishingExpanded.dll> [i18nDir]");
                return 2;
            }
            string dllPath = Path.GetFullPath(args[0]);
            string i18nDir = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(Path.GetDirectoryName(dllPath) ?? "", "i18n");

            _fe = Assembly.LoadFrom(dllPath);
            BootstrapModEntry(dllPath);
            _calculator = _fe.GetType("FishingExpanded.Utils.DifficultyCalculator");
            _manager = _fe.GetType("FishingExpanded.Services.DifficultyManager");
            _dataType = _fe.GetType("FishingExpanded.Data.FishDifficultyData");
            _statsType = _fe.GetType("FishingExpanded.Data.FishStats");
            if (_calculator == null || _manager == null || _dataType == null || _statsType == null)
            {
                Console.WriteLine("FAIL: 无法解析 FishingExpanded 类型");
                return 1;
            }

            SetGameRandom(20260809);
            Check("i18n: hud.assist.1~20 + 12 个 rank 键 (zh/default)", CheckI18n(i18nDir));
            CheckCountableFishSet();
            CheckChance();

            Farmer low = MakeFarmer(424242L);
            Farmer tie = MakeFarmer(424243L);
            SetupData(low, tie);
            CheckCountable(low);
            CheckRank(low, tie);
            CheckDistribution();

            Console.WriteLine($"==== {_pass} PASS / {_fail} FAIL ====");
            return _fail == 0 ? 0 : 1;
        }

        // ---------- 通用工具 ----------

        private static void Check(string name, bool ok, string detail = "")
        {
            Console.WriteLine($"{(ok ? "[PASS]" : "[FAIL]")} {name}{(detail.Length > 0 ? " | " + detail : "")}");
            if (ok) _pass++; else _fail++;
        }

        private static object InvokeStatic(Type type, string name, params object[] args)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, args);
        }

        private static PropertyInfo GetProp(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        }

        private static void SetGameRandom(int seed)
        {
            FieldInfo f = typeof(Game1).GetField("random", BindingFlags.Public | BindingFlags.Static);
            if (f != null) f.SetValue(null, new Random(seed));
        }

        private static Farmer MakeFarmer(long id)
        {
            Farmer farmer = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
            // 未初始化 Farmer 的 NetLong 字段为 null，先构造 NetLong 再赋值
            FieldInfo idField = typeof(Farmer).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(x => x.Name.IndexOf("ultiplayer", StringComparison.OrdinalIgnoreCase) >= 0
                    && x.FieldType.Name == "NetLong");
            if (idField != null)
            {
                object netLong = Activator.CreateInstance(idField.FieldType);
                idField.FieldType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).SetValue(netLong, id);
                idField.SetValue(farmer, netLong);
                return farmer;
            }
            PropertyInfo prop = typeof(Farmer).GetProperty("UniqueMultiplayerID", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(farmer, id);
                return farmer;
            }
            throw new InvalidOperationException("Farmer multiplayer id not found");
            idField.SetValue(farmer, id);
            return farmer;
        }

        private static IDictionary GetPlayerDataDict()
        {
            FieldInfo f = _manager.GetField("_dataByPlayer", BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null)
                f = _manager.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name.IndexOf("ataByPlayer", StringComparison.OrdinalIgnoreCase) >= 0);
            if (f == null) throw new InvalidOperationException("_dataByPlayer field not found");
            return (IDictionary)f.GetValue(null);
        }

        private static object MakeStats(int level)
        {
            object s = Activator.CreateInstance(_statsType);
            GetProp(_statsType, "SuccessCount").SetValue(s, level);
            return s;
        }

        private static void SetupData(Farmer low, Farmer tie)
        {
            IDictionary dict = GetPlayerDataDict();

            // 玩家1: 可计数皇冠鱼 128(难度0) 129(难度4) 130(难度8) + Mod鱼 9999(难度8，不计入)
            object d1 = Activator.CreateInstance(_dataType);
            ICollection<string> stars1 = (ICollection<string>)GetProp(_dataType, "CollectionStars").GetValue(d1);
            IDictionary stats1 = (IDictionary)GetProp(_dataType, "FishStatistics").GetValue(d1);
            stars1.Add("(O)128"); stats1.Add("(O)128", MakeStats(0));
            stars1.Add("(O)129"); stats1.Add("(O)129", MakeStats(4));
            stars1.Add("(O)130"); stats1.Add("(O)130", MakeStats(8));
            stars1.Add("(O)9999"); stats1.Add("(O)9999", MakeStats(8));
            dict.Add(low.UniqueMultiplayerID, d1);

            // 玩家2: 全部同难度（排位应取 0.5）
            object d2 = Activator.CreateInstance(_dataType);
            ICollection<string> stars2 = (ICollection<string>)GetProp(_dataType, "CollectionStars").GetValue(d2);
            IDictionary stats2 = (IDictionary)GetProp(_dataType, "FishStatistics").GetValue(d2);
            stars2.Add("(O)131"); stats2.Add("(O)131", MakeStats(4));
            stars2.Add("(O)132"); stats2.Add("(O)132", MakeStats(4));
            dict.Add(tie.UniqueMultiplayerID, d2);
        }

        // ---------- 检查项 ----------

        private static bool CheckI18n(string dir)
        {
            bool ok = true;
            foreach (string lang in new[] { "zh", "default" })
            {
                string path = Path.Combine(dir, lang + ".json");
                if (!File.Exists(path)) { Console.WriteLine($"  i18n 缺失: {path}"); ok = false; continue; }
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement root = doc.RootElement;
                for (int i = 1; i <= 20; i++)
                {
                    string key = $"hud.assist.{i}";
                    if (!root.TryGetProperty(key, out JsonElement v) || string.IsNullOrWhiteSpace(v.GetString()))
                    {
                        Console.WriteLine($"  {lang}.json 缺失/为空: {key}");
                        ok = false;
                    }
                }
                string[] rankKeys = { "rank.weak", "rank.elite", "rank.knight", "rank.lord", "rank.count",
                    "rank.duke", "rank.prince", "rank.emperor", "rank.godking", "rank.divineking", "rank.creator", "rank.chaos" };
                foreach (string key in rankKeys)
                {
                    if (!root.TryGetProperty(key, out JsonElement v) || string.IsNullOrWhiteSpace(v.GetString()))
                    {
                        Console.WriteLine($"  {lang}.json 缺失/为空: {key}");
                        ok = false;
                    }
                }
            }
            return ok;
        }

        private static void CheckCountableFishSet()
        {
            FieldInfo f = _manager.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(x => x.Name.IndexOf("CountableFishIds", StringComparison.OrdinalIgnoreCase) >= 0);
            if (f == null) { Check("白名单: CountableFishIds 字段可解析", false); return; }
            var set = (System.Collections.Generic.HashSet<string>)f.GetValue(null);
            Check("白名单: 恰 61 条", set.Count == 61, $"实际 {set.Count}");
            string[] legendary = { "(O)159", "(O)160", "(O)163", "(O)682", "(O)775" };
            bool allLegend = legendary.All(id => set.Contains(id));
            Check("白名单: 5 条原版传奇在内", allLegend, string.Join(",", legendary.Where(id => !set.Contains(id))));
            Check("白名单: 不含 Mod 鱼 (O)9999", !set.Contains("(O)9999"));
            Check("白名单: 不含蟹笼 (O)715", !set.Contains("(O)715"));
        }

        private static void CheckChance()
        {
            var m = _calculator.GetMethod("GetAssistChance", BindingFlags.Public | BindingFlags.Static);
            double c0 = (double)m.Invoke(null, new object[] { 0, 61 });
            double cFull = (double)m.Invoke(null, new object[] { 61, 61 });
            double cMid = (double)m.Invoke(null, new object[] { 20, 61 });
            double cOver = (double)m.Invoke(null, new object[] { 99, 61 });
            double cSmall = (double)m.Invoke(null, new object[] { 5, 100 });
            Check("概率: 0 皇冠 = 0%", Math.Abs(c0) < 1e-9, $"实际 {c0:P3}");
            Check("概率: 满 61 = 10%", Math.Abs(cFull - 0.10) < 1e-9, $"实际 {cFull:P3}");
            Check("概率: 20/61 = 3.279%", Math.Abs(cMid - 0.10 * 20.0 / 61.0) < 1e-9, $"实际 {cMid:P3}");
            Check("概率: 99 钳制 = 10%", Math.Abs(cOver - 0.10) < 1e-9, $"实际 {cOver:P3}");
            Check("概率: 5/100 = 0.5%", Math.Abs(cSmall - 0.10 * 5.0 / 100.0) < 1e-9, $"实际 {cSmall:P3}");
        }

        private static void CheckCountable(Farmer player)
        {
            var list = (List<string>)InvokeStatic(_manager, "GetCountableStarredFish", player);
            int count = (int)InvokeStatic(_manager, "GetCountableCrownCount", player);
            Check("可计数: 只含 3 条原生鱼", list.Count == 3 && list.Contains("(O)128") && list.Contains("(O)129") && list.Contains("(O)130"),
                string.Join(",", list));
            Check("可计数: Mod 鱼皇冠不计入", !list.Contains("(O)9999"), "含 Mod 鱼则失败");
            Check("可计数数 = 3", count == 3, $"实际 {count}");
            foreach (string id in list)
            {
                bool ok = (bool)InvokeStatic(_manager, "IsCountableFish", id);
                if (!ok) { Check($"可计数: {id} 判定一致", false); return; }
            }
            Check("可计数: 每条都在 IsCountableFish 白名单", true);
        }

        private static void CheckRank(Farmer low, Farmer tie)
        {
            var listLow = (List<string>)InvokeStatic(_manager, "GetCountableStarredFish", low);
            var listTie = (List<string>)InvokeStatic(_manager, "GetCountableStarredFish", tie);
            double r0 = (double)InvokeStatic(_manager, "GetAssistRank", "(O)128", low, listLow);
            double r1 = (double)InvokeStatic(_manager, "GetAssistRank", "(O)129", low, listLow);
            double r2 = (double)InvokeStatic(_manager, "GetAssistRank", "(O)130", low, listLow);
            Check("排位: 最低难度=0", Math.Abs(r0) < 1e-9, $"实际 {r0:F3}");
            Check("排位: 中间难度=0.5", Math.Abs(r1 - 0.5) < 1e-9, $"实际 {r1:F3}");
            Check("排位: 最高难度=1", Math.Abs(r2 - 1.0) < 1e-9, $"实际 {r2:F3}");
            Check("排位: 全同难度=0.5", Math.Abs((double)InvokeStatic(_manager, "GetAssistRank", "(O)131", tie, listTie) - 0.5) < 1e-9);
            Check("排位: 空列表=0.5", Math.Abs((double)InvokeStatic(_manager, "GetAssistRank", "(O)128", low, new List<string>()) - 0.5) < 1e-9);
            Check("排位: null 列表=0.5", Math.Abs((double)InvokeStatic(_manager, "GetAssistRank", "(O)128", low, null) - 0.5) < 1e-9);
        }

        private static void CheckDistribution()
        {
            var m = _calculator.GetMethod("GetRandomAssistLevel", BindingFlags.Public | BindingFlags.Static);
            double[] ranks = { 0.0, 0.25, 0.5, 0.75, 1.0 };
            const int N = 500_000;
            double[] mean = new double[5], p40 = new double[5], p0 = new double[5];
            int[] min = new int[5], max = new int[5];
            for (int r = 0; r < 5; r++)
            {
                min[r] = 99; max[r] = -1;
                long[] hist = new long[41];
                for (int i = 0; i < N; i++)
                {
                    int level = (int)m.Invoke(null, new object[] { ranks[r] });
                    hist[level]++;
                    if (level < min[r]) min[r] = level;
                    if (level > max[r]) max[r] = level;
                }
                for (int L = 0; L <= 40; L++)
                {
                    mean[r] += L * hist[L];
                }
                mean[r] /= N;
                p0[r] = hist[0] / (double)N;
                p40[r] = hist[40] / (double)N;
            }

            Check("分布: 最低鱼均值≈13.45", Math.Abs(mean[0] - 13.4516) < 0.10, $"实际 {mean[0]:F4}");
            Check("分布: 中位鱼均值≈20", Math.Abs(mean[2] - 20.0) < 0.10, $"实际 {mean[2]:F4}");
            Check("分布: 最高鱼均值≈26.55", Math.Abs(mean[4] - 26.5484) < 0.10, $"实际 {mean[4]:F4}");
            Check("分布: 最低+最高均值=40", Math.Abs(mean[0] + mean[4] - 40.0) < 0.15, $"实际 {mean[0]:F3}+{mean[4]:F3}");
            Check("分布: 40级比值≈30", Math.Abs(p40[4] / p40[0] - 30.0) < 3.0, $"实际 {p40[4] / p40[0]:F1} (P40: {p40[0]:P4} vs {p40[4]:P4})");
            Check("分布: 0级镜像比值≈30", Math.Abs(p0[0] / p0[4] - 30.0) < 3.0, $"实际 {p0[0] / p0[4]:F1} (P0: {p0[0]:P4} vs {p0[4]:P4})");
            // 镜像用大样本直方图验证（各采 200 万次）
            long[] h0 = SampleHistogram(m, 0.0, 2_000_000);
            long[] h1 = SampleHistogram(m, 1.0, 2_000_000);
            bool mirrorOk = true;
            foreach (int L in new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40 })
            {
                double pL1 = h1[L] / 2_000_000.0;
                double pMirror0 = h0[40 - L] / 2_000_000.0;
                if (Math.Abs(pL1 - pMirror0) > 0.004) { mirrorOk = false; Console.WriteLine($"  镜像偏差 L={L}: {pL1:P4} vs {pMirror0:P4}"); }
            }
            Check("分布: 镜像对称（最高鱼=最低鱼反向）", mirrorOk);
            bool reach = min[0] == 0 && max[0] == 40 && min[4] == 0 && max[4] == 40;
            Check("分布: 最低/最高鱼均可抽到 0 和 40", reach, $"min/max rank0: {min[0]}/{max[0]}, rank1: {min[4]}/{max[4]}");
            bool mono = p40[0] < p40[1] && p40[1] < p40[2] && p40[2] < p40[3] && p40[3] < p40[4];
            Check("分布: P(40) 随排位单调递增", mono, string.Join(" < ", p40.Select(x => x.ToString("P3"))));
            bool midFlat = Math.Abs(p40[2] - 1.0 / 41.0) < 0.004 && Math.Abs(p0[2] - 1.0 / 41.0) < 0.004;
            Check("分布: 中位鱼均匀（P0/P40≈2.44%）", midFlat, $"P0 {p0[2]:P4} / P40 {p40[2]:P4}");
        }

        private static long[] SampleHistogram(MethodInfo m, double rank, int n)
        {
            long[] hist = new long[41];
            for (int i = 0; i < n; i++)
                hist[(int)m.Invoke(null, new object[] { rank })]++;
            return hist;
        }

        // ---------- 无 SMAPI 宿主引导 ----------

        private static void BootstrapModEntry(string dllPath)
        {
            try
            {
                Type modEntryType = _fe.GetType("FishingExpanded.ModEntry");
                object entry = Activator.CreateInstance(modEntryType);
                modEntryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, entry);
                PropertyInfo monitorProp = typeof(StardewModdingAPI.Mod).GetProperty("Monitor", BindingFlags.Public | BindingFlags.Instance);
                monitorProp?.SetValue(entry, DispatchProxy.Create<IMonitor, NoopProxy>());
            }
            catch (Exception ex)
            {
                Console.WriteLine("BootstrapModEntry warning: " + ex.Message);
            }
        }

        private class NoopProxy : DispatchProxy
        {
            public NoopProxy() { }
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.ReturnType == typeof(bool)) return false;
                if (targetMethod.ReturnType == typeof(int)) return 0;
                return null;
            }
        }

        private static Assembly ResolveGameAssemblies(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            string[] dirs = { @"D:\GGGGG\K1515", @"D:\GGGGG\K1515\smapi-internal", @"D:\GGGGG\K1515\Mods\FishingExpanded", @"D:\GGGGG\K1515\Mods\FishingExpanded\bin" };
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path))
                {
                    try { return Assembly.LoadFrom(path); }
                    catch (Exception ex) { Console.WriteLine($"  resolve {name}: LoadFrom failed: {ex.Message}"); }
                }
            }
            Console.WriteLine($"  resolve {name}: NOT FOUND in {string.Join(",", dirs)}");
            return null;
        }
    }
}