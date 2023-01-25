using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void PlaySimuation()
    {
        SceneManager.LoadScene("Simulation");
    }

    public void AgentRewardDetails()
    {
        SceneManager.LoadScene("Agent Reward Details");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}