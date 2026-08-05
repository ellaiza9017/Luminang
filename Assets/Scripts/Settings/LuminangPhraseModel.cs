using System;
using System.Collections.Generic;

[Serializable]
public class LuminangPhraseData
{
    public List<LuminangPhrase> phrases;
}

[Serializable]
public class LuminangPhrase
{
    public string id;
    public string category;
    public string type;
    public string english;
    public string ilokano;
    public string cebuano;
}
