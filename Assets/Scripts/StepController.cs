using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class StepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StackController stack;
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text codeTitle;
    [SerializeField] private TMP_Text modeButtonLabel;

    [Header("Button groups")]
    [SerializeField] private GameObject freeButtons;
    [SerializeField] private GameObject guidedButtons;
    [SerializeField] private GameObject predictButtons;

    [Header("Predict answer labels (A, B, C)")]
    [SerializeField] private TMP_Text[] optionLabels;

    private enum Op { Push, Pop, Peek }
    private enum Mode { Free, Guided, Predict }

    // Lesson program for Guided mode. Edit to change what the learner steps through.
    private static readonly (Op op, int value)[] GuidedProgram =
    {
        (Op.Push, 5), (Op.Push, 8), (Op.Push, 3),
        (Op.Pop, 0), (Op.Push, 9), (Op.Peek, 0)
    };

    // Questions for Predict mode. Edit to change the challenge.
    private static readonly (Op op, int value)[] PredictProgram =
    {
        (Op.Push, 4), (Op.Push, 7), (Op.Pop, 0),
        (Op.Push, 1), (Op.Push, 6), (Op.Pop, 0)
    };

    private Mode mode = Mode.Free;

    // Guided state
    private int step;

    // Predict state
    private readonly List<int> sim = new();
    private readonly string[] options = new string[3];
    private int correctIndex;
    private int pStep;
    private int score;
    private bool awaiting;
    private bool finished;

    private void Start()
    {
        ApplyModeUi();
    }

    // ---------- Mode switching ----------

    public void ToggleMode()
    {
        if (stack.IsBusy) return;
        StopAllCoroutines();

        mode = mode == Mode.Free ? Mode.Guided
             : mode == Mode.Guided ? Mode.Predict
             : Mode.Free;

        ApplyModeUi();
        stack.ClearInstant();

        switch (mode)
        {
            case Mode.Free:
                stack.ResetCodeDisplay();
                codeTitle.text = "CODE";
                stack.Say("Free mode. Use Push, Pop and Peek.");
                break;
            case Mode.Guided:
                Reset();
                break;
            case Mode.Predict:
                StartPredict();
                break;
        }
    }

    private void ApplyModeUi()
    {
        freeButtons.SetActive(mode == Mode.Free);
        guidedButtons.SetActive(mode == Mode.Guided);
        predictButtons.SetActive(mode == Mode.Predict);
        modeButtonLabel.text = mode == Mode.Free ? "Mode: Free"
                             : mode == Mode.Guided ? "Mode: Guided"
                             : "Mode: Predict";
    }

    // ---------- Guided mode ----------

    public void Next()
    {
        if (mode != Mode.Guided || stack.IsBusy) return;
        if (step >= GuidedProgram.Length)
        {
            stack.Say("Program finished. Press Reset to run it again.");
            return;
        }

        var (op, value) = GuidedProgram[step];
        bool ok = op switch
        {
            Op.Push => stack.TryPush(value),
            Op.Pop => stack.TryPop(),
            _ => stack.TryPeek()
        };
        if (ok) step++;
        RenderGuided();
    }

    public void Back()
    {
        if (mode != Mode.Guided || stack.IsBusy || step == 0) return;
        step--;
        stack.ClearInstant();
        for (int i = 0; i < step; i++)
        {
            var (op, value) = GuidedProgram[i];
            if (op == Op.Push) stack.PushInstant(value);
            else if (op == Op.Pop) stack.PopInstant();
        }
        stack.Say(step == 0
            ? "Back at the start. The stack is empty."
            : "Went back. Line " + step + " is the last line run.");
        RenderGuided();
    }

    public void Reset()
    {
        if (mode != Mode.Guided || stack.IsBusy) return;
        step = 0;
        stack.ClearInstant();
        stack.Say("Stack is empty. Press Next to run the first line.");
        RenderGuided();
    }

    private void RenderGuided()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < GuidedProgram.Length; i++)
        {
            string line = LineText(GuidedProgram[i]);
            if (i == step - 1)
                sb.AppendLine("<mark=#FFD54A55><color=#FFFFFF>" + line + "</color></mark>");
            else if (i < step)
                sb.AppendLine("<color=#6FD6A0>" + line + "</color>");
            else
                sb.AppendLine("<color=#8A93A6>" + line + "</color>");
        }
        codeText.text = sb.ToString();
        codeTitle.text = "CODE   " + step + " / " + GuidedProgram.Length;
    }

    // ---------- Predict mode ----------

    private void StartPredict()
    {
        sim.Clear();
        pStep = 0;
        score = 0;
        finished = false;
        stack.ClearInstant();
        AskQuestion();
    }

    private void AskQuestion()
    {
        if (pStep >= PredictProgram.Length)
        {
            Finish();
            return;
        }

        var (op, value) = PredictProgram[pStep];

        var before = new List<int>(sim);
        var after = new List<int>(sim);
        if (op == Op.Push) after.Add(value);
        else if (op == Op.Pop && after.Count > 0) after.RemoveAt(after.Count - 1);

        string correct = StateOf(after);

        // Plausible wrong answers a learner might pick
        var reversed = new List<int>(after);
        reversed.Reverse();
        var droppedTop = new List<int>(after);
        if (droppedTop.Count > 0) droppedTop.RemoveAt(droppedTop.Count - 1);
        var extra = new List<int>(after);
        extra.Add(after.Count > 0 ? after[after.Count - 1] : 1);

        var candidates = new[] { StateOf(before), StateOf(reversed), StateOf(droppedTop), StateOf(extra) };
        var wrong = new List<string>();
        foreach (string c in candidates)
        {
            if (wrong.Count >= 2) break;
            if (c != correct && !wrong.Contains(c)) wrong.Add(c);
        }
        while (wrong.Count < 2) wrong.Add("[" + (wrong.Count + 9) + "]");

        var list = new List<string> { correct, wrong[0], wrong[1] };
        for (int i = list.Count - 1; i > 0; i--)   // shuffle
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            options[i] = list[i];
            optionLabels[i].text = list[i];
            if (list[i] == correct) correctIndex = i;
        }

        for (int i = 0; i < 3; i++)
            optionLabels[i].transform.parent.gameObject.SetActive(true);
        awaiting = true;
        stack.Say("Q" + (pStep + 1) + ": what will the stack be after  " + LineText(PredictProgram[pStep]) + " ?");
        RenderPredict(false);
    }

    // Called by the three answer buttons (0, 1, 2)
    public void Answer(int index)
    {
        if (mode != Mode.Predict || stack.IsBusy) return;
        if (finished) { StartPredict(); return; }
        if (!awaiting) return;
        awaiting = false;

        bool right = index == correctIndex;
        if (right) score++;

        var (op, value) = PredictProgram[pStep];
        if (op == Op.Push) { stack.TryPush(value); sim.Add(value); }
        else if (op == Op.Pop) { stack.TryPop(); sim.RemoveAt(sim.Count - 1); }

        RenderPredict(true);
        stack.Say(right
            ? "<color=#6FE3A0>Correct!</color> That is the new stack."
            : "<color=#FF7B7B>Not quite.</color> The answer was " + options[correctIndex]);

        StartCoroutine(NextQuestionLater());
    }

    private IEnumerator NextQuestionLater()
    {
        while (stack.IsBusy) yield return null;
        yield return new WaitForSeconds(1.5f);
        pStep++;
        AskQuestion();
    }

    private void Finish()
    {
        finished = true;
        awaiting = false;

        int best = Mathf.Max(PlayerPrefs.GetInt("StackPredictBest", 0), score);
        PlayerPrefs.SetInt("StackPredictBest", best);
        PlayerPrefs.Save();

        optionLabels[0].transform.parent.gameObject.SetActive(false);
        optionLabels[2].transform.parent.gameObject.SetActive(false);
        optionLabels[1].text = "Retry";
        stack.Say("Finished! Score " + score + " / " + PredictProgram.Length
                  + "  (best " + best + "). Tap Retry to try again.");
        RenderPredict(false);
        codeTitle.text = "PREDICT   DONE";
    }

    private void RenderPredict(bool executed)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < PredictProgram.Length; i++)
        {
            string line = LineText(PredictProgram[i]);
            if (i == pStep && !finished)
            {
                if (executed)
                    sb.AppendLine("<mark=#FFD54A55><color=#FFFFFF>" + line + "</color></mark>");
                else
                    sb.AppendLine("<mark=#4DABF755><color=#FFFFFF>> " + line + "</color></mark>");
            }
            else if (i < pStep || finished)
                sb.AppendLine("<color=#6FD6A0>" + line + "</color>");
            else
                sb.AppendLine("<color=#8A93A6>" + line + "</color>");
        }
        codeText.text = sb.ToString();
        codeTitle.text = "PREDICT  " + (pStep + 1) + "/" + PredictProgram.Length + "  Score " + score;
    }

    // ---------- Helpers ----------

    private static string StateOf(List<int> values)
    {
        return values.Count == 0 ? "[ ]" : "[" + string.Join(", ", values) + "]";
    }

    private static string LineText((Op op, int value) line)
    {
        return line.op switch
        {
            Op.Push => "stack.Push(" + line.value + ");",
            Op.Pop => "stack.Pop();",
            _ => "stack.Peek();"
        };
    }
}