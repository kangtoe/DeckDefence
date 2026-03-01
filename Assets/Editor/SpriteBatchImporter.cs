using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 스프라이트 일괄 임포트 설정 도구.
/// 소스 스프라이트의 TextureImporter 설정을 대상 폴더의 모든 스프라이트에 복사한다.
/// </summary>
public class SpriteBatchImporter : EditorWindow
{
    private Texture2D _sourceTexture;
    private DefaultAsset _targetFolder;
    private bool _includeSubfolders = true;
    private Vector2 _scrollPosition;

    private TextureImporter _cachedSourceImporter;
    private string _cachedSourcePath;

    private readonly List<string> _targetSpritePaths = new List<string>();

    [MenuItem("Window/Sprite Batch Importer")]
    public static void ShowWindow()
    {
        var window = GetWindow<SpriteBatchImporter>("Sprite Batch Importer");
        window.minSize = new Vector2(380, 500);
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // ── 소스 스프라이트 ──
        EditorGUILayout.LabelField("소스 스프라이트 (Source)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
            "소스 텍스처", _sourceTexture, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            _cachedSourceImporter = null;
            _cachedSourcePath = null;
        }

        if (_sourceTexture != null)
        {
            TextureImporter importer = GetSourceImporter();
            if (importer != null)
                DrawSourceSettings(importer);
            else
                EditorGUILayout.HelpBox(
                    "소스 텍스처의 TextureImporter를 찾을 수 없습니다.",
                    MessageType.Error);
        }

        EditorGUILayout.Space(10);

        // ── 대상 폴더 ──
        EditorGUILayout.LabelField("대상 폴더 (Target)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "대상 폴더", _targetFolder, typeof(DefaultAsset), false);
        _includeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", _includeSubfolders);
        if (EditorGUI.EndChangeCheck())
            RefreshTargetSprites();

        if (_targetFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorGUILayout.HelpBox("유효한 폴더를 선택해주세요.", MessageType.Warning);
            }
            else
            {
                MessageType msgType = _targetSpritePaths.Count > 0
                    ? MessageType.Info
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(
                    $"대상 스프라이트: {_targetSpritePaths.Count}개", msgType);
            }
        }

        EditorGUILayout.Space(10);

        // ── 적용 버튼 ──
        bool canApply = _sourceTexture != null
            && _targetFolder != null
            && _targetSpritePaths.Count > 0
            && GetSourceImporter() != null;

        EditorGUI.BeginDisabledGroup(!canApply);
        if (GUILayout.Button("설정 적용 (Apply)", GUILayout.Height(30)))
            ApplySettings();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    private TextureImporter GetSourceImporter()
    {
        if (_sourceTexture == null) return null;

        string path = AssetDatabase.GetAssetPath(_sourceTexture);
        if (path == _cachedSourcePath && _cachedSourceImporter != null)
            return _cachedSourceImporter;

        _cachedSourcePath = path;
        _cachedSourceImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        return _cachedSourceImporter;
    }

    private void DrawSourceSettings(TextureImporter importer)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("현재 소스 설정", EditorStyles.miniLabel);

        EditorGUI.indentLevel++;
        EditorGUI.BeginDisabledGroup(true);

        EditorGUILayout.EnumPopup("Texture Type", importer.textureType);
        EditorGUILayout.EnumPopup("Sprite Mode", importer.spriteImportMode);
        EditorGUILayout.FloatField("Pixels Per Unit", importer.spritePixelsPerUnit);
        EditorGUILayout.EnumPopup("Filter Mode", importer.filterMode);
        EditorGUILayout.Toggle("Alpha Is Transparency", importer.alphaIsTransparency);
        EditorGUILayout.Toggle("Mipmap Enabled", importer.mipmapEnabled);
        EditorGUILayout.EnumPopup("Compression", importer.textureCompression);
        EditorGUILayout.IntField("Max Texture Size", importer.maxTextureSize);
        EditorGUILayout.EnumPopup("NPOT Scale", importer.npotScale);

        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel--;
    }

    private void RefreshTargetSprites()
    {
        _targetSpritePaths.Clear();

        if (_targetFolder == null) return;

        string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        string sourcePath = _sourceTexture != null
            ? AssetDatabase.GetAssetPath(_sourceTexture)
            : null;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (!_includeSubfolders)
            {
                string dir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                if (dir != folderPath)
                    continue;
            }

            if (assetPath == sourcePath)
                continue;

            _targetSpritePaths.Add(assetPath);
        }
    }

    private void ApplySettings()
    {
        TextureImporter sourceImporter = GetSourceImporter();
        if (sourceImporter == null) return;

        int total = _targetSpritePaths.Count;

        if (!EditorUtility.DisplayDialog(
            "스프라이트 설정 일괄 적용",
            $"{total}개의 스프라이트에 소스 설정을 적용합니다.\n계속하시겠습니까?",
            "적용", "취소"))
            return;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = _targetSpritePaths[i];

                EditorUtility.DisplayProgressBar(
                    "스프라이트 설정 적용 중...",
                    $"({i + 1}/{total}) {Path.GetFileName(path)}",
                    (float)(i + 1) / total);

                TextureImporter targetImporter =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (targetImporter == null) continue;

                CopyImporterSettings(sourceImporter, targetImporter);
                targetImporter.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "완료", $"{total}개의 스프라이트에 설정을 적용했습니다.", "확인");

        RefreshTargetSprites();
    }

    private static void CopyImporterSettings(
        TextureImporter source, TextureImporter target)
    {
        target.textureType = source.textureType;
        target.spriteImportMode = source.spriteImportMode;
        target.spritePixelsPerUnit = source.spritePixelsPerUnit;
        target.filterMode = source.filterMode;
        target.alphaIsTransparency = source.alphaIsTransparency;
        target.mipmapEnabled = source.mipmapEnabled;
        target.textureCompression = source.textureCompression;
        target.maxTextureSize = source.maxTextureSize;
        target.npotScale = source.npotScale;

        TextureImporterPlatformSettings defaultSettings =
            source.GetDefaultPlatformTextureSettings();
        target.SetPlatformTextureSettings(defaultSettings);
    }
}
