using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Video;
using UnityEngine.UI;

using System;
using System.IO;
using System.Net.Sockets;

using System.Text;

public class vr_pres_client : MonoBehaviour {

	//Textures
	public GameObject dome;
	public GameObject canvas;
	public GameObject fade_canvas;

	public Color fadecolor;
	Texture2D slide;
	public string[] textureNames;

	private string nextTexture;

	public VideoClip[] videoclips;

	private VideoClip nextclip;

	public bool useTextures = true;

	public int currentSlide = 0;
	public int targetSlide = 0;
	public int targetVideo = 0;

	//Socket Server
	public String host = "vrsrvj8j32h84h4239dk.breakingwalls.co";
	//public String host = "127.0.0.1";
	public Int32 port = 50007;

	TcpClient tcp_socket;

	const int READ_BUFFER_SIZE = 255;
	const int PORT_NUM = 50007;
	private byte[] readBuffer = new byte[READ_BUFFER_SIZE];
	public string strMessage=string.Empty;
	public string res=String.Empty;

	public bool bConnActive = false;


	// FadeInOut

	public Texture2D fadeTexture;
	public float fadeSpeed = 0.2f;
	public int drawDepth = -1000;

	private float alpha = 1.0f; 
	private float fadeDir = 1.0f;
	private bool bShouldFade = true;

	private bool bLoadingVideo = false;


	private IEnumerator listenerCoroutine;
	//private IEnumerator faderCoroutine;

	private string titleText = "";
	private string contentText = "";


	// Use this for initialization
	void Start () {

		ChangeTexture (textureNames [0]);
		//ChangeVideo(videoclips[0]);

		listenerCoroutine = isConnectionActive ();
		StartCoroutine (listenerCoroutine);

		//faderCoroutine = Gui_fade ();
		//StartCoroutine(faderCoroutine)



	}
	
	// Update is called once per frame
	void Update () {

		// request slide every few seconds
		// this will allow to send quit request


		if (!bConnActive) {
			try
			{
				Debug.Log("trying to reconnect to server");
				tcp_socket.Close ();
			}
			catch (Exception e)
			{
				// Something went wrong
				Debug.Log("Socket error: " + e);
			}
			setupSocket ();
		}


		if (currentSlide != targetSlide) {
			ChangeTexture (textureNames [targetVideo]);
			//ChangeVideo(videoclips[targetVideo]);
			currentSlide = targetSlide;
		}

		// execute the GuiFade
		// parameters are set by DoFade
		Gui_fade();


		// now check for input to recenter screen
		/*
		if (Input.touchCount > 0 && Input.GetTouch (0).phase == TouchPhase.Began) {
			GvrCardboardHelpers.Recenter ();
		}
		*/
				
	}

	IEnumerator isConnectionActive(){
		while (true) {
			if (bConnActive) {
				try {
					SendData ("ping");
				} catch (Exception e) {
					// Something went wrong
					Debug.Log ("Socket error: " + e);
					bConnActive = false;
				}
			}
			yield return new WaitForSeconds (5.0f);
		}
	}



	void Awake()
	{
		setupSocket();
	}

	void OnApplicationQuit()
	{	
		if (bConnActive) {
			try {
				SendData ("q");
			} catch (Exception e) {
				// Something went wrong
				Debug.Log ("Socket error: " + e);
			}
		}
		StopCoroutine (listenerCoroutine);
		tcp_socket.Close ();
	}

