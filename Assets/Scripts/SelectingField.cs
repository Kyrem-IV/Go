using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

public static class GetCoordinatesOf
{
    public static Tuple<int, int> CoordinatesOf<T>(this T[,] matrix, T value)
    {
        int w = matrix.GetLength(0); // width
        int h = matrix.GetLength(1); // height

        for (int x = 0; x < w; ++x)
        {
            for (int y = 0; y < h; ++y)
            {
                if (matrix[x, y] != null && matrix[x, y].Equals(value))
                {
                    return Tuple.Create(x, y);
                }
            }
        }
        SelectingField.error = true; // if the stone isnt in the array; stop the code
        return Tuple.Create(- 1, -1);
    }
}

public class SelectingField : MonoBehaviour, IPointerClickHandler
{
    public static bool error = false;
    static bool turn = true;
    static bool passing = false;
    static bool libertyPresent;
    static bool protectionActive = false;
    static bool something = false;
    static string playerColour;
    static string opponentColour;
    static string currentColour;
    static UnityEngine.Object[,] nineByNine = new UnityEngine.Object[9, 9];
    static UnityEngine.Object currentStone;
    static UnityEngine.Object protectedStone;
    static int xAxis;
    static int yAxis;
    static int protectedX;
    static int protectedY;
    static int whitePoints = 0;
    static int blackPoints = 0;
    GameObject[] waypoints;
    GameObject gameOverCard;
    public TMP_Text winText;

    static List<UnityEngine.Object> stonesOnBoard = new List<UnityEngine.Object>();
    static List<UnityEngine.Object> stonesToDestroy = new List<UnityEngine.Object>();
    static List<UnityEngine.Object> surroundingStones = new List<UnityEngine.Object>();
    static List<UnityEngine.Object> surroundingOpponents = new List<UnityEngine.Object>();
    static List<Tuple<int, int>> protectedPositions = new List<Tuple<int, int>>();

    public void Start()
    {
        waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        gameOverCard = GameObject.Find("GameOverCard");
        protectedStone = GameObject.Find("Protected Stone");
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (turn)
        {
            playerColour = "Black";
            opponentColour = "White";
        }
        else
        {
            playerColour = "White";
            opponentColour = "Black";
        }

        var waypointName = transform.name; // get the name of current waypoint and related row
        var rowName = transform.parent.name;
        xAxis = Convert.ToInt32(new string(waypointName.Where(char.IsDigit).ToArray())); // extract their numbers to define position
        yAxis = Convert.ToInt32(new string(rowName.Where(char.IsDigit).ToArray()));
        
        if (nineByNine[xAxis, yAxis] == null) // place stone only on empty fields
        {
            UnityEngine.Object stone = Instantiate(Resources.Load(playerColour), transform.position, Quaternion.identity); // create stone at given position
            nineByNine[xAxis, yAxis] = stone; // write position in array
            stonesOnBoard.Add(stone);
            currentStone = stone;

            CheckForDestruction(stone);

            if (surroundingStones.Count > 0 && libertyPresent == false) // if there is an adjacent stone of same colour; check for chain
            {
                for (int b = 0; b < surroundingStones.Count; b++)
                {
                    CheckForDestruction(surroundingStones[b]);
                }
            }
            if (libertyPresent == false) // if all adjacent stones are of opposing colour; check for destruction of enemy stones, if it were to be placed here regardless and destroys that one instead on potential suicide play
            {
                surroundingOpponents.Clear();
                CheckForDestruction(currentStone);
                playerColour = opponentColour;                
                int trie = surroundingOpponents.Count;
                
                for (int b = 0; b < trie; b++)
                {
                    surroundingStones.Clear();
                    CheckForDestruction(surroundingOpponents[b]);
                    if (surroundingStones.Count > 0 && libertyPresent == false) // if there is an adjacent stone of same colour; check for chain
                    {
                        for (int z = 0; z < surroundingStones.Count; z++)
                        {
                            CheckForDestruction(surroundingStones[z]);
                        }
                    }
                    if (libertyPresent == false) // if stone were to be placed here, enemy stone gets taken before own stone dies
                    {
                        currentStone = surroundingOpponents[b];
                        stonesToDestroy.Add(currentStone);
                        protectionActive = true;
                        protectedX = GetCoordinatesOf.CoordinatesOf(nineByNine, currentStone).Item1;
                        protectedY = GetCoordinatesOf.CoordinatesOf(nineByNine, currentStone).Item2;
                        protectedPositions.Add(Tuple.Create(protectedX, protectedY));                                      
                        something = true;
                    }
                }
                if (something) // activated in previous for loop, if ko rule was used
                {
                    surroundingStones.Clear();
                    for (int p = 0; p < stonesToDestroy.Count; p++)
                    {                        
                        CheckForDestruction(stonesToDestroy[p]);

                        if (surroundingStones.Count > 0 && libertyPresent == false) // if there is an adjacent stone of same colour; check for chain
                        {
                            for (int b = 0; b < surroundingStones.Count; b++)
                            {
                                CheckForDestruction(surroundingStones[b]);

                                if (!libertyPresent && !stonesToDestroy.Contains(surroundingStones[b]))
                                {
                                    stonesToDestroy.Add(surroundingStones[b]);
                                }
                            }
                        }
                    }
                    passing = false;    
                    turn = !turn;
                    libertyPresent = false;
                    something = false;
                }
                else // stone cannot be placed on clicked field
                {
                    xAxis = GetCoordinatesOf.CoordinatesOf(nineByNine, currentStone).Item1;
                    yAxis = GetCoordinatesOf.CoordinatesOf(nineByNine, currentStone).Item2;
                    nineByNine[xAxis, yAxis] = null;
                    stonesOnBoard.Remove(currentStone);
                    Destroy(currentStone);
                }
            }
            else // stone is allowed to stay
            {
                passing = false; // if a player passed before, reset that condition
                turn = !turn; // switch turn

                if (protectionActive)
                {
                    protectionActive = false;

                    for (int l = 0; l < protectedPositions.Count; l++)
                    {
                        nineByNine[protectedPositions[l].Item1, protectedPositions[l].Item2] = null;
                    }
                    protectedPositions.Clear();
                }
            }
            DestroyStone();
        }
    }

