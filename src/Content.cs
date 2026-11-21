using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UI.GenericSlot;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace PoisonIvy
{
    static class Content
    {
        public static BlueprintCharacterClass Druid;

        const string ArchetypeGuid = "b7e2c4a19d3f4c6e8a0b1d2e3f4a5b6c";
        const string FeatureGuid = "b7e2c4a19d3f4c6e8a0b1d2e3f4a5b6d";
        const string AbilityGuid = "b7e2c4a19d3f4c6e8a0b1d2e3f4a5b6e";
        const string BuffGuid = "b7e2c4a19d3f4c6e8a0b1d2e3f4a5b6f";
        const string WeaponFxGuid = "b7e2c4a19d3f4c6e8a0b1d2e3f4a5b70";
        const string CorrosiveGuid = "633b38ff1d11de64a91d490c683ab1c8";

        const string DruidGuid = "610d836f3a3a9ed42a4349b62f002e96";
        const string NatureFangGuid = "f50ef8e6ceda4277a6c672f8a7ba0ffb";
        const string StudiedTargetBuffGuid = "45548967b714e254aa83f23354f174b0";
        const string Talent2Guid = "04430ad24988baa4daa0bcd4f1c7d118";
        const string Talent6Guid = "43d1b15873e926848be2abf0ea3ad9a8";
        const string Talent10Guid = "913b9cf25c9536949b43a2651b7ffb66";
        const string PainfulFallbackGuid = "052c82521039f7b4e978207eb533ead5";
        const string PainfulCloneGuid = "c3b7e1a24d6f4e8a9b0c1d2e3f4a5b61";
        const string VenomIconGuid = "5078622eb5cecaf4683fa16a9b948c2c";

        const string Description =
            "Druids who dwell in underground caverns. From the poisons of herbs and roots they learned the magic that commands the venom of plants. Through their hands they work magical infusions, drawing extra power from poisons and acids.";

        const string AbilityText =
            "Toggle Poison Ivy to infuse any weapon you wield with plant venom. The weapon deals 1d3 extra acid damage, 1d6 acid at 14th level, and 2d6 acid at 20th level. At 10th level you gain a +1 alchemical bonus to Wisdom and Intelligence while this infusion is on. The bonus increases to +2 at 16th level and +3 at 20th level.";

        public static void Create()
        {
            Druid = BlueprintUtil.Load<BlueprintCharacterClass>(DruidGuid);
            var natureFang = BlueprintUtil.Load<BlueprintArchetype>(NatureFangGuid);
            if (Druid == null || natureFang == null)
            {
                Main.Log("Poison Ivy needs the Druid class and Nature Fang from Tabletop Tweaks.");
                return;
            }

            if (HasArchetype(Druid, ArchetypeGuid))
            {
                return;
            }

            var archetype = BuildArchetype(natureFang);
            var added = BlueprintUtil.CloneEntries(natureFang.AddFeatures);
            var removed = BlueprintUtil.CloneEntries(natureFang.RemoveFeatures);
            DropAt(added, 4, Talent2Guid);
            DropAt(added, 6, Talent6Guid);
            DropAt(added, 10, Talent10Guid);
            DropAt(added, 14, Talent10Guid);
            DropAt(added, 18, Talent10Guid);

            var painful = FindPainfulStrike();
            if (painful == null)
            {
                Main.Log("Painful Strike was not found on the slayer talent list.");
            }
            else
            {
                BlueprintUtil.AddAtLevel(added, 4, AdaptPainfulStrike(painful));
            }

            try
            {
                var poisonIvy = CreatePoisonIvy();
                BlueprintUtil.AddAtLevel(added, 6, poisonIvy);
                Main.Log("Poison Ivy toggle added at level 6.");
            }
            catch (Exception ex)
            {
                Main.Log("Poison Ivy toggle failed: " + ex);
            }

            archetype.AddFeatures = added.OrderBy(entry => entry.Level).ToArray();
            archetype.RemoveFeatures = removed.OrderBy(entry => entry.Level).ToArray();
            BlueprintUtil.Register(archetype);
            AddArchetype(Druid, archetype);
            try
            {
                ShareStudiedTarget(archetype);
                AllowTalent("ca5274d057152fa45b7527cad0927840", archetype, 4);
                AllowTalent("e821c61b2711cea4cb993725b910e7e8", archetype, 12);
                AllowTalent("61f82ba786fe05643beb3cd3910233a8", archetype, 6);
                AllowTalent("78177315fc63b474ea3cbb8df38fafcd", archetype, 10);
            }
            catch (Exception ex)
            {
                Main.Log("Poison Ivy talent access failed: " + ex);
            }
        }

        static BlueprintArchetype BuildArchetype(BlueprintArchetype natureFang)
        {
            var archetype = BlueprintUtil.Create<BlueprintArchetype>();
            BlueprintUtil.CopyMergedFields(natureFang, archetype);
            archetype.name = "PoisonIvyArchetype";
            archetype.AssetGuid = BlueprintGuid.Parse(ArchetypeGuid);
            BlueprintUtil.Set(archetype, "m_ParentClass", Druid);
            BlueprintUtil.Set(archetype, "m_HiddenInUI", false);
            archetype.LocalizedName = BlueprintUtil.MakeString("PoisonIvy.Archetype.Name", "Poison Ivy");
            archetype.LocalizedDescription = BlueprintUtil.MakeString("PoisonIvy.Archetype.Desc", Description);
            archetype.LocalizedDescriptionShort = BlueprintUtil.MakeString(
                "PoisonIvy.Archetype.Short",
                "A cavern druid whose hands infuse weapons with the venom of plants.");
            return archetype;
        }

        static BlueprintFeature CreatePoisonIvy()
        {
            var icon = BlueprintUtil.LoadAny(VenomIconGuid) as BlueprintUnitFact;
            var buff = BlueprintUtil.Create<BlueprintBuff>();
            buff.name = "PoisonIvyBuff";
            buff.AssetGuid = BlueprintGuid.Parse(BuffGuid);
            BlueprintUtil.SetName(buff, "PoisonIvy.Buff", "Poison Ivy", AbilityText);
            BlueprintUtil.CopyIcon(icon, buff);
            var wisdom = new PoisonIvyAlchemicalBonus { name = "PoisonIvyWisdom", Stat = StatType.Wisdom };
            var intelligence = new PoisonIvyAlchemicalBonus { name = "PoisonIvyIntelligence", Stat = StatType.Intelligence };
            var coating = new PoisonIvyCoating { name = "PoisonIvyCoating" };
            BlueprintUtil.Own(wisdom, buff);
            BlueprintUtil.Own(intelligence, buff);
            BlueprintUtil.Own(coating, buff);
            var components = new List<BlueprintComponent> { coating, wisdom, intelligence };
            var glow = WeaponGlow();
            if (glow != null)
            {
                components.Add(WeaponSlotGlow(glow, buff, EquipSlotBase.SlotType.PrimaryHand));
                components.Add(WeaponSlotGlow(glow, buff, EquipSlotBase.SlotType.SecondaryHand));
            }

            BlueprintUtil.Set(buff, "Components", components.ToArray());
            BlueprintUtil.Register(buff);

            var toggle = BlueprintUtil.Create<BlueprintActivatableAbility>();
            toggle.name = "PoisonIvyToggle";
            toggle.AssetGuid = BlueprintGuid.Parse(AbilityGuid);
            BlueprintUtil.SetName(toggle, "PoisonIvy.Toggle", "Poison Ivy", AbilityText);
            BlueprintUtil.CopyIcon(icon, toggle);
            BlueprintUtil.Set(toggle, "m_Buff", buff.ToReference<BlueprintBuffReference>());
            toggle.IsOnByDefault = true;
            toggle.DoNotTurnOffOnRest = true;
            toggle.ActivationType = AbilityActivationType.Immediately;
            toggle.DeactivateIfCombatEnded = false;
            toggle.OnlyInCombat = false;
            toggle.DeactivateIfOwnerDisabled = false;
            toggle.DeactivateIfOwnerUnconscious = false;
            BlueprintUtil.Set(toggle, "Components", new BlueprintComponent[0]);
            BlueprintUtil.Register(toggle);

            var feature = BlueprintUtil.Create<BlueprintFeature>();
            feature.name = "PoisonIvyFeature";
            feature.AssetGuid = BlueprintGuid.Parse(FeatureGuid);
            BlueprintUtil.SetName(feature, "PoisonIvy.Feature", "Poison Ivy", AbilityText);
            BlueprintUtil.CopyIcon(icon, feature);
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            var facts = new AddFacts { name = "PoisonIvyFacts" };
            BlueprintUtil.Set(facts, "m_Facts", new[] { toggle.ToReference<BlueprintUnitFactReference>() });
            BlueprintUtil.Own(facts, feature);
            BlueprintUtil.Set(feature, "Components", new BlueprintComponent[] { facts });
            BlueprintUtil.Register(feature);
            return feature;
        }

        static BlueprintWeaponEnchantment WeaponGlow()
        {
            var corrosive = BlueprintUtil.Load<BlueprintWeaponEnchantment>(CorrosiveGuid);
            if (corrosive == null)
            {
                Main.Log("Corrosive weapon effect was not found. Poison Ivy will not glow.");
                return null;
            }

            var glow = BlueprintUtil.Create<BlueprintWeaponEnchantment>();
            BlueprintUtil.CopyMergedFields(corrosive, glow);
            glow.name = "PoisonIvyWeaponFx";
            glow.AssetGuid = BlueprintGuid.Parse(WeaponFxGuid);
            BlueprintUtil.Set(glow, "m_EnchantName", BlueprintUtil.MakeString("PoisonIvy.WeaponFx.Name", "Poison Ivy"));
            BlueprintUtil.Set(glow, "m_Prefix", BlueprintUtil.MakeString("PoisonIvy.WeaponFx.Prefix", ""));
            BlueprintUtil.Set(glow, "m_Suffix", BlueprintUtil.MakeString("PoisonIvy.WeaponFx.Suffix", ""));
            BlueprintUtil.Set(glow, "m_EnchantmentCost", 0);
            BlueprintUtil.Set(glow, "m_IdentifyDC", 0);
            BlueprintUtil.Set(glow, "m_HiddenInUI", true);
            BlueprintUtil.Set(glow, "Components", new BlueprintComponent[0]);
            BlueprintUtil.Register(glow);
            return glow;
        }

        static BuffEnchantAnyWeapon WeaponSlotGlow(BlueprintWeaponEnchantment glow, BlueprintBuff buff, EquipSlotBase.SlotType slot)
        {
            var component = new BuffEnchantAnyWeapon { name = "PoisonIvyGlow" + slot, Slot = slot };
            BlueprintUtil.Set(component, "m_EnchantmentBlueprint", glow.ToReference<BlueprintItemEnchantmentReference>());
            BlueprintUtil.Own(component, buff);
            return component;
        }

        static BlueprintFeature FindPainfulStrike()
        {
            foreach (var guid in new[] { Talent2Guid, Talent6Guid, Talent10Guid })
            {
                var selection = BlueprintUtil.LoadAny(guid) as BlueprintFeatureSelection;
                var field = selection == null ? null : AccessTools.Field(selection.GetType(), "m_AllFeatures");
                var references = field?.GetValue(selection) as IEnumerable;
                if (references == null)
                {
                    continue;
                }

                foreach (var reference in references)
                {
                    var feature = Resolve(reference) as BlueprintFeature;
                    if (feature?.name != null && feature.name.IndexOf("Painful", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Main.Log("Painful Strike taken from the slayer talent list: " + feature.name + ".");
                        return feature;
                    }
                }
            }

            var fallback = BlueprintUtil.LoadAny(PainfulFallbackGuid) as BlueprintFeature;
            if (fallback != null)
            {
                Main.Log("Painful Strike uses " + fallback.name + ".");
            }

            return fallback;
        }

        static BlueprintFeature AdaptPainfulStrike(BlueprintFeature source)
        {
            var clone = (BlueprintFeature)BlueprintUtil.Clone(source, "PoisonIvyPainfulStrike", PainfulCloneGuid);
            var cloned = new Dictionary<string, BlueprintScriptableObject>(StringComparer.OrdinalIgnoreCase)
            {
                [Norm(source.AssetGuid.ToString())] = clone
            };
            RetargetRanks(clone);
            RewriteReferences(clone, cloned, 0, new HashSet<object>(new ReferenceComparer()));
            DescribePainfulStrike(clone);
            Main.Log("Painful Strike save DC uses druid level.");
            return clone;
        }

        static void DescribePainfulStrike(BlueprintFeature feature)
        {
            var description = AccessTools.Field(feature.GetType(), "m_Description")?.GetValue(feature);
            var text = description?.ToString();
            if (string.IsNullOrEmpty(text) || text.IndexOf("executioner", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var updated = System.Text.RegularExpressions.Regex.Replace(text, "executioner's", "druid's", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            updated = System.Text.RegularExpressions.Regex.Replace(updated, "executioner", "druid", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            BlueprintUtil.SetName(feature, "PoisonIvy.PainfulStrike", "Painful Strike", updated);
        }

        static void RetargetRanks(BlueprintScriptableObject blueprint)
        {
            if (blueprint?.ComponentsArray == null)
            {
                return;
            }

            foreach (var rank in blueprint.ComponentsArray.OfType<ContextRankConfig>())
            {
                var classes = AccessTools.Field(typeof(ContextRankConfig), "m_Class");
                if (classes != null)
                {
                    var replacement = Array.CreateInstance(classes.FieldType.GetElementType(), 1);
                    replacement.SetValue(Druid.ToReference<BlueprintCharacterClassReference>(), 0);
                    classes.SetValue(rank, replacement);
                }

                var additional = AccessTools.Field(typeof(ContextRankConfig), "m_AdditionalArchetypes");
                var additionalValue = additional?.GetValue(rank) as Array;
                if (additional != null && additionalValue != null && additionalValue.Length > 0)
                {
                    additional.SetValue(rank, Array.CreateInstance(additional.FieldType.GetElementType(), 0));
                }

                var archetype = AccessTools.Field(typeof(ContextRankConfig), "Archetype")
                    ?? AccessTools.Field(typeof(ContextRankConfig), "m_Archetype");
                if (archetype != null && !archetype.FieldType.IsValueType)
                {
                    archetype.SetValue(rank, Activator.CreateInstance(archetype.FieldType));
                }
            }
        }

        static void RewriteReferences(object target, Dictionary<string, BlueprintScriptableObject> cloned, int depth, HashSet<object> seen)
        {
            if (target == null || depth > 5 || target is string || !seen.Add(target))
            {
                return;
            }

            var type = target.GetType();
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal))
            {
                return;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType.IsPrimitive || field.FieldType.IsEnum)
                {
                    continue;
                }

                var value = field.GetValue(target);
                if (value == null)
                {
                    continue;
                }

                if (value is Array array)
                {
                    var elementType = field.FieldType.GetElementType();
                    if (elementType == null || elementType.IsValueType)
                    {
                        continue;
                    }

                    for (var i = 0; i < array.Length; i++)
                    {
                        var item = array.GetValue(i);
                        var replacement = ReplacementFor(item, cloned, depth, seen);
                        if (replacement != null)
                        {
                            array.SetValue(replacement, i);
                        }
                        else if (item != null && !(item is string))
                        {
                            RewriteReferences(item, cloned, depth + 1, seen);
                        }
                    }
                }
                else
                {
                    var replacement = ReplacementFor(value, cloned, depth, seen);
                    if (replacement != null)
                    {
                        field.SetValue(target, replacement);
                    }
                    else if (!field.FieldType.IsValueType)
                    {
                        RewriteReferences(value, cloned, depth + 1, seen);
                    }
                }
            }
        }

        static object ReplacementFor(object reference, Dictionary<string, BlueprintScriptableObject> cloned, int depth, HashSet<object> seen)
        {
            if (reference == null || reference is BlueprintScriptableObject || reference is string)
            {
                return null;
            }

            var getter = reference.GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (getter == null || !reference.GetType().Name.EndsWith("Reference", StringComparison.Ordinal))
            {
                return null;
            }

            var resolved = getter.Invoke(reference, null) as BlueprintScriptableObject;
            if (resolved == null)
            {
                return null;
            }

            var key = Norm(resolved.AssetGuid.ToString());
            if (!cloned.TryGetValue(key, out var copy))
            {
                if (resolved.ComponentsArray == null || !resolved.ComponentsArray.OfType<ContextRankConfig>().Any())
                {
                    return null;
                }

                copy = BlueprintUtil.Clone(resolved, "PoisonIvy" + resolved.name, StableGuid(key));
                cloned[key] = copy;
                RetargetRanks(copy);
                RewriteReferences(copy, cloned, depth + 1, seen);
            }

            return MakeReference(copy, reference.GetType());
        }

        static object MakeReference(BlueprintScriptableObject blueprint, Type referenceType)
        {
            var method = SafeTypes(typeof(BlueprintScriptableObject).Assembly)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .First(candidate => candidate.Name == "ToReference" && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 1);
            return method.MakeGenericMethod(referenceType).Invoke(null, new object[] { blueprint });
        }

        static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        static string StableGuid(string sourceGuid)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("PoisonIvy:" + sourceGuid));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        static string Norm(string guid)
        {
            return guid == null ? "" : guid.Replace("-", "");
        }

        sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        static void ShareStudiedTarget(BlueprintArchetype archetype)
        {
            var buff = BlueprintUtil.Load<BlueprintBuff>(StudiedTargetBuffGuid);
            var rank = BlueprintUtil.Component<ContextRankConfig>(buff);
            if (rank == null)
            {
                Main.Log("Studied Target could not count Poison Ivy levels.");
                return;
            }

            AppendReference(rank, "m_Class", Druid.ToReference<BlueprintCharacterClassReference>(), Druid.AssetGuid);
            AppendReference(rank, "m_AdditionalArchetypes", archetype.ToReference<BlueprintArchetypeReference>(), archetype.AssetGuid);
        }

        static void AppendReference(object target, string fieldName, object reference, BlueprintGuid guid)
        {
            var field = AccessTools.Field(target.GetType(), fieldName);
            var current = field.GetValue(target) as Array;
            var list = new List<object>();
            if (current != null)
            {
                foreach (var item in current)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var itemGuid = item.GetType().GetProperty("Guid")?.GetValue(item, null)?.ToString();
                    if (Same(itemGuid, guid.ToString()))
                    {
                        return;
                    }

                    list.Add(item);
                }
            }

            list.Add(reference);
            var replacement = Array.CreateInstance(field.FieldType.GetElementType(), list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                replacement.SetValue(list[i], i);
            }

            field.SetValue(target, replacement);
        }

        static void AllowTalent(string featureGuid, BlueprintArchetype archetype, int level)
        {
            var feature = BlueprintUtil.LoadAny(featureGuid) as BlueprintFeature;
            if (feature == null)
            {
                Main.Log("Missing talent " + featureGuid + ".");
                return;
            }

            var prerequisite = new PrerequisiteArchetypeLevel { name = "PoisonIvyAccess" + level };
            BlueprintUtil.Set(prerequisite, "m_CharacterClass", Druid.ToReference<BlueprintCharacterClassReference>());
            BlueprintUtil.Set(prerequisite, "m_Archetype", archetype.ToReference<BlueprintArchetypeReference>());
            BlueprintUtil.Set(prerequisite, "Level", level);
            var group = AccessTools.Field(prerequisite.GetType(), "Group") ?? AccessTools.Field(typeof(Prerequisite), "Group");
            if (group != null && group.FieldType.IsEnum)
            {
                group.SetValue(prerequisite, Enum.ToObject(group.FieldType, 1));
            }

            BlueprintUtil.Own(prerequisite, feature);
            var components = feature.ComponentsArray?.ToList() ?? new List<BlueprintComponent>();
            components.Add(prerequisite);
            BlueprintUtil.Set(feature, "Components", components.ToArray());
        }

        static void DropAt(List<LevelEntry> entries, int level, string guid)
        {
            var entry = entries.FirstOrDefault(candidate => candidate.Level == level);
            if (entry?.Features == null)
            {
                return;
            }

            var kept = new List<BlueprintFeatureBase>();
            foreach (var feature in entry.Features)
            {
                if (feature != null && !Same(feature.AssetGuid.ToString(), guid))
                {
                    kept.Add(feature);
                }
            }

            entry.SetFeatures(kept);
            Main.Log("Poison Ivy drops the slayer talent at level " + level + ".");
        }

        static bool HasArchetype(BlueprintCharacterClass characterClass, string guid)
        {
            var field = AccessTools.Field(typeof(BlueprintCharacterClass), "m_Archetypes");
            var current = (BlueprintArchetypeReference[])field.GetValue(characterClass) ?? new BlueprintArchetypeReference[0];
            return current.Any(reference => reference != null && Same(reference.Guid.ToString(), guid));
        }

        static void AddArchetype(BlueprintCharacterClass characterClass, BlueprintArchetype archetype)
        {
            var field = AccessTools.Field(typeof(BlueprintCharacterClass), "m_Archetypes");
            var current = (BlueprintArchetypeReference[])field.GetValue(characterClass) ?? new BlueprintArchetypeReference[0];
            var list = current.ToList();
            list.Add(archetype.ToReference<BlueprintArchetypeReference>());
            field.SetValue(characterClass, list.ToArray());
            Main.Log("Poison Ivy is on the Druid archetype list (" + list.Count + ").");
        }

        static BlueprintScriptableObject Resolve(object reference)
        {
            if (reference == null)
            {
                return null;
            }

            if (reference is BlueprintScriptableObject blueprint)
            {
                return blueprint;
            }

            var method = reference.GetType().GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            return method?.Invoke(reference, null) as BlueprintScriptableObject;
        }

        static bool Same(string left, string right)
        {
            string Clean(string value)
            {
                return value == null ? "" : value.Replace("-", "");
            }

            return string.Equals(Clean(left), Clean(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}
