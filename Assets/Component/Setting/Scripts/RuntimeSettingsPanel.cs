using System.Collections.Generic;
using UnityEngine;

public class RuntimeSettingsPanel : MonoBehaviour
{
    [Header("UI Settings")] public KeyCode m_toggleKey = KeyCode.F1; // 更改默认键位，避免与游戏冲突
    [SerializeField, Tooltip("窗口高度使用百分比")] private bool m_usePercentage;

    [SerializeField] private float m_windowHeightPercentage = 0.8f;

    [SerializeField] private Rect m_windowRect = new Rect(50, 50, 400, 600); // 调整窗口大小


    private int windowID = 123456;
    private Vector2 scrollPosition = Vector2.zero;

    [Header("References")] public RuntimeSettingsManager settingsManager;

    private bool _showPanel = false;

    private List<SettingItem> _currentSettingsDataWrapper
    {
        get { return settingsManager.GetSettingsDataWrapper(); }
    } // 使用包装类

    void Start()
    {
        if (settingsManager == null)
        {
            settingsManager = FindObjectOfType<RuntimeSettingsManager>();
        }

        if (settingsManager == null)
        {
            Debug.LogError("RuntimeSettingsManager not assigned or found!", this);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_toggleKey))
        {
            _showPanel = !_showPanel;
        }
    }

    private void OnGUI()
    {
        if (!_showPanel || _currentSettingsDataWrapper == null || settingsManager == null) return;

        m_windowRect = GUI.Window(windowID, m_windowRect, DrawWindow, "Game Settings");
        m_windowRect.x = Mathf.Clamp(m_windowRect.x, 0, Screen.width - m_windowRect.width);
        m_windowRect.height = Mathf.Clamp(m_windowRect.y, 0, Screen.height - m_windowRect.height);
        if (m_usePercentage)
        {
            m_windowRect.height = Screen.height * m_windowHeightPercentage;
        }
    }

    private void DrawWindow(int _)
    {
        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        foreach (var setting in _currentSettingsDataWrapper)
        {
            if (!string.IsNullOrEmpty(setting.description))
            {
                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = (int)(descStyle.fontSize * 0.8f);
                descStyle.fontStyle = FontStyle.Italic;
                GUILayout.Label(setting.description, descStyle);
            }

            GUILayout.BeginHorizontal();
            switch (setting.InternalType)
            {
                case SettingItemTypeInternal.Bool:
                    var newBoolValue = GUILayout.Toggle(setting.boolValue, setting.label);
                    if (newBoolValue != setting.boolValue)
                    {
                        settingsManager.SetValue(setting.key, newBoolValue);
                        setting.boolValue = newBoolValue;
                    }

                    break;
                case SettingItemTypeInternal.String:
                    GUILayout.Label(setting.label);
                    string newStringValue = GUILayout.TextField(setting.stringValue);
                    if (newStringValue != setting.stringValue)
                    {
                        settingsManager.SetValue(setting.key, newStringValue);
                        setting.stringValue = newStringValue; // 更新本地副本
                    }

                    break;
                case SettingItemTypeInternal.Float:
                    GUILayout.Label($"{setting.label} ({setting.floatValue:F2})");
                    float newFloatValue = GUILayout.HorizontalSlider(setting.floatValue, setting.range.I1, setting.range.I2, GUILayout.ExpandWidth(true));
                    if (Mathf.Abs(newFloatValue - setting.floatValue) > 0.001f)
                    {
                        settingsManager.SetValue(setting.key, newFloatValue);
                        setting.floatValue = newFloatValue; // 更新本地副本
                    }

                    break;
                case SettingItemTypeInternal.Enum:
                    GUILayout.Label(setting.label);
                    if (setting.options != null && setting.options.Count > 0)
                    {
                        // 使用 SelectionGrid 模拟 Popup
                        int newIndex = GUILayout.SelectionGrid(setting.SelectedOptionIndex, setting.options.ToArray(), 1, GUILayout.ExpandWidth(true));
                        if (newIndex != setting.SelectedOptionIndex)
                        {
                            setting.SelectedOptionIndex = newIndex;
                            setting.stringValue = setting.options[newIndex];
                            settingsManager.SetValue(setting.key, setting.stringValue);
                        }
                    }
                    else
                    {
                        GUILayout.Label("(No options defined)");
                    }

                    break;
                case SettingItemTypeInternal.Button:
                    if (GUILayout.Button(setting.label))
                    {
                        settingsManager.HandleButtonAction(setting.buttonAction);
                    }

                    break;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("应用"))
        {
            settingsManager.HandleButtonAction("applySettings");
        }

        if (GUILayout.Button("确认"))
        {
            settingsManager.HandleButtonAction("applySettings");
            _showPanel = false;
        }

        if (GUILayout.Button("取消"))
        {
            settingsManager.HandleButtonAction("cancel");
            _showPanel = false;
        }


        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}