using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtendedRaceAutoPatcher
{
    public class Mod : HugsLib.ModBase
    {
        const string ModName = "CombatExtended-RaceAutoPatcher";
        public override string ModIdentifier => ModName;

        private readonly StringBuilder trace = new StringBuilder();

        private BodyShapeDef humanoidBodyShapeDef;
        private StatDef[] combatExtendedStatBases;

        public override void DefsLoaded()
        {
            if (!ModIsActive)
                return;

            try
            {
                PatchAliens();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                Logger.Message(trace.ToString());
            }
        }

        private string GetVanillaRaceBodyShape(ThingDef def)
        {
            if (def.race.body?.defName?.Contains("Quadruped") ?? false)
            {
                if (def.race.baseBodySize < 1f)
                {
                    return "QuadrupedLow";
                }
                else
                {
                    return "Quadruped";
                }
            }

            // Birdlike?

            return null;
        }

        private void PatchAliens()
        {
            trace.AppendLine();
            trace.AppendLine("INITIALIZING ALIEN CE RACES");

            humanoidBodyShapeDef = DefDatabase<BodyShapeDef>.AllDefsListForReading.FirstOrDefault(x => x.defName == "Humanoid");
            if (humanoidBodyShapeDef == null)
            {
                Logger.Error("Could not find humanoid body shape def");
                return;
            }
            trace.AppendLine("Loaded CE Humanoid body shape");

            combatExtendedStatBases = new[]
            {
                CE_StatDefOf.AimingAccuracy,
                CE_StatDefOf.MeleeDodgeChance,
                CE_StatDefOf.MeleeCritChance,
                CE_StatDefOf.MeleeParryChance,
                CE_StatDefOf.ReloadSpeed,
                CE_StatDefOf.Suppressability,
            };
            trace.AppendLine("Loaded CE stat defs");

            var allAlienRaces = DefDatabase<AlienRace.ThingDef_AlienRace>.AllDefs.ToList();
            trace.AppendLine($"Found {allAlienRaces.Count} alien race defs");

            foreach (var race in allAlienRaces)
            {
                trace.AppendLine($"***** Processing race {race} *****");

                SetHumanoidBodyShape(race);
                SetSuppressable(race);
                SetDefaultStatBases(race);
                ReplaceTools(race);
            }
        }

        private void SetHumanoidBodyShape(AlienRace.ThingDef_AlienRace race)
        {
            // RaceDef CombatExtended.RacePropertiesExtensionCE bodyShape humanoid
            if (!race.HasModExtension<RacePropertiesExtensionCE>())
            {
                trace.AppendLine("* Adding body shape mod extension");
                var extension = new RacePropertiesExtensionCE { bodyShape = humanoidBodyShapeDef };
                trace.AppendLine("Initialized mod extension");
                if (race.modExtensions == null)
                {
                    race.modExtensions = new List<DefModExtension>();
                    trace.AppendLine("Initialized mod extension list");
                }
                race.modExtensions.Add(extension);
                trace.AppendLine($"Added humanoid body shape to race def");
            }
        }

        private void SetSuppressable(AlienRace.ThingDef_AlienRace race)
        {
            // comps element?
            if (race.comps == null)
            {
                trace.AppendLine("* Adding missing comps list");
                race.comps = new List<CompProperties>();
            }

            // compclass CombatExtended.CompPawnGizmo
            if (!race.comps.Any(x => x.compClass == typeof(CompPawnGizmo)))
            {
                trace.AppendLine("* Adding CompPawnGizmo to comps list");

                var gizmoProp = new CompProperties { compClass = typeof(CompPawnGizmo) };
                trace.AppendLine("Initialized gizmo prop");

                race.comps.Add(gizmoProp);
                trace.AppendLine("Added gizmo to comps");
            }

            // CombatExtended.CompProperties_Suppressable
            if (!race.comps.OfType<CompProperties_Suppressable>().Any())
            {
                trace.AppendLine("* Adding supressable to comps list");

                var suppressableProps = new CompProperties_Suppressable();
                trace.AppendLine("Initialized supressable prop");

                race.comps.Add(suppressableProps);
                trace.AppendLine("Added supressable to comps");
            }
        }

        private void SetDefaultStatBases(AlienRace.ThingDef_AlienRace race)
        {
            // statbases @ 1
            foreach (var statDef in combatExtendedStatBases)
            {
                var existing = race.statBases.FirstOrDefault(x => x.stat == statDef);
                if (existing == null)
                {
                    trace.AppendLine($"* Adding default {statDef} stat");
                    race.statBases.Add(new RimWorld.StatModifier { stat = statDef, value = 1 });
                }
            }
        }

        private void ReplaceTools(AlienRace.ThingDef_AlienRace race)
        {
            // tools - li Class CombatExtended.ToolCE / armorPenetration
            var oldTools = race.tools.ToList();
            foreach (var tool in oldTools)
            {
                if (tool is ToolCE)
                    continue;

                trace.AppendLine($"* Replacing race tool {tool}");

                var newTool = new ToolCE
                {
                    label = tool.label,
                    untranslatedLabel = tool.untranslatedLabel,

                    alwaysTreatAsWeapon = tool.alwaysTreatAsWeapon,
                    capacities = tool.capacities,
                    chanceFactor = tool.chanceFactor,
                    cooldownTime = tool.cooldownTime,
                    ensureLinkedBodyPartsGroupAlwaysUsable = tool.ensureLinkedBodyPartsGroupAlwaysUsable,
                    id = tool.id,
                    labelUsedInLogging = tool.labelUsedInLogging,
                    linkedBodyPartsGroup = tool.linkedBodyPartsGroup,
                    power = tool.power,
                    surpriseAttack = tool.surpriseAttack,
                    hediff = tool.hediff,

                    armorPenetration = 0.10f,
                };
                trace.AppendLine("Initialized new tool");

                race.tools.Remove(tool);
                trace.AppendLine("Removed existing tool");

                race.tools.Add(newTool);
                trace.AppendLine($"Added new tool {newTool}");
            }
        }

        private void AddInventorySupport(AlienRace.ThingDef_AlienRace race)
        {
            // ThingDef/inspectorTabs/ li.ITab_Pawn_Gear -> li.CombatExtended.ITab_Inventory
            if (race.inspectorTabs == null)
            {
                race.inspectorTabs = new List<Type>();
            }

            var newInventoryTab = typeof(CombatExtended.ITab_Inventory);

            var oldInventoryTab = race.inspectorTabs.FirstOrDefault(x => x.Name == "ITab_Pawn_Gear");
            if (oldInventoryTab != null)
            {
                race.inspectorTabs.Remove(oldInventoryTab);
            }

            race.inspectorTabs.Add(newInventoryTab);

            // ThingDef/comps/CombatExtended.CompProperties_Inventory
            if (race.comps == null)
            {
                trace.AppendLine("* Adding missing comps list");
                race.comps = new List<CompProperties>();
            }

            if (!race.comps.Any(x => x.compClass == typeof(CombatExtended.CompProperties_Inventory)))
            {
                race.comps.Add(new CompProperties(typeof(CombatExtended.CompProperties_Inventory)));
            }
        }
    }
}
