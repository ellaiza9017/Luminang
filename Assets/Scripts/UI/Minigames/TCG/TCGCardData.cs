using System;
using System.Collections.Generic;

namespace Luminang.UI.Minigames
{
    [Serializable]
    public class TCGEnemyCardEntry
    {
        public string id;
        public string phraseId;
        public string spriteName;
        public string spritePath;
        public string situationText;
        public string category;
    }

    [Serializable]
    public class TCGEnemyCardDatabase
    {
        public List<TCGEnemyCardEntry> cards;
    }

    public class TCGRoundData
    {
        public TCGEnemyCardEntry enemyCard;
        public PhraseEntry phrase;
    }
}
