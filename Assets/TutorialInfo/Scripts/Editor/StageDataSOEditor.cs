#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(StageDataSO))]
public class StageDataSOEditor : Editor
{
    private string[] sceneNames;
    private string[] scenePaths;
    private double lastRefreshTime = 0f;

    private void OnEnable()
    {
        RefreshSceneList();
        EditorBuildSettings.sceneListChanged += RefreshSceneList;
    }

    private void OnDisable()
    {
        EditorBuildSettings.sceneListChanged -= RefreshSceneList;
    }

    private void RefreshSceneList()
    {
        // 너무 자주 호출되지 않게 (Repaint 방지)
        if (EditorApplication.timeSinceStartup - lastRefreshTime < 0.2f)
            return;

        var scenes = EditorBuildSettings.scenes;
        sceneNames = new string[scenes.Length];
        scenePaths = new string[scenes.Length];

        for (int i = 0; i < scenes.Length; i++)
        {
            sceneNames[i] = Path.GetFileNameWithoutExtension(scenes[i].path);
            scenePaths[i] = scenes[i].path;
        }

        lastRefreshTime = EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        StageDataSO data = (StageDataSO)target;

        if (sceneNames == null || sceneNames.Length == 0)
            RefreshSceneList();

        if (sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("빌드 세팅에 등록된 씬이 없습니다.", MessageType.Warning);
            return;
        }

        int currentIndex = Mathf.Max(0, System.Array.IndexOf(sceneNames, data.SceneName));

        EditorGUI.BeginChangeCheck();
        int selectedIndex = EditorGUILayout.Popup("Scene Name", currentIndex, sceneNames);
        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Play 모드 중에는 StageDataSO의 SceneName을 변경할 수 없습니다.");
                return;
            }

            Undo.RecordObject(data, "Change Scene Name");
            data.SetSceneInfo(sceneNames[selectedIndex], scenePaths[selectedIndex]);
            EditorUtility.SetDirty(data);

            // 실제 .asset 파일에 즉시 저장 (빌드 반영용)
            AssetDatabase.SaveAssets();

            Debug.Log($"{data.name} : {sceneNames[selectedIndex]} 씬으로 설정 및 저장 완료");
        }
    }
}
#endif
