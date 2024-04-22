using System;
using System.Collections.Generic;
using TLTLPredicateBuilder.Windows;
using TLTLUnity.Data;
using UnityEditor;
using UnityEngine;

namespace TLTLPredicateBuilder.Data
{
    public class TLTLGraphSaveData : ScriptableObject
    {
        [Serializable]
        public struct NodePositionEntry
        {
            public string nodeId;
            public Vector2 position;
        }
        [field: SerializeField] public string FileName { get; set; }
        [field: SerializeField] public TLTLPredicateDataAsset PredicateAsset { get; set; }
        [field: SerializeField] public Vector2 ReturnNodePosition { get; set; }
        [field: SerializeField] public List<NodePositionEntry> NodePositions { get; set; }
        [field: SerializeField] public List<string> OldNodeNames { get; set; }

        public void Initialize(string fileName)
        {
            FileName = fileName;

            NodePositions = new List<NodePositionEntry>();
            OldNodeNames = new List<string>();
            ReturnNodePosition = Vector2.zero;
        } // Initialize

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            TLTLGraphSaveData data = Selection.activeObject as TLTLGraphSaveData;
            if (data != null)
            {
                TLTLPredicateBuilderEditorWindow.Open(data);
                return true; //catch open file
            }

            return false; // let unity open the file
        }

    } // TLTLGraphSaveData

} // namespace TLTLPredicateBuilder.Data
