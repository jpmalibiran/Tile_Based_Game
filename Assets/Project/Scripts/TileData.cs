using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Holds the data of each tile
public class TileData : MonoBehaviour {
    public int m_value_x = 0;
    public int m_value_y = 0;
    public int m_resource_value = 0;
    public TileType m_type = TileType.Default;
    public bool bIs_discovered = false;
}
