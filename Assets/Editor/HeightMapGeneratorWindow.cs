using UnityEngine;
using UnityEditor;
using System.IO;

public class HeightMapGeneratorWindow : EditorWindow
{
    Texture2D texture;
    HeightMap map;
    int size = 256;

    // Circle and Cone
    float radius;
    int h, k;
    float circleWeight = 1;

    // Perlin
    float perlinScale = 1;
    float perlinWeight = 1;
    int perlinOctaves = 1;

    // Fading
    int fadeRadius = 1;

    [MenuItem("Window/Height Map")]
    public static void Init()
    {
        HeightMapGeneratorWindow window = GetWindow<HeightMapGeneratorWindow>();
        window.titleContent = new GUIContent("Height Map");
    }

    void OnEnable()
    {
        map = ScriptableObject.CreateInstance<HeightMap>();
        map.Init(size, size);
        texture = new Texture2D(size, size);
        Undo.undoRedoPerformed += UndoRedoCallback;
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Texture Generation");
        size = EditorGUILayout.IntPopup(size, new string[] { "32", "64", "128", "256", "512", "1024" }, new int[] { 32, 64, 128, 256, 512, 1024 }, GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            texture = new Texture2D(size, size);
            map = ScriptableObject.CreateInstance<HeightMap>();
            map.Init(size, size);
        }

        // Save as png, the height map object is 10x smaller
        if (GUILayout.Button("Save"))
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Height Maps/Map.png");
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }

        EditorGUILayout.LabelField(new GUIContent(texture), GUILayout.Width(size), GUILayout.Height(size));

        EditorGUILayout.Separator();

        using (var horizontal = new EditorGUILayout.HorizontalScope())
        {
            using (var vertical = new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(300)))
            {
                EditorGUILayout.LabelField("Draw Circle", EditorStyles.largeLabel);
                radius = EditorGUILayout.Slider("Radius", radius, 0, size / 1.5f);
                EditorGUILayout.LabelField("Center");
                h = EditorGUILayout.IntSlider("x", h, 0, size);
                k = EditorGUILayout.IntSlider("y", k, 0, size);
                circleWeight = EditorGUILayout.Slider("Weight", circleWeight, 0, 1);

                if (GUILayout.Button("Add Circle"))
                {
                    Undo.RecordObject(map, "Add Circle");
                    map.AddCircle(radius, circleWeight, h, k);
                    texture = map.GetTexture();
                }

                if (GUILayout.Button("Add Cone"))
                {
                    Undo.RecordObject(map, "Add Cone");
                    map.AddCone(radius, circleWeight, h, k);
                    texture = map.GetTexture();
                }
            }

            EditorGUILayout.Separator();

            using (var vertical = new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(300)))
            {
                EditorGUILayout.LabelField("Perlin Noise", EditorStyles.largeLabel);

                perlinScale = EditorGUILayout.Slider("Scale", perlinScale, 0.1f, 15);
                perlinWeight = EditorGUILayout.Slider("Weight", perlinWeight, 0.01f, 1f);
                perlinOctaves = EditorGUILayout.IntSlider("Octaves", perlinOctaves, 1, 15);

                if (GUILayout.Button("Add Perlin"))
                {
                    Undo.RecordObject(map, "Add Perlin");
                    map.AddPerlin(perlinScale, perlinWeight, perlinOctaves);
                    texture = map.GetTexture();
                }
            }
        }

        EditorGUILayout.Separator();

        using (var vertical = new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(300)))
        {
            EditorGUILayout.LabelField("Masking", EditorStyles.largeLabel);
            fadeRadius = EditorGUILayout.IntSlider("Radius", fadeRadius, 1, map.width / 2);

            if (GUILayout.Button("Circle"))
            {
                Undo.RecordObject(map, "Circle Mask");
                map.MaskCircle(fadeRadius);
                texture = map.GetTexture();
            }
        }
    }

    void UndoRedoCallback()
    {
        texture = map.GetTexture();
        Repaint();
    }
}