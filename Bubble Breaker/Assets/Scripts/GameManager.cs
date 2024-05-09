using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    [SerializeField]
    GameObject EndGame;
    [SerializeField]
    TMP_Text EndScore;
    [SerializeField]
    TMP_Text EndBest;
    [SerializeField]
    TMP_Text Timer;

    private static int multiplier = 1;
    private static int score;
    private static int best;
    private static int combo;
    private static int amountScorePopup;
    private static float scorePopupInit;

    //timers
    private static float gameTimerTarget = 0;
    private static float gameTimer = 5;
    private static float pullColumnsTarget = .5f;
    private static float pullColumnsTimer = 0;

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
    private static Dictionary<GameObject, int> postponeBringDown;
    private static List<Vector2> spacesToFill;
    private static List<Vector2> locationsToRepopulate;
    private static List<Vector2> bubblesToReplace;
    private static bool repopulate = false;
    private static bool updateScores = false;
    private static bool showScorePopup = false;
    private static bool showingScorePopup = false;
    private static bool playAudio = false;
    private static bool pullColumns = false;
    private static bool waitBeforePullColumns = false;
    private static bool performingMove = false;
    private static bool endGame = false;

    private static int audioLevel = -1;
    //set to 0 to include black
    private static int includeBlack = 1;
    private static Color recentColor = Color.white;
    private static Vector2 recentLocation = Vector2.zero;
    Button[] buttons;

    // Start is called before the first frame update
    void Start()
    {
        postponeBringDown = new Dictionary<GameObject, int>();
        bubblesToReplace = new List<Vector2>();
        matchingBubbles = new List<GameObject>();
        locationsToRepopulate = new List<Vector2>();
        spacesToFill = new List<Vector2>();
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
    void FixedUpdate()
    {
        OnTimerTick();
        if (gameTimer <= gameTimerTarget)
        {
            //end game
            endGame = true;
            EndGame.SetActive(true);
            EndScore.text = score.ToString();
            EndBest.text = best.ToString();
            return;
        }
        else
        {
            Timer.text = gameTimer.ToString();
        }
        if (waitBeforePullColumns)
        {
            if (pullColumnsTimer >= pullColumnsTarget)
            {
                waitBeforePullColumns = false;
                pullColumns = true;
            }
        }
        if (pullColumns)
        {
            if (!PullColumns()) 
            {
                pullColumns = false;
                repopulate = true;
            }
        }
        if (repopulate)
        {
            foreach (Vector2 location in locationsToRepopulate)
            {
                if (!bubbles.ContainsKey(location))
                {
                    int color = Random.Range(0, bubblePrefabs.Count - includeBlack);
                    bubbles.Add(location, Instantiate(bubblePrefabs[color], location, Quaternion.identity, transform));
                }
            }
            locationsToRepopulate.Clear();
            bubblesToReplace.Clear();
            repopulate = false;
            performingMove = false;
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
                ScorePopup.transform.position = new Vector3(ScorePopup.transform.position.x, ScorePopup.transform.position.y + .01f, -2);
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
        if (spacesToFill != null && locationsToRepopulate != null && spacesToFill.Count == 0 && locationsToRepopulate.Count == 0)
        {
            performingMove = false;
        }
    }

    public static void OnClick()
    {
        if (performingMove || endGame)
        {
            return;
        }
        performingMove = true;
        matchingBubbles.Clear();
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        string[] buttonXY = buttonName.Split(",");
        Vector2 location = new Vector2(float.Parse(buttonXY[0]), float.Parse(buttonXY[1]));
        recentLocation = location;

        ClickButton(location);
    }

    private static void ClickButton(Vector2 location)
    {
        if (!bubbles.TryGetValue(location, out GameObject bubble))
        {
            return;
        }

        //compare bubble color to colors around
        bubble.GetComponent<BubbleBehavior>().MatchBubbles();

        //break bubbles from list
        if (matchingBubbles.Count < 2)
        {
            return;
        }
        else if (matchingBubbles.Count > 5)
        {
            audioLevel = 3;
            playAudio = true;
            if (multiplier < 3 + combo)
            {
                multiplier = 3 + combo;
            }
            else
            {
                multiplier++;
            }
            combo += 2;
        }
        else if (matchingBubbles.Count > 3)
        {
            audioLevel = 2;
            playAudio = true;
            if (multiplier < 2 + combo)
            {
                multiplier = 2 + combo;
            }
            else
            {
                multiplier++;
            }
            combo += 1;
        }
        else
        {
            audioLevel = 1;
            playAudio = true;
            if (matchingBubbles.Count == 3)
            {
                if (multiplier > 1)
                {
                    multiplier -= 1;
                }
            }
            else
            {
                multiplier = 1;
            }
            combo = 0;
        }
        UpdateScores(10 * matchingBubbles.Count * multiplier);
        if (!matchingBubbles.Contains(bubble))
        {
            matchingBubbles.Add(bubble);
        }

        foreach (GameObject bub in matchingBubbles)
        {
            if (!spacesToFill.Contains(bub.transform.position))
            {
                spacesToFill.Add(bub.transform.position);
            }
            bubblesToReplace.Add(bub.transform.position);
            bubbles.Remove(bub.transform.position);
            Destroy(bub);
        }

        //replace with new bubbles.
        SetTimer(0.5f);
        waitBeforePullColumns = true;
    }

    private static void ExplodeBomb(Vector2 location)
    {
        List<GameObject> bubblesToExplode = new List<GameObject>();
        bubblesToExplode.Add(GetBubble(new Vector2(location.x - 1, location.y - 1)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x - 1, location.y + 0)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x - 1, location.y + 1)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x + 0, location.y - 1)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x + 0, location.y + 1)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x + 1, location.y - 1)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x + 1, location.y + 0)));
        bubblesToExplode.Add(GetBubble(new Vector2(location.x + 1, location.y + 1)));
        spacesToFill.Add(location);
        for (int i = 0; i < bubblesToExplode.Count; i++)
        {
            spacesToFill.Add(bubblesToExplode[i].transform.position);
            bubbles.Remove(bubblesToExplode[i].transform.position);
            Destroy(bubblesToExplode[i]);
        }
        waitBeforePullColumns = true;
    }

    public void Panic()
    {
        GetMissingBubbles();
    }

    private static List<Vector2> GetMissingBubbles()
    {
        float x = -2.5f;
        float y = -4.5f;
        float xVal = 0;
        float yVal = 0;
        List<Vector2> missingBubbles = new List<Vector2>();

        for (int i = 1; i < 10; i++)
        {
            xVal = x + i;
            for (int j = 1; j < 6; j++)
            {
                yVal = y + i;
                if (!GetBubble(new Vector2(xVal, yVal)))
                {
                    missingBubbles.Add(new Vector2(xVal, yVal));
                }
            }
        }
        return missingBubbles;
    }

    private static Dictionary<float, List<Vector2>> GetColumnsToPull()
    {
        Dictionary<float, List<Vector2>> columns = new Dictionary<float, List<Vector2>>();
        foreach (Vector2 location in spacesToFill)
        {
            if (columns.ContainsKey(location.x))
            {
                columns[location.x].Add(location);
            }
            else
            {
                columns.Add(location.x, new List<Vector2>());
                columns[location.x].Add(location);
            }
        }
        return columns;
    }

    private static bool PullColumns()
    {
        Dictionary<float, List<Vector2>> columns = GetColumnsToPull();
        bool didWork = false;
        foreach (KeyValuePair<float, List<Vector2>> column in columns)
        {
            PullColumn(column.Value);
            didWork = true;
        }
        return didWork;
    }
    private static void PullColumn(List<Vector2> column)
    {
        Vector2 lowestEmpty = Vector2.one * 100;
        Vector2 highestEmpty = Vector2.one * -100;
        List<Vector2> sortedColumn = new List<Vector2>();
        sortedColumn.AddRange(column);

        foreach (Vector2 location in column)
        {
            if (location.y < lowestEmpty.y)
            {
                lowestEmpty = location;
            }
            if (location.y > highestEmpty.y)
            {
                highestEmpty = location;
            }
        }

        //sort the column
        Vector2 low = lowestEmpty;
        Vector2 high = highestEmpty;
        for (int i = 0; i < column.Count; i++)
        {
            int lowest = i;

            for (int j = i + 1; j < column.Count; j++)
            {
                if (sortedColumn[j].y < sortedColumn[lowest].y)
                {
                    lowest = j;
                }
            }

            Vector2 temp = sortedColumn[lowest];
            sortedColumn[lowest] = sortedColumn[i];
            sortedColumn[i] = temp;
        }

        //bring bubbles down
        for (float k = -4.5f; k < 4.5f; k += 1)
        {
            int i = 0;
            while (i < 10 && !BringNextBubbleDown(new Vector2(sortedColumn[0].x, k + i), sortedColumn.Count))
            {
                i++;
            }
        }
        //foreach (KeyValuePair<GameObject, int> bub in postponeBringDown)
        //{
        //    BringNextBubbleDown(bub.Key.transform.position, bub.Value);
        //}
        //foreach (Vector2 loc in spacesToFill)
        //{
        //    int i = 0;
        //    while (!BringNextBubbleDown(new Vector2(loc.x, loc.y + i), sortedColumn.Count) || i < 10)
        //    {
        //        i++;
        //    }
        //}
        postponeBringDown.Clear();
        spacesToFill.Clear();

        //repopulate 
        float l = 4.5f;
        for (int m = 0; m < sortedColumn.Count; m++)
        {
            locationsToRepopulate.Add(new Vector2(sortedColumn[0].x, l));
            l -= 1;
        }
    }

    private static bool IsSorted(List<Vector2> locations)
    {
        for (int i = 0; i < locations.Count - 1; i++)
        {
            if (locations[i].y < locations[i + 1].y)
            {
                continue;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private static GameObject GetBubble(Vector2 location)
    {
        if (bubbles.ContainsKey(location))
        {
            return bubbles[location];
        }
        return null;
    }

    private static bool BringNextBubbleDown(Vector2 location, int amount)
    {
        bool success = false;
        //get the bubble in the space above the given location
        Vector2 higherLoc = new Vector2(location.x, location.y + amount);
        GameObject bubble = GetBubble(higherLoc);

        //if bubble exists, drop it one space
        if (bubble)
        {
            for (int i = 0; i < amount; i++)
            {
                BubbleBehavior bb = bubble.GetComponent<BubbleBehavior>();
                if (bb.HasEmptySpaceBelow())
                {
                    bubbles.Remove(bubble.transform.position);
                    bb.DropOneSpace();

                    if (!bubbles.ContainsKey(bubble.transform.position))
                    {
                        bubbles.Add(bubble.transform.position, bubble);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (!postponeBringDown.ContainsKey(bubble))
                    {
                        postponeBringDown.Add(bubble, amount);
                    }
                }
            }
            success = true;
        }
        return success;
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
        //add extra second for big pops
        if (multiplier >= 3)
        {
            gameTimer += 1;
        }
        gameTimer += 1;
        showScorePopup = true;
        updateScores = true;
    }

    private void OnTimerTick()
    {
        if (pullColumnsTimer < pullColumnsTarget)
        {
            pullColumnsTimer += Time.deltaTime;
        }
        if (gameTimer > gameTimerTarget)
        {
            gameTimer -= Time.deltaTime;
        }
    }

    private static void SetTimer(float seconds)
    {
        pullColumnsTarget = seconds;
        pullColumnsTimer = 0;
    }

    public void PlayAgain()
    {
        gameTimer = 60;
        score = 0;
        updateScores = true;
        endGame = false;
        SceneManager.LoadScene(0);
    }
}
