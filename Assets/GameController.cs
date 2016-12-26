using UnityEngine;
using System.Collections;
using System;
using System.Text;
using HoloToolkit.Sharing;

public class GameController : MonoBehaviour
{

    UIVA_Client_WiiFit theClient;
    string ipUIVAServer = "127.0.0.1";

    private double gravX = 0.0, gravY = 0.0, weight = 0.0, prevGravX = 0.0, prevGravY = 0.0, pathX = 0.0, pathY = 0.0, path = 0.0;
    private bool isFirstRow = true;
    private double tl = 0.0, tr = 0.0, bl = 0.0, br = 0.0;
    private string fitbutt = "";
    private double elapsedTime = 0.0f;

    public bool isWriting = false;
    private string displayMessage = null;
    private StringBuilder sb;

    public int sampleRate = 10;
    public String resourceFile;
    String studyCondition;
    int totalBall = 1000; // a large value
    double studyDuration = 1000; // a large value
    bool isGameCondition = true;


    void Awake()
    {
        if (Application.platform == RuntimePlatform.WindowsWebPlayer ||
            Application.platform == RuntimePlatform.OSXWebPlayer)
        {
            if (Security.PrefetchSocketPolicy(ipUIVAServer, 843, 500))
            {
                Debug.Log("Got socket policy");
            }
            else
            {
                Debug.Log("Cannot get socket policy");
            }
        }
    }

    void Start()
    {
        CustomMessages.Instance.MessageHandlers[CustomMessages.TestMessageID.NetworkMessage] = this.handleNetworkMessage;

        SharingSessionTracker.Instance.SessionJoined += Instance_SessionJoined;


        try
        {
            theClient = new UIVA_Client_WiiFit(ipUIVAServer);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        displayMessage = "Press 'P' to start recording data";
        sb = new StringBuilder();
        sb.Append("System Time,Elapsed Time,Gravity X,Gravity Y, PathX, PathY, Path, Weight\n");
        StartCoroutine(WriteInFile());



    }

    IEnumerator WriteInFile()
    {
        //yield return new WaitForSeconds (0.001f);
        while (true)
        {

            try
            {
                theClient.GetWiiFitRawData(out tl, out tr, out bl, out br, out fitbutt);
                theClient.GetWiiFitGravityData(out weight, out gravX, out gravY, out fitbutt);
            }
            catch (Exception ex)
            {
                //Debug.Log (ex.ToString ());
            }
            if (!isFirstRow)
            {
                pathX = Math.Abs(gravX - prevGravX);
                pathY = Math.Abs(gravY - prevGravY);
                path = Math.Sqrt(pathX * pathX + pathY * pathY);
            }
            if (isWriting)
            {
                sb.Append(String.Format("{0},{1},{2},{3},{4},{5},{6},{7}\n", System.DateTime.Now, elapsedTime, gravX, gravY, pathX, pathY, path, weight));
                prevGravX = gravX;
                prevGravY = gravY;
                isFirstRow = false;
                elapsedTime += (1.0f / sampleRate);
            }

            yield return new WaitForSeconds((1.0f / sampleRate));
        }

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {

            if (isWriting)
            {
                isWriting = false;
                displayMessage = "Press 'P' to start playing";
                CustomMessages.Instance.SendNetworkMessage("pause");
            }
            else
            {
                isWriting = true;
                displayMessage = "Press 'P' to pause playing";
                CustomMessages.Instance.SendNetworkMessage("play");
            }
        }
        /*if (Input.GetKeyDown(KeyCode.F))
        {
            CustomMessages.Instance.SendNetworkMessage("noframe");
        }*/
        if (!isGameCondition && (elapsedTime >= studyDuration))
        {
            gameOver();

        }
    }

    void gameOver()
    {
        isWriting = false;
        displayMessage = "Data recording is done";
    }
    //handles command that is coming from the server 
    void handleNetworkMessage(NetworkInMessage msg)
    {
        Debug.Log("handle command");
        msg.ReadInt64();// important! the id of the message.
        string message = msg.ReadString().ToString();
        string command = message.Split('+')[0] ; //the messages from the server;
        switch (command)
        {
            case "gameover":
                gameOver();
                break;
            case "hit":
                System.IO.File.AppendAllText(resourceFile + "-score-" + System.DateTime.Now.Ticks.ToString() + ".txt", message);
                break;
            default:
                break;
        }
        Debug.Log(command);

    }

    private void Instance_SessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        /*if (GotTransform)
		{
			CustomMessages.Instance.SendStageTransform(transform.localPosition, transform.localRotation);
		}*/
        Debug.Log("instance_sessionjoined called");
        TextAsset textFile = (TextAsset)Resources.Load(resourceFile, typeof(TextAsset));
        System.IO.StringReader textStream = new System.IO.StringReader(textFile.text);
        studyCondition = textStream.ReadLine();

        switch (studyCondition)
        {
            case "srf":
                totalBall = Convert.ToInt32(textStream.ReadLine());
                CustomMessages.Instance.SendNetworkMessage("totalball+" + totalBall);
                Debug.Log("sending message");
                isGameCondition = true;
                break;
            case "nosrf":
                totalBall = Convert.ToInt32(textStream.ReadLine());
                CustomMessages.Instance.SendNetworkMessage("totalball+" + totalBall);
                CustomMessages.Instance.SendNetworkMessage("nosrf");
                Debug.Log("sending message");
                isGameCondition = true;
                break;
            case "eyesopen": //eyes open
            case "eyesclosed": //eyes closed
                isGameCondition = false;
                studyDuration = Convert.ToInt32(textStream.ReadLine()); ;
                break;
        }
    }
    void OnGUI()
    {
        GUI.skin.label.fontSize = 30;

        GUI.color = Color.green;
        GUI.Label(new Rect(10, 0, Screen.width, 40), displayMessage);
        GUI.Label(new Rect(Screen.width / 2 + 10, 100, Screen.width, 40), "Study Condition: " + studyCondition);
        if (isGameCondition)
        {
            GUI.Label(new Rect(Screen.width / 2 + 10, 140, Screen.width, 40), "Total Balls: " + totalBall);
        }
        else
        {
            GUI.Label(new Rect(Screen.width / 2 + 10, 140, Screen.width, 40), "Study Duration: " + studyDuration);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 100, Screen.width / 2, 40), "WiiFit Data:");
        GUI.Label(new Rect(10, 140, Screen.width / 2, 40), "Weight:\t\t" + weight.ToString());
        GUI.Label(new Rect(10, 180, Screen.width / 2, 40), "Grav X:\t\t" + gravX.ToString());
        GUI.Label(new Rect(10, 220, Screen.width / 2, 40), "Grav Y:\t\t" + gravY.ToString());

        GUI.color = Color.red;
        GUI.Label(new Rect(10, 300, Screen.width / 2, 40), "WiiFit Raw Data:");
        GUI.Label(new Rect(10, 340, Screen.width / 2, 40), "Top Left:\t\t" + tl.ToString());
        GUI.Label(new Rect(10, 380, Screen.width / 2, 40), "Bottom Left:\t" + bl.ToString());
        GUI.Label(new Rect(10, 420, Screen.width / 2, 40), "Top Right :\t" + tr.ToString());
        GUI.Label(new Rect(10, 460, Screen.width / 2, 40), "Bottom Right :\t" + br.ToString());

    }

    private void OnApplicationQuit()
    {
        System.IO.File.AppendAllText(resourceFile + "-balance-" + System.DateTime.Now.Ticks.ToString() + ".csv", sb.ToString());
    }
}




