using System;
using StardewValley;

namespace FishingExpanded.Utils;

public static class DifficultyCalculator
{
	public static readonly (float Minute, float Percent)[] ExhaustionNodes = new(float, float)[7]
	{
		(1f, 0.01f),
		(3f, 0.03f),
		(5f, 0.1f),
		(7f, 0.2f),
		(9f, 0.35f),
		(12f, 0.5f),
		(15f, 1f)
	};

	public static float GetDifficultyMultiplier(int level)
	{
		if (level < 0)
		{
			return Lerp(0.5f, 1f, (float)(level + 10) / 10f);
		}
		if (level >= 50)
		{
			return Lerp(20f, 50f, (float)(level - 50) / 50f);
		}
		if (level >= 20)
		{
			return Lerp(5f, 20f, (float)(level - 20) / 30f);
		}
		if (level >= 4)
		{
			return Lerp(1.2f, 5f, (float)(level - 4) / 16f);
		}
		return Lerp(1f, 1.2f, (float)level / 4f);
	}

	public static int GetQuantityMultiplier(int level)
	{
		if (level <= 0)
		{
			return 1;
		}
		return level;
	}

	public static int GetExperienceMultiplier(int level)
	{
		if (level <= 0)
		{
			return 1;
		}
		return (int)Math.Max(1.0, Math.Round((double)level * 0.5, MidpointRounding.AwayFromZero));
	}

	public static int GetQualityTier(int level)
	{
		if (level >= 50)
		{
			return 4;
		}
		if (level >= 25)
		{
			return 2;
		}
		if (level >= 10)
		{
			return 1;
		}
		return 0;
	}

	public static int ApplyQualityBonus(int baseQuality, int level)
	{
		return Math.Max(baseQuality, GetQualityTier(level));
	}

	public static float GetFishSizeMultiplier(int level)
	{
		if (level > 0)
		{
			return 1f + 0.1f * (float)level;
		}
		if (level < 0)
		{
			return Math.Max(0.1f, 1f - 0.05f * (float)Math.Abs(level));
		}
		return 1f;
	}

	public static float GetVisualScale(int level)
	{
		if (level <= 0)
		{
			return 1f;
		}
		return Math.Min(3.70843f, 1f + (float)level * 0.0270843f);
	}

	public static double GetAssistChance(int countableCrownCount, int crownTarget)
	{
		if (countableCrownCount <= 0 || crownTarget <= 0)
		{
			return 0.0;
		}
		return 0.1 * (double)Math.Min(countableCrownCount, crownTarget) / (double)crownTarget;
	}

	public static int GetRandomAssistLevel(double rank)
	{
		double num = Math.Max(0.0, Math.Min(1.0, rank));
		double num2 = -0.9354838709677419 + 1.8709677419354838 * num;
		double num3 = 0.0;
		for (int i = 0; i <= 40; i++)
		{
			num3 += 1.0 + num2 * (double)(i - 20) / 20.0;
		}
		double num4 = Game1.random.NextDouble() * num3;
		double num5 = 0.0;
		for (int j = 0; j <= 40; j++)
		{
			num5 += 1.0 + num2 * (double)(j - 20) / 20.0;
			if (num4 < num5)
			{
				return j;
			}
		}
		return 40;
	}

	public static float GetCatchPenaltyModifier(int level, float catchProgress)
	{
		float val = 1f;
		if (catchProgress <= 0.01f)
		{
			val = 0.5f;
		}
		else if (catchProgress <= 0.2f)
		{
			val = 0.8f;
		}
		float val2 = 1f;
		if (level < 0)
		{
			if (catchProgress <= 0.01f)
			{
				val2 = Lerp(0.2f, 1f, (float)(level + 10) / 10f);
			}
			else if (catchProgress <= 0.2f)
			{
				val2 = Lerp(0.6f, 1f, (float)(level + 10) / 10f);
			}
			else if (catchProgress <= 0.4f)
			{
				val2 = Lerp(0.99f, 1f, (float)(level + 10) / 10f);
			}
		}
		return Math.Min(val, val2);
	}

	public static float GetEscapeFailBonusModifier(int level, int consecutiveFails, float catchProgress)
	{
		float catchPenaltyModifier = GetCatchPenaltyModifier(level, catchProgress);
		float catchPenaltyModifier2 = GetCatchPenaltyModifier(-10, catchProgress);
		float t = Math.Clamp((float)consecutiveFails / 5f, 0f, 1f);
		return Lerp(catchPenaltyModifier, catchPenaltyModifier2, t);
	}

	public static float GetExhaustionPercent(float elapsedSeconds)
	{
		if (elapsedSeconds <= 0f)
		{
			return 0f;
		}
		float num = elapsedSeconds / 60f;
		for (int i = 0; i < ExhaustionNodes.Length; i++)
		{
			if (num <= ExhaustionNodes[i].Minute)
			{
				if (i == 0)
				{
					return Lerp(0f, ExhaustionNodes[i].Percent, num / ExhaustionNodes[i].Minute);
				}
				return Lerp(ExhaustionNodes[i - 1].Percent, ExhaustionNodes[i].Percent, (num - ExhaustionNodes[i - 1].Minute) / (ExhaustionNodes[i].Minute - ExhaustionNodes[i - 1].Minute));
			}
		}
		return 1f;
	}

	public static float GetExhaustedDifficulty(float originalAdjustedDifficulty, float elapsedSeconds)
	{
		return originalAdjustedDifficulty + (80f - originalAdjustedDifficulty) * GetExhaustionPercent(elapsedSeconds);
	}

	private static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * Math.Max(0f, Math.Min(1f, t));
	}

	public static string GetRankKey(int level)
	{
		if (level < 1)
		{
			return "rank.weak";
		}
		if (level <= 3)
		{
			return "rank.elite";
		}
		if (level <= 6)
		{
			return "rank.knight";
		}
		if (level <= 8)
		{
			return "rank.lord";
		}
		if (level <= 15)
		{
			return "rank.count";
		}
		if (level <= 22)
		{
			return "rank.duke";
		}
		if (level <= 33)
		{
			return "rank.prince";
		}
		if (level <= 45)
		{
			return "rank.emperor";
		}
		if (level <= 66)
		{
			return "rank.godking";
		}
		if (level <= 88)
		{
			return "rank.divineking";
		}
		if (level <= 99)
		{
			return "rank.creator";
		}
		return "rank.taiyi";
	}

	public static int GetNextRankCeiling(int currentLevel)
	{
		if (currentLevel < 1)
		{
			return 3;
		}
		if (currentLevel <= 3)
		{
			return 6;
		}
		if (currentLevel <= 6)
		{
			return 8;
		}
		if (currentLevel <= 8)
		{
			return 15;
		}
		if (currentLevel <= 15)
		{
			return 22;
		}
		if (currentLevel <= 22)
		{
			return 33;
		}
		if (currentLevel <= 33)
		{
			return 45;
		}
		if (currentLevel <= 45)
		{
			return 66;
		}
		if (currentLevel <= 66)
		{
			return 88;
		}
		if (currentLevel <= 88)
		{
			return 99;
		}
		_ = 99;
		return 100;
	}

	public static bool TryGetStarfruitTeaDrop(int difficultyLevel)
	{
		if (difficultyLevel < 50)
		{
			return false;
		}
		double num = (double)difficultyLevel / 400.0;
		return Game1.random.NextDouble() < num;
	}
}
