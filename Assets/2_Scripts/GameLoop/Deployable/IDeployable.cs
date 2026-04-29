using DNExtensions.Utilities.CustomFields;

namespace ProjectWallE.GameLoop
{
    public interface IDeployable
    {
        void Deploy(DeploymentRequest request);
    }

    public interface IDeployableWithPod : IDeployable
    {
        Pod PodPrefab { get; }
    }
}