﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonflow
{
    
    public class CaptureAnalyzer : EditorWindow
    {
        private string _capturePath;
        private DrawcallAnalyzer[] _drawcallAnalyzers;
        private List<HLSLAnalyzer> _hlslAnalyzers;
        private Dictionary<ShaderCodeIdPair, ShaderCodePair> _shaderCodePairs = new Dictionary<ShaderCodeIdPair, ShaderCodePair>();
        
        public Dictionary<int, BufferDeclaration> cbuffers;
        [MenuItem("Tools/Moonflow/Utility/Capture Analyzer")]
        public static void ShowWindow()
        {
            var _ins = GetWindow<CaptureAnalyzer>("Capture Analyzer");
            _ins.minSize = new Vector2(200, 300);
            _ins.maxSize = new Vector2(200, 300);
        }

        private void OnDestroy()
        {
            DestroyImmediate(this);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_capturePath);
            if (GUILayout.Button("Select Folder"))
            {
                _capturePath = EditorUtility.OpenFolderPanel("Select RenderDoc Capture Folder", "", "");
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Analyze"))
            {
                Analyze(_capturePath);
            }

            if (GUILayout.Button("Save"))
            {
                Save();
            }
        }

        private void Analyze(string capturePath)
        {
            _shaderCodePairs = new Dictionary<ShaderCodeIdPair, ShaderCodePair>();
            _capturePath = capturePath;
            if (Directory.Exists(_capturePath))
            {
                //读取子文件夹列表
                string[] subFolders = Directory.GetDirectories(capturePath);
                _drawcallAnalyzers = new DrawcallAnalyzer[subFolders.Length];
                Debug.Log($"Recognize {subFolders.Length} drawcall in captures");
                for (int i = 0; i < subFolders.Length; i++)
                {
                    string correctFolder = subFolders[i].Replace('\\', '/');
                    Debug.Log($"Analyze {correctFolder}");
                    _drawcallAnalyzers[i] = new DrawcallAnalyzer();
                    _drawcallAnalyzers[i].Setup(correctFolder, this);
                }
            }
            else
            {
                Debug.LogError("Capture folder is not found!");
            }
            Debug.Log("Finish setup drawcall data, starting analyze hlsl code");
            AnalyzeHLSL();
            Debug.Log("Finish!!");
        }

        private void AnalyzeHLSL()
        {
            _hlslAnalyzers = new List<HLSLAnalyzer>();
            foreach (var codepair in _shaderCodePairs)
            {
                var _HLSLAnalyzer = new HLSLAnalyzer();
                _HLSLAnalyzer.Setup(this, codepair.Value);
                _hlslAnalyzers.Add(_HLSLAnalyzer);
            }

            foreach (var hlslAnalyzer in _hlslAnalyzers)
            {
                hlslAnalyzer.Analyze();
            }
        }

        private void Save()
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < _drawcallAnalyzers.Length; i++)
            {
                _drawcallAnalyzers[i].Save();
            }

            for (int i = 0; i < _hlslAnalyzers.Count; i++)
            {
                _hlslAnalyzers[i].SaveAsFile(_capturePath);
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        public void AddShaderFile(ShaderCodePair pair)
        {
            _shaderCodePairs.TryAdd(pair.id, pair);
        }
    }
}