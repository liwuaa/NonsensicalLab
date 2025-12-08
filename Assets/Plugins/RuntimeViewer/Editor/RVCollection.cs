using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;

public class RVCollection : RVControlBase
{
    List<RVControlBase> children = new List<RVControlBase>();
    //string parentNameLabel;
    bool isFirstOpen = true;
    RVSettingData settingData;
    RVCStatus rvcStatus; 

    public RVCollection(string UID, string nameLabel, object data, int depth, RVVisibility rvVisibility)
         : base(UID, nameLabel,data, depth, rvVisibility)
    {
        //this.parentNameLabel = parentNameLabel;
    }

    bool IsFold()
    {
        if (rvcStatus.IsOpens.ContainsKey(this.UID) == false)
            rvcStatus.IsOpens.Add(this.UID, false);

        rvcStatus.IsOpens[this.UID] = CollectionUI(rvcStatus.IsOpens[this.UID], settingData);
        return rvcStatus.IsOpens[this.UID];
    }

    public override void OnGUIUpdate(bool isRealtimeUpdate, RVSettingData settingData, RVCStatus rvcStatus)
    {
        this.settingData = settingData;
        this.rvcStatus = rvcStatus;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("     ", GUILayout.Width(depth * RVControlBase.Indent_class+ IndentPlus));
        EditorGUILayout.BeginVertical();

        if (IsFold() == true)
        {
            if (isFirstOpen == true)
            {
                AnalyzeAndAddChildren();
                isFirstOpen = false;
            }
            else if (isRealtimeUpdate == true)
            {
                AnalyzeAndAddChildren();
            }

            foreach (var item in children)
            {
                item.OnGUIUpdate(isRealtimeUpdate, settingData, rvcStatus);
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    public override void OnDestroy()
    {
        foreach (var item in children)
        {
            //item.Value.OnDestroy();
            item.OnDestroy();
        }
        children.Clear();
    }

    protected virtual void AnalyzeAndAddChildren()
    {
        OnDestroy();
        if (data == null)
        {
            return;
        }

        Type t = data.GetType();

        if (RVHelper.IsCollection(t) == true)
        {
            children.AddRange(AnalyzeCollection(data, t));
        }
        else
        {
            children.AddRange(AnalyzeClass(data, t));
        }
    }

    List<RVControlBase> CreateDictionaryItemControl(DictionaryEntry item, int collectionNum = 0)
    {
        List<RVControlBase> dicItems = new List<RVControlBase>();

        Type _typeKey = IsNull(item.Key) == false ? item.Key.GetType() : null;
        RVVisibility rvvKey = new RVVisibility(RVVisibility.NameType.CollectionItem, _typeKey);

        Type _typeValue = IsNull(item.Value) == false ? item.Value.GetType() : null;
        RVVisibility rvvValue = new RVVisibility(RVVisibility.NameType.CollectionItem, _typeValue);
         
        RVControlBase key = CreateControl(item.Key, "┌ key  ┐", rvvKey, collectionNum);
        dicItems.Add(key);
        RVControlBase _value= CreateControl(item.Value, "└value┘", rvvValue, collectionNum);
        dicItems.Add(_value);

        //对齐
        if(key is RVCollection  == true && _value is RVCollection == false)
        {
            _value.IndentPlus = 14;
        }
        else if (_value is RVCollection == true && key is RVCollection == false)
        {
            key.IndentPlus = 14;
        }

        return dicItems;
    }

    RVControlBase CreateControl(object ob, string fieldName, RVVisibility rvVisibility, int collectionNum = 0)
    {
        RVControlBase b = null;
        string nextUID = this.UID + fieldName;
        if(collectionNum > 0)
            nextUID = this.UID+ collectionNum+ fieldName;

        if (IsNull(ob) == true)
        {
            b = new RVText(nextUID, fieldName, null, depth + 1, rvVisibility);
        }
        else
        {
            Type t = ob.GetType();
            if (RVHelper.IsCanToStringDirently(t) == true)
            {
                b = new RVText(nextUID, fieldName, ob, depth + 1, rvVisibility);
            }
            else
            {
                RVVisibility rvv = rvVisibility.GetCopy();
                if (RVHelper.IsCollection(t) == false)
                    rvv.RVType = RVVisibility.NameType.Class;
                //    Debug.Log("--------------" + this.NameLabel);
                b = new RVCollection(nextUID, GetSpecialNameLabel(ob, fieldName), ob, depth + 1, rvv);
            }
        }

        return b;
    }

    bool IsForbidThis(object obj, string fieldName)
    {
        if (obj == null)
            return false;

        if (Application.isPlaying == false)
        {
            if (obj is MeshFilter)
                return true;
            if (obj is Renderer)
                return true;
            if (obj is Collider)
                return true;
        }

        if (RuntimeViewer.IsEnableForbidNames == true)
        {
            if (this.settingData.IsForbid(fieldName) == true)
                return true;
        }
        return false;
    }

    string GetSpecialNameLabel(object ob, string fieldName)
    {
        if (ob == null)
            return fieldName;

        Type obType = ob.GetType();
        if (RVHelper.IsCollection(obType) == true)
        {
            fieldName += " ¤ " + GetTypeName(obType);
            return fieldName;
        }

        if (ob is UnityEngine.Object && (ob as UnityEngine.Object) != null)
        {
            fieldName += " : '" + (ob as UnityEngine.Object).name+"'";
        }

        return fieldName;
    }

    bool IsNull(object ob)
    {
        if (ob == null)
        {
            return true;
        }
        else if (ob is UnityEngine.Object && (ob as UnityEngine.Object) == null)
        {
            return true;
        }

        return false;
    }

    //所有集合判断,数组,字典,list等等
    List<RVControlBase> AnalyzeCollection(object ob, Type type)
    {
        List<RVControlBase> result = new List<RVControlBase>();

        if (typeof(IDictionary).IsAssignableFrom(type) == true)//是个字典
        {
            IDictionary dic = ob as IDictionary;
            foreach (DictionaryEntry item in dic)
            {
                List<RVControlBase> rvc = CreateDictionaryItemControl(item, result.Count + 1);
                result.AddRange(rvc);
            }
        }
        else if (typeof(ICollection).IsAssignableFrom(type) == true) //是个集合
        {
            int index = 0;
            foreach (var _v in (ICollection)ob)
            {
                string itemName = "["+index+"]";
           
                Type _type = null;
                if (IsNull(_v) == false)
                    _type = _v.GetType();

                RVVisibility rvv = new RVVisibility(RVVisibility.NameType.CollectionItem, _type);
                RVControlBase rvc = CreateControl(_v, itemName, rvv, result.Count + 1);
                result.Add(rvc);
                index++;
            }
        }

        return result;
    }

    List<RVControlBase> AnalyzeClass(object data, Type thisType)
    {
        List<RVControlBase> result = new List<RVControlBase>();

        while (thisType.IsSubclassOf(typeof(object)))
        {
            FieldInfo[] fields = thisType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                if (IsForbidThis(data, field.Name) == true)
                {
                    continue;
                }
                object value = field.GetValue(data);

                RVVisibility rvv = new RVVisibility();
                rvv.RVType = RVVisibility.NameType.Field;
                rvv.ValueType = field.FieldType;
                rvv.IsPrivate = field.IsPrivate;
                rvv.IsPublic = field.IsPublic;

                RVControlBase cb = CreateControl(value, field.Name, rvv);
                if (result.Contains(cb) == false)
                    result.Add(cb);
            }

            PropertyInfo[] properties = thisType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (PropertyInfo property in properties)
            {
                object value = null;
                try
                {
                    if (IsForbidThis(data, property.Name) == true)
                    {
                        continue;
                    }
                    value = property.GetValue(data, null);
                }
                catch
                {
                    value = null;
                }

                RVVisibility rvv = new RVVisibility();
                rvv.RVType = RVVisibility.NameType.Property;
                rvv.PropertyCanRead = property.CanRead;
                rvv.PropertyCanWrite = property.CanWrite;
                rvv.ValueType = property.PropertyType;

                RVControlBase cb = CreateControl(value, property.Name, rvv);
                if (result.Contains(cb) == false)
                    result.Add(cb);
            }

            thisType = thisType.BaseType;
        }
        //
        //result.Sort((a, b) =>
        //{
        //    return string.Compare(a.NameLabel, b.NameLabel, false, System.Globalization.CultureInfo.InvariantCulture);
        //});

        return result;
    }

    GUIStyle GetCollectionGUIStyle(RVSettingData settingData)
    {
        if (this.rvVisibility.RVType == RVVisibility.NameType.Class)
            return settingData.Get_name_class(EditorStyles.foldout);

        return settingData.Get_name_container(EditorStyles.foldout);
    }
     
    bool CollectionUI(bool isOpen, RVSettingData settingData)
    {
        bool isSelected = this.rvcStatus.IsSelected(this.NameLabel);

        GUIStyle guistyle = settingData.Get_name_container(EditorStyles.foldout);

        if (this.rvVisibility.RVType == RVVisibility.NameType.Class)
            guistyle = settingData.Get_name_class(EditorStyles.foldout);

        isOpen = EditorGUILayout.Foldout(isOpen, new GUIContent(this.NameLabel), guistyle);
        RVText.RightClickMenu(GUILayoutUtility.GetLastRect(), 1200, 16, settingData, "Copy", RVText.OnMenuClick_Copy, this.NameLabel, isSelected);

        return isOpen;
    }

    public static string GetTypeName(Type t)
    {
        if (!t.IsGenericType) return t.Name;
        if (t.IsNested && t.DeclaringType.IsGenericType) throw new NotImplementedException();
     
        string txt = t.Name.Substring(0, t.Name.IndexOf('`')) + "<";
        int cnt = 0;
        foreach (Type arg in t.GetGenericArguments())
        {
            if (cnt > 0) txt += ", ";
            txt += GetTypeName(arg);
            cnt++;
        }
        return txt + ">";
    }
}

 