using System;
using DNExtensions.Utilities.Button;
using UnityEngine;

public class TerrainCamera : MonoBehaviour
{
        [SerializeField] private  Camera cam;
        [SerializeField] private Terrain terrain;


        private void OnValidate()
        {
                UpdatePosition();
        }

        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void UpdatePosition()
        {
                if (!cam || !terrain) return;
                
                var terrainPosition = terrain.transform.position;
                var terrainSize = terrain.terrainData.size;
                
                cam.transform.position = new Vector3(
                        terrainPosition.x + terrainSize.x / 2f,
                        terrainPosition.y + 250f,
                        terrainPosition.z + terrainSize.z / 2f
                );
                
                cam.transform.eulerAngles = new Vector3(90f, 0f, 0f);
                cam.orthographicSize = Math.Max(terrainSize.x, terrainSize.z) / 2f;
        }
}
