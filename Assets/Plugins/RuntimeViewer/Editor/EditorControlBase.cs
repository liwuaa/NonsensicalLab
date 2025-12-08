using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class RVControlBase
{
    public string UID { private set; get; }//用于识别的唯一ID, 就是名字路径,如果在集合中则还需加上编号

    public readonly int depth = 0;

    public string NameLabel { private set; get; }
    protected object data = null;

    //层缩进长度,其实值为 Indent * depth
    public static readonly int Indent_class = 5;

    public static readonly int Indent_field = 5;
    public float IndentPlus = 0;

    protected RVVisibility rvVisibility;

    public RVControlBase(string UID,string nameLabel, object data, int depth, RVVisibility rvVisibility)
    {
        this.UID = UID;
        this.NameLabel = nameLabel;
        this.data = data;
        this.depth = depth;
        this.rvVisibility = rvVisibility;
    }

    public virtual void OnGUIUpdate(bool isRealtimeUpdate, RVSettingData settingData, RVCStatus rvcStatus)
    {
    }

    public virtual void OnDestroy()
    {

    }

    public override bool Equals(object obj)
    {
        if (obj is RVControlBase == false)
            return false;
        return this.UID.Equals(((RVControlBase)obj).UID);
    }

    public override int GetHashCode()
    {
        return this.UID.GetHashCode();
    }
}
