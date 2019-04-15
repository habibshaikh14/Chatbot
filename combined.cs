#region HEADER FILES
using UnityEngine;
using System.Collections;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;
using System.Collections.Generic;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Widgets;
using IBM.Watson.DeveloperCloud.Connection;
using System.IO;
using FullSerializer;
#endregion

public class combined : MonoBehaviour {
	#region PLEASE SET THESE VARIABLES FOR SPEECH TO TEXT IN THE INSPECTOR
	[SerializeField]
	private string _username;
	[SerializeField]
	private string _password;
	[SerializeField]
	private string _url;
	#endregion
	public Text ResultsField;
    #region PLEASE SET THESE VARIABLES FOR CONV AND TTS IN THE INSPECTOR
    [SerializeField]
    private string conv_username;
    [SerializeField]
    private string conv_password;
    [SerializeField]
    private string conv_url;
    [SerializeField]
    private string workspace_id;
	[SerializeField]
    private string version_date;
    [SerializeField]
    private string tts_username;
    [SerializeField]
    private string tts_password;
    [SerializeField]
    private string tts_url;
    #endregion
	private string speechtext = "Bedroom";
    private string outputText = "";

    #region INITIALIZING PRIVATE VARIABLES
    private int _recordingRoutine = 0;
	private string _microphoneID = null;
	private AudioClip _recording = null;
	private int _recordingBufferSize = 1;
	private int _recordingHZ = 22050;

	private SpeechToText _speechToText;
	private Conversation _conversation;
	private TextToSpeech _textToSpeech;
	private fsSerializer _serializer = new fsSerializer();
	private Dictionary<string, object> _context = null;
	#endregion

	#region INITIALIZING GAME OBJECTS
	public GameObject Hall;
	public GameObject MasterBedRoom;
    public GameObject BedRoom;
    public GameObject Kitchen;
    public GameObject Top;
    public GameObject Dining;
    public GameObject GuestRoom;
	public GameObject Player;
	#endregion 
	public float speed = 0.5f;
	private Vector3 targetPosition;
	private int move = 0;

    void Start()
	{
		LogSystem.InstallDefaultReactors();
        
		#region INITIALIZING SERVICES
		Credentials credentials1 = new Credentials(conv_username, conv_password, conv_url);
        _conversation = new Conversation(credentials1);
        _conversation.VersionDate = version_date;

        Credentials credentials2 = new Credentials(tts_username, tts_password, tts_url);
        _textToSpeech = new TextToSpeech(credentials2);
        //give Watson a voice type
        _textToSpeech.Voice = VoiceType.en_US_Lisa;

        Credentials credentials3 = new Credentials(_username, _password, _url);
        _speechToText = new SpeechToText(credentials3);
		#endregion

        //This kicks off the conversation
        if (!_conversation.Message(OnMessage, OnFail, workspace_id, "Hello"))
        {
            Log.Debug("ExampleConversation.Message()", "Failed to message!");
        }
	}

	void Update()
	{
		if(move == 1)
		{
            Player.transform.position = Vector3.Lerp(Player.transform.position, targetPosition, speed*Time.deltaTime);
		}
		if(Player.transform.position == targetPosition)
		{
			move = 0;
		}
	}
	private void OnMessage(object resp, Dictionary<string, object> customData)
	{
		fsData fsdata = null;
		fsResult r = _serializer.TrySerialize(resp.GetType(), resp, out fsdata);
		if (!r.Succeeded)
			throw new WatsonException(r.FormattedMessages);
		//  Convert fsdata to MessageResponse
		MessageResponse messageResponse = new MessageResponse();
		object obj = messageResponse;
		r = _serializer.TryDeserialize(fsdata, obj.GetType(), ref obj);
		if (!r.Succeeded)
			throw new WatsonException(r.FormattedMessages);
		//  Set context for next round of messaging
		object _tempContext = null;
		(resp as Dictionary<string, object>).TryGetValue("context", out _tempContext);

		if (_tempContext != null)
			_context = _tempContext as Dictionary<string, object>;
		else
			Log.Debug("ExampleConversation.OnMessage()", "Failed to get context");
		//if we get a response, do something with it (find the intents, output text, etc.)
		if (resp != null && (messageResponse.intents.Length > 0 || messageResponse.entities.Length > 0))
		{
			string intent = messageResponse.intents[0].intent;
			foreach (string WatsonResponse in messageResponse.output.text) {
				outputText += WatsonResponse + " ";
			}
			Debug.Log("Intent/Entity/Output Text: " + intent + "/" + outputText);
			//checking for the location
			switch (intent)
			{
				case "Hall":	
					targetPosition = Hall.transform.position;
        			move = 1;
					break;
				case "MasterBedRoom":	
					targetPosition = MasterBedRoom.transform.position;
                    move = 1;
					break;
				case "BedRoom":	
					targetPosition = BedRoom.transform.position;
                	move = 1;
					break;
				case "Kitchen":	
					targetPosition = Kitchen.transform.position;
                	move = 1;
					break;
				case "Top":	
					targetPosition = Top.transform.position;
            		move = 1;
					break;
				case "Dining":	
					targetPosition = Dining.transform.position;
        			move = 1;
					break;
				case "GuestRoom":	
					targetPosition = GuestRoom.transform.position;
            		move = 1;
					break;
				
				default:
					break;
			}
            if (!_textToSpeech.ToSpeech(OnSynthesize, OnFail, outputText, false))
			{
                Log.Debug("ExampleTextToSpeech.ToSpeech()", "Failed to synthesize!");
			}
			outputText = "";
		}
	}
	private void OnSynthesize(AudioClip clip, Dictionary<string, object> customData)
	{
        if (Application.isPlaying && clip != null)
        {
            GameObject audioObject = new GameObject("AudioObject");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            source.loop = false;
            source.clip = clip;
            source.Play();
            Destroy(audioObject, clip.length);
        }
	}
	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
	{
		Log.Error("ExampleTextToSpeech.OnFail()", "Error received: {0}", error.ToString());
	}
	public void startstt()
	{
		Active = true;
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
	}

