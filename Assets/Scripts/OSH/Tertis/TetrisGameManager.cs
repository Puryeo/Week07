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
            Debug.LogError("[TetrisGameManager] blockSpawner가 설정되지 않았습니다!");
            enabled = false;
            return;
        }

        if (lineChecker == null)
        {
            Debug.LogError("[TetrisGameManager] lineChecker가 설정되지 않았습니다!");
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
        Debug.Log($"[TetrisGameManager] 라인 제거됨 - 높이: {height}, 총 라인: {totalLinesCleared}");

        // 라인 제거할 때마다 폭탄 블록 1개 소환 (제한 없음)
        if (spawnBombOnLineClear)
        {
            blockSpawner.SpawnBombBlock();
            Debug.Log($"[TetrisGameManager] 폭탄 블록 소환 (총 라인: {totalLinesCleared})");
        }

        // 2줄 이상 지우면 일반 블록 스폰 중지
        if (totalLinesCleared >= linesToStopNormalSpawn)
        {
            blockSpawner.StopSpawning();
            Debug.Log($"[TetrisGameManager] 일반 블록 스폰 중지 ({linesToStopNormalSpawn}줄 도달)");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 게임 상태 정보 반환
    /// </summary>
    public string GetGameStateInfo()
    {
        return $"총 라인 제거: {totalLinesCleared}, 폭탄 블록: {blockSpawner.GetSpawnedBombBlocks().Count}개";
    }

    /// <summary>
    /// 라인 카운터 리셋 (테스트용)
    /// </summary>
    public void ResetLineCounter()
    {
        totalLinesCleared = 0;
        Debug.Log("[TetrisGameManager] 라인 카운터 리셋");
    }

    /// <summary>
    /// 폭탄 블록 수동 소환 (테스트용)
    /// </summary>
    public void SpawnBombBlockManually()
    {
        blockSpawner.SpawnBombBlock();
    }

    #endregion

}