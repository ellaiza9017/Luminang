using System.Collections.Generic;
using UnityEngine;

namespace Luminang.SpeechValidation
{
    public enum PhraseCategory
    {
        Greetings,
        Identity,
        Requests,
        Directions,
        Gratitude
    }

    public enum NativeLanguage
    {
        Ilokano,
        Cebuano,
        Maranao
    }

    [System.Serializable]
    public class ValidPhrase
    {
        public string EnglishBase;
        public string NativeText;
        public PhraseCategory Category;
        public NativeLanguage Language;

        public ValidPhrase(string englishBase, string nativeText, PhraseCategory category, NativeLanguage language)
        {
            EnglishBase = englishBase;
            NativeText = nativeText;
            Category = category;
            Language = language;
        }
    }

    [CreateAssetMenu(fileName = "PhraseDatabase", menuName = "Luminang/PhraseDatabase")]
    public class PhraseDatabase : ScriptableObject
    {
        public List<ValidPhrase> phrases = new List<ValidPhrase>();

        [ContextMenu("Populate Default Dataset")]
        public void PopulateDefaultDataset()
        {
            phrases.Clear();

            // GREETINGS
            AddGroup("Good morning", PhraseCategory.Greetings, 
                "Naimbag a bigat", "Maayong buntag", "Mapia a kapipita");
            AddGroup("Good afternoon", PhraseCategory.Greetings, 
                "Naimbag a malem", "Maayong hapon", "Mapia a kaapon");
            AddGroup("Good evening", PhraseCategory.Greetings, 
                "Naimbag a rabii", "Maayong gabii", "Mapia a kagabii");
            AddGroup("How are you?", PhraseCategory.Greetings, 
                "Kumusta ka?", "Kumusta ka?", "Kapya ka?");
            AddGroup("I’m doing well", PhraseCategory.Greetings, 
                "Nasayaatak", "Maayo ra ko", "Mapia ako");

            // IDENTITY EXPRESSIONS
            AddGroup("What is your name?", PhraseCategory.Identity, 
                "Ania ti nagan mo?", "Unsay imong ngalan?", "Ngai ngaran ka?");
            AddGroup("My name is ___", PhraseCategory.Identity, 
                "Ti nagan ko ket ___", "Ako si ___", "So ngaran ko si ___");
            AddGroup("Where are you from?", PhraseCategory.Identity, 
                "Taga sadino ka?", "Taga asa ka?", "Taga anda ka?");
            AddGroup("I am from ___", PhraseCategory.Identity, 
                "Taga ___ ak", "Taga ___ ko", "Taga ___ ako");

            // REQUESTS
            AddGroup("Can you help me?", PhraseCategory.Requests, 
                "Mabalin kadi a tulunganak?", "Pwede ko nimo tabangan?", "Mapakay ka tabanga ako?");
            AddGroup("Please help me", PhraseCategory.Requests, 
                "Tulunganak man", "Tabangi ko palihug", "Tabanga ako");
            AddGroup("Please wait for me", PhraseCategory.Requests, 
                "Urayennak man", "Hulata ko palihug", "Antay ako");
            AddGroup("Can I ask something?", PhraseCategory.Requests, 
                "Mabalin kadi agsaludsod?", "Pwede ko mangutana?", "Pwede ako magtanong?");

            // DIRECTIONS
            AddGroup("Please go straight", PhraseCategory.Directions, 
                "Agdiretso ka man", "Padayon lang palihug", "Diretsu lang");
            AddGroup("Please turn left", PhraseCategory.Directions, 
                "Agliko ka iti kannigid", "Liko sa wala palihug", "Liko sa wala");
            AddGroup("Please turn right", PhraseCategory.Directions, 
                "Agliko ka iti kannawan", "Liko sa tuo palihug", "Liko sa tuo");
            AddGroup("Please go up", PhraseCategory.Directions, 
                "Umuli ka iti ngato", "Saka pataas palihug", "Saka pataas");
            AddGroup("Please go down", PhraseCategory.Directions, 
                "Bumaba ka man", "Naog paubos palihug", "Manaog");
            AddGroup("Please stop here", PhraseCategory.Directions, 
                "Agsardeng ka ditoy", "Hunong diri palihug", "Hinto dito");
            AddGroup("Please come here", PhraseCategory.Directions, 
                "Umay ka ditoy man", "Ari diri palihug", "Diri ka");
            AddGroup("Please go there", PhraseCategory.Directions, 
                "Mapan ka idiay man", "Adto didto palihug", "Lakad doon");
            AddGroup("Please follow me", PhraseCategory.Directions, 
                "Surotennak man", "Sunda ko palihug", "Sumunod ka");
            AddGroup("Please wait here", PhraseCategory.Directions, 
                "Uray ka ditoy man", "Hulata diri palihug", "Antay dito");

            // EXPRESSIONS OF GRATITUDE
            AddGroup("Thank you very much", PhraseCategory.Gratitude, 
                "Agyamanak unay", "Daghang salamat", "Mapiya salamat");
            AddGroup("Thank you for your help", PhraseCategory.Gratitude, 
                "Agyamanak iti tulong mo", "Salamat sa imong tabang", "Salamat sa tulong mo");
            AddGroup("I am sorry", PhraseCategory.Gratitude, 
                "Pakawanen nak", "Pasayloa ko", "Pasensya ako");
            AddGroup("Excuse me please", PhraseCategory.Gratitude, 
                "Pakawanen nak man", "Pasayloa ko palihug", "Tabi lang");
        }

        private void AddGroup(string english, PhraseCategory category, string ilokano, string cebuano, string maranao)
        {
            phrases.Add(new ValidPhrase(english, ilokano, category, NativeLanguage.Ilokano));
            phrases.Add(new ValidPhrase(english, cebuano, category, NativeLanguage.Cebuano));
            phrases.Add(new ValidPhrase(english, maranao, category, NativeLanguage.Maranao));
        }
    }
}
