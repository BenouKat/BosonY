using UnityEngine;
using System.Collections;

public class RoadCreator : MonoBehaviour {

    public PathEngine pathEngine;
    public GameObject bigThing;
	// Use this for initialization
	void Start () {
        pathEngine.init();
        pathEngine.addFullRoad();
        for(int i=0; i<1000; i++)
        {
            int size = (int)Mathf.Lerp(8.2f, 1f, (float)i / 1000f);
            pathEngine.constructPath(size);

            GameObject circle = Instantiate(bigThing, new Vector3(0f, 0f, i*10f), bigThing.transform.rotation) as GameObject;
            for(int j=0; j<8; j++)
            {
                if(pathEngine.getLastPathInfoAt(j) == PathEngine.PathInfo.VALID)
                {
                    circle.transform.FindChild(j.ToString()).gameObject.SetActive(true);
                }
                else
                {
                    circle.transform.FindChild(j.ToString()).gameObject.SetActive(false);
                }
            }
        }

    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
