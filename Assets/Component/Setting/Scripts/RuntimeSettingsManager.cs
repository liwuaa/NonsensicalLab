using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using NonsensicalKit.Core;
using UnityEngine;

public class RuntimeSettingsManager : MonoBehaviour
{
    [SerializeField] private bool m_log;

    [Header("File Configuration")] [SerializeField]
    private string defaultSettingsResourceName = "settings_default"; // Resources 文件夹下的默认设置文件名 (不含扩展名)

    [SerializeField] private string userSettingsFileName = "user_settings.json"; // 用户设置文件名


    private static RuntimeSettingsManager _instance;

    public static RuntimeSettingsManager Instance
    {
        get { return _instance; }
    }


    private List<SettingItem> _settingsDataWrapper = new();
    private Dictionary<string, SettingItem> _settingsDict = new Dictionary<string, SettingItem>();
    private Dictionary<string, (object, SettingItem)> _settingsTempDict = new();
    private string UserSettingsFilePath => Path.Combine(Application.persistentDataPath, userSettingsFileName);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            LoadSettings();
        }
    }

    private void LoadSettings()
    {
        string jsonData = null;
        bool loadedFromFile = false;

        // 1. 首先尝试从 persistentDataPath 加载用户设置
        if (File.Exists(UserSettingsFilePath))
        {
            try
            {
                jsonData = File.ReadAllText(UserSettingsFilePath);
                loadedFromFile = true;
                if (m_log) Debug.Log($"Loaded user settings from {UserSettingsFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load user settings from {UserSettingsFilePath}: {e.Message}");
            }
        }

        // 2. 如果没有用户设置文件，则加载默认设置
        if (string.IsNullOrEmpty(jsonData))
        {
            TextAsset textAsset = Resources.Load<TextAsset>(defaultSettingsResourceName);
            if (textAsset != null)
            {
                jsonData = textAsset.text;
                Debug.Log($"Loaded default settings from Resources/{defaultSettingsResourceName}.json");
            }
            else
            {
                Debug.LogError($"Default settings file 'Resources/{defaultSettingsResourceName}.json' not found!");
                return; // 无法加载任何设置
            }
        }

        if (!string.IsNullOrEmpty(jsonData))
        {
            try
            {
                if (m_log) Debug.Log(jsonData);
                _settingsDataWrapper = JsonConvert.DeserializeObject<List<SettingItem>>(jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse settings JSON: {e.Message}");
                return;
            }
        }

        // 4. 将列表转换为字典以便快速查找，并初始化内部状态
        _settingsDict.Clear();
        if (_settingsDataWrapper is { Count: > 0 })
        {
            foreach (var item in _settingsDataWrapper)
            {
                item.InternalType = GetInternalTypeFromString(item.type);
                item.SetInitialValue(item.value);
                if (item.InternalType == SettingItemTypeInternal.Enum && item.options.Count > 0)
                {
                    // stringValue 应该已经被反序列化为选中的值

                    if (item.options.Contains(item.stringValue))
                    {
                        item.SelectedOptionIndex = item.options.IndexOf(item.stringValue);
                    }
                    else
                    {
                        Debug.LogWarning($"Enum value '{item.stringValue}' not found in options for key '{item.key}'. Defaulting to first option.");
                        item.SelectedOptionIndex = 0;
                        item.stringValue = item.options.Count > 0 ? item.options[0] : "";
                    }
                }

                _settingsDict[item.key] = item;
            }
        }
        
        if (!loadedFromFile && _settingsDataWrapper != null)
        {
            SaveSettings();
            Debug.Log("Created initial user settings file from defaults.");
        }
    }

    private SettingItemTypeInternal GetInternalTypeFromString(string typeString)
    {
        switch (typeString?.ToLowerInvariant())
        {
            case "bool": return SettingItemTypeInternal.Bool;
            case "string": return SettingItemTypeInternal.String;
            case "float": return SettingItemTypeInternal.Float;
            case "enum": return SettingItemTypeInternal.Enum;
            case "button": return SettingItemTypeInternal.Button;
            default:
                Debug.LogWarning($"Unknown type string '{typeString}', defaulting to String.");
                return SettingItemTypeInternal.String;
        }
    }

    private void SaveSettings()
    {
        if (_settingsDataWrapper is { Count: > 0 })
        {
            try
            {
                // 更新
                foreach (SettingItem item in _settingsDataWrapper)
                {
                    item.value = item.GetJsonValue()?.ToString();
                }

                var a = JsonConvert.SerializeObject(_settingsDataWrapper);
                if (m_log) Debug.Log(a);
                File.WriteAllText(UserSettingsFilePath, a);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save user settings to {UserSettingsFilePath}: {e.Message}");
            }
        }
    }

    public List<SettingItem> GetSettingsDataWrapper() // 返回包装类
    {
        return _settingsDataWrapper;
    }

    // 设置值的方法 (主要用于非按钮类型)
    public void SetValue(string key, object value)
    {
        if (_settingsDict.TryGetValue(key, out SettingItem item))
        {
            bool valueChanged = false;
            switch (item.InternalType)
            {
                case SettingItemTypeInternal.Bool:
                    if (value is bool boolVal && item.boolValue != boolVal)
                    {
                        item.boolValue = boolVal;
                        valueChanged = true;
                    }

                    break;
                case SettingItemTypeInternal.String:
                    if (value is string strVal && item.stringValue != strVal)
                    {
                        item.stringValue = strVal;
                        valueChanged = true;
                    }

                    break;
                case SettingItemTypeInternal.Float:
                    if (value is float floatVal && !Mathf.Approximately(item.floatValue, floatVal))
                    {
                        item.floatValue = floatVal;
                        valueChanged = true;
                    }
                    else if (value is int intVal)
                    {
                        float fVal = intVal;
                        if (!Mathf.Approximately(item.floatValue, fVal))
                        {
                            item.floatValue = fVal;
                            valueChanged = true;
                        }
                    }
                    else if (value is double doubleVal)
                    {
                        float fVal = (float)doubleVal;
                        if (!Mathf.Approximately(item.floatValue, fVal))
                        {
                            item.floatValue = fVal;
                            valueChanged = true;
                        }
                    }

                    break;
                case SettingItemTypeInternal.Enum:
                    if (value is int enumIntVal)
                    {
                        int clampedIndex = Mathf.Clamp(enumIntVal, 0, item.options.Count - 1);
                        string newValue = item.options[clampedIndex];
                        if (item.stringValue != newValue)
                        {
                            item.stringValue = newValue;
                            item.SelectedOptionIndex = clampedIndex;
                            valueChanged = true;
                        }
                    }
                    else if (value is string enumStrVal)
                    {
                        int index = item.options.IndexOf(enumStrVal);
                        if (index != -1 && item.stringValue != enumStrVal)
                        {
                            item.stringValue = enumStrVal;
                            item.SelectedOptionIndex = index;
                            valueChanged = true;
                        }
                        else if (index == -1)
                        {
                            Debug.LogWarning($"Attempted to set invalid enum value '{enumStrVal}' for key '{key}'. Valid options: {string.Join(", ", item.options)}");
                        }
                    }

                    break;
                case SettingItemTypeInternal.Button:
                    Debug.LogWarning($"SetValue called on button type setting '{key}'. Buttons don't store values.");
                    break;
            }

            // 如果值改变了，则自动保存
            if (valueChanged)
            {
                if (_settingsTempDict.TryAdd(key, (value, item)) == false)
                {
                    _settingsTempDict[key] = (value, item);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Setting with key '{key}' not found.");
        }
    }

    // --- 按钮动作处理 ---
    public void HandleButtonAction(string actionKey)
    {
        switch (actionKey)
        {
            case "resetSettings":
                Debug.Log("Resetting settings to defaults...");
                ResetToDefaults();
                break;

            case "applySettings":
                Debug.Log("Apply settings...");
                foreach (var item in _settingsTempDict)
                {
                    IOCC.PublishWithID("OnSettingValueChanged", item.Key, item.Value.Item1, item.Value.Item2);
                }
                _settingsTempDict.Clear();
                SaveSettings();
                break;
            case "cancel":
                _settingsTempDict.Clear();
                break;
            default:
                IOCC.PublishWithID("OnSettingButtonCLick", actionKey, actionKey);
                break;
        }
    }

    // 重置为默认设置
    private void ResetToDefaults()
    {
        // 删除用户设置文件
        if (File.Exists(UserSettingsFilePath))
        {
            try
            {
                File.Delete(UserSettingsFilePath);
                Debug.Log("User settings file deleted.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete user settings file: {e.Message}");
            }
        }
        LoadSettings();
    }
}