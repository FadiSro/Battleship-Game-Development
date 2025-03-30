using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Missile : MonoBehaviourPunCallbacks
{
    private bool isOccupied; /*to return from the RpcTarget.Other player isOccupied*/
    #region Enemy
    [PunRPC]
    private void RequestIsOccupied(Vector2 position)/*PVP only*/
    {
        GameBoardManager player = GameManager.Instance.PlayerBoardManager;
        Tile tileAtPosition;
        if (player.TryGetTile(Vector2Int.RoundToInt(position), out tileAtPosition))
        {
            bool sunk = false;
            Ship hitShip = tileAtPosition.OccupyingShip;
            if (hitShip != null)
            {
                GameManager.Instance.PlayerColor[tileAtPosition.position] = 2;
                tileAtPosition.hitHighlightTile(Color.red);
                hitShip.Hit();
                sunk = hitShip.IsSunk;
                if (sunk)
                    player.DecShipPlacedNum();
            }
            else
            {
                GameManager.Instance.PlayerColor[tileAtPosition.position] = 1;
                tileAtPosition.hitHighlightTile(Color.grey);
            }
            photonView.RPC("RespondIsOccupied", RpcTarget.Others,tileAtPosition.IsOccupied, sunk);
        }
    }
    [PunRPC]
    private void AddFire(int photonViewID)/*PVP only*/
    {
        PhotonView photonView = PhotonView.Find(photonViewID);
        if (photonView != null)
        {
            GameObject fire = photonView.gameObject;
            GameManager.Instance.PlayerFire.Add(fire);
        }
    }
    #endregion

    #region Player 
    [PunRPC]
    private void RespondIsOccupied(bool isOccupied, bool sunk)/*PVP only*/
    {
        if (sunk)
            GameManager.Instance.EnemyBoardManager.DecShipPlacedNum();
        this.isOccupied = isOccupied;
    }
    private IEnumerator HandleHitAfterDelay(Tile hitTile)/*PVP only*/
    {
        yield return new WaitForSeconds(1f); // Wait for RPC response
        if (!hitTile.IsMissileHit)
        {
            if (isOccupied)
            {
                hitTile.hitHighlightTile(Color.red);
                //GameManager.Instance.EnemyFire.Add(hitTile.transform.position);
                GameManager.Instance.EnemyColor[hitTile.position] = 2;
                GameObject fire= PhotonNetwork.Instantiate(GameManager.Instance.firePrefab.name, hitTile.transform.position, Quaternion.identity);
                GameManager.Instance.EnemyFire.Add(fire);
                PhotonView firePhotonView = fire.GetComponent<PhotonView>();
                photonView.RPC("AddFire", RpcTarget.Others, firePhotonView.ViewID);
            }
            else
            {
                hitTile.hitHighlightTile(Color.grey);
                GameManager.Instance.EnemyColor[hitTile.position] = 1;
            }
            hitTile.IsMissileHit = true;
        }
        // Use PhotonNetwork.Destroy for networked GameObjects
        PhotonNetwork.Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)/*PVP and PVC*/
    {
        if (GameManager.Instance.CurrentGameMode == GameMode.PvP)
        {
            isOccupied = false;
            Tile hitTile = collision.gameObject.GetComponent<Tile>();
            if (hitTile != null && photonView.IsMine)
            {
                photonView.RPC("RequestIsOccupied", RpcTarget.Others, (Vector2)hitTile.position);
                StartCoroutine(HandleHitAfterDelay(hitTile));
            }
        }
        else
            OnCollisionEnterPvC(collision);
    }
    private void OnCollisionEnterPvC(Collision collision)
    {
        isOccupied = false;
        Tile hitTile = collision.gameObject.GetComponent<Tile>();
        if (hitTile != null)
        {
            if (GameManager.Instance.PlayerTurn)/*0 => normal, 1 => grey, 2 => red*/
            {
                bool isMissileHit = false;
                string numberPart = hitTile.name.Replace("tile (", "").Replace(")", "");
                if (int.TryParse(numberPart, out int tileNumber))
                    isMissileHit = GameManager.Instance.AiManager.IsMissileHit(tileNumber);
                if (!isMissileHit)
                {
                    bool isOccupied = GameManager.Instance.AiManager.IsOccupyingShip(tileNumber);
                    if (isOccupied)
                    {
                        hitTile.hitHighlightTile(Color.red);
                        GameManager.Instance.AiManager.checkSunk(tileNumber);
                        GameManager.Instance.EnemyColor[hitTile.position] = 2;
                        GameObject fire = Instantiate(GameManager.Instance.firePrefab, hitTile.transform.position, Quaternion.identity);
                        GameManager.Instance.EnemyFire.Add(fire);
                    }
                    else
                    {
                        GameManager.Instance.EnemyColor[hitTile.position] = 1;
                        hitTile.hitHighlightTile(Color.grey);
                    }
                }
                GameManager.Instance.AiManager.HitTile(tileNumber);
            }
            else
            {/*AIManager*/
                Ship hitShip = hitTile.OccupyingShip;
                if (!hitTile.IsMissileHit)
                {
                    if (hitShip != null)
                    {
                        hitTile.hitHighlightTile(Color.red);
                        hitShip.Hit();
                        if(hitShip.IsSunk)
                            GameManager.Instance.PlayerBoardManager.DecShipPlacedNum();
                        GameManager.Instance.PlayerColor[hitTile.position] = 2;
                        GameObject fire = Instantiate(GameManager.Instance.firePrefab, hitTile.transform.position, Quaternion.identity);
                        GameManager.Instance.PlayerFire.Add(fire);
                    }
                    else
                    {
                        GameManager.Instance.PlayerColor[hitTile.position] = 1;
                        hitTile.hitHighlightTile(Color.grey);
                    }
                    hitTile.IsMissileHit = true;
                }
            }
        }
        Destroy(gameObject);
    }

    #endregion
}