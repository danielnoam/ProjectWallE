using UnityEngine;

public class Base : Structure
{


    protected override void OnBuild()
    {

    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        Debug.Log("Game Lost");
    }
}
