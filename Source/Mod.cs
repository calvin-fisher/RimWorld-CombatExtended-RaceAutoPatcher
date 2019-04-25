using CombatExtended;
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

        public override void DefsLoaded()
        {
            if (!ModIsActive)
                return;

            var trace = new StringBuilder();
            try
            {
                trace.AppendLine("INITIALIZING CE RACES");

                var humanoidBodyShapeDef = DefDatabase<BodyShapeDef>.AllDefsListForReading.FirstOrDefault(x => x.defName == "Humanoid");
                if (humanoidBodyShapeDef == null)
                {
                    Logger.Error("Could not find humanoid body shape def");
                    return;
                }
                trace.AppendLine("Loaded CE Humanoid body shape");

                var combatExtendedStatBases = new[]
                {
                    CE_StatDefOf.AimingAccuracy,
                    CE_StatDefOf.MeleeCritChance,
                    CE_StatDefOf.MeleeParryChance,
                    CE_StatDefOf.ReloadSpeed,
                };
                trace.AppendLine("Loaded CE stat defs");

                var allAlienRaces = DefDatabase<AlienRace.ThingDef_AlienRace>.AllDefs.ToList();
                trace.AppendLine($"Found {allAlienRaces.Count} alien race defs");

                foreach (var race in allAlienRaces)
                {
                    trace.AppendLine($"***** Processing race {race} *****");
                    // TODO: Root body part hit points increased by 25%?

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
                            capacities = tool.capacities,
                            power = tool.power,
                            cooldownTime = tool.cooldownTime,
                            linkedBodyPartsGroup = tool.linkedBodyPartsGroup,
                            armorPenetration = 0.10f,
                        };
                        trace.AppendLine("Initialized new tool");

                        race.tools.Remove(tool);
                        trace.AppendLine("Removed existing tool");

                        race.tools.Add(newTool);
                        trace.AppendLine($"Added new tool {newTool}");
                    }

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
    }
}
