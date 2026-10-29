using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public struct LocalizedString
{
    [TextArea(1, 8)] [SerializeField] private string english;
    [TextArea(1, 8)] [SerializeField] private string spanish;

    public LocalizedString(string english, string spanish)
    {
        this.english = english;
        this.spanish = spanish;
    }

    public string Value => Get(GameLocalization.Language);
    public string Get(GameLanguage language)
    {
        string value = language == GameLanguage.Spanish ? spanish : english;
        return string.IsNullOrEmpty(value) ? (language == GameLanguage.Spanish ? english : spanish) ?? "" : value;
    }
    public string Format(params object[] arguments) => string.Format(GameLocalization.Culture, Value, arguments);
    public bool HasBothLanguages => !string.IsNullOrEmpty(english) && !string.IsNullOrEmpty(spanish);
}
