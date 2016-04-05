using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathEngine : MonoBehaviour {


    public enum PathInfo { EMPTY, VALID, BOOST }

    List<PathInfo[]> fullRoad;
    List<int> currentRoads;
    List<int> tempRoads;

    public int maxRoadSize;
    public bool circular;
    public int maxListSize;
    public int maxSplit;
    public float chanceToTurn;
    public float chanceOfSplitDuringTurn;

    int currentPathSize;

    public float chanceToBonusRange;

    int bonusRange = 0;
    int bonusRangeDuration = 0;


    public void init()
    {
        fullRoad = new List<PathInfo[]>();
        currentRoads = new List<int>();
        tempRoads = new List<int>();
        currentRoads.Add(0);
        currentPathSize = maxRoadSize;
    }

    public PathInfo[] giveNewPathInfo()
    {
        if (fullRoad.Count == maxListSize)
        {
            PathInfo[] path = fullRoad[0];
            for (int i = 0; i < path.Length; i++)
            {
                path[i] = PathInfo.EMPTY;
            }
            fullRoad.RemoveAt(0);
            fullRoad.Add(path);
            return path;
        } else
        {
            PathInfo[] path = new PathInfo[maxRoadSize];
            for (int i = 0; i < path.Length; i++)
            {
                path[i] = PathInfo.EMPTY;
            }
            fullRoad.Add(path);
            return path;
        }
    }

    public PathInfo[] getLastPathInfo()
    {
        return fullRoad[fullRoad.Count - 1];
    }

    public PathInfo getLastPathInfoAt(int index)
    {
        return fullRoad[fullRoad.Count - 1][index];
    }

    public void addFullRoad()
    {
        PathInfo[] path = giveNewPathInfo();

        for (int i = 0; i < path.Length; i++)
        {
            path[i] = PathInfo.VALID;
        }
    }

    public void constructPath(int size)
    {
        currentPathSize = size;
        if (currentPathSize < maxRoadSize)
        {
            if (bonusRangeDuration == 0 && Random.Range(0f, 100f) < chanceToBonusRange)
            {
                bonusRange = Random.Range(0, maxRoadSize - currentPathSize + 1);
                bonusRangeDuration = Random.Range(0, 5);
            }
        }

        if (bonusRangeDuration > 0) bonusRangeDuration--;

        tempRoads.Clear();
        tempRoads.AddRange(currentRoads);
        PathInfo[] newPath = giveNewPathInfo();
        foreach (int currentRoad in tempRoads)
        {
            TurnInfo turnInfo = TurnInfo.DENIED;
            bool isOnCumulativeTurn = currentCumulativeTurn.duration > 0 && currentCumulativeTurn.roadID == currentRoad;
            if (Random.Range(0f, 100f) < chanceToTurn || isOnCumulativeTurn)
            {
                if (!isOnCumulativeTurn && currentRoads.Count < maxSplit && Random.Range(0f, 100f) < chanceOfSplitDuringTurn)
                {
                    turnInfo = makeASplit(currentRoad);
                }
                else
                {
                    bool willBeCumulative = false;
                    if(!isOnCumulativeTurn && Random.Range(0f, 100f) < 33f)
                    {
                        currentCumulativeTurn.roadID = currentRoad;
                        currentCumulativeTurn.duration = Random.Range(2, 5);
                        willBeCumulative = true;
                    }

                    turnInfo = makeATurn(currentRoad, isOnCumulativeTurn ? currentCumulativeTurn.turn : TurnInfo.BOTH);

                    if(willBeCumulative)
                    {
                        currentCumulativeTurn.turn = turnInfo;
                    }
                }
            }

            constructRoad(ref newPath, currentRoad, turnInfo);

            addNoise(ref newPath);
        }
    }


    public enum BorderInfo { CENTRAL, RIGHT, LEFT };
    BorderInfo border = BorderInfo.CENTRAL;
    public void constructRoad(ref PathInfo[] path, int roadNumber, TurnInfo turnInfo)
    {
        path[roadNumber] = PathInfo.VALID;
        int addMoreLeft = 0;
        int addMoreRight = 0;
        switch(turnInfo)
        {
            case TurnInfo.LEFT:
                addMoreLeft++;
                break;
            case TurnInfo.RIGHT:
                addMoreRight++;
                break;
            case TurnInfo.BOTH:
                addMoreRight++;
                addMoreLeft++;
                break;
            default:
                break;
        }

        if (addMoreLeft == 0 && addMoreRight == 0 && currentPathSize % 2 == 0)
        {

            if(border == BorderInfo.CENTRAL)
            {
                border = Random.Range(0, 2) == 0 ? BorderInfo.LEFT : BorderInfo.RIGHT;
            }
            
            if(border == BorderInfo.LEFT)
            {
                addMoreLeft += 1;
            }
            else if(border == BorderInfo.RIGHT)
            {
                addMoreRight += 1;
            }

            if(border != BorderInfo.CENTRAL && Random.Range(0f, 100f) < 15f)
            {
                border = BorderInfo.CENTRAL;
            }
        }

        if(bonusRangeDuration > 0)
        {
            addMoreRight += (int)((float)bonusRange / 2f);
            addMoreLeft += (int)((float)bonusRange / 2f);
        }
        
        setRoadAside(ref path, roadNumber, true, (int)((currentPathSize-1f) / 2f) + addMoreRight);
        setRoadAside(ref path, roadNumber, false, (int)((currentPathSize-1f) / 2f) + addMoreLeft);
    }

    public enum TurnInfo { DENIED, LEFT, RIGHT, BOTH };

    class TurnCumulative
    {
        public int roadID = -1;
        public int duration = 0;
        public TurnInfo turn = TurnInfo.BOTH;
    }
    TurnCumulative currentCumulativeTurn = new TurnCumulative();

    public TurnInfo makeATurn(int roadID, TurnInfo info = TurnInfo.BOTH)
    {
        currentRoads.Remove(roadID);

        bool left = info == TurnInfo.BOTH ? (Random.Range(0, 2) == 0) : info == TurnInfo.LEFT;
        int newRoad = left ? roadID - 1 : roadID + 1;
        if (newRoad < 0) newRoad = maxRoadSize - 1;
        if (newRoad >= maxRoadSize) newRoad = 0;

        if(!currentRoads.Contains(newRoad))
        {
            currentRoads.Add(newRoad);
        }

        if(currentCumulativeTurn.duration > 0 && roadID == currentCumulativeTurn.roadID)
        {
            currentCumulativeTurn.roadID = newRoad;
            currentCumulativeTurn.duration--;
        }

        return left ? TurnInfo.LEFT : TurnInfo.RIGHT;
    }

    public TurnInfo makeASplit(int roadID)
    {
        currentRoads.Remove(roadID);

        int left = roadID - 1;
        if (left < 0) left = maxRoadSize - 1;
        if (left >= maxRoadSize) left = 0;

        int right = roadID + 1;
        if (right < 0) right = maxRoadSize - 1;
        if (right >= maxRoadSize) right = 0;

        currentRoads.Add(left);
        currentRoads.Add(right);

        return TurnInfo.BOTH;
    }

    public void setRoadAside(ref PathInfo[] path, int startingRoad, bool right, int size)
    {
        if (size == 0) return;
        int roadToFollow = startingRoad;
        roadToFollow += right ? 1 : -1;
        if (roadToFollow < 0) roadToFollow = maxRoadSize-1;
        if (roadToFollow >= maxRoadSize) roadToFollow = 0;

        path[roadToFollow] = PathInfo.VALID;

        if(size > 1)
        {
            size--;
            setRoadAside(ref path, roadToFollow, right, size);
        }
    }

    public void addNoise(ref PathInfo[] path)
    {
        if (fullRoad.Count < 2) return;
        PathInfo[] beforeLastPath = fullRoad[fullRoad.Count - 2];
        for (int i=0; i<path.Length; i++)
        {
            if((path[i == 0 ? path.Length-1 : i-1] == PathInfo.EMPTY
                || path[i == path.Length-1 ? 0 : i+1] == PathInfo.EMPTY)
                && beforeLastPath[i] == PathInfo.EMPTY && path[i] == PathInfo.EMPTY
                && Random.Range(0f, 100f) < 20f)
            {
                path[i] = PathInfo.VALID;
            }
        }
    }


}
