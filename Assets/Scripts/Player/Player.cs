using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UniRx;
using DG.Tweening;

/// <summary>
/// プレイヤー
/// </summary>
public class Player : MonoBehaviour {
    [Header( "Fly Movement" )]
    [SerializeField] Vector2 m_flyVector;
    [Header( "Glide Movement" )]
    [SerializeField] Vector2 m_glideVector;
    [SerializeField] float m_glideDuration;
    [Header( "Fall Movement" )]
    [SerializeField] Vector2 m_fallVector;
    [Header( "----------------------------------------" )]
    [Header( "Death" )]
    [SerializeField] float m_deathIlluminationRadius;
    [SerializeField] Color m_deathColor;
    [SerializeField] Animator m_deathAnim;
    [SerializeField] SpriteRenderer m_deathSpriteRenderer;
    [Header( "----------------------------------------" )]
    [Header( "Inner Glow Speed" )]
    [SerializeField] [Range( 0f, 2f )] float m_innerGlowOnSpeed = 0f;
    [SerializeField] [Range( 0f, 2f )] float m_innerGlowOffSpeed = 0f;
    [Header( "Illumination Radius" )]
    [SerializeField] [Range( 0f, 1.5f) ] float m_minIlluminateRadius = 0f;
    [SerializeField] [Range( 0f, 1.5f )] float m_maxIlluminateRadius = 0f;
    [Header( "Illumination Speed" )]
    [SerializeField] [Range( 0f, 2f )] float m_illuminateOnSpeed = 0f;
    [SerializeField] [Range( 0f, 20f )] float m_illuminateOffSpeed = 0f;
    [SerializeField] [Range( 0f, 2f )] float m_illuminateSwitchInterval = 0f;
    [Header( "----------------------------------------" )]
    [Header("Sprites")]
    [SerializeField] SpriteRenderer m_wispSprite;
    [SerializeField] SpriteRenderer m_wispInnerGlow;
    [SerializeField] SpriteRenderer m_wispIllumination;
    [Header( "Other" )]
    [SerializeField] Rigidbody2D m_rb;

    public enum MoveState { Init, Fly, Glide, Fall, Die }
    public ReactiveProperty<MoveState> Move = new ReactiveProperty<MoveState>( MoveState.Init );
    public bool IsInvulnerable { set; get; }
    public bool CanInteract => Move.Value != MoveState.Die && GameModel.GameState.Value == Define.GameState.Playing;

    bool m_isPaused => ( GameModel.GameState.Value != Define.GameState.Playing );
    Sequence m_illuminateSeq;
    Tweener m_innerGlowTween;
    Action m_movePlayerAction = () => { };
    float m_glideTime;
    float? m_startPosX;
    float m_playTime;

    //***********************************************************
    // 初期化
    //***********************************************************
    void Awake() {
        var onMoveStateChanged = Move.TakeUntilDisable( this ).DistinctUntilChanged();
        onMoveStateChanged.Where( state => state == MoveState.Init ).Subscribe( _ => OnInit() );
        onMoveStateChanged.Where( state => state == MoveState.Fly ).Subscribe( _ => OnFly() );
        onMoveStateChanged.Where( state => state == MoveState.Glide ).Subscribe( _ => OnGlide() );
        onMoveStateChanged.Where( state => state == MoveState.Fall ).Subscribe( _ => OnFall() );
        onMoveStateChanged.Where( state => state == MoveState.Die ).Subscribe( _ => OnDie() );

        GameModel.GameState.TakeUntilDisable( this ).Where( state => state != Define.GameState.Playing ).Subscribe( _ => OnPaused() );
        GameModel.GameState.TakeUntilDisable( this ).Where( state => state == Define.GameState.Playing ).Subscribe( _ => OnUnpause() );

        MessageBroker.Default.Receive<PlayerDeathEvent>().TakeUntilDisable( this ).Subscribe( _ => Move.Value = MoveState.Die );
        MessageBroker.Default.Receive<ConsumeOrbEvent>().TakeUntilDisable( this ).Subscribe( orbEvent => OnPlayerConsumedOrb( orbEvent.Orb ) );
    }

    void OnPlayerConsumedOrb( OrbView orb ) {
        Illuminate();
        StageMgr.Instance.ConsumedOrbsInStage++;
        SetWispSprite( orb.Data );
    }

    public void SetWispSprite( OrbData orbData ) {
        m_wispSprite.sprite = orbData.WispSprite;
        m_wispIllumination.sprite = orbData.WispIlluminationSprite;
    }

    void Illuminate() {
        if( m_illuminateSeq != null && m_illuminateSeq.IsPlaying() ) {
            m_illuminateSeq.Kill();
        }

        m_illuminateSeq = DOTween.Sequence();
        m_illuminateSeq.Append( m_wispIllumination.transform.DOScaleAtSpeed( m_maxIlluminateRadius, m_illuminateOnSpeed ).SetUpdate( true ) )
                       .AppendInterval( m_illuminateSwitchInterval )
                       .Append( m_wispIllumination.transform.DOScale( m_minIlluminateRadius, m_illuminateOffSpeed ).SetUpdate( true ) )
                       .SetUpdate( true )
                       .Play();
    }

