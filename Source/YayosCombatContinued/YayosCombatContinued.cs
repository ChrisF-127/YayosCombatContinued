using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	public class YayosCombatContinued : Mod
	{
		#region CONSTANTS
		public const string AmmoCategoryName = nameof(YCC_ThingCategoryDefOf.YCC_AmmoCategory);
		#endregion

		#region PROPERTIES
		public static YayosCombatContinued Instance { get; private set; }
		public static YayosCombatContinuedSettings Settings { get; private set; }

		public static bool SimpleSidearmsCompatibility { get; } =
			ModsConfig.IsActive("petetimessix.simplesidearms") || ModsConfig.IsActive("petetimessix.simplesidearms_steam");
		#endregion

		#region CONSTRUCTORS
		public YayosCombatContinued(ModContentPack content) : base(content)
		{
			Instance = this;

			LongEventHandler.ExecuteWhenFinished(Initialize);
		}
		#endregion

		#region OVERRIDES
		public override string SettingsCategory() =>
			"Yayo's Combat - Continued";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Settings.DoSettingsWindowContents(inRect);
		}
		#endregion

		#region PRIVATE METHODS
		private void Initialize()
		{
			if (SimpleSidearmsCompatibility)
				Log.Message($"[{nameof(YayosCombatAddon)}] SimpleSidearms active, compatibility will be applied");

			Settings = GetSettings<YayosCombatContinuedSettings>();

			// Dynamic "Assign"-tab ammo-column initialization
			var assignTableDef = DefDatabase<PawnTableDef>.AllDefs.First(def => def.defName == "Assign");
			var ammoDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsAmmo(true))?.ToList();
			if (ammoDefs?.Count > 0)
			{
				for (int i = 0; i < Settings.NumberOfAmmoColumns; i++)
				{
					var name = $"yy_ammo{i + 1}";
					var inventory = new InventoryStockGroupDef
					{
						defName = name,
						modContentPack = Content,
						defaultThingDef = ammoDefs[0],
						thingDefs = ammoDefs,
						min = 0,
						max = 10000,
					};
					var column = new PawnColumnDef
					{
						defName = name,
						label = "ammo " + (i + 1),
						workerClass = typeof(PawnColumnWorker_CarryAmmo),
						sortable = true,
					};

					Content.AddDef(inventory);
					Content.AddDef(column);

					DefGenerator.AddImpliedDef(inventory);
					DefGenerator.AddImpliedDef(column);

					assignTableDef.columns.Insert(assignTableDef.columns.Count - 1, column);
				}
			}
			else
				Log.Error($"{nameof(YayosCombatAddon)}: could not find any things using the '{AmmoCategoryName}'-ThingCategory (no ammo found); assign tab columns could not be created!");
		}
		#endregion
	}
}