    public void ChoosingToPass() // lets players pass their turns
    {
        if (!passing)
        {
            turn = !turn;
            passing = true;
        }
        else
        {
            foreach (var waypoint in nineByNine) // add points not working
            {
                currentColour = null;
                surroundingStones.Clear();
                libertyPresent = true;
                // AddPoints(waypoint);
            
                if (surroundingStones.Count > 0)
                {
                    for (int b = 0; b < surroundingStones.Count; b++)
                    {
                        // AddPoints(surroundingStones[b]);
                    }
                }
                if (libertyPresent == true)
                {
                    if (currentColour == "White(Clone)")
                    {
                        whitePoints += 1;
                    }
                    else if (currentColour == "Black(Clone)")
                    {
                        blackPoints += 1;
                    }                
                }
            }

            for (int t = 0; t < 81; t++) // deactivate field
            {
                waypoints[t].SetActive(false);
            }

            if (whitePoints > blackPoints) // show who won
            {
                winText.text = $"Schwarz hat {blackPoints} und Weiß hat {whitePoints} Punkte! Weiß gewinnt!";
            }
            else if (whitePoints < blackPoints)
            {
                winText.text = $"Schwarz hat {blackPoints} und Weiß hat {whitePoints} Punkte! Schwarz gewinnt!";
            }
            else
            {
                winText.text = $"Schwarz hat {blackPoints} und Weiß hat {whitePoints} Punkte! Ein Unentschieden!";
            }
            gameOverCard.transform.localScale = new Vector2(200, 130);
        }
    }

