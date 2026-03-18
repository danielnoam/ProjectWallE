using UnityEngine;

public static class TransformCopyUtility
{
    public static void CopyHierarchy(Transform source, Transform target)
    {
        if (source == null || target == null) return;

        // Copy local transform
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        // Loop children
        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);

            Transform targetChild = target.Find(sourceChild.name);

            if (targetChild != null)
            {
                CopyHierarchy(sourceChild, targetChild);
            }
            else
            {
                Debug.LogWarning($"Missing child: {sourceChild.name} in target");
            }
        }
    }
}