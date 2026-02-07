using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private CarController carController;
        [SerializeField] private RobotController robotController;
        
        private IPlayerController _currentController;

        void Start()
        {
            _currentController = carController.GetComponent<IPlayerController>();
        }
        private void FixedUpdate()
        {
            _currentController.ApplyMovement();
        }
    }
}
