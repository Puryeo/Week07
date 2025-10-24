using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 테트리스 게임 로직 관리자
/// - 라인 제거 카운트
/// - 모드 전환 결정
/// - 블록 제거 처리
/// 
/// 실제 블록 스폰은 TetrisBlockSpawner가 담당
/// </summary>
public class TetrisGameManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [Tooltip("테트리스 블록 스포너 참조")]
    [SerializeField] private TetrisBlockSpawner blockSpawner;

    [Tooltip("테트리스 라인 체커 참조")]
    [SerializeField] private TetrisLineChecker lineChecker;

    [Header("Game Settings")]
    [Tooltip("라인이 제거될 때마다 폭탄 블록 소환")]
    [SerializeField] private bool spawnBombOnLineClear = true;

    [Tooltip("일반 블록 스폰을 중지할 라인 수")]
    [SerializeField] private int linesToStopNormalSpawn = 2;

    #endregion

    #region Private Fields

    private int totalLinesCleared = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateSettings();
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    #endregion

    #region Validation

    private void ValidateSettings()
    {
        if (blockSpawner == null)
        {
            enabled = false;
            return;
        }

        if (lineChecker == null)
        {
            enabled = false;
            return;
        }
    }

    #endregion

    #region Event Registration

    private void RegisterEvents()
    {
        if (lineChecker != null)
        {
            lineChecker.onLineRemoved.AddListener(OnLineRemoved);
        }
    }

    private void UnregisterEvents()
    {
        if (lineChecker != null)
        {
            lineChecker.onLineRemoved.RemoveListener(OnLineRemoved);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 라인 제거 이벤트 핸들러
    /// </summary>
    private void OnLineRemoved(float height, bool isBombLine)
    {
        totalLinesCleared++;

        // 라인 제거할 때마다 폭탄 블록 1개 소환 (제한 없음)
        if (spawnBombOnLineClear)
        {
            blockSpawner.SpawnBombBlock();
        }

        // 2줄 이상 지우면 일반 블록 스폰 중지
        if (totalLinesCleared >= linesToStopNormalSpawn)
        {
            blockSpawner.StopSpawning();
        }
    }

    #endregion

}