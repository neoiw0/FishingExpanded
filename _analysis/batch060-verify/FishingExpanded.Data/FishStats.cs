using System;

namespace FishingExpanded.Data;

public class FishStats
{
	public int SuccessCount { get; set; }

	public int FailCount { get; set; }

	public int ConsecutiveFailCount { get; set; }

	public int DifficultyLevel
	{
		get
		{
			try
			{
				long val = (long)SuccessCount - (long)FailCount;
				return (int)Math.Max(-10L, Math.Min(100L, val));
			}
			catch
			{
				return 0;
			}
		}
	}
}
