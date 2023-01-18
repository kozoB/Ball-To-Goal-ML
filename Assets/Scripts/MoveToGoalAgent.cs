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

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform[] lavaPlatforms;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform spawnLeftBorder;
    [SerializeField] private Transform spawnRightBorder;
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private SpriteRenderer agentSpriteRenderer;
    [SerializeField] private Text winScoreUI;
    [SerializeField] private Text loseScoreUI;
    [SerializeField] private Text winLoseRatioUI;
    [SerializeField] private Text timerUI;
    [SerializeField] private Text bestTimeUI;

    private float TimeInterval = 0;
    private Rigidbody2D rb;
    private int numWin = 0;
    private int numLose = 0;
    private float winLoseRatio = 0;
    private float currentTime;
    private float startingTime = 0f;
    private float bestTime = Mathf.Infinity;

    public bool isGrounded;
    
   
    private void Start()
    {
        rb = transform.GetComponent<Rigidbody2D>();
        UpdateScoreUI();
    }

    public override void OnEpisodeBegin()
    {
        currentTime = startingTime;

        rb.velocity = Vector3.zero;

        float x_agent;
        float x_goal;

        x_goal = Random.Range(spawnLeftBorder.localPosition.x, spawnRightBorder.localPosition.x);

        do
        {
            x_agent = Random.Range(spawnLeftBorder.localPosition.x, spawnRightBorder.localPosition.x);
        } while (x_agent >= -1.889f && x_agent <= 1.313f || x_agent >= 33.74f && x_agent <= 47.07f || x_agent >= 59.58f && x_agent <= 72.98f || x_agent >= 85.43f && x_agent <= 98.86f);

        transform.localPosition = new Vector3(x_agent, 0.58f, 0f);
        targetTransform.localPosition = new Vector3(x_goal, 0.58f, 0f);
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
        //float moveSpeed = 10f;
        //float jumpForce = 7f;

        int moveSide = actions.DiscreteActions[0];
        int jump = actions.DiscreteActions[1];

        int moveSpeed = actions.DiscreteActions[2];
        int jumpForce = actions.DiscreteActions[3];

        if (jump == 1 && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            AddReward(-2f);
        }

        //transform.position += new Vector3(moveX, 0, 0) * Time.deltaTime * moveSpeed;
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
            AddReward(+100f);
            //Debug.Log("Won - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            if (currentTime < bestTime)
            {
                bestTime = currentTime;
                UpdateTimeUI();
            }

            agentSpriteRenderer.color = Color.cyan;
            numWin++;
            if (numLose != 0)
                winLoseRatio = (float)numWin / (float)numLose;
            UpdateScoreUI();
            EndEpisode();
        }

        if (collision.tag == "Lava")
        {
            AddReward(-1f);
            //Debug.Log("Lost - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            agentSpriteRenderer.color = Color.red;
            numLose++;
            if (numLose != 0)
                winLoseRatio = (float)numWin / (float)numLose;
            UpdateScoreUI();
            EndEpisode();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        currentTime += 1 * Time.deltaTime;
        UpdateTimeUI();

        if (transform.localPosition.y <= -4f)
        {
            AddReward(-1f);
            //Debug.Log("Lost - Cumulative reward this episode: " + GetCumulativeReward().ToString());
            agentSpriteRenderer.color = Color.red;
            numLose++;
            if (numLose != 0)
                winLoseRatio = (float)numWin / (float)numLose;
            UpdateScoreUI();
            EndEpisode();
        }

        TimeInterval += Time.deltaTime;
        if (TimeInterval >= 1)
        {
            TimeInterval = 0;
            AddReward(-0.1f);
        }
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
        winLoseRatioUI.text = "W/L Ratio:\n" + winLoseRatio.ToString();
    }

    private void UpdateTimeUI()
    {
        if (this.transform.parent.gameObject.name != "Environment")
            return;
        timerUI.text = "Episode Time:\n" + currentTime.ToString("0.000");
        bestTimeUI.text = "Best Time:\n" + bestTime.ToString("0.000");
    }
}
