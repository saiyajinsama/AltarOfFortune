﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckBehavior : MonoBehaviour
{
    public SessionDetails session;

    public List<GameObject> targetsToDrawCardOn = new List<GameObject>();
    public int nextFrameIndex = 0;

    public List<GameObject> collectionToDrawFrom = new List<GameObject>();
    public int sleepBetweenDraws = 500;

    public enum DrawMethod { replaceTargetSprite, CreateNewCardAtTarget };
    public DrawMethod myDrawMethod = DrawMethod.CreateNewCardAtTarget;

    private DateTime timeOfLastAction = DateTime.Now;
    private string NL = "\r\n";

    public GameObject LastFrame;
    public bool ResetOnLastFrame = true;

    public AudioClip ShuffleSoundEffect;

    // Use this for initialization
    void Start()
    {
        if (LastFrame == null)
            print("No LastFrame defined for when a 'draw' click occurs!" + NL);

        if (ShuffleSoundEffect == null)
            print("No ShuffleSoundEffect defined for reshuffling!" + NL);

        if (targetsToDrawCardOn == null)
            print("No targets for when a 'draw' click occurs!" + NL);

        if (collectionToDrawFrom.Count == 0)
            print("No source Card GameObject collection defined to randomly 'draw' from!" + NL);

        timeOfLastAction = DateTime.Now;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Returns true if the current time is on/after the last clicked time + the specified secs between draws (Read-only)
    /// </summary>
    bool isResting
    {
        get
        {
            return DateTime.Now < timeOfLastAction.AddMilliseconds(sleepBetweenDraws);
        }
    }
    
    void OnMouseUp()
    {
        try
        {
            //Draw a card if not resting  
            if (!isResting && targetsToDrawCardOn.Count > (nextFrameIndex-1) && collectionToDrawFrom.Count > 0)
            {
                GameObject targetCard = targetsToDrawCardOn[nextFrameIndex];

                if (targetCard && targetCard != LastFrame)
                {
                    DrawCard(targetCard, nextFrameIndex, session.CurrentRow);
                    session.TriggerRulesCheck();
                }
                else if (targetCard == LastFrame && ResetOnLastFrame)
                {
                    ResetAndReshuffle(true);
                }

                timeOfLastAction = DateTime.Now;

                //Go to the next index or reset it to Zero
                MoveToNextIndex();
            }
        }
        catch(Exception error)
        {
            string msg = string.Empty;

            if (error.Message != null)
                msg = error.Message + " ";

            if (error.InnerException != null && error.InnerException.Message != null)
                msg += error.InnerException.Message;

            print(msg + NL);
        }
    }

    private void MoveToNextIndex()
    {
        if (nextFrameIndex + 1 > targetsToDrawCardOn.Count - 1)
            nextFrameIndex = 0;
        else
            nextFrameIndex++;
    }

    public void ResetAndReshuffle(bool PlayShuffleSound)
    {
        //Reset lives if resetting after Game Over
        if (session.Lives <= 0)
            session.ResetLives();

        //Remove all card game objects
        GameObject[] gObjs = FindObjectsOfType<GameObject>();
        foreach(GameObject g in gObjs)
        {
            if (g.name.Contains("Clubs")            ||
                g.name.Contains("Spades")           ||
                g.name.Contains("Hearts")           ||
                g.name.Contains("Diamonds")         ||
                g.name.Contains("Joker")            ||
                g.name.Contains("deathAxeEffect"))
                Destroy(g);
        }
        
        //Play shuffle sfx
        if(PlayShuffleSound && ShuffleSoundEffect != null)
        {
            AudioSource aSource = gameObject.GetComponent<AudioSource>(); 
            aSource.clip = ShuffleSoundEffect;
            aSource.Play();
        }
    }

    bool drawCardVerboseLogging = false;
    private void DrawCard(GameObject targetCard, int NewCardIndex, int NewCardRow)
    {
        //Select a random card from those in the collection
        System.Random randomInt = new System.Random();
        int randomIndex = randomInt.Next(collectionToDrawFrom.Count);
        GameObject randomCard = collectionToDrawFrom[randomIndex];

        Log("Random index: " + randomIndex + NL + "Random card: " + randomCard.name);

        //"Draw" a random card
        if (myDrawMethod == DrawMethod.replaceTargetSprite)
        {
            Log("Changing " + targetCard.name + " to " + randomCard.name);
            CopyCardSprite(randomCard, targetCard);
        }
        else if (myDrawMethod == DrawMethod.CreateNewCardAtTarget)
        {
            Log("Created " + randomCard.name + " at " + targetCard.transform.position);
            GameObject newCard = Instantiate(randomCard, targetCard.transform.position, new Quaternion());

            //Set sorting layer so newest drawn card is topmost
            newCard.GetComponent<SpriteRenderer>().sortingOrder = session.CurrentCardLayer;
            session.CurrentCardLayer++;

            //Disable the "dragToMove"
            CardBehavior newCardBehavior = newCard.GetComponent<CardBehavior>();
            newCardBehavior.ClickToDrag = false;
            newCardBehavior.Row   = NewCardRow;
            newCardBehavior.Index = NewCardIndex;

            //Update current card
            session.CurrentCard = newCard;
        }

        session.TotalDrawnCards++;
        
        //Play Sfx
        SfxPlayer player = GetComponent<SfxPlayer>();
        if (player != null)
        {
            player.SelectNewClip();
            player.GetComponent<AudioSource>().Play();
        }
    }

    private void Log(string message)
    {
        if (drawCardVerboseLogging)
            print(message + NL);
    }

    void CopyCardSprite(GameObject fromGameObject, GameObject toGameObject)
    {
        //Copy the game object's name
        toGameObject.name = fromGameObject.name;

        //Copy the game object's sprite (property of the Sprite Renderer)
        SpriteRenderer sourceSprite = fromGameObject.GetComponent<SpriteRenderer>();
        SpriteRenderer targetSprite = toGameObject.GetComponent<SpriteRenderer>();

        if (sourceSprite != null && targetSprite != null)
            targetSprite.sprite = sourceSprite.sprite;

        //Enable the target's renderer (in case it was disabled)
        targetSprite.enabled = true;
    }

    public void SetNextCardClickToShuffle()
    {
        nextFrameIndex = targetsToDrawCardOn.Count - 1;
    }
}
