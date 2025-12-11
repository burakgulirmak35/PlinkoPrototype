using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using PlinkoPrototype;

public class LevelCreatorWindow : EditorWindow
{
    private int levelId = 1;
    private int bucketCount = 5;
    private int ballCount = 200;

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
        GenerateDefaultLevel(); // ilk açılış
    }

    // ---------------------------
    //  LOAD LEVEL JSON FROM FILE
    // ---------------------------
    private bool LoadLevelFromJson(int level)
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets/Levels");
        string path = Path.Combine(dir, $"level_{level}.json");

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Not Found",
                $"level_{level}.json bulunamadı.\nYeni bir level oluşturabilirsiniz.",
                "Tamam");
            return false;
        }

        string json = File.ReadAllText(path);
        LevelData loaded = JsonUtility.FromJson<LevelData>(json);

        // Editor alanına doldur
        bucketCount = loaded.bucketCount;
        ballCount = loaded.ballCount;

        buckets = loaded.buckets;
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
    //  DEFAULT LEVEL TEMPLATE
    // ---------------------------
    private void GenerateDefaultLevel()
    {
        buckets = new List<BucketData>();
        bucketColors = new List<Color>();

        for (int i = 0; i < bucketCount; i++)
        {
            buckets.Add(new BucketData()
            {
                score = 10,
                color = "#FFFFFF"
            });

            bucketColors.Add(Color.white);
        }
    }

    // ---------------------------
    //  GUI
    // ---------------------------
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Plinko Level Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        levelId = EditorGUILayout.IntField("Level ID", levelId);

        if (GUILayout.Button("Load Level", GUILayout.Width(120)))
        {
            if (LoadLevelFromJson(levelId))
                Debug.Log($"Loaded level_{levelId}.json");
        }
        EditorGUILayout.EndHorizontal();

        int newBucketCount = EditorGUILayout.IntField("Bucket Count", bucketCount);
        if (newBucketCount != bucketCount)
        {
            bucketCount = newBucketCount;
            GenerateDefaultLevel();
        }

        ballCount = EditorGUILayout.IntField("Ball Count", ballCount);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bucket Definitions", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));

        for (int i = 0; i < buckets.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Bucket {i + 1}");

            buckets[i].score = EditorGUILayout.IntField("Score", buckets[i].score);
            bucketColors[i] = EditorGUILayout.ColorField("Color", bucketColors[i]);

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
    //  SAVE LEVEL JSON
    // ---------------------------
    private void SaveLevelJSON()
    {
        // Color → Hex formatı
        for (int i = 0; i < buckets.Count; i++)
            buckets[i].color = "#" + ColorUtility.ToHtmlStringRGB(bucketColors[i]);

        LevelData levelData = new LevelData()
        {
            id = levelId,
            bucketCount = bucketCount,
            ballCount = ballCount,
            buckets = buckets
        };

        string dir = Path.Combine(Application.dataPath, "StreamingAssets/Levels");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, $"level_{levelId}.json");

        string json = JsonUtility.ToJson(levelData, true);
        File.WriteAllText(filePath, json);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Level Saved",
            $"level_{levelId}.json başarıyla kaydedildi.",
            "OK"
        );
    }
}
