using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SpriteInfo : MonoBehaviour
{
    public Sprite sprite;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(sprite.vertices.Length);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
