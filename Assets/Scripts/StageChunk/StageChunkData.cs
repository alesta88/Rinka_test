﻿using System;
using UnityEngine;
using Malee;

[Serializable]
public class StageChunkData : ScriptableObject {
    [Range( 0, 10 )] public int Difficulty; 
    public Vector2 LowerEntryPosition;
    public Vector2 LowerExitPosition;
    public Sprite ItemSpawnAreaSprite;
    public StageBackgroundData BackgroundData;
    public GameObject WallObject;
    public Vector2 PlayerSpawnPosition;
    [Header( "Orb Distance" )]
    [Range( 0f, 4f )] public float MinOrbDistance;
    [Range( 0f, 4f )] public float MaxOrbDistance;
    [Header( "Orb Count" )]
    [Range( 0, 10 )] public int MinOrbCount;
    [Range( 0, 10 )] public int MaxOrbCount;
}

[Serializable]
public class StageChunkDataList : ReorderableArray<StageChunkData> {
}
