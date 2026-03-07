using UnityEngine;

[SelectionBase]
[DisallowMultipleComponent]
public abstract class BaseSpawnPoint : MonoBehaviour
{
    private void OnEnable()
    {
        LevelManager.OnLevelInitializing += OnLevelInitializing;
        LevelManager.OnLevelStarted += OnLevelStarted;
        LevelManager.OnLevelCompleted += OnLevelFinished;
        LevelManager.OnLevelFailed += OnLevelFinished;
    }

    private void OnDisable()
    {
        LevelManager.OnLevelInitializing -= OnLevelInitializing;
        LevelManager.OnLevelStarted -= OnLevelStarted;
        LevelManager.OnLevelCompleted -= OnLevelFinished;
        LevelManager.OnLevelFailed -= OnLevelFinished;
    }

    protected virtual void OnLevelInitializing() { }
    protected virtual void OnLevelStarted() { }
    protected virtual void OnLevelFinished() { }
}