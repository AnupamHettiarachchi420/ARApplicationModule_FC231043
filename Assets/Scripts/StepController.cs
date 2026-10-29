using System.Text;
using TMPro;
using UnityEngine;

public class StepController : MonoBehaviour
{
    [SerializeField] private StackController stack;
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text codeTitle;
    [SerializeField] private GameObject freeButtons;
    [SerializeField] private GameObject guidedButtons;
    [SerializeField] private TMP_Text modeButtonLabel;

    private enum Op { Push, Pop, Peek }

    // The lesson program. Edit this list to change what the learner steps through.
    private static readonly (Op op, int value)[] Program =
    {
        (Op.Push, 5), (Op.Push, 8), (Op.Push, 3),
        (Op.Pop, 0), (Op.Push, 9), (Op.Peek, 0)
    };

    private int step;      // how many lines have been run
    private bool guided;

    private void Start()
    {
        guided = false;
        freeButtons.SetActive(true);
        guidedButtons.SetActive(false);
        modeButtonLabel.text = "Mode: Free";
    }

    // ---------- Buttons ----------

    public void ToggleMode()
    {
        if (stack.IsBusy) return;
        guided = !guided;
        freeButtons.SetActive(!guided);
        guidedButtons.SetActive(guided);
        modeButtonLabel.text = guided ? "Mode: Guided" : "Mode: Free";

        if (guided)
        {
            Reset();
        }
        else
        {
            stack.ClearInstant();
            stack.ResetCodeDisplay();
            codeTitle.text = "CODE";
            stack.Say("Free mode. Use Push, Pop and Peek.");
        }
    }

    public void Next()
    {
        if (stack.IsBusy) return;
        if (step >= Program.Length)
        {
            stack.Say("Program finished. Press Reset to run it again.");
            return;
        }

        var (op, value) = Program[step];
        bool ok = op switch
        {
            Op.Push => stack.TryPush(value),
            Op.Pop => stack.TryPop(),
            _ => stack.TryPeek()
        };
        if (ok) step++;
        Render();
    }

    public void Back()
    {
        if (stack.IsBusy || step == 0) return;
        step--;
        stack.ClearInstant();
        for (int i = 0; i < step; i++)
        {
            var (op, value) = Program[i];
            if (op == Op.Push) stack.PushInstant(value);
            else if (op == Op.Pop) stack.PopInstant();
        }
        stack.Say(step == 0
            ? "Back at the start. The stack is empty."
            : "Went back. Line " + step + " is the last line run.");
        Render();
    }

    public void Reset()
    {
        if (stack.IsBusy) return;
        step = 0;
        stack.ClearInstant();
        stack.Say("Stack is empty. Press Next to run the first line.");
        Render();
    }

    // ---------- Display ----------

    private static string LineText(int i)
    {
        var (op, value) = Program[i];
        return op switch
        {
            Op.Push => "stack.Push(" + value + ");",
            Op.Pop => "stack.Pop();",
            _ => "stack.Peek();"
        };
    }

    private void Render()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Program.Length; i++)
        {
            string line = LineText(i);
            if (i == step - 1)
                sb.AppendLine("<mark=#FFD54A55><color=#FFFFFF>" + line + "</color></mark>");
            else if (i < step)
                sb.AppendLine("<color=#6FD6A0>" + line + "</color>");
            else
                sb.AppendLine("<color=#8A93A6>" + line + "</color>");
        }
        codeText.text = sb.ToString();
        codeTitle.text = "CODE   " + step + " / " + Program.Length;
    }
}