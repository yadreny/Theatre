using System;
using System.Collections.Generic;
using UnityEngine;

public enum DragPositionStatus { Prev, Self, Next }
public class DropCursorState<T>
{
    public Action CurrentPutAction { get; private set; }
    public bool isGood => CurrentPutAction != null;
    public DragPositionStatus Status { get; private set; }
    public int id { get; private set; }

    public DropCursorState(Action currentPutAction, DragPositionStatus status, int id) //T target, 
    {
        this.CurrentPutAction = currentPutAction;
        this.Status = status;
        this.id = id ;
    }
}

public interface IDropCursor<T>
{
    //void Init(List<T> currentData, List<T> selected, RectTransform rect, int RendHeight,
    //    Func<List<T>, List<T>, int, DragPositionStatus, Action> _putValidator);
}

public class DropCursor<T> : IDropCursor<T>
{
    protected List<T> currentData;
    protected List<T> selected;
    protected RectTransform rect;
    protected int RendHeight;
    protected Func<List<T>, List<T>, int, DragPositionStatus, Action> _putValidator;

    public DropCursor(List<T> currentData, List<T> selected, RectTransform rect, int RendHeight,
        Func<List<T>, List<T>, int, DragPositionStatus, Action> _putValidator)
    {
        this.currentData = currentData;
        this.selected = selected;
        this.rect = rect;
        this.RendHeight = RendHeight;
        this._putValidator = _putValidator;
    }

    public virtual DropCursorState<T> GetState()
    {
        return null;
        //float topOffset = rect.relativeMousePositionFlash().y;
        //int DropItemIndex = (int)Mathf.Clamp(Mathf.Floor(topOffset / RendHeight), 0, currentData.Count - 1);

        //float p = topOffset % RendHeight;
        //float q = p / RendHeight;
        //DragPositionStatus currentDragPositionStatus = DragPositionStatus.Next;

        //if (topOffset < currentData.Count * RendHeight)
        //{
        //    if (q < 0.25f) currentDragPositionStatus = DragPositionStatus.Prev;
        //    if (q < 0.75f) currentDragPositionStatus = DragPositionStatus.Self;
        //}

        //Action currentPutAction = _putValidator(currentData, selected, DropItemIndex, currentDragPositionStatus);

        //DropCursorState<T> result = new DropCursorState<T>(currentPutAction, currentDragPositionStatus, DropItemIndex * RendHeight); //currentData[DropItemIndex], 
        //return result;
    }
}