    public void AddPoints(UnityEngine.Object waypoint)
    {        
        // go through array of every waypoint, look for adjacent stones; if all of them are of 1 colour, add 1 point for each adjacent vacant waypoint to that colour
        if (waypoint == null)
        {
            for (int x = 0; x < 9; ++x)
        {
            for (int y = 0; y < 9; ++y)
            {
                if (nineByNine[x, y] == null && nineByNine[x, y].Equals(waypoint))
                {
                    xAxis = x;
                    yAxis = y;
                    break;
                }
            }
        }




            if (xAxis != 8 && nineByNine[xAxis + 1, yAxis] == null && !surroundingStones.Contains(nineByNine[xAxis + 1, yAxis]))
            {
                surroundingStones.Add(nineByNine[xAxis + 1, yAxis]);                    
            }
            else if (xAxis != 8 && nineByNine[xAxis + 1, yAxis] != null && currentColour == null)
            {
                currentColour = nineByNine[xAxis + 1, yAxis].name;
            }
            else if (xAxis != 8 && nineByNine[xAxis + 1, yAxis].name != currentColour)
            {
                libertyPresent = false; // this is the only moment a true false should happen
                surroundingStones.Clear();
                return;
            }

            if (yAxis != 8 && nineByNine[xAxis, yAxis + 1] == null && !surroundingStones.Contains(nineByNine[xAxis, yAxis  + 1]))
            {
                surroundingStones.Add(nineByNine[xAxis, yAxis + 1]);                    
            }
            else if (yAxis != 8 && nineByNine[xAxis, yAxis + 1] != null && currentColour == null)
            {
                currentColour = nineByNine[xAxis, yAxis + 1].name;
            }
            else if (yAxis != 8 && nineByNine[xAxis, yAxis + 1].name != currentColour)
            {
                libertyPresent = false; // this is the only moment a true false should happen
                surroundingStones.Clear();
                return;
            }

            if (xAxis != 0 && nineByNine[xAxis - 1, yAxis] == null && !surroundingStones.Contains(nineByNine[xAxis - 1, yAxis]))
            {
                surroundingStones.Add(nineByNine[xAxis - 1, yAxis]);                    
            }
            else if (xAxis != 0 && nineByNine[xAxis - 1, yAxis] != null && currentColour == null)
            {
                currentColour = nineByNine[xAxis - 1, yAxis].name;
            }
            else if (xAxis != 0 && nineByNine[xAxis - 1, yAxis].name != currentColour)
            {
                libertyPresent = false; // this is the only moment a true false should happen
                surroundingStones.Clear();
                return;
            }

            if (yAxis != 0 && nineByNine[xAxis, yAxis - 1] == null && !surroundingStones.Contains(nineByNine[xAxis, yAxis - 1]))
            {
                surroundingStones.Add(nineByNine[xAxis, yAxis - 1]);                    
            }
            else if (yAxis != 0 && nineByNine[xAxis, yAxis - 1] != null && currentColour == null)
            {
                currentColour = nineByNine[xAxis, yAxis - 1].name;
            }
            else if (yAxis != 0 && nineByNine[xAxis, yAxis - 1].name != currentColour)
            {
                libertyPresent = false; // this is the only moment a true false should happen
                surroundingStones.Clear();
                return;
            }
        }
    }

    public bool CheckForDestruction(UnityEngine.Object stone) // need to check all surrounding fields for liberties
    {
        xAxis = GetCoordinatesOf.CoordinatesOf(nineByNine, stone).Item1;
        yAxis = GetCoordinatesOf.CoordinatesOf(nineByNine, stone).Item2;

        if (error)
        {
            error = false;
            return libertyPresent = false;
        }

        if (xAxis != 8 && nineByNine[xAxis + 1, yAxis] == null || xAxis != 8 && nineByNine[xAxis + 1, yAxis].name == "Protected Stone") // checks right to current stone
        {
            surroundingStones.Clear();
            return libertyPresent = true;
        }
        else if (xAxis != 8 && nineByNine[xAxis + 1, yAxis].name == playerColour + "(Clone)" && !surroundingStones.Contains(nineByNine[xAxis + 1, yAxis]))
        {
            surroundingStones.Add(nineByNine[xAxis + 1, yAxis]);
        }
        else if (xAxis != 8 && nineByNine[xAxis + 1, yAxis].name != playerColour + "(Clone)" && !surroundingOpponents.Contains(nineByNine[xAxis + 1, yAxis]))
        {
            surroundingOpponents.Add(nineByNine[xAxis + 1, yAxis]);
        }

        if (yAxis != 8 && nineByNine[xAxis, yAxis + 1] == null || yAxis != 8 && nineByNine[xAxis, yAxis + 1].name == "Protected Stone") // checks above current stone
        {
            surroundingStones.Clear();
            return libertyPresent = true;
        }
        else if (yAxis != 8 && nineByNine[xAxis, yAxis + 1].name == playerColour + "(Clone)" && !surroundingStones.Contains(nineByNine[xAxis, yAxis + 1]))
        {
            surroundingStones.Add(nineByNine[xAxis, yAxis + 1]);
        }
        else if (yAxis != 8 && nineByNine[xAxis, yAxis + 1].name != playerColour + "(Clone)" && !surroundingOpponents.Contains(nineByNine[xAxis, yAxis + 1]))
        {
            surroundingOpponents.Add(nineByNine[xAxis, yAxis + 1]);
        }

        if (xAxis != 0 && nineByNine[xAxis - 1, yAxis] == null|| xAxis != 0 && nineByNine[xAxis - 1, yAxis].name == "Protected Stone") // checks left to current stone
        {
            surroundingStones.Clear();
            return libertyPresent = true;
        }
        else if (xAxis != 0 && nineByNine[xAxis - 1, yAxis].name == playerColour + "(Clone)" && !surroundingStones.Contains(nineByNine[xAxis - 1, yAxis])) 
        {
            surroundingStones.Add(nineByNine[xAxis - 1, yAxis]);
        }
        else if (xAxis != 0 && nineByNine[xAxis - 1, yAxis].name != playerColour + "(Clone)" && !surroundingOpponents.Contains(nineByNine[xAxis - 1, yAxis]))
        {
            surroundingOpponents.Add(nineByNine[xAxis - 1, yAxis]);
        }

        if (yAxis != 0 && nineByNine[xAxis, yAxis - 1] == null || yAxis != 0 && nineByNine[xAxis, yAxis - 1].name == "Protected Stone") // checks below current stone
        {
            surroundingStones.Clear();
            return libertyPresent = true;
        }
        else if (yAxis != 0 && nineByNine[xAxis, yAxis - 1].name == playerColour + "(Clone)" && !surroundingStones.Contains(nineByNine[xAxis, yAxis - 1]))
        {
            surroundingStones.Add(nineByNine[xAxis, yAxis - 1]);
        }
        else if (yAxis != 0 && nineByNine[xAxis, yAxis - 1].name != playerColour + "(Clone)" && !surroundingOpponents.Contains(nineByNine[xAxis, yAxis - 1]))
        {
            surroundingOpponents.Add(nineByNine[xAxis, yAxis - 1]);
        }

        return libertyPresent = false;
    }

