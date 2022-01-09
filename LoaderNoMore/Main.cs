﻿using BepInEx;
using R2API;
using RoR2;
using System;
using UnityEngine;

using System.Security;
using System.Security.Permissions;
using BepInEx.Configuration;
using RoR2.Skills;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace LoaderNoMore
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(
        "com.Borbo.LoaderNoMore",
        "LOADER IS NO MORE",
        "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<string> SurvivorsToRemove { get; set; }
        public static ConfigEntry<string> SkillsToRemove { get; set; }

        public void Awake()
        {
            InitializeConfig();

            On.RoR2.SurvivorCatalog.SetSurvivorDefs += NukeLoader;
            //On.RoR2.BodyCatalog.GetBodyPrefabSkillSlots += NukeIonSurge;
            NukeSurge();
        }

        private GenericSkill[] NukeIonSurge(On.RoR2.BodyCatalog.orig_GetBodyPrefabSkillSlots orig, BodyIndex bodyIndex)
        {
            string fullSkillString = SkillsToRemove.Value;
            fullSkillString = fullSkillString.Replace(" ", "");
            fullSkillString = fullSkillString.Replace("-", " ");
            string[] skillNames = fullSkillString.Split(',');

            return orig(bodyIndex);
        }

        internal static List<LoaderNoMoreSkillSlotData> allSkillSlotData = new List<LoaderNoMoreSkillSlotData>(0);

        internal struct LoaderNoMoreSkillSlotData
        {
            internal string bodyName;
            internal SkillFamily skillFamily;
            internal int advance;
            internal List<string> indices;
        }

        private void NukeSurge()
        {
            string fullSkillString = SkillsToRemove.Value;
            fullSkillString = fullSkillString.Replace(" ", "");
            fullSkillString = fullSkillString.Replace("-", " ");
            string[] skillNames = fullSkillString.Split(',');
            foreach (string nameToken in skillNames)
            {
                //Debug.Log(nameToken);
            }
            foreach (string nameToken in skillNames)
            {
                string[] substrings = nameToken.Split('_');
                foreach (string substring in substrings)
                {
                    Debug.Log(substring);
                }

                LoaderNoMoreSkillSlotData currentSlotData = new LoaderNoMoreSkillSlotData();
                SkillFamily skillFamily = null;
                foreach (LoaderNoMoreSkillSlotData skillSlotData in allSkillSlotData)
                {
                    SkillFamily family = skillSlotData.skillFamily;
                    if(skillSlotData.bodyName == substrings[0] && family.name == substrings[1])
                    {
                        Debug.Log("A");
                        currentSlotData = skillSlotData;
                        skillFamily = family;
                    }
                }

                if(skillFamily == null)
                {
                    Debug.Log("B");
                    GameObject body = Resources.Load<GameObject>("prefabs/characterbodies/" + substrings[0]);//BodyCatalog.FindBodyPrefab(substrings[0]);
                    if (body != null)
                    {
                        Debug.Log("C");
                        SkillLocator skillLocator = body.GetComponent<SkillLocator>();
                        if (skillLocator != null)
                        {
                            Debug.Log("D");
                            GenericSkill skillSlot = null;
                            bool doPassive = false;

                            switch (substrings[1].ToLower())
                            {
                                case "passive":
                                    doPassive = true;
                                    break;
                                case "primary":
                                    skillSlot = skillLocator.primary;
                                    break;
                                case "secondary":
                                    skillSlot = skillLocator.secondary;
                                    break;
                                case "utility":
                                    skillSlot = skillLocator.utility;
                                    break;
                                case "special":
                                    skillSlot = skillLocator.special;
                                    break;
                            }

                            if (doPassive)
                            {
                                SkillLocator.PassiveSkill passiveSkill = skillLocator.passiveSkill;
                                passiveSkill.enabled = false;
                            }
                            else if (skillSlot != null)
                            {
                                Debug.Log("E");
                                skillFamily = skillSlot.skillFamily;
                                currentSlotData.indices = new List<string>();
                                currentSlotData.skillFamily = skillFamily;
                                allSkillSlotData.Add(currentSlotData);
                            }
                        }
                    }
                }

                if(skillFamily != null)
                {
                    Debug.Log("F");
                    SkillFamily.Variant[] variants = skillFamily.variants;
                    string index = substrings[2];
                    Debug.Log(index);

                    currentSlotData.indices.Add(index);
                }
            }


            foreach (LoaderNoMoreSkillSlotData skillSlotData in allSkillSlotData)
            {
                SkillFamily skillFamily = skillSlotData.skillFamily;
                if(skillFamily != null)
                {
                    List<SkillFamily.Variant> variants = new List<SkillFamily.Variant>(skillFamily.variants);

                    int advance = 0;
                    int c = variants.Count;
                    for (int i = 0; i < c; i++)
                    {
                        Debug.Log("G");
                        bool match = false;
                        foreach (string index in skillSlotData.indices)
                        {
                            if (index == i.ToString())
                            {
                                match = true;
                                //Debug.Log($"Match found for {nameToken} in the survivor catalog! Removing!");
                            }
                        }

                        if (match)//skip this entry
                        {
                            Debug.Log("H");

                            //HG.ArrayUtils.ArrayRemoveAtAndResize(ref variants, i - advance);
                            variants.RemoveAt(i - advance);
                            advance++;
                        }
                        else if(advance > 0)//back up every other entry for each one skipped
                        {
                            Debug.Log("I");
                            
                            //HG.ArrayUtils.ArrayRemoveAtAndResize(ref variants, i - advance);
                            //variants[i - advance] = variants[i];
                        }
                    }

                    if(advance > 0)
                    {
                        skillFamily.variants = variants.ToArray();
                    }
                }
                else
                {
                    Debug.Log("saghfbaehgbrfhjrqwaef");
                }
            }
        }

        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\LoaderNoMore.cfg", true);

            SurvivorsToRemove = CustomConfigFile.Bind<string>("Loader Is No More", "Survivors", "LOADER_BODY_NAME, Loader-2",
                "For each SURVIVOR DEF you wish to hide from the lobby: " +
                "enter in the display name token from the Survivor Def; separated by commas. " +
                "Ex: 'LOADER_BODY_NAME, MAGE_BODY_NAME, COMMANDO_BODY_NAME' (spaces will be ignored, dashes will be turned to spaces).");

            SkillsToRemove = CustomConfigFile.Bind<string>("Loader Is No More", "Skills", "MageBody_Special_1, TreeBotBody_Utility_1",
                "For each SKILL DEF you wish to hide from the lobby: " +
                "enter in the Character Body name, then the skill slot name, then the skill index (starting at 0); separated by commas. " +
                "Ex: 'MageBody_Special_1, TreeBotBody_Utility_1' (spaces will be ignored, dashes will be turned to spaces).");
        }

        private void NukeLoader(On.RoR2.SurvivorCatalog.orig_SetSurvivorDefs orig, SurvivorDef[] newSurvivorDefs)
        {
            string fullSurvivorString = SurvivorsToRemove.Value;
            fullSurvivorString = fullSurvivorString.Replace(" ", "");
            fullSurvivorString = fullSurvivorString.Replace("-", " ");
            string[] survivorNames = fullSurvivorString.Split(',');
            foreach (string nameToken in survivorNames)
            {
                //Debug.Log(nameToken);
            }

            int advance = 0;
            int c = newSurvivorDefs.Length;
            for (int i = 0; i < c; i++)
            {
                SurvivorDef def = newSurvivorDefs[i];
                bool match = false;
                foreach (string nameToken in survivorNames)
                {
                    if (def.displayNameToken == nameToken)
                    {
                        match = true;
                        Debug.Log($"Match found for {nameToken} in the survivor catalog! Removing!");
                    }
                }
                if (match)
                {
                    advance++;
                }
                else if(advance > 0)
                {
                    newSurvivorDefs[i - advance] = newSurvivorDefs[i];
                }
                /*if (def.displayNameToken != "LOADER_BODY_NAME" && def.displayNameToken != "Loader 2")
                {
                    if(advance > 0)
                        newSurvivorDefs[i - advance] = newSurvivorDefs[i];
                }
                else
                {
                    advance++;
                }*/
            }
            Array.Resize(ref newSurvivorDefs, c - advance);

            orig(newSurvivorDefs);
        }
    }
}