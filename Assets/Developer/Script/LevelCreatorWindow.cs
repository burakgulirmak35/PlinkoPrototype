
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using PlinkoPrototype;

public class LevelCreatorWindow : EditorWindow
{
    private int levelId = 1;
    private int bucketCount = 5;
    private int ballsRequiredForLevel = 15;

    private List<BucketData> buckets = new List<BucketData>();
    private List<Color> bucketColors = new List<Color>();

    private Vector2 scrollPos;

    [MenuItem("Plinko Tools/Level Creator")]
    public static void ShowWindow()
    {
        GetWindow<LevelCreatorWindow>("Level Creator");
    }

    private void OnEnable()
    {
        GenerateDefaultLevel();
    }

    // ---------------------------
    // LOAD LEVEL JSON
    // ---------------------------
    private bool LoadLevelFromJson(int level)
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets/Levels");
        string path = Path.Combine(dir, $"level_{level}.json");

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog(
                "Not Found",
                $"level_{level}.json bulunamadı.",
                "OK"
            );
            return false;
        }

        string json = File.ReadAllText(path);
        LevelData loaded = JsonUtility.FromJson<LevelData>(json);

        ballsRequiredForLevel = loaded.ballsRequiredForLevel;

        buckets = loaded.buckets;
        bucketCount = buckets.Count;

        bucketColors = new List<Color>();
        foreach (var b in buckets)
        {
            if (ColorUtility.TryParseHtmlString(b.color, out Color c))
                bucketColors.Add(c);
            else
                bucketColors.Add(Color.white);
        }

        return true;
    }

    // ---------------------------
    // DEFAULT TEMPLATE
    // ---------------------------
    private void GenerateDefaultLevel()
    {
        buckets = new List<BucketData>();
        bucketColors = new List<Color>();

        for (int i = 0; i < bucketCount; i++)
        {
            buckets.Add(new BucketData
            {
                score = 10,
                color = "#FFFFFF"
            });

            bucketColors.Add(Color.white);
        }
    }

    // ---------------------------
    // GUI
    // ---------------------------
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Plinko Level Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        levelId = EditorGUILayout.IntField("Level ID", levelId);

        if (GUILayout.Button("Load Level", GUILayout.Width(120)))
        {
            LoadLevelFromJson(levelId);
        }
        EditorGUILayout.EndHorizontal();

        int newBucketCount = EditorGUILayout.IntField("Bucket Count", bucketCount);
        if (newBucketCount != bucketCount)
        {
            bucketCount = newBucketCount;
            GenerateDefaultLevel();
        }

        ballsRequiredForLevel =
            EditorGUILayout.IntField("Balls Required For Level", ballsRequiredForLevel);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bucket Definitions", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));

        for (int i = 0; i < buckets.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Bucket {i + 1}");

            buckets[i].score =
                EditorGUILayout.IntField("Score", buckets[i].score);

            bucketColors[i] =
                EditorGUILayout.ColorField("Color", bucketColors[i]);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Save Level JSON", GUILayout.Height(40)))
        {
            SaveLevelJSON();
        }
    }

    // ---------------------------
    // SAVE JSON
    // ---------------------------
    private void SaveLevelJSON()
    {
        for (int i = 0; i < buckets.Count; i++)
            buckets[i].color =
                "#" + ColorUtility.ToHtmlStringRGB(bucketColors[i]);

        LevelData levelData = new LevelData
        {
            ballsRequiredForLevel = ballsRequiredForLevel,
            buckets = buckets
        };

        string dir = Path.Combine(Application.dataPath, "StreamingAssets/Levels");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, $"level_{levelId}.json");

        string json = JsonUtility.ToJson(levelData, true);
        File.WriteAllText(filePath, json);

        AssetDatabase.Refresh();
    }
}

#endif
