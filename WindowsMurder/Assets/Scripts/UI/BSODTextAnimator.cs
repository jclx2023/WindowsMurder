using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class BSODTextAnimator : MonoBehaviour
{
    [TextArea(5, 20)]
    public string fullText;            // ��������ԭ��
    public float lineDelay = 0.1f;     // ÿ�м��
    public float charDelay = 0.02f;    // ���ַ����
    public bool playOnEnable = true;

    [SerializeField] private AudioClip audioClip;

    private TMP_Text textMesh;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
        textMesh.text = "";
    }

    void OnEnable()
    {
        if (playOnEnable)
            StartCoroutine(PlayText());
        if (audioClip != null)
        {
            GlobalSystemManager.Instance.PlaySFX(audioClip);
        }
    }

    public IEnumerator PlayText()
    {
        textMesh.text = "";
        yield return new WaitForSeconds(0.3f);

        string[] lines = fullText.Split('\n');

        foreach (string line in lines)
        {
            string current = "";
            foreach (char c in line)
            {
                current += c;
                textMesh.text = textMesh.text + c; // ׷��
                yield return new WaitForSeconds(charDelay);
            }

            textMesh.text += "\n";
            yield return new WaitForSeconds(lineDelay);
        }
    }
}
