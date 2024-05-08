using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text Score;
    [SerializeField]
    TMP_Text Best;
    [SerializeField]
    GameObject ScorePopup;
    [SerializeField]
    TMP_Text ScorePopupText;

    private static int score;
    private static int best;
    private static int combo;
    private static int amountScorePopup;
    private static float scorePopupInit; 
    
    //timer
    private static float targetTime = 60.0f;
    private static float timer = 0;

    [SerializeField]
    List<GameObject> bubblePrefabs = new List<GameObject>();

    [SerializeField]
    AudioClip popSmall;
    [SerializeField]
    AudioClip popMed;
    [SerializeField]
    AudioClip popBig;
    static AudioSource audio;

    public static Dictionary<Vector2, GameObject> bubbles;
    public static List<GameObject> matchingBubbles;
    private static List<Vector2> locationsToPopulate;
    private static bool repopulate = false;
    private static bool updateScores = false;
    private static bool showScorePopup = false;
    private static bool showingScorePopup = false;
    private static bool playAudio = false;

    private static int audioLevel = -1;
    //set to 0 to include black
    private static int includeBlack = 1;
    private static Color recentColor = Color.white;
    private static Vector2 recentLocation = Vector2.zero;
    Button[] buttons;

    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();
        bubbles = new Dictionary<Vector2, GameObject>();
        buttons = GameObject.FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            string[] buttonXY = button.name.Split(",");
            Vector2 location = new Vector2(float.Parse(buttonXY[0]), float.Parse(buttonXY[1]));
            bubbles.Add(location, Instantiate(bubblePrefabs[Random.Range(0, bubblePrefabs.Count - includeBlack)], location, Quaternion.identity, transform));
        }
        best = PlayerPrefs.GetInt("best");
        score = 0;
        updateScores = true;
    }

    // Update is called once per frame
    void Update()
    {
        OnTimerTick();
        if (repopulate)
        {
            if (timer >= targetTime)
            {
                foreach (Vector2 location in locationsToPopulate)
                {
                    if (!bubbles.ContainsKey(location))
                    {
                        int color = Random.Range(0, bubblePrefabs.Count - includeBlack);
                        while (recentColor == bubblePrefabs[color].GetComponent<Renderer>().sharedMaterial.color)
                        {
                            color = Random.Range(0, bubblePrefabs.Count - includeBlack);
                        }
                        bubbles.Add(location, Instantiate(bubblePrefabs[color], location, Quaternion.identity, transform));
                    }
                }
                repopulate = false;
            }
        }
        if (updateScores)
        {
            Score.text = score.ToString();
            Best.text = best.ToString();
            updateScores = false;
        }
        if (showScorePopup)
        {
            ScorePopup.SetActive(true);
            ScorePopup.transform.position = new Vector3(recentLocation.x + 0.5f, recentLocation.y + 0.5f, -2);
            scorePopupInit = ScorePopup.transform.position.y;
            ScorePopupText.text = amountScorePopup.ToString();
            showScorePopup = false;
            showingScorePopup = true;
        }
        if (showingScorePopup)
        {
            if (ScorePopup.transform.position.y < scorePopupInit + 1)
            {
                ScorePopup.transform.position = new Vector3(ScorePopup.transform.position.x, ScorePopup.transform.position.y + .025f, -2);
            }
            else
            {
                ScorePopup.SetActive(false);
                showingScorePopup = false;
            }
        }
        if (playAudio)
        {
            switch (audioLevel)
            {
                case 1:
                    audio.PlayOneShot(popSmall);
                    break;
                case 2:
                    audio.PlayOneShot(popMed);
                    break;
                case 3:
                    audio.PlayOneShot(popBig);
                    break;
            }
            playAudio = false;
        }
    }

    public static void OnClick()
    {
        if (matchingBubbles != null)
        {
            matchingBubbles.Clear();
        }
        else
        {
            matchingBubbles = new List<GameObject>();
        }
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        string[] buttonXY = buttonName.Split(",");
        Vector2 location = new Vector2(float.Parse(buttonXY[0]), float.Parse(buttonXY[1]));
        recentLocation = location;

        foreach (KeyValuePair<Vector2, GameObject> bubble in bubbles)
        {
            if (bubble.Value.transform.position.x == location.x 
                && bubble.Value.transform.position.y == location.y)
            {
                //TODO:
                //compare bubble color to colors around
                recentColor = bubble.Value.GetComponent<Renderer>().material.color;
                bubble.Value.GetComponent<BubbleBehavior>().MatchBubbles();
                int multiplier = 1;

                //break bubbles from list
                if (matchingBubbles.Count < 2)
                {
                    return;
                }
                else if (matchingBubbles.Count > 5)
                {
                    audioLevel = 3;
                    playAudio = true;
                    multiplier = 3 + combo;
                    combo += 1;
                }
                else if (matchingBubbles.Count > 3)
                {
                    audioLevel = 2;
                    playAudio = true;
                    multiplier = 2 + combo;
                    combo += 1;
                }
                else
                {
                    audioLevel = 1;
                    playAudio = true;
                    combo = 0;
                }
                UpdateScores(10 * matchingBubbles.Count * multiplier);
                matchingBubbles.Add(bubble.Value);
            }
        }
        if (locationsToPopulate == null)
        {
            locationsToPopulate = new List<Vector2>();
        }
        else
        {
            locationsToPopulate.Clear();
        }

        foreach (GameObject bub in matchingBubbles)
        {
            locationsToPopulate.Add(bub.transform.position);
            bubbles.Remove(bub.transform.position);
            Destroy(bub);
        }

        //replace with new bubbles.
        SetTimer(.5f);
        repopulate = true;
    }

    private static void UpdateScores(int addToScore)
    {
        score += addToScore;
        if (score > best)
        {
            best = score;
        }
        PlayerPrefs.SetInt("best", best);
        amountScorePopup = addToScore;
        showScorePopup = true;
        updateScores = true;
    }

    private void OnTimerTick()
    {
        if (timer < targetTime)
        {
            timer += Time.deltaTime;
        }
    }

    private static void SetTimer(float seconds)
    {
        GameManager.targetTime = seconds;
        GameManager.timer = 0;
    }
}
