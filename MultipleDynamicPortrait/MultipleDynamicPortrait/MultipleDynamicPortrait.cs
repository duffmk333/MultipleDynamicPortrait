using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Storm;
using Storm.ExternalEvent;
using Storm.StardewValley;
using Storm.StardewValley.Event;
using Storm.StardewValley.Wrapper;
using Storm.Collections;
using Storm.StardewValley.Accessor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace MultipleDynamicPortrait
{
    [Mod]
    public class MultipleDynamicPortrait : DiskResource
    {
        #region Fields
        public static PortraitConfig PConfig { get; private set; }
        public int LastNumberOfCharacters;
        public string LastLocation;
        public List<int> WeekPortraitRandomListIndex = new List<int>();
        public List<int> DayPortraitRandomListIndex = new List<int>();
        public List<int> RandomPortraitListIndex = new List<int>();
        public WrappedProxyList<NPCAccessor, NPC> TempCharacters{ get; set; }
        public Dictionary<string, PortratInternalSettings> PortraitsTextures = new Dictionary<string, PortratInternalSettings>();

        #endregion
        #region MainDictionaryInit
        public Dictionary<string, CharacterPortraitSettings> ListOfCharacterPortraits = new Dictionary<string, CharacterPortraitSettings>()
        {            
            {"Abigail", new CharacterPortraitSettings()
                {
                    ListOfPortraits = new List<PortraitSettings>()
                    {
                        new PortraitSettings()
                        {
                           MonthDays = new List<int>()
                           {

                           },
                           WeekDays = new List<DaysOfWeek>()
                           {

                           }
                        }
                    }
                }
            }
        };
        #endregion

        #region Debug Fields
        public string currentD;
        #endregion

        public MultipleDynamicPortrait()
        {
            LastNumberOfCharacters = 0;
            LastLocation = "";
            currentD = "";
        }

        #region Storm Events
        [Subscribe]
        public void InitializeCallback(InitializeEvent @e)
        {
            //Initialize config / json
            PConfig = new PortraitConfig(ListOfCharacterPortraits);
            PConfig = (PortraitConfig)Config.InitializeConfig(Config.GetBasePath(this), PConfig);            

            //loop through keys 
            foreach (var key in PConfig.ListOfCharacterPortraits.Keys)
            {
                for (int i = 0; i < PConfig.ListOfCharacterPortraits[key].ListOfPortraits.Count; i++)
                {
                    //to find which key have a valid path and a valid name
                    if (PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].Path != null && PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].FileName != null)
                    {
                        //add the key to texture Dictionary
                        if (!PortraitsTextures.ContainsKey(key))
                        {
                            PortraitsTextures.Add(key, new PortratInternalSettings() { TextureList = new List<Texture2D>(), CurrentTextureIndex = 0, Changed = false, StandardPortraitSaved = false });
                        }

                        //create a complete path to the texture
                        var path = Path.Combine(PathOnDisk, PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].Path + PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].FileName);

                        //loads it                       
                        PortraitsTextures[key].TextureList.Add(@e.Root.LoadResource(path));

                        //set current texture index of the key to default (obteined from json)
                        if (PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].SetAsDefaultPortrait)
                        {
                            PortraitsTextures[key].CurrentTextureIndex = i;
                        }

                        //debug
                        currentD += "Successful Loaded " + PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].FileName + " " + PConfig.ListOfCharacterPortraits[key].ListOfPortraits.Count + " Portraits\n";
                        currentD += "Current Index Portrait " + PortraitsTextures[key].CurrentTextureIndex.ToString() + "\n";

                    }
                }
            }

        }
        [Subscribe]
        public void debugger(PreUIRenderEvent @e)
        {
            if (PConfig.Debug)
            {
                var textLen = 50;
                var width = (textLen * 10);
                var root = @e.Root;
                var batch = root.SpriteBatch;
                var font = root.SmoothFont;
                var pos = new Vector2 { X = 100, Y = 100 };

                batch.DrawString(font, currentD, pos, Color.White);
            }
        }
        [Subscribe]
        public void UpdateCallback(PostUpdateEvent @e)
        {
            //verify if there's a new character into the scene or player changed location
            //to make sure the portraits will change properly needs double check
            //till find a better way
            if (LastNumberOfCharacters != e.Location.Characters.Count || LastLocation != e.Location.Name)
            {
                //loop through characters
                for(int i = 0; i < @e.Location.Characters.Count; i++)
                {
                    //verify if the character is into the list of new textures
                    if(PortraitsTextures.ContainsKey(@e.Location.Characters[i].Name))
                    {
                        var key = @e.Location.Characters[i].Name;

                        //verify if it needs to change
                        if (!PortraitsTextures[key].Changed)
                        {
                            //verify if the index is valid
                            if(PortraitsTextures[key].CurrentTextureIndex < PortraitsTextures[key].TextureList.Count)
                            {
                                var index = PortraitsTextures[key].CurrentTextureIndex;

                                //verify if the texture is valid
                                if (PortraitsTextures[key].TextureList[index] != null)
                                {
                                    //saves the Standard portrait if it isn't saved already
                                    if (!PortraitsTextures[key].StandardPortraitSaved)
                                    {
                                        //since the List is made first at the InitializeCallback
                                        //the Standard portrait is added to the last position of the list
                                        PortraitsTextures[key].TextureList.Add(@e.Location.Characters[i].Portrait);
                                        PortraitsTextures[key].StandardPortraitSaved = true;
                                    }

                                    //change de texture
                                    @e.Location.Characters[i].Portrait = PortraitsTextures[key].TextureList[index];

                                    //set the flag
                                    PortraitsTextures[key].Changed = true;

                                    //debug
                                    currentD += "Update " + key + " Portrait\n";
                                }
                            }
                        }
                    }
                    LastNumberOfCharacters = e.Location.Characters.Count;
                    LastLocation = e.Location.Name;
                }
                //currentD += "Characters: Temp " + TempLocationCharacters + " Current " + @e.Location.Characters.Count + "\n";
            }
        }        
        [Subscribe]
        public void NewDayCallback(PostNewDayEvent @e)
        {
            currentD = null; //debug

            //loop through keys 
            foreach (var key in PConfig.ListOfCharacterPortraits.Keys)
            {
                //verify if the key is valid
                if (PortraitsTextures.ContainsKey(key))
                {
                    //reset to default
                    ResetToDefaultPortrait(key);

                    //set the portrait index
                    SetPortraitIndex(e.Root.DayOfMonth, key);

                    //if the index isn't the same as before
                    if(PortraitsTextures[key].LastTextureIndex != PortraitsTextures[key].CurrentTextureIndex)
                    {
                        //set the flag to change it
                        PortraitsTextures[key].Changed = false;
                        PortraitsTextures[key].LastTextureIndex = PortraitsTextures[key].CurrentTextureIndex;
                    }
                }
            }
            currentD += "Week Day: " + GetWeekDay(e.Root.DayOfMonth).ToString() + "\n";
        }
        #endregion

        #region Main Fuctions        
        
        public void ResetToDefaultPortrait(string key)
        {
            //loop through json data to find and set the index of Default texture chosen by the user
            for (int i = 0; i < PConfig.ListOfCharacterPortraits[key].ListOfPortraits.Count; i++)
            {
                if (PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].SetAsDefaultPortrait)
                {
                    PortraitsTextures[key].CurrentTextureIndex = i;
                    currentD += key + " Default Portrait by user \n";
                }
            }
        }
        public void SetPortraitIndex(int day, string key)
        {
            //loop through json data, split the index portrait through 3 lists
            for (int i = 0; i < PConfig.ListOfCharacterPortraits[key].ListOfPortraits.Count; i++)
            {
                //month day
                if (PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].MonthDays.Contains(day))
                {
                    DayPortraitRandomListIndex.Add(i);
                    
                }
                //or week day
                else if (PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].WeekDays.Contains(GetWeekDay(day)))
                {
                    WeekPortraitRandomListIndex.Add(i);
                    
                }
                //or randons
                else if(PConfig.ListOfCharacterPortraits[key].ListOfPortraits[i].SetRandomDaily)
                {
                    RandomPortraitListIndex.Add(i);
                }               
            }
            //then
            SetRandomize(key);
            ClearTemp(key);
        }        
        public void SetRandomize(string key)
        {
            //verify if there's a index into month day list
            if (DayPortraitRandomListIndex.Count > 0)
            {
                //randomize and return one index
                PortraitsTextures[key].CurrentTextureIndex = DayPortraitRandomListIndex[Randomize(0, DayPortraitRandomListIndex.Count)];
                currentD += key + " Day Portrait \n";
            }
            //if isn't a index into the list of month day, it makes the same for the week day
            else if (WeekPortraitRandomListIndex.Count > 0)
            {
                PortraitsTextures[key].CurrentTextureIndex = WeekPortraitRandomListIndex[Randomize(0, WeekPortraitRandomListIndex.Count)];
                currentD += key + " Week Portrait \n";
            }            
            else if (RandomPortraitListIndex.Count > 0)
            {                
                if (PConfig.AllowStandardPortraitAtRandomDaily)
                {
                    RandomPortraitListIndex.Add(PConfig.ListOfCharacterPortraits[key].ListOfPortraits.Count - 1);
                }
                PortraitsTextures[key].CurrentTextureIndex = RandomPortraitListIndex[Randomize(0, RandomPortraitListIndex.Count)];
                currentD += key + " Random Portrait \n";
            }
        }
        public void ClearTemp(string key)
        {
            //clean all the lists
            WeekPortraitRandomListIndex.Clear();
            DayPortraitRandomListIndex.Clear();
            RandomPortraitListIndex.Clear();            
        }
        public DaysOfWeek GetWeekDay(int day)
        {
            //math to find week day
            if (day % 7 == 0)
            {
                return DaysOfWeek.Su;
            }
            else
            {
                return (DaysOfWeek)((day % 7) - 1);
            }
        }        
        public int Randomize(int min, int max)
        {
            return new Random().Next(min, max);
        }
        #endregion
    }

    public class PortratInternalSettings
    {
        public List<Texture2D> TextureList { get; set; }
        public bool Changed { get; set; }
        public bool StandardPortraitSaved { get; set; }
        public int CurrentTextureIndex { get; set; }
        public int LastTextureIndex { get; set; }
    }

    #region JsonStuff
    public enum DaysOfWeek
    {
        M,
        T,
        W,
        Th,
        F,
        Sa,
        Su

    }
    public class PortraitSettings
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        public bool SetAsDefaultPortrait { get; set; }
        public bool SetRandomDaily { get; set; }
        public List<DaysOfWeek> WeekDays { get; set; }
        public List<int> MonthDays { get; set; }

    }
    public class CharacterPortraitSettings
    {
        public List<PortraitSettings> ListOfPortraits { get; set; }
    }
    public class PortraitConfig : Config
    {
        public Dictionary<string, CharacterPortraitSettings> ListOfCharacterPortraits { get; set; }
        public bool Debug;
        public bool AllowStandardPortraitAtRandomDaily;
        private Dictionary<string, CharacterPortraitSettings> NewListOfCharacterPortraits;

        //kludge... till I find other way... 
        public PortraitConfig(Dictionary<string, CharacterPortraitSettings> NewListOfCharacterPortraits)
        {
            this.NewListOfCharacterPortraits = NewListOfCharacterPortraits;
        }
        public override Config GenerateBaseConfig(Config baseConfig)
        {
            ListOfCharacterPortraits = NewListOfCharacterPortraits;
            Debug = false;
            AllowStandardPortraitAtRandomDaily = false;
            return this;
        }
    }
    #endregion
}