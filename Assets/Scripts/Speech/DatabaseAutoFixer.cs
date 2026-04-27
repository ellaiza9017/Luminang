using UnityEngine;

namespace Luminang.Speech
{
    public class DatabaseAutoFixer : MonoBehaviour
    {
        public PhraseDatabase database;

        [ContextMenu("Run Fix Now")]
        public void RunFix()
        {
            if (database == null)
            {
                database = Resources.Load<PhraseDatabase>("PhraseDatabase");
                if (database == null)
                {
                    Debug.LogError("Could not find PhraseDatabase asset. Please make sure it is in your project!");
                    return;
                }
            }

            database.PopulateDefaultData();
            Debug.Log("Database has been FIXED and POPULATED automatically!");
        }
    }
}
