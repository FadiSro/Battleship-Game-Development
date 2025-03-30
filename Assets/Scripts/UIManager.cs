#if UNITY_EDITOR
using UnityEditor.PackageManager.UI;
#endif
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

public class UIManager : MonoBehaviourPunCallbacks
{
    [Header("Timer")]
    public float TimeLeft;
    public bool TimerOn = false;
    public TMP_Text TimerTxt;
    [Header("Buttons")]
    public Button setButton;
    public Button forfeitButton;
    [Header("Text")]
    public TMP_Text msg;
    public TMP_Text playerShipNumTXT;
    public TMP_Text enemyShipNumTXT;
    private void Start()
    {
        TimerTxt.gameObject.SetActive(false);
    }
    void Update()
    {
        if (TimerOn)
        {
            if (TimeLeft > 0)
            {
                TimeLeft -= Time.deltaTime;
                updateTimer(TimeLeft);
            }
            else
            {
                Debug.Log("Time is UP!");
                TimeLeft = 0;
                TimerOn = false;
            }
        }
    }

    void updateTimer(float currentTime)
    {
        currentTime += 1;
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);

        TimerTxt.text = string.Format("Time Left:{0:00}:{1:00}", minutes, seconds);
        if (TimeLeft <= 20)
            TimerTxt.color = Color.red;
        //if(TimeLeft==0)/*then lost the game*/
    }
    public void OnforfeitButton()
    {
        /*need to destroy the*/
        Destroy(GameManager.Instance.gameObject);
        GameManager.Instance = null;
        SceneManager.LoadScene(0);
    }
    public void OnsetButton()
    {
        if (GameManager.Instance.AreAllShipsPlaced())
        {
            TimerOn = false;
            setButton.gameObject.SetActive(false);
            GameManager.Instance.OnPlayerSetupComplete();
        }
    }

    public void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
        }
    }
}
