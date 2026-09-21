using System.Collections;
using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class GenerateDialogues : MonoBehaviour
{
    [TextArea(3, 8)]
    public string sentence;

    [Min(0f)]
    public float letterDelay = 0.05f;

    private TMP_Text dialogueText;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        dialogueText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        TypeSentence(sentence);
    }

    public void TypeSentence(string newSentence, Action onComplete = null)
    {
        sentence = newSentence;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeSentenceCoroutine(onComplete));
    }

    private IEnumerator TypeSentenceCoroutine(Action onComplete)
    {
        if (dialogueText == null)
        {
            typingCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        dialogueText.text = string.Empty;

        foreach (char letter in sentence)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(letterDelay);
        }

        typingCoroutine = null;
        onComplete?.Invoke();
    }

}
