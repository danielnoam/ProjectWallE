using DNExtensions.Utilities.CustomFields;

namespace ProjectWallE.GameLoop
{
    public interface IDeployable
    {
        void Deploy(DeploymentRequest deploymentRequest, BehaviorRequest behaviorRequest);
    }

    public interface IDeployableWithPod : IDeployable
    {
        Pod PodPrefab { get; }
    }
}