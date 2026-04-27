using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luminang.Speech
{
    [Serializable]
    public enum PhraseCategory
    {
        Greetings,
        Identity,
        Requests,
        Directions,
        Gratitude
    }

    [Serializable]
    public class Phrase
    {
        public string english;
        public string ilokano;
        public string cebuano;
        public string maranao;
        public PhraseCategory category;

        public string GetTextByLanguage(string language)
        {
            switch (language.ToLower())
            {
                case "ilokano": return ilokano;
                case "cebuano": return cebuano;
                case "maranao": return maranao;
                default: return english;
            }
        }
    }

    [CreateAssetMenu(fileName = "PhraseDatabase", menuName = "Luminang/Phrase Database")]
    public class PhraseDatabase : ScriptableObject
    {
        public List<Phrase> phrases = new List<Phrase>();

        [ContextMenu("Populate with Full Dataset")]
        public void PopulateDefaultData()
        {
            phrases.Clear();
            
            // --- GREETINGS ---
            AddPhrase("Good morning", "Naimbag a bigat", "Maayong buntag", "Mapia a kapipita", PhraseCategory.Greetings);
            AddPhrase("Good afternoon", "Naimbag a malem", "Maayong hapon", "Mapia a kaapon", PhraseCategory.Greetings);
            AddPhrase("Good evening", "Naimbag a rabii", "Maayong gabii", "Mapia a kagabii", PhraseCategory.Greetings);
            AddPhrase("How are you?", "Kumusta ka?", "Kumusta ka?", "Kapya ka?", PhraseCategory.Greetings);
            AddPhrase("I’m doing well", "Nasayaatak", "Maayo ra ko", "Mapia ako", PhraseCategory.Greetings);

            // --- IDENTITY ---
            AddPhrase("What is your name?", "Ania ti nagan mo?", "Unsay imong ngalan?", "Ngai ngaran ka?", PhraseCategory.Identity);
            AddPhrase("My name is ___", "Ti nagan ko ket ___", "Ako si ___", "So ngaran ko si ___", PhraseCategory.Identity);
            AddPhrase("Where are you from?", "Taga sadino ka?", "Taga asa ka?", "Taga anda ka?", PhraseCategory.Identity);
            AddPhrase("I am from ___", "Taga ___ ak", "Taga ___ ko", "Taga ___ ako", PhraseCategory.Identity);

            // --- REQUESTS ---
            AddPhrase("Can you help me?", "Mabalin kadi a tulunganak?", "Pwede ko nimo tabangan?", "Mapakay ka tabanga ako?", PhraseCategory.Requests);
            AddPhrase("Please help me", "Tulunganak man", "Tabangi ko palihug", "Tabanga ako", PhraseCategory.Requests);
            AddPhrase("Please wait for me", "Urayennak man", "Hulata ko palihug", "Antay ako", PhraseCategory.Requests);
            AddPhrase("Can I ask something?", "Mabalin kadi agsaludsod?", "Pwede ko mangutana?", "Pwede ako magtanong?", PhraseCategory.Requests);

            // --- DIRECTIONS ---
            AddPhrase("Please go straight", "Agdiretso ka man", "Padayon lang palihug", "Diretsu lang", PhraseCategory.Directions);
            AddPhrase("Please turn left", "Agliko ka iti kannigid", "Liko sa wala palihug", "Liko sa wala", PhraseCategory.Directions);
            AddPhrase("Please turn right", "Agliko ka iti kannawan", "Liko sa tuo palihug", "Liko sa tuo", PhraseCategory.Directions);
            AddPhrase("Please go up", "Umuli ka iti ngato", "Saka pataas palihug", "Saka pataas", PhraseCategory.Directions);
            AddPhrase("Please go down", "Bumaba ka man", "Naog paubos palihug", "Manaog", PhraseCategory.Directions);
            AddPhrase("Please stop here", "Agsardeng ka ditoy", "Hunong diri palihug", "Hinto dito", PhraseCategory.Directions);
            AddPhrase("Please come here", "Umay ka ditoy man", "Ari diri palihug", "Diri ka", PhraseCategory.Directions);
            AddPhrase("Please go there", "Mapan ka idiay man", "Adto didto palihug", "Lakad doon", PhraseCategory.Directions);
            AddPhrase("Please follow me", "Surotennak man", "Sunda ko palihug", "Sumunod ka", PhraseCategory.Directions);
            AddPhrase("Please wait here", "Uray ka ditoy man", "Hulata diri palihug", "Antay dito", PhraseCategory.Directions);

            // --- GRATITUDE ---
            AddPhrase("Thank you very much", "Agyamanak unay", "Daghang salamat", "Mapiya salamat", PhraseCategory.Gratitude);
            AddPhrase("Thank you for your help", "Agyamanak iti tulong mo", "Salamat sa imong tabang", "Salamat sa tulong mo", PhraseCategory.Gratitude);
            AddPhrase("I am sorry", "Pakawanen nak", "Pasayloa ko", "Pasensya ako", PhraseCategory.Gratitude);
            AddPhrase("Excuse me please", "Pakawanen nak man", "Pasayloa ko palihug", "Tabi lang", PhraseCategory.Gratitude);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            Debug.Log("Phrase Database fully populated with 28 phrases!");
        }

        private void AddPhrase(string eng, string ilo, string ceb, string mar, PhraseCategory cat)
        {
            phrases.Add(new Phrase { 
                english = eng, ilokano = ilo, cebuano = ceb, maranao = mar, category = cat 
            });
        }
    }
}
