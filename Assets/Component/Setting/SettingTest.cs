using System.Collections;
using System.Collections.Generic;
using NonsensicalKit.Tools.GUITool;
using TMPro;
using UnityEngine;

public class SettingTest : SettingItemBase
{
    [SerializeField] private TMP_Text m_text;
    [SerializeField] private GameObject A;
    [SerializeField] private GUIFPS m_fps;

    protected override void OnSettingValueChanged(string key, object value, SettingItem item)
    {
        switch (key)
        {
            case "targetFrameRate":
                Application.targetFrameRate = int.TryParse(item.GetJsonValue().ToString(), out int frameRate) ? frameRate : 60;
                m_text.text = "当前设置帧率  :" + value;
                break;
            case "showFPS":
                m_fps.gameObject.SetActive(bool.TryParse(item.GetJsonValue().ToString(), out bool showFPS) && showFPS);
                break;
        }
    }

    protected override void OnAddListenButtonClick(string actionKey)
    {
        //Debug.Log("点击了按钮  :" +actionKey);
        switch (actionKey)
        {
            case "hideObj":
                A.SetActive(!A.activeInHierarchy);
                break;
        }
    }
}