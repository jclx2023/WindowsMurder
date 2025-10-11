using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zip�ļ����� - ˫����ѹ��򿪴��ڣ��Ҽ���ѹ
/// </summary>
public class ZipFileAction : IconAction
{
    [Header("˫����Ϊ")]
    public DoubleClickBehavior doubleClickBehavior = DoubleClickBehavior.DirectExtract;

    [Header("��ѹ����")]
    public List<GameObject> filesToActivate = new List<GameObject>();
    public string dialogueBlockId = "";

    [Header("�������ã�OpenWindowģʽ��Ҫ��")]
    public GameObject windowPrefab;
    public Canvas targetCanvas;

    private bool isExtracted = false;
    private GameFlowController gameFlowController;

    public enum DoubleClickBehavior
    {
        DirectExtract,  // ˫��ֱ�ӽ�ѹ
        OpenWindow      // ˫���򿪴���
    }

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        }
    }

    void OnEnable()
    {
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;
    }

    void OnDisable()
    {
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;
    }

    public override void Execute()
    {
        if (doubleClickBehavior == DoubleClickBehavior.DirectExtract)
        {
            ExtractFiles();
        }
        else
        {
            OpenWindow();
        }
    }

    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        if (icon.gameObject != gameObject) return;

        switch (itemId)
        {
            case "open":
                OpenWindow();
                break;
            case "extract":
                ExtractFiles();
                break;
        }
    }

    private void OpenWindow()
    {
        if (windowPrefab == null || targetCanvas == null) return;
        Instantiate(windowPrefab, targetCanvas.transform);
    }

    public void ExtractFiles()
    {
        if (isExtracted) return;

        foreach (GameObject file in filesToActivate)
        {
            if (file != null) file.SetActive(true);
        }

        if (!string.IsNullOrEmpty(dialogueBlockId) && gameFlowController != null)
        {
            gameFlowController.StartDialogueBlock(dialogueBlockId);
        }

        isExtracted = true;
    }
}