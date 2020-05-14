using System;
using UnityEngine;
using UniRx;

/// <summary>
/// プレイヤーを管理するクラス
/// </summary>
public class PlayerMgr : MonoSingleton<PlayerMgr> {
    [SerializeField] GameObject PlayerPrefab;
    [SerializeField] bool IsInvulnerable;

    public Player PlayerInstance { private set; get; }


    public void InstantiatePlayer( Action<Player> onFinish ) {
        if( PlayerInstance == null ) {
            var playerObj = GameObject.Instantiate<GameObject>( PlayerPrefab );
            PlayerInstance = playerObj.GetComponent<Player>();
            PlayerInstance.IsInvulnerable = IsInvulnerable;
        }

        MovePlayerToSpawnPosition( onFinish );
    }

    void MovePlayerToSpawnPosition( Action<Player> onFinish ) {
        PlayerInstance.Move.Value = Player.MoveState.Init;
        CameraMgr.Instance.Follow( PlayerInstance.transform );
        Observable.NextFrame().Subscribe( _ => AfterCameraInit( onFinish ) );
    }

    void AfterCameraInit( Action<Player> onFinish ) {
        PlayerInstance.transform.position = Define.Origin + GameModel.SpawnStageChunk.Value.PlayerSpawnPosition;
        SceneMgr.Instance.FadeOut( onFinish: () => onFinish?.Invoke( PlayerInstance ) );
    }
}
