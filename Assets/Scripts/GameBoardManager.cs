using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

public class GameBoardManager : MonoBehaviourPunCallbacks
{
    [Header("Ships , tiles")]
    private Vector2Int[] tmpShip_positions;
    private int shipPlacedNum;/*number */
    private bool horizontalShip;
    private Ship selectedShip;
    public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
    public GameObject[] ships;

    [Header("Objects")]
    public GameObject woodDock;

    #region GameBoardManager lambda function for ship
    public int ReturnshipPlacedNum() => shipPlacedNum;
    public void DecShipPlacedNum() => shipPlacedNum--;
    public void SelectShip(Ship ship) => selectedShip = ship;/*save the slected ship*/
    public bool IsSelectedShip() => selectedShip != null;/*check if there is selected ship*/
    public bool TryGetTile(Vector2Int position, out Tile tile) => tiles.TryGetValue(position, out tile);
    public void ResetHighlights() => tiles.Values.ToList().ForEach(tile => tile.ResetHighlight());/*reset all tiles color*/
    private bool IsHorizontalShip() => horizontalShip = false;
    private bool NotOccupied(Vector2Int[] pos) => pos.All(p => !tiles[p].IsOccupied);/*check if the tile is occupied*/
    #endregion

    #region GameBoardManager start
    void Start()
    {
        InitializeTiles();
    }