	public void setupSocket()
	{
		try
		{
			tcp_socket = new TcpClient(host, port);
			tcp_socket.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(DoRead), null);
			bConnActive = true;


		}
		catch (Exception e)
		{
			// Something went wrong
			Debug.Log("Socket error: " + e);
		}
	}

	private void DoRead(IAsyncResult ar)
	{ 

		int BytesRead;
		try
		{
			// Finish asynchronous read into readBuffer and return number of bytes read.
			BytesRead = tcp_socket.GetStream().EndRead(ar);
			if (BytesRead < 1) 
			{
				// if no bytes were read server has close.  
				res="Disconnected";
				return;
			}
			// Convert the byte array the message was saved into, minus two for the
			// Chr(13) and Chr(10)
			//strMessage = Encoding.ASCII.GetString(readBuffer, 0, BytesRead - 2);
			strMessage = Encoding.ASCII.GetString(readBuffer, 0, BytesRead);
			if(strMessage == "q")
			{
				bConnActive = false;
				res="Disconnected";
				return;
			}
			else if(strMessage.Substring(0,7) != "Welcome")
			{				
				//processSlideNumber(strMessage);
				processMessage(strMessage);
			}
			// Start a new asynchronous read into readBuffer.
			tcp_socket.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(DoRead), null);

		} 
		catch (Exception e)
		{
			Debug.Log ("Error : " + e);
			res="Disconnected";
		}
	}

	private void processMessage(string strMessage)
	{
		string[] dataArray;
		if (strMessage != "")
		{
			dataArray = strMessage.Split((char) 124);

			// first read slide number
			int read_slide = 0;
			Int32.TryParse (dataArray[0], out read_slide);
			targetSlide = read_slide;

			int read_video = 0;
			Int32.TryParse (dataArray[1], out read_video);
			targetVideo = read_video;

			// now read text
			//foreach (string line in dataArray) {
			titleText = "";
			//titleText += String.Format("{0}\n", dataArray [1].Substring (1));
			titleText += String.Format("{0}\n", dataArray [2]);

			contentText = "";
			for(int i = 3; i < dataArray.Length; i++)
			{
				//contentText += String.Format("{0}\n", dataArray [i].Substring (1));
				contentText += String.Format("{0}\n", dataArray [i]);
			}
			//Debug.Log (titleText);
			//Debug.Log(string.Format("{0}", dataArray.Length));

		}


	}

	// Process the command received from the server, and take appropriate action.
	private void processSlideNumber(string strMessage)
	{

		if (strMessage != "")
		{
			int read_slide = 0;
			Debug.Log(strMessage);
			Int32.TryParse (strMessage, out read_slide);
			targetSlide = read_slide;
		}
	}

	// Use a StreamWriter to send a message to server.
	private void SendData(string data)
	{
		StreamWriter writer = new StreamWriter(tcp_socket.GetStream());
		writer.Write(data);
		writer.Flush();
	}

	void ChangeTexture(string textureName){
		doFade ();
		nextTexture = textureName;
	}

	void ChangeVideo(VideoClip clip){
		doFade ();
		nextclip = clip;
	}

	void doFade()
	{
		bShouldFade = true;
		fadeDir = 1.0f;

	}

	void updateText()
	{
		Transform TitleObj = canvas.transform.Find ("Title");
		TitleObj.GetComponent<Text> ().text = titleText;

		Transform ContentObj = canvas.transform.Find ("ContentText");
		ContentObj.GetComponent<Text> ().text = contentText;
	}



	//void OnGUI () {
	void Gui_fade () {
		//GUILayout.Label(bConnActive.ToString());

		//Debug.Log (string.Format("S: alpha : {0}    fadeDir : {1}    bLoadingVideo : {2}    isprep : {3}    shouldFade : {4}", alpha, fadeDir, bLoadingVideo, dome.GetComponent<VideoPlayer> ().isPrepared, bShouldFade));
		VideoPlayer videoPlayer = dome.GetComponent<VideoPlayer> ();

		if (bShouldFade) {
			
			alpha += fadeDir * fadeSpeed * Time.deltaTime;  
			alpha = Mathf.Clamp01 (alpha); 


			if (alpha >= 1.0f) {


				if (useTextures) {
					fadeDir = -1.0f;
					bLoadingVideo = false;

					//Update Texture
					slide = Resources.Load<Texture2D> (nextTexture);
					dome.GetComponent<Renderer> ().material.SetTexture ("_MainTex", slide);
					updateText ();
				} else {
					if (!bLoadingVideo) {
						videoPlayer.clip = nextclip;
						videoPlayer.Prepare ();
						if (!videoPlayer.isPrepared) {
							return;
						}

						bLoadingVideo = true;
					} else if (videoPlayer.isPrepared) {
						fadeDir = -1.0f;
						bLoadingVideo = false;
						videoPlayer.Play ();

						updateText ();
					}
				}

			}
			if ((alpha <= 0.0f) && (fadeDir < 0.0f)) {
				bShouldFade = false;
			}


			fadecolor.a = alpha;


			//GUI.color = newColor;
			//Debug.Log (string.Format ("Changing canvas alpha - 1"));
			fade_canvas.GetComponent<Image>().color = fadecolor;
			//Debug.Log (string.Format ("Changing canvas alpha - 2"));


			//GUI.depth = drawDepth;

			//GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), fadeTexture);
		}
		//Debug.Log (string.Format("E: alpha : {0}    fadeDir : {1}    bLoadingVideo : {2}    isprep : {3}    shouldFade : {4}", alpha, fadeDir, bLoadingVideo, dome.GetComponent<VideoPlayer> ().isPrepared, bShouldFade));
	}





}
