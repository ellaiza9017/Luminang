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
    public string ilokano_target;
    public string cebuano_target;
    public List<string> ilokano_required_tokens;
}
