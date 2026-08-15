using System.Collections.Generic;

namespace FishingExpanded.Data;

public class FishDifficultyData
{
	public Dictionary<string, FishStats> FishStatistics { get; set; } = new Dictionary<string, FishStats>();

	public HashSet<string> CollectionStars { get; set; } = new HashSet<string>();

	public HashSet<string> ChallengeCrowns { get; set; } = new HashSet<string>();

	public HashSet<string> Level100FlowCrowns { get; set; } = new HashSet<string>();

	public Dictionary<string, int> ChallengePatternSeeds { get; set; } = new Dictionary<string, int>();
}
