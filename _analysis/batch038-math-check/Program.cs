using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Batch038MathCheck
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        private static int Main(string[] args)
        {
            string dllPath = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string name = new AssemblyName(e.Name).Name;
                string candidate = Path.Combine(Path.GetDirectoryName(dllPath), name + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };

            Assembly fe = Assembly.LoadFrom(dllPath);
            Type calc = fe.GetType("FishingExpanded.Utils.DifficultyCalculator");
            if (calc == null) { Console.WriteLine("DifficultyCalculator not found"); return 1; }

            // 1) ExhaustionNodes
            var nodesField = calc.GetField("ExhaustionNodes", BindingFlags.Public | BindingFlags.Static);
            var nodes = (Array)nodesField.GetValue(null);
            (double min, double pct)[] expectedNodes = {
                (1, 0.01), (3, 0.03), (5, 0.10), (7, 0.20), (9, 0.35), (12, 0.50), (15, 1.00)
            };
            bool nodesOk = nodes.Length == expectedNodes.Length;
            for (int i = 0; i < nodes.Length && nodesOk; i++)
            {
                var item = nodes.GetValue(i);
                double minute = Convert.ToDouble(item.GetType().GetField("Item1").GetValue(item));
                double pct = Convert.ToDouble(item.GetType().GetField("Item2").GetValue(item));
                if (Math.Abs(minute - expectedNodes[i].min) > 0.001 || Math.Abs(pct - expectedNodes[i].pct) > 0.00001)
                        nodesOk = false;
            }
            Check("ExhaustionNodes 表", nodesOk, string.Join(",", expectedNodes.Select(n => $"{n.min}min={n.pct:P0}")));

            MethodInfo getPct = calc.GetMethod("GetExhaustionPercent", BindingFlags.Public | BindingFlags.Static);
            MethodInfo getExh = calc.GetMethod("GetExhaustedDifficulty", BindingFlags.Public | BindingFlags.Static);
            MethodInfo getSize = calc.GetMethod("GetFishSizeMultiplier", BindingFlags.Public | BindingFlags.Static);
            MethodInfo getQty = calc.GetMethod("GetQuantityMultiplier", BindingFlags.Public | BindingFlags.Static);
            MethodInfo getRank = calc.GetMethod("GetRankKey", BindingFlags.Public | BindingFlags.Static);

            double Pct(double s) => Convert.ToDouble(getPct.Invoke(null, new object[] { (float)s }));
            double Exh(double orig, double s) => Convert.ToDouble(getExh.Invoke(null, new object[] { (float)orig, (float)s }));
            double Size(int lvl) => Convert.ToDouble(getSize.Invoke(null, new object[] { lvl }));
            int Qty(int lvl) => (int)getQty.Invoke(null, new object[] { lvl });
            string Rank(int lvl) => (string)getRank.Invoke(null, new object[] { lvl });

            Check("1分钟=1%", Near(Pct(60), 0.01), Pct(60).ToString("P2"));
            Check("3分钟=3%", Near(Pct(180), 0.03), Pct(180).ToString("P2"));
            Check("5分钟=10%", Near(Pct(300), 0.10), Pct(300).ToString("P2"));
            Check("7分钟=20%", Near(Pct(420), 0.20), Pct(420).ToString("P2"));
            Check("9分钟=35%", Near(Pct(540), 0.35), Pct(540).ToString("P2"));
            Check("12分钟=50%", Near(Pct(720), 0.50), Pct(720).ToString("P2"));
            Check("15分钟=100%", Near(Pct(900), 1.00), Pct(900).ToString("P2"));
            Check("2分钟线性=2%", Near(Pct(120), 0.02), Pct(120).ToString("P2"));
            Check("4分钟线性=6.5%", Near(Pct(240), 0.065), Pct(240).ToString("P2"));
            Check("30秒=0.5%", Near(Pct(30), 0.005), Pct(30).ToString("P2"));
            Check("超过15分钟保持100%", Near(Pct(1800), 1.00), Pct(1800).ToString("P2"));

            Check("难度200@15min=80", Near(Exh(200, 900), 80), Exh(200, 900).ToString("F1"));
            Check("难度150@1min=149.3", Near(Exh(150, 60), 149.3), Exh(150, 60).ToString("F1"));
            Check("难度100@15min=80", Near(Exh(100, 900), 80), Exh(100, 900).ToString("F1"));
            Check("难度120@5min=116", Near(Exh(120, 300), 116), Exh(120, 300).ToString("F1"));
            Check("难度200@0s=200", Near(Exh(200, 0), 200), Exh(200, 0).ToString("F1"));

            Check("尺寸倍率100级=11", Near(Size(100), 11), Size(100).ToString("F2"));
            Check("尺寸倍率1级=1.1", Near(Size(1), 1.1), Size(1).ToString("F2"));
            Check("尺寸倍率0级=1", Near(Size(0), 1), Size(0).ToString("F2"));
            Check("尺寸倍率-10级=0.5", Near(Size(-10), 0.5), Size(-10).ToString("F2"));

            Check("数量倍率100级=200", Qty(100) == 200, Qty(100).ToString());
            Check("数量倍率0级=1", Qty(0) == 1, Qty(0).ToString());

            Check("100级称号=rank.taiyi", Rank(100) == "rank.taiyi", Rank(100));
            Check("99级称号=rank.creator", Rank(99) == "rank.creator", Rank(99));
            Check("0级称号=rank.weak", Rank(0) == "rank.weak", Rank(0));

            Console.WriteLine($"RESULT: pass={_pass} fail={_fail}");
            return _fail == 0 ? 0 : 1;
        }

        private static bool Near(double a, double b) => Math.Abs(a - b) < 0.001;

        private static void Check(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Console.WriteLine($"[PASS] {name}  ({detail})"); }
            else { _fail++; Console.WriteLine($"[FAIL] {name}  ({detail})"); }
        }
    }
}
