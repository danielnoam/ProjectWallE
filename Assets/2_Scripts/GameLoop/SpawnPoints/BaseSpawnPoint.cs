using DNExtensions.Utilities.Button;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
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

        protected virtual void OnLevelInitializing()
        {
        }

        protected virtual void OnLevelStarted()
        {
        }

        protected virtual void OnLevelFinished()
        {
        }
        
        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void AlignToGround()
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 50f))
            {
                transform.position = hit.point;
                Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                if (projectedForward.sqrMagnitude < 0.001f) projectedForward = Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
                transform.rotation = Quaternion.LookRotation(projectedForward, hit.normal);
            } 
        }
    }
}