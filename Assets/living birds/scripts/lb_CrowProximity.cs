using UnityEngine;
using System.Collections;

public class lb_CrowProximity : MonoBehaviour {
	
	void OnTriggerEnter (Collider col) {
		if(col.tag == "lb_bird")
		{
			var birdScript = col.GetComponent<lb_Bird>();
			if (birdScript != null)
				birdScript.CrowIsClose();
			//col.SendMessage("CrowIsClose");
		}
	}

}
