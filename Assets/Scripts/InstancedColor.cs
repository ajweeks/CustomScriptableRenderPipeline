﻿using UnityEngine;

public class InstancedColor : MonoBehaviour
{
    [SerializeField]
    Color color = Color.white;

    private static MaterialPropertyBlock propertyBlock;
    private static int colorID = Shader.PropertyToID("_Color");

    void Awake()
    {
        OnValidate();
    }
    
    void OnValidate()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        propertyBlock.SetColor(colorID, color);
        GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }
    
}
