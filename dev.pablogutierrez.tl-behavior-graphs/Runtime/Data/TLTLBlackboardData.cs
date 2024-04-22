using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLTLUnity.Data
{
    [Serializable]
    public class TLTLBlackboardData
    {
        [Serializable]
        public struct Entry
        {
            public string type;
            public string name;
        }

        [field: SerializeField] public List<Entry> Parameters { get; set; }
    }
} // namespace TLTLUnity.Data
