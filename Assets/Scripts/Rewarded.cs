using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rewarded : MonoBehaviour
{
    [SerializeField] private Text rewardText;
    private int effectSpeed = 40;

    // Start is called before the first frame update
    void Start()
    {
        rewardText = this.gameObject.GetComponent<Text>();
        Invoke("DestoryObject", 1.5f);
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.Translate(Vector2.up * Time.deltaTime * effectSpeed);   
    }

    private void DestoryObject()
    {
        Destroy(gameObject);
    }

    public void SetRewardValue(double reward)
    {
        if (reward > 0)
        {
            rewardText.text = "+" + reward.ToString("0.0");
            rewardText.color = Color.green;
        }
        
        else
        {
            rewardText.text = reward.ToString("0.0");
            rewardText.color = Color.red;
        }
    }
}