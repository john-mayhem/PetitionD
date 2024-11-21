// File: UI/ConsoleUI/FigletBuilder.cs
namespace PetitionD.UI.ConsoleUI;

public class FigletBuilder
{
    private string _text = "";

    public FigletBuilder AddText(string text)
    {
        _text = text;
        return this;
    }

    public FigletBuilder SetColor(ConsoleColor _)
    {
        return this;
    }

    public string Build()
    {
        return $@"
 ____       _   _ _   _             ____  
|  _ \ ___ | |_(_) |_(_) ___  _ __ |  _ \ 
| |_) / _ \| __| | __| |/ _ \| '_ \| | | |
|  __/ (_) | |_| | |_| | (_) | | | | |_| |
|_|   \___/ \__|_|\__|_|\___/|_| |_|____/ 
                                          
Version: {_text}";
    }
}