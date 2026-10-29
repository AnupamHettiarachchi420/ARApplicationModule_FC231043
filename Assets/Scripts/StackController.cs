using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class StackController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text topMarker;

    [Header("Settings")]
    [SerializeField] private int maxSize = 6;
    [SerializeField] private float blockSize = 0.05f;
    [SerializeField] private float gap = 0.006f;
    [SerializeField] private float baseHeight = 0.012f;

    private readonly Stack<(GameObject block, int value)> items = new();
    private int nextValue = 1;
    private bool busy;
    private Camera cam;

    private static readonly Color Red = new Color(1f, 0.42f, 0.42f);
    private static readonly Color Gold = new Color(1f, 0.84f, 0.29f);
    private static readonly Color Green = new Color(0.4f, 0.9f, 0.55f);

    private static readonly string[] CodeLines =
    {
        "stack.Push(value);",
        "stack.Pop();",
        "stack.Peek();"
    };

    private void Start()
    {
        cam = Camera.main;
        topMarker.fontSize = 0.35f;
        topMarker.fontStyle = FontStyles.Bold;
        topMarker.alignment = TextAlignmentOptions.Center;
        topMarker.color = Gold;
        topMarker.rectTransform.sizeDelta = new Vector2(0.3f, 0.08f);
        ShowCode(-1);
        SetStatus("Stack is empty. Tap Push to add a block.", Color.white);
        UpdateMarker();
    }

    private void LateUpdate()
    {
        if (cam == null || !topMarker.gameObject.activeInHierarchy) return;
        topMarker.transform.rotation =
            Quaternion.LookRotation(topMarker.transform.position - cam.transform.position);
    }

    // ---------- Button actions ----------

    public void Push()
    {
        if (busy) return;
        ShowCode(0);
        if (items.Count >= maxSize)
        {
            SetStatus("Stack overflow! The stack is full (max " + maxSize + ").", Red);
            return;
        }
        StartCoroutine(PushRoutine());
    }

    public void Pop()
    {
        if (busy) return;
        ShowCode(1);
        if (items.Count == 0)
        {
            SetStatus("Stack underflow! The stack is empty.", Red);
            return;
        }
        StartCoroutine(PopRoutine());
    }

    public void Peek()
    {
        if (busy) return;
        ShowCode(2);
        if (items.Count == 0)
        {
            SetStatus("Nothing to peek. The stack is empty.", Red);
            return;
        }
        StartCoroutine(PeekRoutine());
    }

    // ---------- Animations ----------

    private IEnumerator PushRoutine()
    {
        busy = true;
        int index = items.Count;
        int value = nextValue++;
        GameObject block = CreateBlock(value, index);
        items.Push((block, value));
        UpdateMarker();
        SetStatus("Pushed " + value + " onto the top. Size = " + items.Count, Green);

        Vector3 target = SlotPosition(index);
        Vector3 start = target + Vector3.up * 0.25f;
        block.transform.localPosition = start;
        block.transform.localScale = Vector3.zero;

        float t = 0f;
        const float duration = 0.45f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // ease out
            block.transform.localPosition = Vector3.Lerp(start, target, e);
            block.transform.localScale = Vector3.one * (blockSize * e);
            yield return null;
        }
        block.transform.localPosition = target;
        block.transform.localScale = Vector3.one * blockSize;
        busy = false;
    }

    private IEnumerator PopRoutine()
    {
        busy = true;
        var (block, value) = items.Pop();
        UpdateMarker();
        SetStatus("Popped " + value + " from the top. Size = " + items.Count, Gold);

        Vector3 start = block.transform.localPosition;
        Vector3 end = start + Vector3.up * 0.25f;
        float t = 0f;
        const float duration = 0.4f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.Clamp01(t);
            block.transform.localPosition = Vector3.Lerp(start, end, e);
            block.transform.localScale = Vector3.one * (blockSize * (1f - e));
            yield return null;
        }
        Destroy(block);
        busy = false;
    }

    private IEnumerator PeekRoutine()
    {
        busy = true;
        var (block, value) = items.Peek();
        SetStatus("Peek: the top is " + value + " (not removed).", Gold);

        float t = 0f;
        const float duration = 0.6f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float pulse = 1f + 0.25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            block.transform.localScale = Vector3.one * (blockSize * pulse);
            yield return null;
        }
        block.transform.localScale = Vector3.one * blockSize;
        busy = false;
    }

    // ---------- Helpers ----------

    private Vector3 SlotPosition(int index)
    {
        return new Vector3(0f, baseHeight + blockSize / 2f + index * (blockSize + gap), 0f);
    }

    private GameObject CreateBlock(int value, int index)
    {
        GameObject block = Instantiate(blockPrefab, stackRoot);
        block.name = "Block " + value;
        // Blue -> purple -> pink gradient going up the stack
        Color c = Color.HSVToRGB(Mathf.Repeat(0.55f + index * 0.07f, 1f), 0.6f, 1f);
        block.GetComponent<Renderer>().material.color = c;
        for (int i = 0; i < 4; i++) AddLabel(block.transform, value.ToString(), i * 90f);
        return block;
    }

    private void AddLabel(Transform parent, string text, float yAngle)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Quaternion rot = Quaternion.Euler(0f, yAngle, 0f);
        go.transform.localRotation = rot;
        go.transform.localPosition = rot * new Vector3(0f, 0f, -0.505f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.08f, 0.1f, 0.18f);
        tmp.rectTransform.sizeDelta = Vector2.one;
    }

    private void UpdateMarker()
    {
        float topY = items.Count == 0
            ? baseHeight
            : SlotPosition(items.Count - 1).y + blockSize / 2f;
        topMarker.transform.localPosition = new Vector3(0f, topY + 0.04f, 0f);
        topMarker.text = items.Count == 0 ? "EMPTY" : "TOP";
    }

    private void SetStatus(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;
    }

    // Highlights the line of code that matches the current operation
    private void ShowCode(int activeLine)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < CodeLines.Length; i++)
        {
            if (i == activeLine)
                sb.AppendLine("<mark=#FFD54A55><color=#FFFFFF>" + CodeLines[i] + "</color></mark>");
            else
                sb.AppendLine("<color=#8A93A6>" + CodeLines[i] + "</color>");
        }
        codeText.text = sb.ToString();
    }
}