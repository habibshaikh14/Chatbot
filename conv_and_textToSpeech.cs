using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Widgets;
using IBM.Watson.DeveloperCloud.DataTypes;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Connection;
using System.IO;
using FullSerializer;

public class conv_and_textToSpeech : MonoBehaviour {

	#region PLEASE SET THESE VARIABLES IN THE INSPECTOR
	[SerializeField]
	private string conv_username;
	[SerializeField]
	private string conv_password;
	[SerializeField]
	private string conv_url;
	[SerializeField]
	private string workspace_id;
	[SerializeField]
	private string tts_username;
	[SerializeField]
	private string tts_password;
	[SerializeField]
	private string tts_url;
	#endregion

	private string outputText = "";
	private Conversation _conversation;
	private TextToSpeech _textToSpeech;
	private fsSerializer _serializer = new fsSerializer();
	private Dictionary<string, object> _context = null;

	speechToText stt;

	void Start()
	{
		InitializeServices();

		//enter workspace_id as string, this kicks off the conversation
		if (!_conversation.Message (OnMessage, OnFail, workspace_id, "Hi")) {
			Log.Debug ("ExampleConversation.Message()", "Failed to message!");
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
			Debug.Log("Intent/Output Text: " + intent + "/" + outputText);
			CallTTS (outputText);
			outputText = "";
		}
	}

	public void BuildSpokenRequest(string spokenText)
	{
		MessageRequest messageRequest = new MessageRequest()
		{
			input = new Dictionary<string, object>()
			{
				{ "text", spokenText }
			},
			context = _context
		};

		if (_conversation.Message(OnMessage, OnFail, workspace_id, messageRequest))
			Log.Debug("ExampleConversation.AskQuestion()", "Failed to message!");
	}

	private void CallTTS (string outputText)
	{
		//Call text to speech
		if(!_textToSpeech.ToSpeech(OnSynthesize, OnFail, outputText, false))
			Log.Debug("ExampleTextToSpeech.ToSpeech()", "Failed to synthesize!");
	}

	private void OnSynthesize(AudioClip clip, Dictionary<string, object> customData)
	{
		PlayClip(clip);
	}

	private void PlayClip(AudioClip clip)
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

			stt.StartAgain ();

		}
	}

	private void InitializeServices()
	{
		Credentials credentials1 = new Credentials (conv_username, conv_password, conv_url);
		_conversation = new Conversation(credentials1);
		//be sure to give it a Version Date
		_conversation.VersionDate = "2018-06-09";

		Credentials credentials2 = new Credentials(tts_username, tts_password, tts_url);
		_textToSpeech = new TextToSpeech(credentials2);
		//give Watson a voice type
		_textToSpeech.Voice = VoiceType.en_US_Allison;
	}


	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
	{
		Log.Error("ExampleTextToSpeech.OnFail()", "Error received: {0}", error.ToString());
	}
}
