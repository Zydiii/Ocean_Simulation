using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DepthMapGenerator : MonoBehaviour
{
    public GameObject waterPlane;

    private int waterPlaneSize;

    private int resolution;

    private Texture2D posTex;
    //private Texture2D velTex;

    private NativeArray<float> posPixData;

    public RawImage rawImage;

    private float pixSize;

    private int waterLayer = 4;

    private int objectLayer = 8;
    //private NativeArray<float> velPixData;
    // Start is called before the first frame update
    void Start()
    {
        waterPlaneSize = (int) waterPlane.GetComponent<MeshFilter>().mesh.bounds.size.x;
        resolution = 216;
        posTex = new Texture2D(resolution, resolution, TextureFormat.RGFloat, false, true);
        //velTex = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);
        posPixData = posTex.GetRawTextureData<float>();
        //velPixData = velTex.GetRawTextureData<float>();
        pixSize = (float)waterPlaneSize / resolution;
        getDepth();
    }

    // Update is called once per frame
    void Update()
    {
        //getDepth();
        //refreshPixTex();
        rawImage.texture = posTex;
    }

    void refreshPixTex()
    {
        Parallel.For(0, posPixData.Length, (i) =>
        {
            posPixData[ i ] = 1 - i % 2;
        });
        posTex.Apply();
    }

    void getDepth()
    {
        // Parallel.For(0, resolution, (i) =>
        // {
        //     Debug.Log(i);
        //     Parallel.For(0, resolution, (j) =>
        //     {
        //         Debug.Log(j);
        //     });
        // });
        // for(int i = 0; i < posPixData.Length / 2; i++)
        // {
        //     int z = (int)(i * pixSize / waterPlaneSize);
        //     int x = i - (int)(z * waterPlaneSize * pixSize);
        //     RaycastHit hit;
        //     // Does the ray intersect any objects excluding the player layer
        //     if (Physics.Raycast(new Vector3(x, 10, z), Vector3.down, out hit, Mathf.Infinity, objectLayer))
        //     {
        //         Debug.Log("Did Hit");
        //         posPixData[ i * 2 + 0 ] = 1;
        //         posPixData[ i * 2 + 1 ] = 1;
        //     }
        //     else
        //     {
        //         Debug.Log("Did not Hit");
        //         posPixData[ i * 2 + 0 ] = 0;
        //     }
        // }
        
        posTex.Apply();
    }
}
