using GameDevKit.UiStack;
using UnityEngine;

public sealed class CanvasPanelView : IUiPanel
{
    private readonly GameObject _root;
    private readonly CanvasGroup _group;

    public CanvasPanelView(GameObject root)
    {
        _root = root;
        _group = root.GetComponent<CanvasGroup>();

        if (_group == null)
        {
            _group = root.AddComponent<CanvasGroup>();
        }
    }

    public void Show()
    {
        _root.SetActive(true);
        _group.alpha = 1;
        _group.interactable = true;
        _group.blocksRaycasts = true;
    }

    public void Hide()
    {
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _group.alpha = 0;
        _root.SetActive(false);
    }

    public void Dispose()
    {
        Object.Destroy(_root);
    }
}
