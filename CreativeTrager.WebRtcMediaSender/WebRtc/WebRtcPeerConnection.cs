using System;
using System.Linq;
using System.Threading.Tasks;
using CreativeTrager.WebRtcMediaSender.Logging;
using Microsoft.Extensions.Logging;
using SIPSorcery;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.Windows;


namespace CreativeTrager.WebRtcMediaSender.WebRtc;
internal static class WebRtcPeerConnection 
{
	private static readonly ILogger _sLogger;
	static WebRtcPeerConnection() 
	{
		LogFactory.Set(LoggerConfigurator.CreateFactory());
		WebRtcPeerConnection._sLogger = LoggerConfigurator.GetLogger();
	}

	internal static async Task<RTCPeerConnection>
		Create(string webcamId) 
	{
		var connection = new RTCPeerConnection(configuration: new () {
			iceServers = new () { new RTCIceServer() { urls = "stun:stun.sipsorcery.com" } }
		});

		var winVideoEp = new WindowsVideoEndPoint(
			new VpxVideoEncoder(), webcamId,
			width: 1280, height: 720, fps: 30
		);

		var videoInitializingSuccess = await winVideoEp.InitialiseVideoSourceDevice();
		if( videoInitializingSuccess is false) { throw new ApplicationException(
			message: "Could not initialise video capture device.");
		}

		var audioSource = new AudioExtrasSource(audioEncoder: new (),
			audioOptions: new () { AudioSource = AudioSourcesEnum.None }
		);

		var videoTrack = new MediaStreamTrack(winVideoEp.GetVideoSourceFormats());
		var audioTrack = new MediaStreamTrack(audioSource.GetAudioSourceFormats());

		connection.addTrack(videoTrack);
		connection.addTrack(audioTrack);

		winVideoEp.OnVideoSourceEncodedSample  += connection.SendVideo;
		audioSource.OnAudioSourceEncodedSample += connection.SendAudio;

		connection.OnVideoFormatsNegotiated += videoFormats => winVideoEp.SetVideoSourceFormat(videoFormats.First());
		connection.OnAudioFormatsNegotiated += audioFormats => audioSource.SetAudioSourceFormat(audioFormats.First());

		connection.onconnectionstatechange += async state => {
			_sLogger.LogDebug(message: "Peer connection state changed to {State}", state);
			switch(state) 
			{
				case RTCPeerConnectionState.connected:
					await audioSource.StartAudio();
					await winVideoEp.StartVideo();
					break;
				case RTCPeerConnectionState.failed:
					connection.Close(reason: "ice disconnection");
					break;
				case RTCPeerConnectionState.closed:
					await winVideoEp.CloseVideo();
					await audioSource.CloseAudio();
					break;

				case RTCPeerConnectionState.disconnected: break;
				case RTCPeerConnectionState.@new: break;
				case RTCPeerConnectionState.connecting: break;

				default: throw new ArgumentOutOfRangeException(
					paramName: nameof(state), actualValue: state,
					message: $"Value {state} undefined."
				);
			}
		};

		connection.OnReceiveReport += (re, media, rr) => _sLogger.LogDebug(message: "RTCP Receive for {Media} from {Re}\n{DebugSummary}", media, re, rr.GetDebugSummary());
		connection.OnSendReport    += (media, sr)     => _sLogger.LogDebug(message: "RTCP Send for {Media}\n{DebugSummary}", media, sr.GetDebugSummary());

		connection.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => _sLogger.LogDebug(message: "STUN {MessageType} received from {Ep}", msg.Header.MessageType, ep);
		connection.oniceconnectionstatechange += state => _sLogger.LogDebug(message: "ICE connection state change to {State}", state);

		return connection;
	}
}