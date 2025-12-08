using System.Collections;
using System.Collections.Generic;
using NonsensicalKit.Core;
using UnityEngine;

public abstract class SettingItemBase : NonsensicalMono
{
    [SerializeField] private string[] m_settingKeys;
    [SerializeField] private string[] m_addlistenButtonActions;

    protected virtual void Awake()
    {
        foreach (var key in m_settingKeys)
        {
            Subscribe<object, SettingItem>("OnSettingValueChanged", key, OnSettingValueChanged);
        }

        foreach (var action in m_addlistenButtonActions)
        {
            Subscribe<string>("OnAddListenButtonClick", action, OnAddListenButtonClick);
        }
    }

    protected abstract void OnSettingValueChanged(object value, SettingItem value2);
    protected abstract void OnAddListenButtonClick(string actionKey);
}