    void InitializeTiles()
    {
        shipPlacedNum = 0;
        IsHorizontalShip();
        tmpShip_positions = null;
        for (int i = 1; i <= 100; i++)
        {
            Tile tile = GameObject.Find($"tile ({i})").GetComponent<Tile>();
            Vector2Int position = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(tile.transform.position.z));
            tile.position = position;
            tiles[position] = tile;
            tiles[position].IsOccupied = false;
            tiles[position].OccupyingShip = null;
        }
        if (GameManager.Instance.CurrentGameMode == GameMode.PvC)/*Disable Photon Views For PvC*/
        {
            PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
            foreach (var pv in photonViews)
                pv.enabled = false;
        }
    }
    #endregion
    
    void Update()
    {
        if (selectedShip == null) 
            return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Tile tile = hitInfo.collider.GetComponent<Tile>();
            if (tile != null)
            {
                HighlightTiles(tile.position);
                if (Input.GetMouseButtonDown(1)) /* Right-click to rotate*/
                {
                    horizontalShip = !horizontalShip;
                }
                if (Input.GetMouseButtonDown(0) && tmpShip_positions!=null && NotOccupied(tmpShip_positions)) /* Left-click to place*/
                {
                    PlaceShip(tile.position);
                }
            }
        }
    }
    #region HighlightTiles: Marks tiles where the ship is allowed to be
    
    private Vector2Int[] Vector2IntTiles(Vector2Int position)/*this function to calcute 'tmpShip_positions' the remaing positions to hightlight*/
    {
        int mid=selectedShip.size%2==0 ? selectedShip.size/2-1 : selectedShip.size/2;
        Vector2Int[] positions = new Vector2Int[selectedShip.size];
        positions[mid] = position;
        string name = tiles[position].name; // tile name "tile (number)"
        int startIndex = name.IndexOf('(') + 1; // Find the position of the first number
        int endIndex = name.IndexOf(')'); // Find the position of the ')' to know last of number
        string numberPart = name.Substring(startIndex, endIndex - startIndex); // Extract the substring containing the number
        int tileNum = int.Parse(numberPart); // Convert the extracted number to an integer

        if (!horizontalShip) {
            int startNum = tileNum - 10 * mid;
            if(startNum < 1||(startNum+10* (selectedShip.size-1))>100)
                return null;
            for (int index = 0; index < selectedShip.size; startNum += 10, index++)
                if (positions[index] == new Vector2Int(0, 0))
                    positions[index] = GameObject.Find($"tile ({startNum})").GetComponent<Tile>().position;
        }
        else{
            int startNum = tileNum - mid;
            int endNum = startNum + selectedShip.size - 1;
            if (startNum < 1 || (startNum + (selectedShip.size - 1)) > 100||(startNum-1)/10!=(endNum-1)/10)
                return null;
            for (int index = 0; index < selectedShip.size; startNum++, index++)
                if (positions[index] == new Vector2Int(0, 0))
                    positions[index] = GameObject.Find($"tile ({startNum})").GetComponent<Tile>().position;
        }
        return positions;
    }
    
    void HighlightTiles(Vector2Int startPosition)/*function to highlight tiles*/
    {
        ResetHighlights();
        Vector2Int pos = new Vector2Int(startPosition.x, startPosition.y);
        if (tiles.ContainsKey(pos))
            {
                //System.Diagnostics.Debug.WriteLine("Tile name: " + tiles[pos].name);
                tmpShip_positions=Vector2IntTiles(pos);
                if(tmpShip_positions != null&& NotOccupied(tmpShip_positions)) 
                {
                    for(int index = 0;index < tmpShip_positions.Length; index++)
                        tiles[tmpShip_positions[index]].HighlightTile();
                }
                
            }
    }
    #endregion
    public void ResetTileShipOccupied(Vector2Int[] pos)/*reset all tile that occupied*/
    {
        foreach (Vector2Int p in pos)
        {
            tiles[p].IsOccupied = false;
            tiles[p].OccupyingShip = null;
        }
    }
    void PlaceShip(Vector2Int startPosition)/*place the ship*/
    {
        if (horizontalShip)
            selectedShip.Rotate();
        selectedShip.transform.position = selectedShip.isHorizontal ?
                                      new Vector3(startPosition.x + selectedShip.zOffsetHorizontal, selectedShip.transform.position.y, startPosition.y + selectedShip.xOffsetHorizontal) :
                                      new Vector3(startPosition.x + selectedShip.xOffset, selectedShip.transform.position.y, startPosition.y + selectedShip.zOffset);
        selectedShip.UpdateGridPositions(tmpShip_positions);
        for (int i = 0; i < selectedShip.size; i++)
        {
            tiles[tmpShip_positions[i]].IsOccupied = true;
            tiles[tmpShip_positions[i]].OccupyingShip = selectedShip;
        }
        selectedShip.IsPlaced = true;
        shipPlacedNum++;
        horizontalShip = false;
        selectedShip = null;
        tmpShip_positions = null;
        ResetHighlights();
    }
    public void GetCurrentTileColor(Dictionary<Vector2Int, int> colorDictionary)/*set the tiles color on turn changes */
    {
        foreach (Tile tile in tiles.Values)/*0 => normal, 1 => grey, 2 => red*/
        {
            if (colorDictionary[tile.position] == 0)
                tile.ResetHighlight();
            else if(colorDictionary[tile.position] == 1)
                tile.hitHighlightTile(Color.grey);
            else 
                tile.hitHighlightTile(Color.red);
        }
    }
    #region PVC checks
    public bool IsContainsKey(int key)
    {
        string tileName = $"tile ({key})";
        GameObject tileObject = GameObject.Find(tileName);
        if (tileObject != null && tileObject.GetComponent<Tile>() != null)
            return true; // Tile exists and has the Tile component
        return false; // Tile does not exist or does not have the Tile component
    }
    public bool IsMissileHit(int key)
    {
        string tileName = $"tile ({key})";
        GameObject tileObject = GameObject.Find(tileName);
        if (tileObject == null || tileObject.GetComponent<Tile>() == null)
            return false;
        return tileObject.GetComponent<Tile>().IsMissileHit;
    }
    public bool IsOccupyingShip(int key)
    {
        string tileName = $"tile ({key})";
        GameObject tileObject = GameObject.Find(tileName);
        if (tileObject == null || tileObject.GetComponent<Tile>() == null)
            return false;
        return tileObject.GetComponent<Tile>().IsOccupied;
    }
    public bool IsNotSunk(int key)
    {
        string tileName = $"tile ({key})";
        GameObject tileObject = GameObject.Find(tileName);
        if (tileObject == null || tileObject.GetComponent<Tile>() == null)
            return false;
        int RemaningSize = tileObject.GetComponent<Tile>().OccupyingShip.RemaningSize() - 1;
        if (RemaningSize>0)
            return true;
        return false;
    }
    #endregion
}