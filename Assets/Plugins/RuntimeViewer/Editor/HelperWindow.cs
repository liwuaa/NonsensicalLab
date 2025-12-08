using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class HelperWindow : EditorWindow
{
    readonly int space_vertical = 8;

    float heigh = 30;

    void OnGUI()
    {
        GUILayout.Space(space_vertical);

        GUILayout.Label("Open Asset store page (check update!)");
        if (GUILayout.Button("Open", GUILayout.Width(120), GUILayout.Height(heigh)))
        {
            Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/67350");
        }

        GUILayout.Space(space_vertical);

        GUILayout.Label("Go to forum");
        if (GUILayout.Button("Open", GUILayout.Width(120), GUILayout.Height(heigh)))
        {
            Application.OpenURL("http://forum.unity3d.com/threads/released-runtimeviewer.428169/");
        }

        GUILayout.Space(space_vertical);

        GUILayout.Label("Video Tutorial ");
        if (GUILayout.Button("Japanese and English", GUILayout.Width(160), GUILayout.Height(heigh)))
        {
            Application.OpenURL("https://youtu.be/rsCuZewynV0?t=5");
        }

        if (GUILayout.Button("Chinese", GUILayout.Width(120), GUILayout.Height(heigh)))
        {
            Application.OpenURL("http://www.tudou.com/programs/view/gIjqWPY5zrM");
        }

        GUILayout.Space(space_vertical);
        GUILayout.Space(space_vertical);
        GUILayout.Label("If you have any question  please feel free to email me!");
        GUILayout.Label("< dsh0079@gmail.com or 710074758@qq.com >");
        GUILayout.Space(space_vertical);
    }

    void OnInspectorUpdate()
    {
        this.Repaint();
    }

    public static void OpenWindow()
    {
        //创建窗口
        HelperWindow window = EditorWindow.GetWindow<HelperWindow>( true, "RuntimeViewer");
        window.Show();
    }
}
