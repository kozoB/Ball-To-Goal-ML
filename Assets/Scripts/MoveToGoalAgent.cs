using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform spawnLeftBorder;
    [SerializeField] private Transform spawnRightBorder;
    //[SerializeField] private Transform[] obstacles;
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private SpriteRenderer agentSpriteRenderer;
    [SerializeField] private Text winScoreUI;
    [SerializeField] private Text loseScoreUI;
    [SerializeField] private Text winPercentageUI;
    [SerializeField] private Text timerUI;
    [SerializeField] private Text avgWinTimeUI;
    [SerializeField] private Text episodeRewardUI;
    [SerializeField] private Text avgRewardUI;
    [SerializeField] private GameObject rewardedEffect;

    private Rigidbody2D rb;
    private Canvas renderCanvas;
    private int numWin = 0;
    private int numLose = 0;
    private float winPercentage = 0;
    private float currentTime;
    private float avgWinTime = 0;
    private float startingTime = 0f;
    private float bestTime = Mathf.Infinity;
    private double episodeReward = 0;
    private double avgReward = 0;
    private float lastDistFromGoal;
    
    #region Reward Variables
    private float losePenalty = -50f;
    private float winReward = +500f;
    private float closerToGoalReward = +1f;
    private float furtherFromGoalPenalty = -3f;
    #endregion

    public bool isGrounded;
   
    private void Start()
    {
        rb = transform.GetComponent<Rigidbody2D>();
        renderCanvas = FindObjectOfType<Canvas>();
        UpdateScoreUI();
    }

    public override void OnEpisodeBegin()
    {
        currentTime = startingTime;
        rb.velocity = Vector3.zero;
        episodeReward = 0;

        float x_agent;
        float x_goal;

        x_goal = Random.Range(spawnLeftBorder.localPosition.x, spawnRightBorder.localPosition.x);

        do
        {
            x_agent = Random.Range(spawnLeftBorder.localPosition.x, spawnRightBorder.localPosition.x);
        } while (x_agent >= -3.93f && x_agent <= 3f || x_agent >= 31.66f && x_agent <= 48.91f || x_agent >= 128f && x_agent <= 137f || x_agent >= 83.07f && x_agent <= 102f && math.abs(x_agent - x_goal) <= 20f);

        transform.localPosition = new Vector3(x_agent, 0.58f, 0f);
        targetTransform.localPosition = new Vector3(x_goal, 0.58f, 0f);
        lastDistFromGoal = Vector3.Distance(this.transform.localPosition, targetTransform.localPosition);
        //lastDistFromGoal = math.abs(this.transform.localPosition.x - targetTransform.localPosition.x);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);

        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveSide = actions.DiscreteActions[0];
        int jump = actions.DiscreteActions[1];
        int moveSpeed = actions.DiscreteActions[2];
        int jumpForce = actions.DiscreteActions[3];

        if (jump == 1 && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        if (moveSide == 1)
            rb.AddForce(Vector2.right * moveSpeed * Time.deltaTime, ForceMode2D.Impulse);
        else
            rb.AddForce(Vector2.left * moveSpeed * Time.deltaTime, ForceMode2D.Impulse);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        if (Input.GetKeyDown(KeyCode.D))
            discreteActions[0] = 1;
        else
            discreteActions[0] = 0;
        if (Input.GetKeyDown(KeyCode.Space))
            discreteActions[1] = 1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Goal")
        {
            AddReward(winReward);
            episodeReward += winReward;
            GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject.transform.SetParent(renderCanvas.transform, false);
            newObject.GetComponent<Rewarded>().SetRewardValue((double)winReward);
            
            avgReward = (avgReward * (numWin + numLose) + episodeReward) / (numWin + numLose + 1);
            UpdateRewardUI();

            //Debug.Log("Won - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            if (currentTime < bestTime && currentTime != 0f)
            {
                bestTime = currentTime;
                UpdateTimeUI();
            }

            avgWinTime = (avgWinTime * numWin + currentTime) / (numWin + 1);

            agentSpriteRenderer.color = Color.cyan;
            numWin++;
            if (numLose != 0)
                winPercentage = winPercentage = (float)numWin / ((float)numLose + (float)numWin) * 100;
            UpdateScoreUI();
            EndEpisode();
        }

        if (collision.tag == "Lava")
        {
            AddReward(losePenalty);
            episodeReward += losePenalty;
            GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject.transform.SetParent(renderCanvas.transform, false);
            newObject.GetComponent<Rewarded>().SetRewardValue((double)losePenalty);
            UpdateRewardUI();
            //Debug.Log("Lost - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            agentSpriteRenderer.color = Color.red;
            avgReward = (avgReward * (numWin + numLose) + episodeReward) / (numWin + numLose + 1);
            numLose++;
            if (numLose != 0)
                winPercentage = winPercentage = (float)numWin / ((float)numLose + (float)numWin) * 100;
            UpdateScoreUI();
            EndEpisode();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu");
        }

        currentTime += 1 * Time.deltaTime;
        UpdateTimeUI();

        /* Punish for the ball falling off the edges */
        if (transform.localPosition.y <= -4f)
        {
            AddReward(losePenalty);
            episodeReward += losePenalty;
            GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject.transform.SetParent(renderCanvas.transform, false);
            newObject.GetComponent<Rewarded>().SetRewardValue((double)losePenalty);
            UpdateRewardUI();
            //Debug.Log("Lost - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            agentSpriteRenderer.color = Color.red;
            avgReward = (avgReward * (numWin + numLose) + episodeReward) / (numWin + numLose + 1);
            numLose++;
            if (numLose != 0)
                winPercentage = (float)numWin / ((float)numLose + (float)numWin) * 100;
            UpdateScoreUI();
            EndEpisode();
        }

        /* Punish for time spent */
        AddReward(-Time.deltaTime);
        episodeReward -= Time.deltaTime;
        /*GameObject newObject2 = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
        newObject2.transform.SetParent(renderCanvas.transform, false);
        newObject2.GetComponent<Rewarded>().SetRewardValue((double)-Time.deltaTime);*/
        UpdateRewardUI();

        
        if (!isGrounded)
        {
            float x_agent = transform.localPosition.x;

            /* Reward for ball airtime spent when above obstacles */
            if (x_agent >= -1.889f && x_agent <= 1.313f || x_agent >= 33.74f && x_agent <= 47.07f || x_agent >= 130f && x_agent <= 134f || x_agent >= 85.43f && x_agent <= 98.86f)
            {
                AddReward(Time.deltaTime);
                episodeReward += Time.deltaTime;
                /*GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
                newObject.transform.SetParent(renderCanvas.transform, false);
                newObject.GetComponent<Rewarded>().SetRewardValue((double)Time.deltaTime);*/
                UpdateRewardUI();
            }

            /* Punish for ball airtime spent in when not needed */
            else
            {
                AddReward(-Time.deltaTime);
                episodeReward -= Time.deltaTime;
                /*GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
                newObject.transform.SetParent(renderCanvas.transform, false);
                newObject.GetComponent<Rewarded>().SetRewardValue((double)-Time.deltaTime);*/
                UpdateRewardUI();
            }

            /* Reward for higer ball velocity when above obstacles */
            AddReward(rb.velocity.magnitude * 0.01f);
            episodeReward += rb.velocity.magnitude * 0.01f;
            /*GameObject newObject3 = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject3.transform.SetParent(renderCanvas.transform, false);
            newObject3.GetComponent<Rewarded>().SetRewardValue((double)rb.velocity.magnitude * 0.01f);*/
            UpdateRewardUI();
        }

        /* Reward for the ball moving towards the target */
        /*math.abs(this.transform.localPosition.x - targetTransform.localPosition.x) < lastDistFromGoal*/
        if (Vector3.Distance(this.transform.localPosition, targetTransform.localPosition) < lastDistFromGoal)
        {
            AddReward(closerToGoalReward);
            episodeReward += closerToGoalReward;
            /*GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject.transform.SetParent(renderCanvas.transform, false);
            newObject.GetComponent<Rewarded>().SetRewardValue((double)closerToGoalReward);*/
            UpdateRewardUI();
        }

        /* Punish for the ball moving further from the target */
        /*math.abs(this.transform.localPosition.x - targetTransform.localPosition.x) > lastDistFromGoal*/
        else if (Vector3.Distance(this.transform.localPosition, targetTransform.localPosition) > lastDistFromGoal)
        {
            AddReward(furtherFromGoalPenalty);
            episodeReward += furtherFromGoalPenalty;
            /*GameObject newObject = Instantiate(rewardedEffect, new Vector3(0, 50, 0), transform.rotation) as GameObject;
            newObject.transform.SetParent(renderCanvas.transform, false);
            newObject.GetComponent<Rewarded>().SetRewardValue((double)furtherFromGoalPenalty);*/
            UpdateRewardUI();
        }
        //lastDistFromGoal = math.abs(this.transform.localPosition.x - targetTransform.localPosition.x);
        lastDistFromGoal = Vector3.Distance(this.transform.localPosition, targetTransform.localPosition); 
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        isGrounded = collision != null && (((1 << collision.gameObject.layer) & platformLayerMask) != 0);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isGrounded = false;
    }

    private void UpdateScoreUI()
    {
        if (this.transform.parent.gameObject.name != "Environment")
            return;
        winScoreUI.text = "Win Score:\n" + numWin.ToString();
        loseScoreUI.text = "Lose Score:\n" + numLose.ToString();
        winPercentageUI.text = "% Win:\n" + winPercentage.ToString("0.00") + "%";

    }

    private void UpdateTimeUI()
    {
        if (this.transform.parent.gameObject.name != "Environment")
            return;
        timerUI.text = "Episode Time:\n" + currentTime.ToString("0.000");
        avgWinTimeUI.text = "Average Win Time: \n" + avgWinTime.ToString("0.000");
    }

    private void UpdateRewardUI()
    {
        if (this.transform.parent.gameObject.name != "Environment")
            return;
        episodeRewardUI.text = "Episode Reward:\n" + episodeReward.ToString("0.0");
        avgRewardUI.text = "Average Reward:\n" + avgReward.ToString("0.0");
    }
}
