﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using MountAndBlade.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using System.Reflection;
using StoryMode;
using StoryMode.Behaviors.Quests.FirstPhase;
using StoryMode.CharacterCreationSystem;
using TaleWorlds.Localization;

namespace zCulturedStart
{
    class CSApplyChoices : CSCharCreationOption
    {
        public static void ApplyStoryOptions()
        {
            //Take away all the stuff to apply to each option
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Hero.MainHero.Gold, true);
            PartyBase.MainParty.ItemRoster.RemoveAllItems();
            int Option = CSCharCreationOption.CSSelectOption;
            switch (Option){
                case 1: //Default
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2, true);
                    break;
                case 2: //Merchant
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 800, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), 5);
                    CSAddTroop(1, 5);
                    CSAddTroop(2, 3);
                    break;
                case 3: //Exiled Option
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1500, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2, true);
                    AddExiledHero();
                    ExiledDmgRelation();
                    break;
                case 4: // Merc
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 125, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1, true);
                    CSAddTroop(1, 10);
                    CSAddTroop(2, 5);
                    CSAddTroop(3, 3);
                    CSAddTroop(4, 1);
                    MobileParty.MainParty.RecentEventsMorale += -40;
                    Hero.MainHero.BattleEquipment.FillFrom((from x in CharacterObject.All
                                                            where x.Tier == 3 && x.Culture.StringId == Hero.MainHero.Culture.StringId && !x.IsHero && (x.Occupation == Occupation.Soldier || x.Occupation == Occupation.Mercenary)
                                                            select x).GetRandomElement<CharacterObject>().Equipment);
                    break;
                case 5: // looter
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 40, true);
                    CSAddLooter(7);
                    foreach (Kingdom x in Campaign.Current.Kingdoms)
                    {
                        ChangeCrimeRatingAction.Apply(x.MapFaction, 35, false);
                       // DeclareWarAction.Apply(x, Hero.MainHero.MapFaction);
                    }
                    
                    break;
                default:
                    break;
            }            
        }
        private static void CSAddTroop(int Tier, int num)
        {
            CharacterObject characterObject = (from x in CharacterObject.All
                                               where x.Tier == Tier && x.Culture.StringId == Hero.MainHero.Culture.StringId && !x.IsHero && (x.Occupation == Occupation.Soldier || x.Occupation == Occupation.Mercenary)
                                               select x).GetRandomElement<CharacterObject>();                                             

            PartyBase.MainParty.AddElementToMemberRoster(characterObject, num, false);            
        }
        private static void CSAddLooter(int num) //Dual purpose cause lazy, adds loots and set players gear as looter
        {
            CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            PartyBase.MainParty.AddElementToMemberRoster(characterObject, num, false);
            Hero.MainHero.BattleEquipment.FillFrom(characterObject.Equipment);
            Hero.MainHero.CivilianEquipment.FillFrom(characterObject.Equipment);
        }
        
        private static void AddExiledHero()
        {
            Hero mainhero = Hero.MainHero;
            CharacterObject wanderer = (from x in CharacterObject.Templates
                                        where x.Occupation == Occupation.Wanderer && x.Culture.StringId == mainhero.Culture.StringId
                                        select x).GetRandomElement<CharacterObject>();            
            
            Equipment equipment = (from y in CharacterObject.All
                                   where y.Level >20 && y.Culture.StringId == wanderer.Culture.StringId && !y.IsHero && y.Tier > 4
                                   select y).GetRandomElement<CharacterObject>().Equipment;
            Equipment equipmentMC = (from z in CharacterObject.All
                                     where z.Tier == 4 && z.Culture.StringId == wanderer.Culture.StringId && !z.IsHero 
                                     select z).GetRandomElement<CharacterObject>().Equipment;
            Settlement randomElement = (from settlement in Settlement.All
                                        where settlement.Culture == wanderer.Culture && settlement.IsTown
                                        select settlement).GetRandomElement<Settlement>();
            //wanderer.Equipment.FillFrom(equipment);
            Hero hero = HeroCreator.CreateSpecialHero(wanderer, randomElement, null, null, 33);
            Campaign.Current.GetCampaignBehavior<IHeroCreationCampaignBehavior>().DeriveSkillsFromTraits(hero, wanderer);
            GiveGoldAction.ApplyBetweenCharacters(null, hero, 2000, true);
            hero.BattleEquipment.FillFrom(equipment);
            mainhero.BattleEquipment.FillFrom(equipmentMC);
            hero.HasMet = true;
            hero.Clan = randomElement.OwnerClan;
            hero.ChangeState(Hero.CharacterStates.Active);
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, true);
            CampaignEventDispatcher.Instance.OnHeroCreated(hero, false);
        }
        private static void ExiledDmgRelation()
        {
            Hero mainhero = Hero.MainHero;
            Hero lord = Hero.FindAll((Hero tmp) => (tmp.Culture.StringId == mainhero.Culture.StringId) && tmp.IsAlive && tmp.IsFactionLeader && !tmp.MapFaction.IsMinorFaction).GetRandomElement<Hero>();
            CharacterRelationManager.SetHeroRelation(mainhero, lord, -50);
            foreach (Hero alllord in Hero.FindAll((alllord) => (alllord.MapFaction == lord.MapFaction) && alllord.IsAlive))
            {
                CharacterRelationManager.SetHeroRelation(mainhero, alllord, -5);
            };
            CharacterRelationManager.SetHeroRelation(mainhero, lord, -50);
            ChangeCrimeRatingAction.Apply(lord.MapFaction, 35, false);
            //float test = Campaign.Current.

        }
    }
}
