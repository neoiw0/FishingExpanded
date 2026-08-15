using System.Collections.Generic;

namespace FishingExpanded.Data;

public class FishDisplayData
{
	public Dictionary<string, (int level, int fishSize)> ActiveGiantFish { get; set; } = new Dictionary<string, (int, int)>();

	public Dictionary<string, HashSet<string>> NPCBubbleTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

	public Dictionary<string, HashSet<string>> NPCDialogueTriggered { get; set; } = new Dictionary<string, HashSet<string>>();

	public void ResetDailyTriggers()
	{
		NPCBubbleTriggered.Clear();
		NPCDialogueTriggered.Clear();
	}

	public void ClearActiveGiantFish()
	{
		ActiveGiantFish.Clear();
	}
}