    public void DestroyStone()
    {
        for (int s = 0; s < protectedPositions.Count; s++)
        {
            if (stonesToDestroy[s].name == "White(Clone)")
            {
                playerColour = "White";
            }
            else
            {
                playerColour = "Black";
            }

            CheckForDestruction(stonesToDestroy[s]);
            if (!libertyPresent)
            {
                nineByNine[protectedPositions[s].Item1, protectedPositions[s].Item2] = protectedStone; // protects fields that were freed from ko method to be immedietly be placed on again
            }
        }

        if (!protectionActive)
        {
        foreach (var stone in stonesOnBoard) // takes care of single stones that need to be destroyed
        {
            if (stone.name == "White(Clone)")
            {
                playerColour = "White";
            }
            else
            {
                playerColour = "Black";
            }

            CheckForDestruction(stone);

            if (surroundingStones.Count > 0 && libertyPresent == false) // check for chain
            {
                for (int b = 0; b < surroundingStones.Count; b++)
                {
                    CheckForDestruction(surroundingStones[b]);
                }
            }

            if (libertyPresent == false && !stonesToDestroy.Contains(stone)) // define stones, with no liberties left, to be destroyed
            {
                stonesToDestroy.Add(stone);                
                // add all stones in chain if it will be destroyed // issue: i have no idea why it already works without this part
            }
        }
        }
        for (int i = 0; i < stonesToDestroy.Count; i++)
        {
            if (stonesToDestroy[i].name == "White(Clone)") // adds points depending on which stones were taken
            {
                blackPoints += 1;
            }
            else
            {
                whitePoints += 1;            
            }

            if (i < protectedPositions.Count) // here is where the stones should be protected over ko rule
            {
                
            }

            Destroy(stonesToDestroy[i]);
            stonesOnBoard.Remove(stonesToDestroy[i]);
        }

        stonesToDestroy.Clear();
        Debug.Log($"white has {whitePoints}, black has {blackPoints}");
    }

    public void RestartGame() // reset all variables to base values
    {
        for (int i = 0; i < stonesOnBoard.Count; i++)
        {
            Destroy(stonesOnBoard[i]);
        }

        stonesOnBoard.Clear();
        stonesToDestroy.Clear();
        Array.Clear(nineByNine, 0, nineByNine.Length);
        turn = true;
        passing = false;
        error = false;
        whitePoints = 0;
        blackPoints = 0;        

        for (int t = 0; t < 81; t++)
        {
            waypoints[t].SetActive(true);
        }
        gameOverCard.transform.localScale = new Vector2(0, 0);
    }
}


            