using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace PoisonIvy
{
    public class PoisonIvyCoating : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt?.Weapon == null || !Wields(evt.Weapon))
            {
                return;
            }

            var level = DruidLevel();
            var dice = level >= 20
                ? new DiceFormula(2, DiceType.D6)
                : level >= 14
                    ? new DiceFormula(1, DiceType.D6)
                    : new DiceFormula(1, DiceType.D3);
            evt.DamageDescription.Add(new DamageDescription
            {
                TypeDescription = new DamageTypeDescription
                {
                    Type = DamageType.Energy,
                    Energy = DamageEnergyType.Acid,
                    Common = new DamageTypeDescription.CommomData(),
                    Physical = new DamageTypeDescription.PhysicalData()
                },
                Dice = dice,
                SourceFact = Fact
            });
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        bool Wields(ItemEntity weapon)
        {
            var body = Owner?.Body;
            if (body == null)
            {
                return false;
            }

            if (body.PrimaryHand.HasWeapon)
            {
                if (body.PrimaryHand.MaybeWeapon == weapon)
                {
                    return true;
                }
            }
            else if (body.EmptyHandWeapon == weapon)
            {
                return true;
            }

            if (body.SecondaryHand.MaybeWeapon == weapon)
            {
                return true;
            }

            if (body.AdditionalLimbs == null)
            {
                return false;
            }

            foreach (var limb in body.AdditionalLimbs)
            {
                if (limb?.MaybeWeapon == weapon)
                {
                    return true;
                }
            }

            return false;
        }

        int DruidLevel()
        {
            var druid = Content.Druid;
            if (druid == null || Owner?.Progression == null)
            {
                return 6;
            }

            var level = Owner.Progression.GetClassLevel(druid);
            return level < 1 ? 1 : level;
        }
    }

    public class PoisonIvyAlchemicalBonus : UnitFactComponentDelegate, IOwnerGainLevelHandler
    {
        public StatType Stat;

        protected override void OnTurnOn()
        {
            Refresh();
        }

        protected override void OnTurnOff()
        {
            Owner?.Stats?.GetStat(Stat)?.RemoveModifiersFrom(Runtime);
        }

        public void HandleUnitGainLevel()
        {
            Refresh();
        }

        void Refresh()
        {
            var stat = Owner?.Stats?.GetStat(Stat);
            if (stat == null)
            {
                return;
            }

            stat.RemoveModifiersFrom(Runtime);
            var bonus = Bonus();
            if (bonus > 0)
            {
                stat.AddModifierUnique(bonus, Runtime, ModifierDescriptor.Alchemical);
            }
        }

        int Bonus()
        {
            var druid = Content.Druid;
            var level = druid == null || Owner?.Progression == null ? 0 : Owner.Progression.GetClassLevel(druid);
            if (level >= 20)
            {
                return 3;
            }

            if (level >= 16)
            {
                return 2;
            }

            if (level >= 10)
            {
                return 1;
            }

            return 0;
        }
    }
}
