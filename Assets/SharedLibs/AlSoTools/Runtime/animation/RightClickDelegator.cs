using System;
using UnityEngine.EventSystems;
using UnityEngine;

public static class ComponentVisibility
{
    public static void SetVisible(this Component comp, bool val)
    {
        if (comp == null) Debug.LogError("comp not found");
        comp.gameObject.SetActive(val);
    }

    public static bool isVissible(this GameObject go)
    {
        if (go.name == "Canvas" || go.name == "GUI") return true;
        if (go.activeInHierarchy) return go.transform.parent.gameObject.isVissible();
        return false;
    }
}

public interface IElemCtrl<ElemType>
{
    ElemType data { set; }

    void FirstFill();

    void ReFill();

    void Destroy();

    void removeEventListeners();
}

public interface IElemSelectorIndicator<ElemType> : IElemCtrlSelectable<ElemType>
{
    void indicateSelection();
    void indicateUnselection();
}

public interface IElemCtrlSelectable<ElemType> : IElemCtrl<ElemType>
{
    Action<ElemType> onSelect { get; set; }
}

public interface IElemCtrlDragable<ElemType> //: IElemCtrlSelectable<ElemType>
{
    void StartListemMove();
    void StopListenMove();
    Action<ElemType> onStartDragHandler { get; set; }
    Action<ElemType> onEndDragHandler { get; set; }

}

public class RightClickDelegator : MonoBehaviour, IPointerClickHandler
{
    public Action handler;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            this.handler();
        }
    }
}

public class MouseOverDelegator : MonoBehaviour, IPointerEnterHandler
{
    public Action handler;

    public void OnPointerEnter(PointerEventData eventData)
    {
        handler();
    }
}

public class MouseOutDelegator : MonoBehaviour, IPointerExitHandler
{
    public Action handler;

    public void OnPointerExit(PointerEventData eventData)
    {
        handler();
    }
}

public class MouseRightDownDelegator : MonoBehaviour, IPointerDownHandler
{
    public Action handler;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) handler();
    }
}

public class MouseRightUpDelegator : MonoBehaviour, IPointerUpHandler
{
    public Action handler;

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) handler();
    }
}


public class MouseDownDelegator : MonoBehaviour, IPointerDownHandler
{
    public Action handler;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) handler();
    }
}



public class MouseUpDelegator : MonoBehaviour, IPointerDownHandler
{
    public Action handler;
    bool isStarted;

    public MouseUpDelegator AsStarted()
    {
        isStarted = true;
        return this;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.LogError("left button down");
        isStarted = true;
    }

    void Update()
    {
        if (!isStarted) return;
        if (Input.GetMouseButtonUp(0))
        {
            //Debug.LogError("left button up");
            isStarted = false;
            handler.Invoke();
        }
    }

    public void Destory()
    {
        this.handler = null;
        GameObject.Destroy(this);
    }
}

public class MouseClickDelegator : MonoBehaviour, IPointerClickHandler
{
    public Action handler;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) handler();
    }
}

public class MouseDoubleClickDelegator : MonoBehaviour, IPointerClickHandler
{
    public Action handler;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount>1) handler();
    }
}

public class MouseMoveDelegator : MonoBehaviour
{
    public Action handler;

    Vector2 prevPosition;
    bool isStarted;

    private void FixedUpdate()
    {
        if (!isStarted)
        {
            isStarted = true;
            prevPosition = Input.mousePosition;
            return;
        }

        Vector2 currentPosition = Input.mousePosition;
        if ((currentPosition - prevPosition).magnitude>1) handler?.Invoke();
        prevPosition = currentPosition;
    }

    public void Destory()
    {
        this.handler = null;
        GameObject.Destroy(this);
    }
}

public static class MouseDelegatorExt
{
    public static MouseDoubleClickDelegator addLeftDoubleClickHandler(this Component button, Action handler)
    {
        MouseDoubleClickDelegator delegator = button.gameObject.AddComponent<MouseDoubleClickDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseClickDelegator addLeftClickHandler(this Component button, Action handler)
    {
        MouseClickDelegator delegator = button.gameObject.AddComponent<MouseClickDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static RightClickDelegator addRightClickHandler(this Component button, Action handler)
    {
        RightClickDelegator delegator = button.gameObject.AddComponent<RightClickDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseOverDelegator onMouseOver(this Component button, Action handler)
    {
        MouseOverDelegator delegator = button.gameObject.AddComponent<MouseOverDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseOutDelegator onMouseOut(this Component button, Action handler)
    {
        MouseOutDelegator delegator = button.gameObject.AddComponent<MouseOutDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseRightDownDelegator addMouseRightDown(this Component button, Action handler)
    {
        MouseRightDownDelegator delegator = button.gameObject.AddComponent<MouseRightDownDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseRightUpDelegator addMouseRightUp(this Component button, Action handler)
    {
        MouseRightUpDelegator delegator = button.gameObject.AddComponent<MouseRightUpDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseDownDelegator onMouseDown(this Component button, Action handler)
    {
        MouseDownDelegator delegator = button.gameObject.AddComponent<MouseDownDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseUpDelegator onMouseUp(this Component button, Action handler)
    {
        MouseUpDelegator delegator = button.gameObject.AddComponent<MouseUpDelegator>();
        delegator.handler = handler;
        return delegator;
    }

    public static MouseMoveDelegator onMouseMove(this Component button, Action handler)
    {
        MouseMoveDelegator delegator = button.gameObject.AddComponent<MouseMoveDelegator>();
        delegator.handler = handler;
        return delegator;
    }
}