using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Battalion : Unit<BattalionDefinition>
    {
        public List<string> inventory;
        
        public BattalionCommander battalionCommander;
        
        public int currentSoliders;
        public int currentMurale;
        public int currentTraining;
        
        public int currentMovement;

        public Battalion()
        {
            InstanceId = 0;
            inventory = new List<string>();
        }
    }
    
    public class BattalionDescriptor
    {
        public int InstanceId;
        public Vector2Int position;
        public BattalionDefinition Definition;
        public BattalionCommander battalionCommander;
        public int currentSoliders;
        public int currentMurale;
        public int currentTraining;
    }
}
