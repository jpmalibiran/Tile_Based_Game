using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScr : MonoBehaviour{

    [Header("References")]
    [SerializeField] private ConsoleManager m_Console_Manager_Ref;
    [SerializeField] private GameObject m_Tile_Prefab;
    [SerializeField] private Sprite m_Grey_Tile_Sprite;
    [SerializeField] private Sprite m_Yellow_Tile_Sprite;
    [SerializeField] private Sprite m_Orange_Tile_Sprite;
    [SerializeField] private Sprite m_OrangeRed_Tile_Sprite;
    [SerializeField] private Transform m_Game_Board_Container_Ref;

    [Header("Read Only Gameplay Values")] //Changing these values in inspector does not affect gameplay
    [SerializeField] private int m_Show_Resource_Counter = 0;
    [SerializeField] private int m_Show_Scans_Left = 0;
    [SerializeField] private int m_Show_Extractions_Left = 0;

    [Header("Settings")]
    [SerializeField] private float m_Tile_Board_Offset_X = -7.0f;
    [SerializeField] private float m_Tile_Board_Offset_Y = -3.5f;
    [SerializeField] private float m_Tiling_Distance = 0.46f;
    [SerializeField] private int m_Board_Dimension_X = 16; 
    [SerializeField] private int m_Board_Dimension_Y = 16; 
    [SerializeField] private int m_Core_Resource_Max_Amount = 5000;
    [SerializeField] private int m_Core_Resource_Min_Amount = 2000;
    [SerializeField] private int m_Core_Cluster_Max_Amount = 8;
    [SerializeField] private int m_Core_Cluster_Min_Amount = 6;
    [SerializeField] private int m_Max_Scans = 6;
    [SerializeField] private int m_Max_Extractions = 3;

    private Transform[,] m_Tile_Transform_Array; //References to every tile in the tile board
    private TileType[,] m_Tile_Array; //used in determining tile locations

    private Vector3 mousePosition;
    private RaycastHit m_hit;

    private int m_Random_Min_Resource_Value = 125;
    private int m_Random_Quarter_Resource_Value = 500;
    private int m_Random_Half_Resource_Value = 1000;
    private int m_Random_Resource_Max_Amount = 2000;

    private int m_Resource_Counter = 0;
    private int m_Scans_Left = 0;
    private int m_Extractions_Left = 0;
    private bool bIs_In_Extract_Mode = false; //true: extract mode; false: scan mode

    private void Start() {
        NewGame(); 
    }

    private void Update() {
        GetInput();
    }

    //Generates the positions of resource clusters.
    private void GenerateTileLocations(int x_size, int y_size, int resource_cluster_amount) {
        if (resource_cluster_amount > (x_size * y_size) ) {
            Debug.LogError("[Error] The amount of specified resource clusters cannot be generated because the grid size is too small! Aborting operation...");
            return;
        }

        List<Vector2> cluster_Coord_List = new List<Vector2>();
        List<Vector2> available_Tile_List = new List<Vector2>();
        int tile_holder = -1;

        m_Tile_Array = new TileType[x_size, y_size];
        available_Tile_List = new List<Vector2>();
        cluster_Coord_List = new List<Vector2>();

        //First: Set all tiles to enum TileType.ZeroResource 
        //and adds every tile to available tile list.
        for (int y_index = 0; y_index < y_size; y_index++) {
            for (int x_index = 0; x_index < x_size; x_index++) {
                m_Tile_Array[x_index, y_index] = TileType.MinResource;
                available_Tile_List.Add(new Vector2(x_index, y_index));
            }
        }

        //Then: Randomly select tiles that will become resource cluster cores (TileType.MaxResource)
        for (int index = 0; index < resource_cluster_amount; index++) {

            if (available_Tile_List.Count <= 0) {
                Debug.LogError("[Error] Cannot generate resource clusters because there are no available tiles! Aborting operation...");
                return;
            }

            //Note: This method of only selecting from a list of available tiles prevent circumvents having to check whether a randomly chosen tile already has a max resource. 
            //Further, it prevents the possibility of constantly randomly selecting unavailable tiles- which incur several uneccessary loop iterations.
            tile_holder = Random.Range(0, available_Tile_List.Count);
            cluster_Coord_List.Add(new Vector2(available_Tile_List[tile_holder].x, available_Tile_List[tile_holder].y));
            available_Tile_List.RemoveAt(tile_holder); 
        }

        //Generate Quarter Resources (Yellow)
        //Loop for each resource cluster 
        for (int cluster_index = 0; cluster_index < cluster_Coord_List.Count; cluster_index++) {
            //Loop 5 times for the y axis of the 5x5 resource cluster
            for (int resource_index_y = ((int)cluster_Coord_List[cluster_index].y - 2); resource_index_y <= ((int)cluster_Coord_List[cluster_index].y + 2); resource_index_y++) {
                //Do not mark outside of tile map y bound.
                if (resource_index_y >= y_size || resource_index_y < 0) {
                    continue;
                }
                //Loop 5 times for the x axis of the 5x5 resource cluster
                for (int resource_index_x = ((int)cluster_Coord_List[cluster_index].x - 2); resource_index_x <= ((int)cluster_Coord_List[cluster_index].x + 2); resource_index_x++) {
                    //Do not mark outside of tile map x bound.
                    if (resource_index_x >= x_size || resource_index_x < 0) {
                        continue;
                    }
                    //Do not mark within the 3x3 of a 5x5 resource cluster; We create a 5x5 ring of Quarter Resources.
                    if (resource_index_y < ((int)cluster_Coord_List[cluster_index].y + 2) && resource_index_y > ((int)cluster_Coord_List[cluster_index].y - 2)) {
                        if (resource_index_x < ((int)cluster_Coord_List[cluster_index].x + 2) && resource_index_x > ((int)cluster_Coord_List[cluster_index].x - 2)) {
                            continue;
                        }
                    }
                    m_Tile_Array[resource_index_x, resource_index_y] = TileType.QuarterResource; //Mark as Quarter Resource.
                }
            }
        }

        //Generate Half Resources (Orange)
        //Loop for each resource cluster 
        for (int cluster_index = 0; cluster_index < cluster_Coord_List.Count; cluster_index++) {
            //Loop 5 times for the y axis of the 3x3 resource cluster
            for (int resource_index_y = ((int)cluster_Coord_List[cluster_index].y - 1); resource_index_y <= ((int)cluster_Coord_List[cluster_index].y + 1); resource_index_y++) {
                //Do not mark outside of tile map y bound.
                if (resource_index_y >= y_size || resource_index_y < 0) {
                    continue;
                }
                //Loop 5 times for the x axis of the 3x3 resource cluster
                for (int resource_index_x = ((int)cluster_Coord_List[cluster_index].x - 1); resource_index_x <= ((int)cluster_Coord_List[cluster_index].x + 1); resource_index_x++) {
                    //Do not mark outside of tile map x bound.
                    if (resource_index_x >= x_size || resource_index_x < 0) {
                        continue;
                    }
                    //Do not mark the core of the resource cluster
                    if (resource_index_x == (int)cluster_Coord_List[cluster_index].x && resource_index_y == (int)cluster_Coord_List[cluster_index].y) {
                        continue;
                    }
                    m_Tile_Array[resource_index_x, resource_index_y] = TileType.HalfResource; //Mark as Quarter Resource.
                }
            }
        }

        //Generate Max (Full) Resources (OrangeRed)
        //Loop for each resource cluster 
        for (int cluster_index = 0; cluster_index < cluster_Coord_List.Count; cluster_index++) {
            m_Tile_Array[(int)cluster_Coord_List[cluster_index].x, (int)cluster_Coord_List[cluster_index].y] = TileType.MaxResource;
        }

    }

    //Spawns Tile board and assigns each tile a random resource value.
    private void SpawnTileboard() {
        if (!m_Tile_Prefab) {
            Debug.LogError("[Error] Missing tile prefab; Aborting operation...");
            return;
        }

        if (m_Tile_Array == null) {
            return;
        }
        else if (m_Tile_Array.Length <= 0) {
            return;
        }

        //Note in my implementation: 
        //- Minimal resource value is randomized per game.
        //- Quarter, Half, and Max resource value is randomized per tile.
        //- Minimal resource is between 1/16 to 1/8 of a randomized value between min/max settings. 
        //- Quarter resource is 1/4 of a randomized value between min/max settings. 
        //- Half resource is 1/2 of a randomized value between min/max settings. 
        //- Max resource is a randomized value between min/max settings. 
        //- This means it is *possible*, with RNG and min/max settings, for lower resource color tiers to yield more than higher color tiers.

        GameObject temp_Tile_Holder;

        m_Tile_Transform_Array = new Transform[m_Tile_Array.GetLength(0), m_Tile_Array.GetLength(1)];

        //Minimal Resource amount IS NOT randomized per tile
        m_Random_Min_Resource_Value = Random.Range(m_Core_Resource_Max_Amount / 16, m_Core_Resource_Max_Amount / 8);

        for (int y_index = 0; y_index < m_Tile_Array.GetLength(1); y_index++) {
            for (int x_index = 0; x_index < m_Tile_Array.GetLength(0); x_index++) {

                //Cluster Resource amount IS randomized per tile
                m_Random_Resource_Max_Amount = Random.Range(m_Core_Resource_Min_Amount, m_Core_Resource_Max_Amount + 1); //Note: Int Random.Range has exclusive max value; hence the + 1.
                m_Random_Quarter_Resource_Value = m_Random_Resource_Max_Amount / 4;
                m_Random_Half_Resource_Value = m_Random_Resource_Max_Amount / 2;

                //Spawn tile
                temp_Tile_Holder = Instantiate(m_Tile_Prefab, new Vector3(m_Tile_Board_Offset_X + (x_index * m_Tiling_Distance), m_Tile_Board_Offset_Y + (y_index * m_Tiling_Distance), 0), Quaternion.identity);
                temp_Tile_Holder.GetComponent<TileData>().m_value_x = x_index;
                temp_Tile_Holder.GetComponent<TileData>().m_value_y = y_index;

                if (m_Game_Board_Container_Ref) {
                    temp_Tile_Holder.transform.SetParent(m_Game_Board_Container_Ref);
                }

                //Set tile type and resource amount
                if (m_Tile_Array[x_index, y_index] == TileType.MinResource) {
                    temp_Tile_Holder.GetComponent<TileData>().m_type = TileType.MinResource;
                    temp_Tile_Holder.GetComponent<TileData>().m_resource_value = m_Random_Min_Resource_Value;
                }
                else if (m_Tile_Array[x_index, y_index] == TileType.QuarterResource) {
                    temp_Tile_Holder.GetComponent<TileData>().m_type = TileType.QuarterResource;
                    temp_Tile_Holder.GetComponent<TileData>().m_resource_value = m_Random_Resource_Max_Amount / 4; 
                }
                else if (m_Tile_Array[x_index, y_index] == TileType.HalfResource) {
                    temp_Tile_Holder.GetComponent<TileData>().m_type = TileType.HalfResource;
                    temp_Tile_Holder.GetComponent<TileData>().m_resource_value = m_Random_Resource_Max_Amount / 2;
                }
                else if (m_Tile_Array[x_index, y_index] == TileType.MaxResource) {
                    temp_Tile_Holder.GetComponent<TileData>().m_type = TileType.MaxResource;
                    temp_Tile_Holder.GetComponent<TileData>().m_resource_value = m_Random_Resource_Max_Amount;
                }
                
                m_Tile_Transform_Array[x_index, y_index] = temp_Tile_Holder.transform;
            }
        }

    }

    //Handle events based on user input.
    private void GetInput() {
        
        if (m_Extractions_Left <= 0) {
            Debug.Log("[Notice] There are no Extractions left to use.");
            return;
        }

        if (Input.GetKeyUp(KeyCode.Mouse0)) { //LMB to activate scan or extraction
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //Debug.DrawRay(mousePosition, Vector3.forward * 10, Color.yellow, 60, false);

            if (Physics.Raycast(mousePosition, Vector3.forward, out m_hit)){
                if (m_hit.transform.tag == "ResourceTile") {
                    if (bIs_In_Extract_Mode) {
                        Extract(m_hit.transform.GetComponent<TileData>().m_value_x, m_hit.transform.GetComponent<TileData>().m_value_y);
                    }
                    else {
                        Scan(m_hit.transform.GetComponent<TileData>().m_value_x, m_hit.transform.GetComponent<TileData>().m_value_y);
                    }
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse1)) { //RMB to toggle between scan or extraction
            if (bIs_In_Extract_Mode) {
                bIs_In_Extract_Mode = false;
                Debug.Log("[Notice] Switched to Scan Mode.");
                if (m_Console_Manager_Ref) {
                    m_Console_Manager_Ref.UpdateChat("[Notice] Switched to Scan Mode.");
                }
            }
            else {
                bIs_In_Extract_Mode = true;
                Debug.Log("[Notice] Switched to Extract Mode.");
                if (m_Console_Manager_Ref) {
                    m_Console_Manager_Ref.UpdateChat("[Notice] Switched to Extract Mode.");
                }
            }
        }
    }

    //Removes the entire tile board.
    public void ClearTiles() {

        if (m_Tile_Transform_Array == null) {
            return;
        }
        else if (m_Tile_Transform_Array.Length <= 0) {
            return;
        }

        for (int y_index = 0; y_index < m_Tile_Transform_Array.GetLength(1); y_index++) {
            for (int x_index = 0; x_index < m_Tile_Transform_Array.GetLength(0); x_index++) {
                Destroy(m_Tile_Transform_Array[x_index, y_index].gameObject);
            }
        }

    }

    //Scans a 3x3 area with the center at mouse position
    public void Scan(int x_index, int y_index) {
        if (bIs_In_Extract_Mode) {
            return;
        }

        if (m_Scans_Left <= 0) {
            Debug.Log("[Notice] There are no Scans left to use.");
            if (m_Console_Manager_Ref) {
                m_Console_Manager_Ref.UpdateChat("[Notice] There are no Scans left to use. Press the <Right Mouse Button> to switch to Extract Mode.");
            }
            return;
        }

        SpriteRenderer spriteRendererHolder;
        TileData tileDataHolder;

        m_Scans_Left--; //Update scan usage amount
        m_Show_Scans_Left = m_Scans_Left;

        Debug.Log("[Notice] You have scanned an area.");
        if (m_Console_Manager_Ref) {
            m_Console_Manager_Ref.UpdateChat("[Notice] You have scanned an area.");
            m_Console_Manager_Ref.UpdateStats(m_Resource_Counter, m_Scans_Left, m_Extractions_Left);
        }

        //Reveal tile true colors in a 3x3 area.
        for (int resource_index_y = (y_index - 1); resource_index_y <= (y_index + 1); resource_index_y++) {
            //Skip outside of tile map y bound.
            if (resource_index_y >= m_Tile_Transform_Array.GetLength(1) || resource_index_y < 0) {
                continue;
            }

            for (int resource_index_x = (x_index - 1); resource_index_x <= (x_index + 1); resource_index_x++) {
                //Skip outside of tile map x bound.
                if (resource_index_x >= m_Tile_Transform_Array.GetLength(1) || resource_index_x < 0) {
                    continue;
                }

                spriteRendererHolder = m_Tile_Transform_Array[resource_index_x, resource_index_y].GetComponent<SpriteRenderer>();
                tileDataHolder = m_Tile_Transform_Array[resource_index_x, resource_index_y].GetComponent<TileData>();
                tileDataHolder.bIs_discovered = true;
                spriteRendererHolder.color = new Color(250,250,250,250);

                if (tileDataHolder.m_type == TileType.MinResource) {
                    spriteRendererHolder.sprite = m_Grey_Tile_Sprite;
                }
                else if (tileDataHolder.m_type == TileType.QuarterResource) {
                    spriteRendererHolder.sprite = m_OrangeRed_Tile_Sprite;
                }
                else if (tileDataHolder.m_type == TileType.HalfResource) {
                    spriteRendererHolder.sprite = m_Orange_Tile_Sprite;
                }
                else if (tileDataHolder.m_type == TileType.MaxResource) {
                    spriteRendererHolder.sprite = m_Yellow_Tile_Sprite;
                }
                else {
                    Debug.LogError("[Error] Cannot discern tile type!");
                }
            }
        }
    }

    //Extracts from one tile and degrades its surrounding 5x5 area.
    public void Extract(int x_index, int y_index) {
        if (!bIs_In_Extract_Mode) {
            return;
        }

        if (m_Extractions_Left <= 0) {
            Debug.Log("[Notice] There are no Extractions left to use.");
            return;
        }

        SpriteRenderer spriteRendererHolder;
        TileData tileDataHolder;
        m_Extractions_Left--; //Update extract usage amount
        m_Show_Extractions_Left = m_Extractions_Left;
        m_Resource_Counter = m_Resource_Counter + m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().m_resource_value; //Update resources
        m_Show_Resource_Counter = m_Resource_Counter;

        //Note:
        //When degrading an extracted tile to Minimal Resource it will have the value of the set randomized Minimal resource value of the game.
        //When degrading a surrounding Quarter, Half, or Max Resource, tile its resource value will be reduced by half of its original resource value.

        Debug.Log("[Notice] You have extracted " + m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().m_resource_value + " resources!");
        if (m_Console_Manager_Ref) {
            m_Console_Manager_Ref.UpdateChat("[Notice] You have extracted " + m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().m_resource_value + " resources!");
            m_Console_Manager_Ref.UpdateStats(m_Resource_Counter, m_Scans_Left, m_Extractions_Left);
        }

        //Update tile resource value and type. This is for the tile the user clicked.
        m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().m_type = TileType.MinResource;
        m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().m_resource_value = m_Random_Min_Resource_Value;

        //Only change the color of discovered tiles. This is for the tile the user clicked.
        if (m_Tile_Transform_Array[x_index, y_index].GetComponent<TileData>().bIs_discovered) { 
            m_Tile_Transform_Array[x_index, y_index].GetComponent<SpriteRenderer>().sprite = m_Grey_Tile_Sprite;
        }

        //Degrade tiles surrounding the tile user extracted from.
        for (int resource_index_y = (y_index - 2); resource_index_y <= (y_index + 2); resource_index_y++) {
            //Skip outside of tile map y bound.
            if (resource_index_y >= m_Tile_Transform_Array.GetLength(1) || resource_index_y < 0) {
                continue;
            }

            for (int resource_index_x = (x_index - 2); resource_index_x <= (x_index + 2); resource_index_x++) {
                //Skip outside of tile map x bound.
                if (resource_index_x >= m_Tile_Transform_Array.GetLength(1) || resource_index_x < 0) {
                    continue;
                }

                //Skip center core because it's already handled above
                if (resource_index_x == x_index && resource_index_y == y_index) {
                    continue;
                }

                spriteRendererHolder = m_Tile_Transform_Array[resource_index_x, resource_index_y].GetComponent<SpriteRenderer>();
                tileDataHolder = m_Tile_Transform_Array[resource_index_x, resource_index_y].GetComponent<TileData>();

                //Update tile resource value and type. This is for the tiles surrounding the tile the user clicked.
                if (tileDataHolder.m_type == TileType.QuarterResource) {
                    tileDataHolder.m_type = TileType.MinResource;
                    tileDataHolder.m_resource_value = m_Random_Min_Resource_Value;
                }
                else if (tileDataHolder.m_type == TileType.HalfResource) {
                    tileDataHolder.m_type = TileType.QuarterResource;
                    tileDataHolder.m_resource_value = tileDataHolder.m_resource_value / 2;
                }
                else if (tileDataHolder.m_type == TileType.MaxResource) {
                    tileDataHolder.m_type = TileType.HalfResource;
                    tileDataHolder.m_resource_value = tileDataHolder.m_resource_value / 2;
                }

                //Only change the color of discovered tiles. This is for the tiles surrounding the tile the user clicked.
                if (m_Tile_Transform_Array[resource_index_x, resource_index_y].GetComponent<TileData>().bIs_discovered) { 
                    if (tileDataHolder.m_type == TileType.MinResource) {
                        spriteRendererHolder.sprite = m_Grey_Tile_Sprite;
                    }
                    else if (tileDataHolder.m_type == TileType.QuarterResource) {
                        spriteRendererHolder.sprite = m_OrangeRed_Tile_Sprite;
                    }
                    else if (tileDataHolder.m_type == TileType.HalfResource) {
                        spriteRendererHolder.sprite = m_Orange_Tile_Sprite;
                    }
                }
            }
        }

        //Check win condition
        if (m_Extractions_Left <= 0) {
            Debug.Log("[Notice] There are no Extractions left to use.");
            if (m_Console_Manager_Ref) {
                m_Console_Manager_Ref.UpdateChat("[Notice] There are no Extractions left to use. Your final score is " + m_Resource_Counter + "!");
                m_Console_Manager_Ref.UpdateChat("[Notice] Press the 'New Game' button below to try again.");
            }
        }
    }

    //Resets the tile board, scans, extractions, and score
    public void NewGame() {
        ClearTiles();

        m_Resource_Counter = 0;

        bIs_In_Extract_Mode = false;

        m_Scans_Left = m_Max_Scans;
        m_Show_Scans_Left = m_Scans_Left;

        m_Extractions_Left = m_Max_Extractions;
        m_Show_Extractions_Left = m_Extractions_Left;

        GenerateTileLocations(m_Board_Dimension_X, m_Board_Dimension_Y, Random.Range(m_Core_Cluster_Min_Amount, m_Core_Cluster_Max_Amount + 1)); //Note: Int Random.Range has exclusive max value; hence the + 1.
        SpawnTileboard();

        if (m_Console_Manager_Ref) {
            m_Console_Manager_Ref.UpdateStats(m_Resource_Counter, m_Scans_Left, m_Extractions_Left);

            m_Console_Manager_Ref.UpdateChat("[Notice] New Game Started. How to play:");
            m_Console_Manager_Ref.UpdateChat("    - Try to extract as much resources as you can!");
            m_Console_Manager_Ref.UpdateChat("    - Press <Right Mouse Button> to toggle between Scan and Extract Mode.");
            m_Console_Manager_Ref.UpdateChat("    - Press <Left Mouse Button> to activate either mode.");
            m_Console_Manager_Ref.UpdateChat("    - Use Scan Mode to reveal a 3x3 area. Max usage: 6");
            m_Console_Manager_Ref.UpdateChat("    - Use Extract Mode to extract resources from a tile. Max usage: 3");
        }
    }
}
