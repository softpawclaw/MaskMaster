using System;
using Enums;
using UnityEngine;

public class Day
{
    public event Action<EDayState> OnDayStateChangedDelegate;
    
    private EDayState currentState = EDayState.None;

    public void UpdateDayState(EDayState state)
    {
        currentState = state;
        Debug.Log($"Day: {currentState.ToString()}");
        OnDayStateChangedDelegate?.Invoke(currentState);
    }
}