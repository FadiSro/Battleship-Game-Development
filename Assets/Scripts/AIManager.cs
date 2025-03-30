using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class AIManager : MonoBehaviour
{
    /*
     *5=>Carrier    id=0
     *4=>Battleship id=1
     *3=> Submarine id=2
     *3=> Cruiser   id=3
     *2=>Destroyer  id=4
     */
    public class Ship
    {
        public int id;
        public int size;
        public bool isHorizontal;
        public int[] positions;
        public int hitCount;
        public bool IsSunk => hitCount == size;
        public void Hit() => hitCount += !IsSunk ? 1 : 0;
    }
    public int shipPlacedNum;/*number */
    private bool horizontalShip;
    private Ship selectedShip;
    public Dictionary<int, (bool isMissileHit, Ship OccupyingShip)> tiles = new Dictionary<int, (bool isMissileHit, Ship OccupyingShip)>();//(bool isMissileHit, Ship OccupyingShip)

    private Queue<int> strafingPatternQueue;
    private List<int> mopUpPattern;

    private int lastHitTile = -1; // Track the last hit tile for continued targeting
    private bool isTargetingShip = false; // Flag to indicate if focusing on sinking a ship

    public bool IsMissileHit(int tileIndex) => tiles.ContainsKey(tileIndex) && tiles[tileIndex].isMissileHit;
    public bool IsOccupyingShip(int tileIndex) => tiles.ContainsKey(tileIndex) && tiles[tileIndex].OccupyingShip != null;


    #region Initialize Tiles and Patterns
    private void Start()
    {
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (var pv in photonViews)
            pv.enabled = false;
        InitializeTiles();
        InitializeStrafingPattern();
        InitializeMopUpPattern();
    }
    void InitializeStrafingPattern()
    {
        // Define the initial strafing pattern
        strafingPatternQueue = new Queue<int>();
        int[] frequencyMap = { 55, 66, 47, 36, 44, 58, 25, 33, 63, 74, 77, 28, 52, 85,
                               69, 39, 17, 14, 22, 88, 50, 6, 41, 96, 82, 80,
                               3, 71, 93, 99, 20, 9, 11 };
        foreach (int item in frequencyMap)
            strafingPatternQueue.Enqueue(item);
    }
    void InitializeMopUpPattern()
    {
        mopUpPattern = new List<int>();
        int[] frequencyMap = { 46,57,35,64,75,53,68,38,27,24,
                               42,86,49,16,13,72,83,79,60,5,
                               31,61,94,97,19,8,30,2,90,91};
        foreach (int item in frequencyMap)
            mopUpPattern.Add(item);
    }

    void InitializeTiles()
    {
        shipPlacedNum = 0;
        horizontalShip = false;
        selectedShip = null;
        for (int i = 1; i <= 100; i++)
            tiles[i] = (false, null);
        PlaceShipPVC();
    }
    #endregion

    #region Place Ships
    void PlaceShipPVC()
    {
        int[] shipSizes = { 5, 4, 3, 3, 2 };  // Sizes of the ships to be placed
        bool taken;
        foreach (int shipSize in shipSizes)
        {
            taken = true;
            /*int[]*/
            int[] shipPositions = new int[shipSize]; // Array to store positions for the current ship
            while (taken)
            {
                taken = false;
                int shipNose = UnityEngine.Random.Range(1, 101); // Random start point for the ship (1 , 101)
                horizontalShip = UnityEngine.Random.Range(0, 2) == 1; // Randomly decide if the ship is horizontal or vertical
                int step = horizontalShip ? 1 : 10; // Step value: 1 for horizontal, 10 for vertical
                // Determine the positions for the current ship
                for (int i = 0; i < shipSize; i++)
                {
                    int currentTileIndex = shipNose + i * step;
                    // Bounds and validation checks
                    if (horizontalShip) // Horizontal placement check
                    {
                        if ((shipNose - 1) / 10 != (currentTileIndex - 1) / 10) // Ensure ship stays within the row
                        {
                            taken = true;
                            break;
                        }
                    }
                    if (currentTileIndex < 1 || currentTileIndex > 100)
                    {
                        taken = true;
                        break;
                    }
                    // Check if the position is already occupied
                    if (tiles[currentTileIndex].OccupyingShip != null)
                    {
                        taken = true;
                        break;
                    }
                    shipPositions[i] = currentTileIndex;
                }
                // If the placement is valid, place the ship
                if (!taken)
                    PlaceShipOnTiles(shipPositions, shipSize);
            }
        }
        GameManager.Instance.CurrentGameState = GameState.PlayerTurn;
        if (GameManager.Instance.CurrentGameMode == GameMode.PvC)
            GameManager.Instance.PlayerTurn = true; //when this line is active the pvp mode crash 
        Debug.Log("Enemy ships placed randomly on the grid for PvC mode.");
    }
    private void PlaceShipOnTiles(int[] positionsTmp, int shipSize)
    {
        selectedShip = new Ship
        {
            id = shipPlacedNum,
            size = shipSize,
            isHorizontal = horizontalShip,
            positions = positionsTmp,
            hitCount = 0
        };
        foreach (int pos in selectedShip.positions)
        {
            (bool isMissileHit, Ship OccupyingShip) tileData = tiles[pos];
            tiles[pos] = (tileData.isMissileHit, selectedShip);
        }
        shipPlacedNum++;
        selectedShip = null;
    }
    #endregion

    #region hit 
    public void HitTile(int  tileIndex)
    {
        Ship tmp = tiles[tileIndex].OccupyingShip;
        tiles[tileIndex] = (true, tmp);
    }
    public void checkSunk(int tileIndex)
    {
        if (tiles.ContainsKey(tileIndex) && tiles[tileIndex].OccupyingShip != null)
            tiles[tileIndex].OccupyingShip.Hit();
        if (tiles[tileIndex].OccupyingShip.IsSunk)
            shipPlacedNum--;
    }
    public int ExecuteTurn()
    {
        int tileToFire = -1;

        if (isTargetingShip && lastHitTile != -1)
        {
            // Continue targeting adjacent tiles to sink the ship
            List<int> adjacentTiles = GetAdjacentTiles(lastHitTile);

            foreach (int tile in adjacentTiles)
            {
                GameBoardManager player = GameManager.Instance.PlayerBoardManager;
                if (player.IsContainsKey(tile) && !player.IsMissileHit(tile))/**/
                {
                    FireAtTile(tile);
                    tileToFire = tile;  // Return this tile number to GameManager for missile launch
                    return tileToFire;
                }
            }
            // If no valid adjacent tiles, fall back to strafing
            isTargetingShip = false;
        }
        // Strafing or mop-up
        if (shipPlacedNum == 1) // Only destroyer remains
        {
            if (mopUpPattern.Count > 0)
            {
                int tile = mopUpPattern[0];
                mopUpPattern.RemoveAt(0);
                FireAtTile(tile);
                tileToFire = tile;
                return tileToFire;
            }
        }
        else if (strafingPatternQueue.Count > 0)
        {
            int tile = strafingPatternQueue.Dequeue();
            FireAtTile(tile);
            tileToFire = tile;
            return tileToFire;
        }

        return tileToFire; // In case no valid tile is found, return -1 (invalid)
    }
    private void FireAtTile(int tileIndex)
    {
        GameBoardManager player = GameManager.Instance.PlayerBoardManager;
        if (!player.IsContainsKey(tileIndex)) return;/**/

        if (!player.IsMissileHit(tileIndex))
        {
            if (player.IsOccupyingShip(tileIndex))
            {
                if (player.IsNotSunk(tileIndex))
                {
                    lastHitTile = tileIndex;
                    isTargetingShip = true;
                }
                else
                {
                    lastHitTile = -1;
                    isTargetingShip = false;
                }
            }
        }
    }

    private List<int> GetAdjacentTiles(int tileIndex)
    {
        List<int> adjacent = new List<int>();
        int[] possibleMoves = { -10, 10, -1, 1 };
        foreach (int move in possibleMoves)
        {
            int adjacentIndex = tileIndex + move;
            if (adjacentIndex >= 1 && adjacentIndex <= 100)
            {
                if (Mathf.Abs(tileIndex % 10 - adjacentIndex % 10) <= 1)
                {
                    if(GameManager.Instance.PlayerBoardManager.IsMissileHit(tileIndex))
                        adjacent.Add(adjacentIndex);
                }
            }
        }
        return adjacent;
    }
    #endregion
}