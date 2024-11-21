// File: UI/ConsoleUI/ConsoleOutputManager.cs
using System.Windows.Forms;

namespace PetitionD.UI.ConsoleUI;

public class ConsoleOutputManager(RichTextBox output)
{
    private readonly RichTextBox _output = output;

    public void WriteLine(string text)
    {
        if (_output.InvokeRequired)
        {
            _output.Invoke(() => WriteLine(text));
            return;
        }

        _output.AppendText(text + Environment.NewLine);
        _output.ScrollToCaret();
    }

    public void WriteLine(FigletBuilder figlet)
    {
        var text = figlet.Build();
        WriteLine(text);
    }

    public void Clear()
    {
        if (_output.InvokeRequired)
        {
            _output.Invoke(Clear);
            return;
        }
        _output.Clear();
    }
}