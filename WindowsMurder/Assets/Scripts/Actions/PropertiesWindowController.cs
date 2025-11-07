using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(WindowsWindow))]
public class PropertiesWindowController : MonoBehaviour
{
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private bool okClosesWindow = true;
    [SerializeField] private bool applyClosesWindow = false;
    [SerializeField] private bool cancelClosesWindow = true;

    public UnityEvent OnOKClicked;
    public UnityEvent OnApplyClicked;
    public UnityEvent OnCancelClicked;

    private WindowsWindow windowComponent;

    void Awake()
    {
        windowComponent = GetComponent<WindowsWindow>();

        if (okButton != null)
            okButton.onClick.AddListener(OnOKClick);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);
    }

    private void OnOKClick()
    {
        OnOKClicked?.Invoke();
        if (okClosesWindow)
            windowComponent.CloseWindow();
    }

    private void OnApplyClick()
    {
        OnApplyClicked?.Invoke();
        if (applyClosesWindow)
            windowComponent.CloseWindow();
    }

    private void OnCancelClick()
    {
        OnCancelClicked?.Invoke();
        if (cancelClosesWindow)
            windowComponent.CloseWindow();
    }

    public void TriggerOK() => OnOKClick();
    public void TriggerApply() => OnApplyClick();
    public void TriggerCancel() => OnCancelClick();
}
