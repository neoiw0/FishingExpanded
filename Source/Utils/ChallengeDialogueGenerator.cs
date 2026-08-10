using System;
using System.Collections.Generic;
using StardewValley;

namespace FishingExpanded.Utils
{
    /// <summary>已获得图鉴星标的鱼进入小游戏时的挑战宣言。</summary>
    public static class ChallengeDialogueGenerator
    {
        private static readonly Dictionary<string, string> HonorificKeys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["rank.weak"] = "hud.starChallenge.honor.weak",
            ["rank.elite"] = "hud.starChallenge.honor.elite",
            ["rank.knight"] = "hud.starChallenge.honor.knight",
            ["rank.lord"] = "hud.starChallenge.honor.lord",
            ["rank.count"] = "hud.starChallenge.honor.count",
            ["rank.duke"] = "hud.starChallenge.honor.duke",
            ["rank.prince"] = "hud.starChallenge.honor.prince",
            ["rank.emperor"] = "hud.starChallenge.honor.emperor",
            ["rank.godking"] = "hud.starChallenge.honor.godking",
            ["rank.divineking"] = "hud.starChallenge.honor.divineking",
            ["rank.creator"] = "hud.starChallenge.honor.creator",
            ["rank.taiyi"] = "hud.starChallenge.honor.taiyi"
        };

        private static readonly string[] FallbackHonorifics = { "可敬的", "令人敬重的", "值得敬佩的" };
        private static readonly string[] FallbackResolve = { "毅然决然" };
        private static readonly string[] FallbackActions = { "应战" };

        public static string Generate(string fishName, int level)
        {
            try
            {
                string rankKey = DifficultyCalculator.GetRankKey(level);
                string rankName = ModEntry.ModHelper.Translation.Get(rankKey);
                string honorificKey = HonorificKeys.TryGetValue(rankKey, out string key)
                    ? key
                    : "hud.starChallenge.honor.weak";

                string honorific = Pick(LoadOptions(honorificKey, FallbackHonorifics));
                string resolve = Pick(LoadOptions("hud.starChallenge.resolve", FallbackResolve));
                string action = Pick(LoadOptions("hud.starChallenge.action", FallbackActions));

                // BATCH-030: 弱称号（等级 <1）只用于零以下胜利提示，不嵌入挑战宣言。
                string templateKey = rankKey == "rank.weak"
                    ? "hud.starChallenge.template.weak"
                    : "hud.starChallenge.template";
                return ModEntry.ModHelper.Translation.Get(
                    templateKey,
                    new
                    {
                        honorific,
                        fishName = string.IsNullOrWhiteSpace(fishName) ? "未知鱼类" : fishName,
                        rank = rankName,
                        resolve,
                        action
                    });
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"[ChallengeDialogue] 生成挑战宣言失败: {ex.Message}", StardewModdingAPI.LogLevel.Error);
                return $"可敬的{fishName ?? "未知鱼类"}毅然决然应战。";
            }
        }

        private static string[] LoadOptions(string translationKey, string[] fallback)
        {
            string raw = ModEntry.ModHelper.Translation.Get(translationKey);
            if (string.IsNullOrWhiteSpace(raw) || raw == translationKey)
                return fallback;

            string[] options = raw.Split('|', StringSplitOptions.RemoveEmptyEntries);
            return options.Length == 0 ? fallback : options;
        }

        private static string Pick(string[] options)
        {
            if (options == null || options.Length == 0)
                return string.Empty;

            return Game1.random != null
                ? options[Game1.random.Next(options.Length)]
                : options[0];
        }
    }
}
