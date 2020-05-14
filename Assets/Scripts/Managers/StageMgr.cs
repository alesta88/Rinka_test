﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// ステージを管理するクラス
/// </summary>
public class StageMgr : MonoSingleton<StageMgr> {
    [Header("Stage Chunks")]
    [SerializeField] StageFlowData m_stageFlow;
    [SerializeField] GameObject m_chunkViewPrefab;

    public int ConsumedOrbsInStage;
    public StageMetaData TitleStage => m_stageFlow.Stages[0];
    public StageChunkData CurrentStage => m_viewQueue.Count == 2 ? m_viewQueue.ElementAt( 0 ).Data : m_viewQueue.ElementAt( 1 ).Data;

    Queue<StageChunkView> m_viewQueue = new Queue<StageChunkView>();
    const int MAX_CHUNKS = 3;
    bool m_isSpawnPlayer => m_viewQueue.Count == 1;
    StageChunkPool m_pool;


    void Awake() {
        m_pool = new StageChunkPool( m_chunkViewPrefab );
    }

    public void InitSpawnStage( Action<Player> onFinish = null ) {
        InitSpawnStage( TitleStage, onFinish );
    }

    public void InitSpawnStage( StageMetaData stageMetaData, Action<Player> onFinish = null ) {
        SceneMgr.Instance.FadeIn( 0f );
        ReturnAllChunks();
        DisplayNextChunk( stageMetaData );
        DisplayNextChunk( stageMetaData );
        PlayerMgr.Instance.InstantiatePlayer( onFinish );
    }

    /// <summary>
    /// 次のステージの一部を表示
    /// </summary>
    public void DisplayNextChunk( StageMetaData stage ) {
        var currChunk = m_viewQueue.LastOrDefault();
        var nextChunk = m_pool.Rent();
        m_viewQueue.Enqueue( nextChunk );
        if( m_viewQueue.Count > MAX_CHUNKS ) {
            var prevChunk = m_viewQueue.Dequeue();
            m_pool.Return( prevChunk );
        }

        StageChunkData nextChunkData = GetNextStageChunk();
        Vector2? playerSpawnPosition = m_isSpawnPlayer ? nextChunkData.PlayerSpawnPosition : default( Vector2? );
        nextChunk.Display( currChunk, nextChunkData, playerSpawnPosition );
        nextChunk.OnHitBoundary = ( col ) => {
            if( col.tag == Define.Tag.PLAYER && m_viewQueue.Count >= 2 ) {
                DisplayNextChunk( stage );
                nextChunk.OnHitBoundary = null;
            }
        };
    }

    /// <summary>
    /// 次のステージ部分を取得
    /// </summary>
    StageChunkData GetNextStageChunk() {
        UpdateCurrentStage();

        StageChunkData nextStageChunk = null;
        // 死んだ場合、同じステージ部分からスタート
        if( GameModel.StageWhenDied.Value != null ) {
            nextStageChunk = GameModel.StageWhenDied.Value;
            GameModel.SpawnStageChunk.Value = nextStageChunk;
            GameModel.StageWhenDied.Value = null;
        // 死んでいなくて最初プレイの場合、、最初のステージ部分スタート
        } else if( m_isSpawnPlayer ) {
            nextStageChunk = GameModel.Stage.Value.Chunks[0];
            GameModel.SpawnStageChunk.Value = nextStageChunk;
            // それ以外は、ランダムステージ部分へ遷移
        } else {
            nextStageChunk = GameModel.Stage.Value.Chunks.Random();
        }

        return nextStageChunk;
    }

    void UpdateCurrentStage() {
        // 最後のステージが生成後の次のUpdate時の設定する
        GameModel.IsLastStage.Value = ( GameModel.Stage.Value.Difficulty == 5 );

        // 決まった玉の数以上を消費した場合、次のステージへ遷移する 
        for( int i = 0; i < m_stageFlow.Stages.Length; i++ ) {
            var stage = m_stageFlow.Stages[i];
            if( stage == GameModel.Stage.Value && ConsumedOrbsInStage >= stage.OrbClearCount ) {
                int nextStageIndex = Mathf.Min( i + 1, m_stageFlow.Stages.Length - 1 );
                GameModel.Stage.Value = m_stageFlow.Stages[nextStageIndex];
                ConsumedOrbsInStage = 0;
                return;
            }
        }
    }

    public void ReturnAllChunks() {
        foreach( var chunk in m_viewQueue ){
            m_pool.Return( chunk );
        }
        m_viewQueue.Clear();
    }
}
