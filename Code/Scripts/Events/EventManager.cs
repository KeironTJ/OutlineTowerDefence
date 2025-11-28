using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private static Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();
    private static Dictionary<string, Action<object>> parameterizedEventDictionary = new Dictionary<string, Action<object>>();

    public static void StartListening(string eventName, Action listener)
    {
        if (eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent += listener;
            eventDictionary[eventName] = thisEvent;
        }
        else
        {
            eventDictionary.Add(eventName, listener);
        }
    }

    public static void StopListening(string eventName, Action listener)
    {
        if (eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent -= listener;
            if (thisEvent == null)
            {
                eventDictionary.Remove(eventName);
            }
            else
            {
                eventDictionary[eventName] = thisEvent;
            }
        }
    }

    public static void TriggerEvent(string eventName)
    {
        //Debug.Log($"Triggering event: {eventName}");
        if (eventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent.Invoke();
        }
    }

    public static void TriggerEvent(string eventName, object parameter)
    {
        //Debug.Log($"Triggering event: {eventName} with parameter: {parameter}");
        if (parameterizedEventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent.Invoke(parameter);
        }
    }

    public static void StartListening(string eventName, Action<object> listener)
    {
        if (parameterizedEventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent += listener;
            parameterizedEventDictionary[eventName] = thisEvent;
        }
        else
        {
            parameterizedEventDictionary.Add(eventName, listener);
        }
    }

    public static void StopListening(string eventName, Action<object> listener)
    {
        if (parameterizedEventDictionary.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent -= listener;
            if (thisEvent == null)
            {
                parameterizedEventDictionary.Remove(eventName);
            }
            else
            {
                parameterizedEventDictionary[eventName] = thisEvent;
            }
        }
    }
}