    //***********************************************************
    // ポーズ関係
    //***********************************************************
    void OnPaused() {
        m_rb.velocity = Vector2.zero;
        m_rb.simulated = false;
        m_illuminateSeq.Pause();
    }

    void OnUnpause() {
        m_rb.simulated = true;
        m_illuminateSeq.Play();
    }

    //***********************************************************
    // 飛ぶ行動
    //***********************************************************
    void FixedUpdate() {
        if( m_isPaused || Move.Value == MoveState.Init )
            return;

        m_movePlayerAction();
    }

    void Update() {
        if( m_isPaused || Move.Value == MoveState.Init )
            return;

        UpdateFlyState();
        UpdateDistance();
    }

    void UpdateFlyState() {
        if(Move.Value == MoveState.Glide) {
            m_glideTime += Time.deltaTime;
            if(m_glideTime > m_glideDuration) {
                Move.Value = MoveState.Fall;
            }
        } else {
            m_glideTime = 0f;
        }
        m_playTime += Time.deltaTime;
    }

    void UpdateDistance() {
        if( !m_startPosX.HasValue ) {
            m_startPosX = transform.position.x;
        }

        float dis = ( transform.position.x - m_startPosX.Value ) * Define.DISTANCE_MULTIPLIER;
        if( (int)dis != GameModel.CurrentLifeDistance.Value ) {
            GameModel.CurrentLifeDistance.Value = (int)dis;
        }
    }

    //***********************************************************
    // ステート変更
    //***********************************************************
    void OnInit() {
        m_playTime = 0f;
        m_illuminateSeq?.Kill();
        m_rb.velocity = Vector2.zero;
        EnableInteraction( false );
        m_wispSprite.enabled = true;
        m_wispInnerGlow.enabled = true;
        m_wispIllumination.transform.localScale = Vector3.one * m_minIlluminateRadius;
        m_wispIllumination.color = Color.white;
        m_movePlayerAction = () => { };
    }

    void OnFly() {
        EnableInteraction( true );
        PlayInnerGlowTween( isOn: true );
        m_movePlayerAction = () => m_rb.AddForce( m_flyVector * Time.fixedDeltaTime, ForceMode2D.Force );
    }

    void OnGlide() {
        EnableInteraction( true );
        PlayInnerGlowTween( isOn: false );
        m_movePlayerAction = () => m_rb.AddForce( m_glideVector * Time.fixedDeltaTime, ForceMode2D.Force );
    }

    void OnFall() {
        EnableInteraction( true );
        m_movePlayerAction = () => m_rb.velocity = m_fallVector * Time.fixedDeltaTime;
    }

    void OnDie() {
        m_rb.simulated = false;
        m_deathAnim.SetTrigger( "play_death" );
        m_wispSprite.enabled = false;
        m_wispInnerGlow.enabled = false;
        m_wispIllumination.transform.localScale = Vector3.one * m_deathIlluminationRadius;
        m_wispIllumination.color = m_deathColor;
        m_movePlayerAction = () => { };

        Analytics.CustomEvent( Define.AnalyticsEvent.STAGE_SELECTION, new Dictionary<string, object>() {
            ["time"] = m_playTime
        } );
        m_playTime = 0f;
    }

    void PlayInnerGlowTween( bool isOn ) {
        if(m_innerGlowTween != null && m_innerGlowTween.IsPlaying()) {
            m_innerGlowTween.Kill();
        }
        m_innerGlowTween = isOn ?
            m_wispInnerGlow.DOFadeAtSpeed( 1f, m_innerGlowOnSpeed ).SetUpdate( true ) :
            m_wispInnerGlow.DOFadeAtSpeed( 0f, m_innerGlowOffSpeed ).SetUpdate( true );
    }

    void EnableInteraction( bool isEnabled ) {
        bool isPlaying = GameModel.GameState.Value == Define.GameState.Playing;
        m_rb.simulated = isPlaying && isEnabled;
        m_rb.gravityScale = isEnabled ? 0.5f : 0f;
    }

    //***********************************************************
    // Collisions
    //***********************************************************
    void OnTriggerEnter2D( Collider2D other ) {
        if( IsInvulnerable || Move.Value == MoveState.Init )
            return;

        if( other.transform.tag == Define.Tag.WALL ) {
            // 壁にぶつけた
            MessageBroker.Default.Publish( new PlayerDeathEvent() );
        } else if( other.tag == Define.Tag.ORB ) {
            // Orbの取得した
            MessageBroker.Default.Publish( new ConsumeOrbEvent( other.GetComponent<OrbView>() ) );
        }
    }

    //***********************************************************
    // Destroy
    //***********************************************************
    void OnDestroy() {
        m_illuminateSeq.Kill();
        m_innerGlowTween.Kill();
    }
}
