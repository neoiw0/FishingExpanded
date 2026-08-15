using System;
using System.Collections.Generic;
using FishingExpanded.Services;
using FishingExpanded.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace FishingExpanded.Patches;

[HarmonyPatch(typeof(CollectionsPage))]
internal class CollectionsPagePatches
{
	private static Texture2D _crownTexture;

	private static bool _collectionsDrawing;

	private static int _collectionsTab = -1;

	[HarmonyPatch("createDescription")]
	[HarmonyPostfix]
	public static void CreateDescription_Postfix(CollectionsPage __instance, string id, ref string __result, int ___currentTab)
	{
		try
		{
			if (___currentTab != 1 || string.IsNullOrEmpty(id))
			{
				return;
			}
			string fishId = SpecialFishHelper.NormalizeItemId(id);
			bool flag = DifficultyManager.HasCollectionStar(fishId, Game1.player);
			List<string> list = new List<string>();
			if (!SpecialFishHelper.IsLegendaryFish(id))
			{
				int difficultyLevel = DifficultyManager.GetDifficultyLevel(fishId, Game1.player);
				if (difficultyLevel > 0)
				{
					string rankKey = DifficultyCalculator.GetRankKey(difficultyLevel);
					string rankName = Translation.op_Implicit(ModEntry.ModHelper.Translation.Get(rankKey));
					list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.challengeRank", (object)new
					{
						rankName = rankName,
						level = difficultyLevel
					})));
				}
			}
			if (flag)
			{
				if (DifficultyManager.IsNonFishItem(fishId))
				{
					list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.noCrownControl")));
				}
				else
				{
					list.Add(Translation.op_Implicit(ModEntry.ModHelper.Translation.Get("collections.crownControl", (object)new
					{
						crownCount = DifficultyManager.GetCountableCrownCount(Game1.player),
						crownTarget = 61
					})));
				}
			}
			if (list.Count > 0)
			{
				__result = __result + Environment.NewLine + string.Join(" | ", list);
			}
		}
		catch (Exception value)
		{
			FishingLog.Log($"[CollectionsPage] createDescription Postfix 失败: {value}", (LogLevel)4);
		}
	}

	[HarmonyPatch("draw")]
	[HarmonyPrefix]
	public static void CollectionsDraw_Prefix(CollectionsPage __instance)
	{
		_collectionsDrawing = true;
		_collectionsTab = __instance.currentTab;
	}

	[HarmonyPatch("draw")]
	[HarmonyPostfix]
	public static void CollectionsDraw_Postfix()
	{
		_collectionsDrawing = false;
		_collectionsTab = -1;
	}

	private static Texture2D GetCrownTexture()
	{
		if (_crownTexture == null || ((GraphicsResource)_crownTexture).IsDisposed)
		{
			_crownTexture = Game1.content.Load<Texture2D>("Characters\\Farmer\\hats");
		}
		return _crownTexture;
	}

	[HarmonyPatch(typeof(ClickableTextureComponent), "draw", new Type[]
	{
		typeof(SpriteBatch),
		typeof(Color),
		typeof(float),
		typeof(int),
		typeof(int),
		typeof(int)
	})]
	[HarmonyPostfix]
	public static void Draw_Postfix(SpriteBatch b, ClickableTextureComponent __instance, float layerDepth)
	{
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!_collectionsDrawing || _collectionsTab != 1)
			{
				return;
			}
			string[] array = ((ClickableComponent)__instance).name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 0 || !DifficultyManager.HasCollectionStar(array[0], Game1.player))
			{
				return;
			}
			if (DifficultyManager.IsNonFishItem(array[0]))
			{
				Rectangle val = ((Game1.mouseCursors.Width >= 900) ? new Rectangle(280, 188, 94, 100) : new Rectangle(196, 131, 57, 57));
				b.Draw(Game1.mouseCursors, new Vector2((float)(((ClickableComponent)__instance).bounds.X + 3), (float)(((ClickableComponent)__instance).bounds.Y + 3)), (Rectangle?)val, Color.White, 0f, Vector2.Zero, 24f / (float)val.Width, (SpriteEffects)0, layerDepth + 0.01f);
				return;
			}
			Texture2D crownTexture = GetCrownTexture();
			Rectangle value = default(Rectangle);
			((Rectangle)(ref value))._002Ector(20, 800, 20, 20);
			bool flag = DifficultyManager.HasChallengeCrown(array[0], Game1.player);
			bool flag2 = DifficultyManager.HasLevel100FlowCrown(array[0], Game1.player);
			if (flag)
			{
				double a = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0;
				float num = (float)Math.Sin(a);
				Color val2 = Color.Lerp(new Color(218, 165, 32), Color.White, (num + 1f) / 2f * 0.4f);
				float num2 = 24f * (flag2 ? 1.2f : 1f) * (1f + 0.05f * num);
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector((float)(((ClickableComponent)__instance).bounds.X + 3 + 12), (float)(((ClickableComponent)__instance).bounds.Y + 3 + 12));
				b.Draw(crownTexture, val3, (Rectangle?)value, val2, 0f, new Vector2(10f, 10f), num2 / 20f, (SpriteEffects)0, layerDepth + 0.01f);
			}
			else
			{
				b.Draw(crownTexture, new Vector2((float)(((ClickableComponent)__instance).bounds.X + 3), (float)(((ClickableComponent)__instance).bounds.Y + 3)), (Rectangle?)value, Color.White, 0f, Vector2.Zero, 1.2f, (SpriteEffects)0, layerDepth + 0.01f);
			}
		}
		catch (Exception value2)
		{
			FishingLog.Log($"[CollectionsPage] 皇冠绘制失败: {value2}", (LogLevel)4);
		}
	}
}
