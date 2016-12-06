using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class GameController : MonoBehaviour
{

	UIVA_Client_WiiFit theClient;
	string ipUIVAServer = "127.0.0.1";
	
	private double gravX = 0.0, gravY = 0.0, weight = 0.0, prevGravX = 0.0, prevGravY = 0.0, pathX = 0.0, pathY = 0.0, path = 0.0;
	private bool isFirstRow = true;
	private double tl = 0.0, tr = 0.0, bl = 0.0, br = 0.0;
	private string fitbutt = "";
	private double elapsedTime = 0.0f;
	public double studyDuration = 40;
	private bool isWriting = false;
	private string displayMessage = null;
	private StringBuilder sb;
	
	public int sampleRate = 10;	
	public String studyCondition;
	
	void Awake ()
	{
		if (Application.platform == RuntimePlatform.WindowsWebPlayer ||
			Application.platform == RuntimePlatform.OSXWebPlayer) {
			if (Security.PrefetchSocketPolicy (ipUIVAServer, 843, 500)) {
				Debug.Log ("Got socket policy");	
			} else {
				Debug.Log ("Cannot get socket policy");	
			}
		}
	}
	
	void Start ()
	{
		try {
			theClient = new UIVA_Client_WiiFit (ipUIVAServer);
		} catch (Exception ex) {
			Debug.Log (ex.ToString ());
		}
		displayMessage = "Press 'R' to start recording data";
		sb = new StringBuilder ();
		sb.Append ("System Time,Elapsed Time,Gravity X,Gravity Y, PathX, PathY, Path, Weight\n");
		StartCoroutine (WriteInFile ());
		
		
		
	}
	
	IEnumerator WriteInFile ()
	{
		//yield return new WaitForSeconds (0.001f);
		while (true) {
		
			try {
				theClient.GetWiiFitRawData (out tl, out tr, out bl, out br, out fitbutt);
				theClient.GetWiiFitGravityData (out weight, out gravX, out gravY, out fitbutt);
			} catch (Exception ex) {
				Debug.Log (ex.ToString ());
			}
			if (!isFirstRow) {
				pathX = Math.Abs (gravX - prevGravX);
				pathY = Math.Abs (gravY - prevGravY);
				path = Math.Sqrt (pathX * pathX + pathY * pathY);
			}
			if (isWriting) {
				sb.Append (String.Format ("{0},{1},{2},{3},{4},{5},{6},{7}\n", System.DateTime.Now, elapsedTime, gravX, gravY, pathX, pathY, path, weight));
				prevGravX = gravX;
				prevGravY = gravY;
				isFirstRow = false;
				elapsedTime += (1.0f / sampleRate);
			}
			
			yield return new WaitForSeconds ((1.0f / sampleRate));
		}
		
	}
	
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.R)) {
		
			if (isWriting) {
				isWriting = false;
				displayMessage = "Press 'R' to start recording data";
                CustomMessages.Instance.SendNetworkMessage("pause");
            } else {
				isWriting = true;
				displayMessage = "Data is recording... Press 'R' to stop";
                CustomMessages.Instance.SendNetworkMessage("play");
            }
		}
		if (elapsedTime >= studyDuration) {
			isWriting = false;
			displayMessage = "Data recording is done";
		}
	}
	
	void OnGUI ()
	{
		GUI.skin.label.fontSize = 30;
		
		GUI.color = Color.green;
		GUI.Label (new Rect (10, 0, Screen.width, Screen.height), displayMessage);
		
		GUI.color = Color.white;
		GUI.Label (new Rect (10, 100, Screen.width, Screen.height), "WiiFit Data:");
		GUI.Label (new Rect (10, 140, Screen.width, Screen.height), "Weight:\t\t" + weight.ToString ());
		GUI.Label (new Rect (10, 180, Screen.width, Screen.height), "Grav X:\t\t" + gravX.ToString ()); 
		GUI.Label (new Rect (10, 220, Screen.width, Screen.height), "Grav Y:\t\t" + gravY.ToString ());

		GUI.color = Color.red;
		GUI.Label (new Rect (10, 300, Screen.width, Screen.height), "WiiFit Raw Data:");
		GUI.Label (new Rect (10, 340, Screen.width, Screen.height), "Top Left:\t\t" + tl.ToString ());
		GUI.Label (new Rect (10, 380, Screen.width, Screen.height), "Bottom Left:\t" + bl.ToString ());
		GUI.Label (new Rect (10, 420, Screen.width, Screen.height), "Top Right :\t" + tr.ToString ());
		GUI.Label (new Rect (10, 460, Screen.width, Screen.height), "Bottom Right :\t" + br.ToString ());

	}
	
	void OnApplicationQuit ()
	{
		System.IO.File.AppendAllText (studyCondition + "-balance-" + System.DateTime.Now.Ticks.ToString () + ".csv", sb.ToString ());
	}
}




