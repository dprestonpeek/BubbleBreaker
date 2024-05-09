using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleBehavior : MonoBehaviour
{
    public enum Color { Cyan, Green, Purple, Red, Yellow, Black }
    [SerializeField]
    public Color color;
    public Vector2 location;

    private float leftEdge = -2.5f;
    private float rightEdge = 2.5f;
    private float topEdge = 4.5f;
    private float botEdge = -4.5f;

    // Start is called before the first frame update
    void Start()
    {
        location = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MatchBubbles()
    {
        //check left
        if (GameManager.bubbles.TryGetValue(new Vector2(location.x - 1, location.y), out GameObject bubbleLeft))
        {
            BubbleBehavior bbLeft = bubbleLeft.GetComponent<BubbleBehavior>();
            if (bbLeft && bbLeft.color == color)
            {
                if (!GameManager.matchingBubbles.Contains(bubbleLeft))
                {
                    GameManager.matchingBubbles.Add(bubbleLeft);
                    bbLeft.MatchBubbles();
                }
            }
        }
        //check right
        if (GameManager.bubbles.TryGetValue(new Vector2(location.x + 1, location.y), out GameObject bubbleRight))
        {
            BubbleBehavior bbRight = bubbleRight.GetComponent<BubbleBehavior>();
            if (bbRight && bbRight.color == color)
            {
                if (!GameManager.matchingBubbles.Contains(bubbleRight))
                {
                    GameManager.matchingBubbles.Add(bubbleRight);
                    bbRight.MatchBubbles();
                }
            }
        }
        //check up
        if (GameManager.bubbles.TryGetValue(new Vector2(location.x, location.y + 1), out GameObject bubbleUp))
        {
            BubbleBehavior bbUp = bubbleUp.GetComponent<BubbleBehavior>();
            if (bbUp && bbUp.color == color)
            {
                if (!GameManager.matchingBubbles.Contains(bubbleUp))
                {
                    GameManager.matchingBubbles.Add(bubbleUp);
                    bbUp.MatchBubbles();
                }
            }
        }
        //check down
        if (GameManager.bubbles.TryGetValue(new Vector2(location.x, location.y - 1), out GameObject bubbleDown))
        {
            BubbleBehavior bbDown = bubbleDown.GetComponent<BubbleBehavior>();
            if (bbDown && bbDown.color == color)
            {
                if (!GameManager.matchingBubbles.Contains(bubbleDown))
                {
                    GameManager.matchingBubbles.Add(bubbleDown);
                    bbDown.MatchBubbles();
                }
            }
        }
    }

    public bool HasEmptySpaceBelow()
    {
        //check down
        if (GameManager.bubbles.TryGetValue(new Vector2(location.x, location.y - 1), out GameObject bubbleDown))
        {
            return false;
        }
        return true;
    }

    public void DropOneSpace()
    {
        transform.position = new Vector2(transform.position.x, transform.position.y - 1);
    }
}
