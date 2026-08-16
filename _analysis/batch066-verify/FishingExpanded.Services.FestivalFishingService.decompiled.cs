using StardewValley;

namespace FishingExpanded.Services;

internal static class FestivalFishingService
{
	internal static bool IsSquidFest()
	{
		return Utility.IsPassiveFestivalDay("SquidFest");
	}

	internal static bool IsTroutDerby()
	{
		return Utility.IsPassiveFestivalDay("TroutDerby");
	}

	internal static bool IsIceFestival()
	{
		if (!Game1.isFestival())
		{
			return false;
		}
		GameLocation currentLocation = Game1.currentLocation;
		if (currentLocation == null)
		{
			return false;
		}
		Event currentEvent = currentLocation.currentEvent;
		return ((currentEvent != null) ? new bool?(currentEvent.isSpecificFestival("winter8")) : ((bool?)null)) == true;
	}

	internal static bool IsFestivalFishingActive()
	{
		if (!IsSquidFest() && !IsTroutDerby())
		{
			return IsIceFestival();
		}
		return true;
	}

	internal static bool IsVanillaFestivalMode()
	{
		if (IsFestivalFishingActive())
		{
			return !ModEntry.Config.EnableFestivalFishingMods;
		}
		return false;
	}
}
