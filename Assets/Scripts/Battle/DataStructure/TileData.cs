using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [System.Serializable]
    public class TileData
    {
        public Battalion Battalion;
        public Facility Facility;
        public bool IsEmpty => Battalion == null && Facility == null;
    }
}
