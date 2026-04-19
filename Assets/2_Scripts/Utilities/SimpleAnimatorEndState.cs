using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.Utilities
{
    [RequireComponent(typeof(SimpleAnimator))]
    public class SimpleAnimatorEndState : MonoBehaviour
    {
        [SerializeField] private int clipIndex;
        [SerializeField, AutoGetSelf] private SimpleAnimator simpleAnimator;

        private void OnEnable()
        {
            simpleAnimator.Play(clipIndex, 1, 0);
        }
    }
}