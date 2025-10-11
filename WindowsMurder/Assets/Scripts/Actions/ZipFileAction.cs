using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zip文件交互 - 双击解压或打开窗口，右键解压
/// </summary>
public class ZipFileAction : IconAction
{
    [Header("双击行为")]
    public DoubleClickBehavior doubleClickBehavior = DoubleClickBehavior.DirectExtract;

    [Header("解压配置")]
    public List<GameObject> filesToActivate = new List<GameObject>();
    public string dialogueBlockId = "";

    [Header("窗口配置（OpenWindow模式需要）")]
    public GameObject windowPrefab;
    public Canvas targetCanvas;

    private bool isExtracted = false;
    private GameFlowController gameFlowController;

    public enum DoubleClickBehavior
    {
        DirectExtract,  // 双击直接解压
        OpenWindow      // 双击打开窗口
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