	public void stopstt()
	{
		Active = false;
		StopRecording();
        if (!_conversation.Message(OnMessage, OnFail, workspace_id, speechtext))
        {
            Log.Debug("ExampleConversation.Message()", "Failed to message!");
        }
	}

	private bool Active
	{
		get { return _speechToText.IsListening; }
		set
		{
			if (value && !_speechToText.IsListening)
			{
				_speechToText.DetectSilence = true;
				_speechToText.EnableWordConfidence = true;
				_speechToText.EnableTimestamps = true;
				_speechToText.SilenceThreshold = 0.01f;
				_speechToText.MaxAlternatives = 0;
				_speechToText.EnableInterimResults = true;
				_speechToText.OnError = OnError;
				_speechToText.InactivityTimeout = -1;
				_speechToText.ProfanityFilter = false;
				_speechToText.SmartFormatting = true;
				_speechToText.SpeakerLabels = false;
				_speechToText.WordAlternativesThreshold = null;
				_speechToText.StartListening(OnRecognize, OnRecognizeSpeaker);
			}
			else if (!value && _speechToText.IsListening)
			{
				_speechToText.StopListening();
			}
		}
	}
	private void StopRecording()
	{
		if (_recordingRoutine != 0)
		{
			Microphone.End(_microphoneID);
			Runnable.Stop(_recordingRoutine);
			_recordingRoutine = 0;
		}
	}

	private void OnError(string error)
	{
		Active = false;
		Log.Debug("ExampleStreaming.OnError()", "Error! {0}", error);
	}

	private IEnumerator RecordingHandler()
	{
		Log.Debug("ExampleStreaming.RecordingHandler()", "devices: {0}", Microphone.devices);
		_recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
		yield return null;      // let _recordingRoutine get set..

		if (_recording == null)
		{
			StopRecording();
			yield break;
		}

		bool bFirstBlock = true;
		int midPoint = _recording.samples / 2;
		float[] samples = null;

		while (_recordingRoutine != 0 && _recording != null)
		{
			int writePos = Microphone.GetPosition(_microphoneID);
			if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
			{
				Log.Error("ExampleStreaming.RecordingHandler()", "Microphone disconnected.");

				StopRecording();
				yield break;
			}

			if ((bFirstBlock && writePos >= midPoint)
				|| (!bFirstBlock && writePos < midPoint))
			{
				// front block is recorded, make a RecordClip and pass it onto our callback.
				samples = new float[midPoint];
				_recording.GetData(samples, bFirstBlock ? 0 : midPoint);

				AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
				record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
				record.Clip.SetData(samples, 0);

				_speechToText.OnListen(record);

				bFirstBlock = !bFirstBlock;
			}
			else
			{
				// calculate the number of samples remaining until we ready for a block of audio, 
				// and wait that amount of time it will take to record.
				int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
				float timeRemaining = (float)remaining / (float)_recordingHZ;

				yield return new WaitForSeconds(timeRemaining);
			}

		}

		yield break;
	}

	private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData)
	{
		if (result != null && result.results.Length > 0)
		{
			foreach (var res in result.results)
			{
				foreach (var alt in res.alternatives)
				{
					speechtext = string.Format(alt.transcript);
					Log.Debug("ExampleStreaming.OnRecognize()", speechtext);
					ResultsField.text = speechtext;
				}

				if (res.keywords_result != null && res.keywords_result.keyword != null)
				{
					foreach (var keyword in res.keywords_result.keyword)
					{
						Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
					}
				}

				if (res.word_alternatives != null)
				{
					foreach (var wordAlternative in res.word_alternatives)
					{
						Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
						foreach(var alternative in wordAlternative.alternatives)
							Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
					}
				}
			}
		}
	}

	private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
	{
		if (result != null)
		{
			foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
			{
				Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
			}
		}
	}
}
